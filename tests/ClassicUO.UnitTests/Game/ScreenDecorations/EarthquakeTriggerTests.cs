using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class EarthquakeTriggerTests
{
    private const int VIEW_RANGE = 18;
    private const int PLAYER_X = 1000;
    private const int PLAYER_Y = 1000;

    private static float IntensityAt(int tilesAway) =>
        EarthquakeTrigger.IntensityFor(PLAYER_X + tilesAway, PLAYER_Y, PLAYER_X, PLAYER_Y, VIEW_RANGE);

    [Fact]
    public void AQuakeUnderfootIsFullStrength()
    {
        IntensityAt(0).Should().Be(1f);
    }

    [Fact]
    public void IntensityFallsOffWithDistance()
    {
        float[] byDistance = [IntensityAt(0), IntensityAt(3), IntensityAt(8), IntensityAt(VIEW_RANGE)];

        byDistance.Should().BeInDescendingOrder();
        byDistance.Should().OnlyContain(intensity => intensity > 0f && intensity <= 1f);
    }

    /// <summary>
    /// The client only plays the sound within view range at all, so a quake that reaches the player
    /// is worth showing - it just should not compete with one underfoot.
    /// </summary>
    [Fact]
    public void AQuakeAtTheEdgeOfEarshotStillRegisters()
    {
        IntensityAt(VIEW_RANGE).Should().BeGreaterThan(0.2f).And.BeLessThan(0.35f);
    }

    [Fact]
    public void AQuakeBeyondTheAudibleRangeDoesNotRegister()
    {
        IntensityAt(VIEW_RANGE + 1).Should().Be(0f);
    }

    /// <summary>Diagonal distance is chebyshev, as everywhere else in the client - a quake three
    /// tiles out in both axes is three away, not four.</summary>
    [Fact]
    public void DistanceIsMeasuredTheWayTheClientMeasuresIt()
    {
        EarthquakeTrigger.IntensityFor(PLAYER_X + 3, PLAYER_Y + 3, PLAYER_X, PLAYER_Y, VIEW_RANGE)
            .Should()
            .Be(IntensityAt(3));
    }

    [Fact]
    public void NothingRegistersWhileTheViewRangeIsUnset()
    {
        EarthquakeTrigger.IntensityFor(PLAYER_X + 1, PLAYER_Y, PLAYER_X, PLAYER_Y, 0).Should().Be(0f);
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
