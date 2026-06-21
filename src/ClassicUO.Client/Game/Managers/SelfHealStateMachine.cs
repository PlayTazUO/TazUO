namespace ClassicUO.Game.Managers
{
    /// <summary>Environment the self-heal loop acts against. Abstracted for testability.</summary>
    public interface ISelfHealEnv
    {
        long Now { get; }                    // monotonic milliseconds
        bool CanAct { get; }                 // player alive/in-world, feature enabled, hotkeys not disabled
        bool IsPoisoned { get; }
        bool IsTargetingAfterCast { get; }   // a post-cast target cursor is up
        bool IsCasting { get; }              // a spell cast is currently in progress
        int HealSpellId { get; }             // spell to cast for healing (Magery Heal / Chivalry Close Wounds)
        int CureSpellId { get; }             // spell to cast to cure poison (Magery Cure / Chivalry Cleanse by Fire)
        long RecastDelayMs { get; }          // pad after a successful heal before the next cast ("recuperation")
        long CastStartGraceMs { get; }       // how long a cast may take to register before we treat it as failed
        long CureVerifyMs { get; }           // how long to wait for poison to clear before recasting Cure
        long InterruptRetryMs { get; }       // delay before recasting after an interrupted cast
        long LastCastFailedAt { get; }       // monotonic tick the server last reported a cast failure (disrupt/mana/etc.)
        void Cast(int spellId);
        void TargetSelf();
    }

    /// <summary>
    /// Drives the hold-to-heal loop: while held, cast Heal (Cure if poisoned), wait for the
    /// post-cast cursor, target self, then repeat. Heal is spammed freely. After a Cure, it
    /// verifies the poison actually cleared before re-casting Cure.
    ///
    /// The wait for the cursor is <b>cast-aware</b>: while <see cref="ISelfHealEnv.IsCasting"/> is
    /// true the deadline is pushed forward so a slow cast is never treated as failed. Crucially, a
    /// cast going <i>quiet</i> (IsCasting dropping to false) is NOT treated as an interrupt on its own:
    /// the client clears its casting flag on any HP change (UpdateHitpoints → ClearCasting), so taking
    /// a hit mid-cast flaps IsCasting off constantly while healing under fire — even though the cast is
    /// still in flight. We therefore keep waiting for the cursor until <see cref="ISelfHealEnv.CastStartGraceMs"/>
    /// elapses, and only then recast after a short <see cref="ISelfHealEnv.InterruptRetryMs"/> delay.
    /// This avoids cancelling a real in-flight heal (and thus never landing one) under sustained damage.
    /// Releasing only prevents the next cast.
    /// </summary>
    public sealed class SelfHealStateMachine
    {
        // Spell ids (the global cast ids GameActions.CastSpell expects).
        public const int MageryHealSpellId = 4;       // Magery: Heal
        public const int MageryCureSpellId = 11;      // Magery: Cure
        public const int ChivalryHealSpellId = 202;   // Chivalry: Close Wounds
        public const int ChivalryCureSpellId = 201;   // Chivalry: Cleanse by Fire

        // Defaults for the configurable timings (all overridable via ISelfHealEnv).
        public const long DefaultRecastDelayMs = 50;       // pad after a successful heal
        public const long DefaultCastStartGraceMs = 800;   // max wait for a cast to register / produce a cursor
        public const long DefaultCureVerifyMs = 600;       // wait for poison to clear before recasting Cure
        public const long DefaultInterruptRetryMs = 100;   // recast delay after an interrupted cast

        private enum State { Idle, WaitingForCursor, Settle, VerifyingCure, InterruptRetry }

        private State _state = State.Idle;
        private long _stallUntil;
        private long _settleUntil;
        private long _verifyUntil;
        private long _interruptUntil;
        private long _castIssuedAt;    // env.Now when we issued the in-flight cast (to detect failures since)
        private bool _lastCastWasCure;

        public void Tick(ISelfHealEnv env, bool held)
        {
            if (!env.CanAct)
            {
                _state = State.Idle;
                return;
            }

            switch (_state)
            {
                case State.Idle:
                    if (held)
                    {
                        _lastCastWasCure = env.IsPoisoned;
                        _castIssuedAt = env.Now;
                        env.Cast(_lastCastWasCure ? env.CureSpellId : env.HealSpellId);
                        _stallUntil = env.Now + env.CastStartGraceMs;
                        _state = State.WaitingForCursor;
                    }
                    break;

                case State.WaitingForCursor:
                    if (env.IsTargetingAfterCast)
                    {
                        env.TargetSelf();

                        if (_lastCastWasCure)
                        {
                            _verifyUntil = env.Now + env.CureVerifyMs;
                            _state = State.VerifyingCure;
                        }
                        else
                        {
                            _settleUntil = env.Now + env.RecastDelayMs;
                            _state = State.Settle;
                        }
                    }
                    else if (env.LastCastFailedAt > _castIssuedAt)
                    {
                        // The server reported THIS cast failed (concentration disturbed, out of mana/
                        // reagents, frozen, ...). It's a real failure, not a benign HP-driven casting-flag
                        // flap, so retry immediately instead of waiting out the full cast grace.
                        _interruptUntil = env.Now + env.InterruptRetryMs;
                        _state = State.InterruptRetry;
                    }
                    else if (env.IsCasting)
                    {
                        // Cast is genuinely in progress — keep waiting and push the grace forward so a
                        // slow cast is never prematurely treated as failed (no double-cast).
                        _stallUntil = env.Now + env.CastStartGraceMs;
                    }
                    else if (env.Now > _stallUntil)
                    {
                        // The cursor never appeared within the grace window. Note we do NOT recast the
                        // instant IsCasting drops to false: the client clears its casting flag on any HP
                        // change, so a hit landing mid-cast would otherwise cancel a heal that is still in
                        // flight. Waiting for the deadline lets the in-flight cursor still arrive; only a
                        // genuine no-show (true interrupt, fizzle, or never-registered cast) falls through.
                        _interruptUntil = env.Now + env.InterruptRetryMs;
                        _state = State.InterruptRetry;
                    }
                    break;

                case State.InterruptRetry:
                    if (env.Now > _interruptUntil)
                    {
                        _interruptUntil = 0;
                        _state = State.Idle;
                    }
                    break;

                case State.Settle:
                    if (env.Now > _settleUntil)     // strictly after settle window
                    {
                        _settleUntil = 0;
                        _state = State.Idle;
                    }
                    break;

                case State.VerifyingCure:
                    // Don't recast Cure until we confirm the poison cleared, or we've waited long
                    // enough that it clearly didn't take (then allow another Cure).
                    if (!env.IsPoisoned || env.Now > _verifyUntil)
                    {
                        _verifyUntil = 0;
                        _state = State.Idle;
                    }
                    break;
            }
        }
    }
}
