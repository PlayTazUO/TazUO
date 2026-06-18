using System.Collections.Generic;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class SelfHealStateMachineTest
    {
        private sealed class FakeEnv : ISelfHealEnv
        {
            public long Now { get; set; }
            public bool CanAct { get; set; } = true;
            public bool IsPoisoned { get; set; }
            public bool IsTargetingAfterCast { get; set; }
            public bool IsCasting { get; set; }
            public long CureVerifyMs { get; set; } = SelfHealStateMachine.DefaultCureVerifyMs;
            public long InterruptRetryMs { get; set; } = SelfHealStateMachine.DefaultInterruptRetryMs;
            public List<int> Casts { get; } = new();
            public int TargetSelfCount { get; private set; }
            public void Cast(int spellId) => Casts.Add(spellId);
            public void TargetSelf() => TargetSelfCount++;
        }

        [Fact]
        public void Held_NotPoisoned_CastsHeal()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);

            env.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.HealSpellId);
        }

        [Fact]
        public void Held_Poisoned_CastsCure()
        {
            var env = new FakeEnv { IsPoisoned = true };
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);

            env.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.CureSpellId);
        }

        [Fact]
        public void CursorUpAfterCast_TargetsSelf()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);
            env.IsTargetingAfterCast = true;
            sm.Tick(env, held: true);

            env.TargetSelfCount.Should().Be(1);
        }

        [Fact]
        public void Release_StopsAfterInFlightCast()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);
            env.IsTargetingAfterCast = true;
            sm.Tick(env, held: false);
            env.Now += SelfHealStateMachine.SettleMs + 1;
            sm.Tick(env, held: false);
            sm.Tick(env, held: false);

            env.Casts.Should().HaveCount(1);
        }

        [Fact]
        public void Timeout_NoCursor_RetriesWhileHeld()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);
            env.IsTargetingAfterCast = false;
            env.Now += SelfHealStateMachine.CastWaitMs + 1;
            sm.Tick(env, held: true);                 // past deadline -> back to Idle (no new cast yet)
            env.Casts.Should().HaveCount(1);
            sm.Tick(env, held: true);                 // Idle + held -> cast #2
            env.Casts.Should().HaveCount(2);
        }

        [Fact]
        public void CannotAct_DoesNothing()
        {
            var env = new FakeEnv { CanAct = false };
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);

            env.Casts.Should().BeEmpty();
        }

        [Fact]
        public void CanActFalseDuringWait_DoesNotTargetSelf()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast, now WaitingForCursor
            env.CanAct = false;                  // e.g. player died mid-cast
            env.IsTargetingAfterCast = true;     // cursor would be up
            sm.Tick(env, held: true);

            env.TargetSelfCount.Should().Be(0);  // must not self-target when it can't act
        }

        [Fact]
        public void ReleaseBeforeCursor_StillTargetsInFlightCast()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast, WaitingForCursor
            sm.Tick(env, held: false);           // released before cursor: still waiting
            env.IsTargetingAfterCast = true;
            sm.Tick(env, held: false);           // cursor up -> target self (finish in-flight)

            env.TargetSelfCount.Should().Be(1);
            env.Casts.Should().HaveCount(1);     // no new cast after release
        }

        [Fact]
        public void Cured_WithinVerifyWindow_DoesNotRecastCure()
        {
            var env = new FakeEnv { IsPoisoned = true };
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // casts Cure, WaitingForCursor
            env.IsTargetingAfterCast = true;
            sm.Tick(env, held: true);            // target self -> VerifyingCure
            env.IsPoisoned = false;              // cure landed
            sm.Tick(env, held: true);            // verify sees cured -> Idle (no recast)
            env.IsTargetingAfterCast = false;
            sm.Tick(env, held: true);            // Idle + held + not poisoned -> Heal

            env.Casts.Should().Equal(SelfHealStateMachine.CureSpellId, SelfHealStateMachine.HealSpellId);
        }

        [Fact]
        public void StillPoisoned_AfterVerifyWindow_RecastsCure()
        {
            var env = new FakeEnv { IsPoisoned = true };
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // casts Cure, WaitingForCursor
            env.IsTargetingAfterCast = true;
            sm.Tick(env, held: true);            // target self -> VerifyingCure
            sm.Tick(env, held: true);            // still poisoned, within window -> no recast
            env.Casts.Should().HaveCount(1);
            env.Now += env.CureVerifyMs + 1;
            sm.Tick(env, held: true);            // verify window elapsed -> Idle
            env.IsTargetingAfterCast = false;
            sm.Tick(env, held: true);            // Idle + held + still poisoned -> Cure again

            env.Casts.Should().Equal(SelfHealStateMachine.CureSpellId, SelfHealStateMachine.CureSpellId);
        }

        [Fact]
        public void CastInterrupted_RetriesAfterShortDelayNotFullTimeout()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast Heal, WaitingForCursor
            env.IsCasting = true;
            sm.Tick(env, held: true);            // observes cast in progress
            env.IsCasting = false;               // damage interrupts the cast (no cursor)
            sm.Tick(env, held: true);            // detects interruption -> InterruptRetry
            env.Casts.Should().HaveCount(1);     // not recast yet

            env.Now += env.InterruptRetryMs + 1; // only the short delay, well under CastWaitMs
            sm.Tick(env, held: true);            // InterruptRetry -> Idle
            sm.Tick(env, held: true);            // Idle + held -> recast

            env.Casts.Should().HaveCount(2);
            env.Now.Should().BeLessThan(SelfHealStateMachine.CastWaitMs); // retried fast, not after the timeout
        }

        [Fact]
        public void CastNotYetStarted_DoesNotFalseTriggerInterrupt()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast, WaitingForCursor; IsCasting still false (not registered yet)
            sm.Tick(env, held: true);            // must NOT treat the not-yet-started cast as interrupted
            sm.Tick(env, held: true);

            env.Casts.Should().HaveCount(1);     // still waiting, no premature recast
        }
    }
}
