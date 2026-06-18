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
        long RecastDelayMs { get; }          // pad after a successful heal before the next cast ("recuperation")
        long CastStartGraceMs { get; }       // how long a cast may take to register before we treat it as failed
        long CureVerifyMs { get; }           // how long to wait for poison to clear before recasting Cure
        long InterruptRetryMs { get; }       // delay before recasting after an interrupted cast
        void Cast(int spellId);
        void TargetSelf();
    }

    /// <summary>
    /// Drives the hold-to-heal loop: while held, cast Heal (Cure if poisoned), wait for the
    /// post-cast cursor, target self, then repeat. Heal is spammed freely. After a Cure, it
    /// verifies the poison actually cleared before re-casting Cure.
    ///
    /// The wait for the cursor is <b>cast-aware</b>: while <see cref="ISelfHealEnv.IsCasting"/> is
    /// true the loop never times out (so a slow cast is never double-cast); the moment casting
    /// stops without a cursor — or a cast never registers within <see cref="ISelfHealEnv.CastStartGraceMs"/>
    /// — it recasts after a short <see cref="ISelfHealEnv.InterruptRetryMs"/> delay instead of stalling.
    /// Releasing only prevents the next cast.
    /// </summary>
    public sealed class SelfHealStateMachine
    {
        public const int HealSpellId = 4;     // Magery: Heal
        public const int CureSpellId = 11;    // Magery: Cure

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
        private bool _lastCastWasCure;
        private bool _castStarted;     // have we observed the in-flight cast actually begin?

        public void Tick(ISelfHealEnv env, bool held)
        {
            if (!env.CanAct)
            {
                _state = State.Idle;
                _castStarted = false;
                return;
            }

            switch (_state)
            {
                case State.Idle:
                    if (held)
                    {
                        _lastCastWasCure = env.IsPoisoned;
                        _castStarted = false;
                        env.Cast(_lastCastWasCure ? CureSpellId : HealSpellId);
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
                    else if (env.IsCasting)
                    {
                        // Cast is genuinely in progress — keep waiting and push the grace forward so a
                        // slow cast is never prematurely treated as failed (no double-cast).
                        _castStarted = true;
                        _stallUntil = env.Now + env.CastStartGraceMs;
                    }
                    else if (_castStarted || env.Now > _stallUntil)
                    {
                        // Either the cast began and then died with no cursor (e.g. damage disrupted it),
                        // or it never registered within the grace window. Recast quickly rather than stall.
                        _castStarted = false;
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
