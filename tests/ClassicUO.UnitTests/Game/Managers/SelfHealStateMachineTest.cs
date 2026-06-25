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
            public int HealSpellId { get; set; } = SelfHealStateMachine.MageryHealSpellId;
            public int CureSpellId { get; set; } = SelfHealStateMachine.MageryCureSpellId;
            public long RecastDelayMs { get; set; } = SelfHealStateMachine.DefaultRecastDelayMs;
            public long CastStartGraceMs { get; set; } = SelfHealStateMachine.DefaultCastStartGraceMs;
            public long CureVerifyMs { get; set; } = SelfHealStateMachine.DefaultCureVerifyMs;
            public long InterruptRetryMs { get; set; } = SelfHealStateMachine.DefaultInterruptRetryMs;
            public long LastCastFailedAt { get; set; }
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

            env.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.MageryHealSpellId);
        }

        [Fact]
        public void Held_Poisoned_CastsCure()
        {
            var env = new FakeEnv { IsPoisoned = true };
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);

            env.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.MageryCureSpellId);
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
            env.Now += env.RecastDelayMs + 1;
            sm.Tick(env, held: false);
            sm.Tick(env, held: false);

            env.Casts.Should().HaveCount(1);
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

            env.Casts.Should().Equal(SelfHealStateMachine.MageryCureSpellId, SelfHealStateMachine.MageryHealSpellId);
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

            env.Casts.Should().Equal(SelfHealStateMachine.MageryCureSpellId, SelfHealStateMachine.MageryCureSpellId);
        }

        [Fact]
        public void CastingGoesQuietWithNoCursor_WaitsForGraceBeforeRecast()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast Heal, WaitingForCursor
            env.IsCasting = true;
            sm.Tick(env, held: true);            // observes cast in progress
            env.IsCasting = false;               // cast goes quiet, no cursor (true interrupt OR benign HP-clear)
            sm.Tick(env, held: true);            // must NOT recast on the falling edge alone
            env.Casts.Should().HaveCount(1);

            env.Now += env.CastStartGraceMs + 1; // only once the grace deadline passes with still no cursor...
            sm.Tick(env, held: true);            // ...-> InterruptRetry
            env.Casts.Should().HaveCount(1);     // still not recast (in the retry delay)

            env.Now += env.InterruptRetryMs + 1;
            sm.Tick(env, held: true);            // InterruptRetry -> Idle
            sm.Tick(env, held: true);            // Idle + held -> recast

            env.Casts.Should().HaveCount(2);
        }

        [Fact]
        public void ServerReportsCastFailed_RetriesImmediatelyNotAfterGrace()
        {
            // A genuine failure (concentration disturbed / out of mana, etc.) is reported by the server
            // via a cliloc. We should retry right away rather than waiting out the full cast grace.
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);                 // cast, WaitingForCursor (castIssuedAt = 0)
            env.Now += 50;
            env.LastCastFailedAt = env.Now;            // server: that cast was disrupted
            sm.Tick(env, held: true);                  // sees failure -> InterruptRetry (no grace wait)
            env.Casts.Should().HaveCount(1);

            env.Now += env.InterruptRetryMs + 1;
            sm.Tick(env, held: true);                  // InterruptRetry -> Idle
            sm.Tick(env, held: true);                  // recast
            env.Casts.Should().HaveCount(2);
            env.Now.Should().BeLessThan(SelfHealStateMachine.DefaultCastStartGraceMs); // fast, not grace-bound
        }

        [Fact]
        public void StaleFailureBeforeThisCast_DoesNotTriggerRetry()
        {
            // A failure recorded BEFORE the current cast was issued must not be mistaken for this cast failing.
            var env = new FakeEnv { LastCastFailedAt = 100 };
            var sm = new SelfHealStateMachine();

            env.Now = 500;
            sm.Tick(env, held: true);                  // cast at Now=500 (castIssuedAt=500), stale failure=100
            sm.Tick(env, held: true);                  // failure(100) < castIssuedAt(500) -> ignored, keep waiting
            env.Casts.Should().HaveCount(1);           // no premature recast
        }

        [Fact]
        public void BenignCastingFlap_CursorStillAppears_TargetsSelfWithoutRecast()
        {
            // Reproduces the real-world bug: UpdateHitpoints calls ClearCasting() on EVERY player HP
            // change, so taking a hit mid-cast drops IsCasting to false even though the heal is still
            // in flight. The loop must keep waiting for the cursor instead of cancelling and recasting.
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast Heal, WaitingForCursor
            env.IsCasting = true;
            sm.Tick(env, held: true);            // cast in progress
            env.IsCasting = false;               // a hit lands -> ClearCasting() flaps IsCasting off
            env.Now += 300;
            sm.Tick(env, held: true);            // within grace, no cursor yet -> keep waiting
            env.Casts.Should().HaveCount(1);     // MUST NOT recast (would cancel the in-flight heal)

            env.IsTargetingAfterCast = true;     // the heal's cursor finally comes up
            env.Now += 200;
            sm.Tick(env, held: true);

            env.TargetSelfCount.Should().Be(1);  // targeted the in-flight cast
            env.Casts.Should().HaveCount(1);     // and never double-cast
        }

        [Fact]
        public void CastNeverRegisters_RetriesAfterGraceNotLongStall()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast, WaitingForCursor; IsCasting never goes true
            sm.Tick(env, held: true);            // within grace -> keep waiting (no premature recast)
            env.Casts.Should().HaveCount(1);

            env.Now += env.CastStartGraceMs + 1; // grace elapsed with no cursor and no casting
            sm.Tick(env, held: true);            // -> InterruptRetry
            env.Now += env.InterruptRetryMs + 1;
            sm.Tick(env, held: true);            // InterruptRetry -> Idle
            sm.Tick(env, held: true);            // recast

            env.Casts.Should().HaveCount(2);
        }

        [Fact]
        public void CastNotYetStarted_DoesNotFalseTriggerInterrupt()
        {
            var env = new FakeEnv();
            var sm = new SelfHealStateMachine();

            sm.Tick(env, held: true);            // cast, WaitingForCursor; IsCasting still false (not registered yet)
            sm.Tick(env, held: true);            // within grace, no cursor -> must NOT recast
            sm.Tick(env, held: true);

            env.Casts.Should().HaveCount(1);     // still waiting, no premature recast
        }

        [Fact]
        public void ChivalrySpellIds_CastCloseWoundsAndCleanseByFire()
        {
            var heal = new FakeEnv
            {
                HealSpellId = SelfHealStateMachine.ChivalryHealSpellId,
                CureSpellId = SelfHealStateMachine.ChivalryCureSpellId,
            };
            new SelfHealStateMachine().Tick(heal, held: true);
            heal.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.ChivalryHealSpellId); // Close Wounds

            var cure = new FakeEnv
            {
                IsPoisoned = true,
                HealSpellId = SelfHealStateMachine.ChivalryHealSpellId,
                CureSpellId = SelfHealStateMachine.ChivalryCureSpellId,
            };
            new SelfHealStateMachine().Tick(cure, held: true);
            cure.Casts.Should().ContainSingle().Which.Should().Be(SelfHealStateMachine.ChivalryCureSpellId); // Cleanse by Fire
        }
    }
}
