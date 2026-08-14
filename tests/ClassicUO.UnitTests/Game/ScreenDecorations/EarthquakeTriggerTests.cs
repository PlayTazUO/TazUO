using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using ClassicUO.Game.ScreenDecorations.Triggers.Definitions;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class EarthquakeTriggerTests
{
    private const int VIEW_RANGE = 18;
    private const int PLAYER_X = 1000;
    private const int PLAYER_Y = 1000;

    private static float NearnessAt(int tilesAway)
    {
        return EarthquakeTrigger.Nearness(PLAYER_X + tilesAway, PLAYER_Y, PLAYER_X, PLAYER_Y, VIEW_RANGE);
    }

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
    /// Only the parameterless constructor sets <see cref="TriggerSignal.Intensity"/> to 1, so a
    /// <c>default</c> would leave an occurrence at zero strength - invisible rather than
    /// unscaled.
    /// </summary>
    [Fact]
    public void DefaultSignalLeavesTheProfileAlone()
    {
        TriggerSignal.Default.Intensity.Should().Be(1f);
        TriggerSignal.Default.Duration.Should().BeNull();

        new TriggerSignal { Duration = System.TimeSpan.FromSeconds(1) }.Intensity.Should().Be(1f);
    }
}
