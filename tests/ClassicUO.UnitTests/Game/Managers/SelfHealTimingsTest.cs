using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class SelfHealTimingsTest
    {
        // Magery: recovery base 6, longest cast = Cure (1.25s base).
        [Fact]
        public void Magery_Fcr6Fc2_NoRecovery_GraceCoversCure()
        {
            var (recast, grace) = SelfHealTimings.Compute(chivalry: false, fc: 2, fcr: 6);
            recast.Should().Be(0);       // (6-6) -> 0ms recovery
            grace.Should().Be(1150);     // max(Heal 500, Cure 750) + 400 margin
        }

        // Chivalry: recovery base 7, longest cast = Close Wounds (1.5s base).
        [Fact]
        public void Chivalry_Fcr6Fc2_HasRecovery_GraceCoversCloseWounds()
        {
            var (recast, grace) = SelfHealTimings.Compute(chivalry: true, fc: 2, fcr: 6);
            recast.Should().Be(250);     // (7-6) x 250 = 250ms recovery
            grace.Should().Be(1400);     // max(Close 1000, Cleanse 500) + 400
        }

        [Fact]
        public void Chivalry_Fcr7Fc3_NoRecovery()
        {
            var (recast, grace) = SelfHealTimings.Compute(chivalry: true, fc: 3, fcr: 7);
            recast.Should().Be(0);       // (7-7) -> 0
            grace.Should().Be(1150);     // max(Close 750, Cleanse 250) + 400
        }

        [Fact]
        public void Chivalry_Fcr7Fc4_CleanseHitsCastFloor()
        {
            var (recast, grace) = SelfHealTimings.Compute(chivalry: true, fc: 4, fcr: 7);
            recast.Should().Be(0);
            grace.Should().Be(900);      // max(Close 500, Cleanse floored 250) + 400
        }

        [Fact]
        public void Magery_FcAboveCap_ClampsToCap()
        {
            // Magery FC cap is 2 - FC 4 must behave like FC 2.
            SelfHealTimings.Compute(chivalry: false, fc: 4, fcr: 6)
                .Should().Be(SelfHealTimings.Compute(chivalry: false, fc: 2, fcr: 6));
        }

        [Fact]
        public void Magery_FcrAboveCap_ClampsToCap()
        {
            SelfHealTimings.Compute(chivalry: false, fc: 2, fcr: 7)
                .Should().Be(SelfHealTimings.Compute(chivalry: false, fc: 2, fcr: 6));
        }

        [Fact]
        public void HigherFcr_NeverIncreasesRecovery()
        {
            int prev = int.MaxValue;
            for (int fcr = 0; fcr <= 7; fcr++)
            {
                int recast = SelfHealTimings.Compute(chivalry: true, fc: 2, fcr: fcr).recastDelayMs;
                recast.Should().BeLessThanOrEqualTo(prev);
                prev = recast;
            }
        }

        [Fact]
        public void CureVerifyWindow_PadsByTwicePing()
        {
            // 150-200ms ping: the fixed 600ms base was too short to see the poison-cleared packet,
            // causing a redundant second cure. Padding by 2x ping widens it to land comfortably after.
            SelfHealTimings.CureVerifyWindow(600, 0).Should().Be(600);
            SelfHealTimings.CureVerifyWindow(600, 200).Should().Be(1000);   // 600 + 2*200
        }

        [Fact]
        public void CureVerifyWindow_ClampsGarbagePing()
        {
            // A bad/huge ping reading must not blow the window up unbounded (clamped at 1000ms ping).
            SelfHealTimings.CureVerifyWindow(600, 999999).Should().Be(600 + 2 * 1000);
            SelfHealTimings.CureVerifyWindow(600, -50).Should().Be(600);    // negative ping ignored
        }
    }
}
