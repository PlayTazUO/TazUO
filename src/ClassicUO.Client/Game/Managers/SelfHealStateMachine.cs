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
        long CureVerifyMs { get; }           // how long to wait for poison to clear before recasting Cure
        long InterruptRetryMs { get; }       // delay before recasting after an interrupted cast
        void Cast(int spellId);
        void TargetSelf();
    }

    /// <summary>
    /// Drives the hold-to-heal loop: while held, cast Heal (Cure if poisoned), wait for the
    /// post-cast cursor, target self, then repeat. Heal is spammed freely. After a Cure, it
    /// verifies the poison actually cleared (waiting up to <see cref="CureVerifyMs"/>) before
    /// re-casting Cure, so a single cure isn't double-cast while the status update is in flight.
    /// Releasing only prevents the next cast.
    /// </summary>
    public sealed class SelfHealStateMachine
    {
        public const int HealSpellId = 4;     // Magery: Heal
        public const int CureSpellId = 11;    // Magery: Cure
        public const long CastWaitMs = 3000;  // max wait for the target cursor before retrying
        public const long SettleMs = 50;      // brief pad after targeting (Heal)
        public const long DefaultCureVerifyMs = 600;     // default for the configurable cure-verify window
        public const long DefaultInterruptRetryMs = 100; // default for the configurable interrupt-retry delay

        private enum State { Idle, WaitingForCursor, Settle, VerifyingCure, InterruptRetry }

        private State _state = State.Idle;
        private long _deadline;
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
                        _deadline = env.Now + CastWaitMs;
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
                            _settleUntil = env.Now + SettleMs;
                            _state = State.Settle;
                        }
                    }
                    else if (env.IsCasting)
                    {
                        _castStarted = true;            // cast is in progress; keep waiting for the cursor
                    }
                    else if (_castStarted)
                    {
                        // The cast began and then stopped without ever producing a target cursor
                        // (e.g. damage disrupted it). Recast quickly instead of waiting the full timeout.
                        _castStarted = false;
                        _interruptUntil = env.Now + env.InterruptRetryMs;
                        _state = State.InterruptRetry;
                    }
                    else if (env.Now > _deadline)   // strictly after deadline; == stays in window one more tick
                    {
                        _deadline = 0;
                        _state = State.Idle;
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
