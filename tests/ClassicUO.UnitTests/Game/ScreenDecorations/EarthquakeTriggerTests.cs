using ClassicUO.Game.ScreenDecorations.Manager.Triggers;
using ClassicUO.Game.ScreenDecorations.Overlays;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations
{
    public class EarthquakeTriggerTests
    {
        private const int VIEW_RANGE = 18;
        private const int PLAYER_X = 1000;
        private const int PLAYER_Y = 1000;

        private static float NearnessAt(int tilesAway) =>
            EarthquakeTrigger.Nearness(PLAYER_X + tilesAway, PLAYER_Y, PLAYER_X, PLAYER_Y, VIEW_RANGE);

        [Fact]
        public void AQuakeUnderfootIsFullStrength()
        {
            NearnessAt(0).Should().Be(1f);
        }

        [Fact]
        public void NearnessFallsOffWithDistance()
        {
            float[] byDistance = [NearnessAt(0), NearnessAt(3), NearnessAt(8), NearnessAt(VIEW_RANGE)];

            byDistance.Should().BeInDescendingOrder();
            byDistance.Should().OnlyContain(n => n > 0f && n <= 1f);
        }

        /// <summary>
        /// The squared falloff exists so the tiles nearest the player carry most of the scale. Linear
        /// would put the midpoint at 0.5; anything at or above that would mean the curve was lost.
        /// </summary>
        [Fact]
        public void NearnessIsWeightedTowardsTheTilesClosestToThePlayer()
        {
            NearnessAt(VIEW_RANGE / 2).Should().BeLessThan(0.4f);
        }

        [Fact]
        public void AQuakeBeyondTheAudibleRangeDoesNotRegister()
        {
            NearnessAt(VIEW_RANGE + 1).Should().Be(0f);
        }

        /// <summary>Diagonal distance is chebyshev, as everywhere else in the client - a quake three
        /// tiles out in both axes is three away, not four.</summary>
        [Fact]
        public void DistanceIsMeasuredTheWayTheClientMeasuresIt()
        {
            EarthquakeTrigger.Nearness(PLAYER_X + 3, PLAYER_Y + 3, PLAYER_X, PLAYER_Y, VIEW_RANGE)
                .Should()
                .Be(NearnessAt(3));
        }

        [Fact]
        public void NothingRegistersWhileTheViewRangeIsUnset()
        {
            EarthquakeTrigger.Nearness(PLAYER_X + 1, PLAYER_Y, PLAYER_X, PLAYER_Y, 0).Should().Be(0f);
        }

        /// <summary>
        /// Only the parameterless constructor sets <see cref="OverlayModulation.Intensity"/> to 1, so
        /// a <c>default</c> would leave an overlay at zero strength - invisible rather than unscaled.
        /// </summary>
        [Fact]
        public void DefaultModulationLeavesTheProfileAlone()
        {
            OverlayModulation.Default.Intensity.Should().Be(1f);
            OverlayModulation.Default.OnsetTrauma.Should().Be(0f);

            new OverlayModulation { OnsetTrauma = 0.5f }.Intensity.Should().Be(1f);
        }
    }
}
