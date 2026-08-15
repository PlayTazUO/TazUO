using ClassicUO.Game.ScreenDecorations.Triggers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class ProximityMathTests
{
    private const int VIEW_RANGE = 18;

    private static float NearnessAt(int tilesAway) => ProximityMath.Nearness(tilesAway, 0, VIEW_RANGE);

    private static float ShapedAt(int tilesAway) =>
        ProximityMath.Shape(NearnessAt(tilesAway), FalloffCurve.Quadratic);

    [Fact]
    public void SomethingUnderfootIsFullStrength()
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
    /// The far edge is divided by the band width plus one, so something sitting exactly on it still
    /// registers faintly. Falling to zero there would cut the effect off while the sound justifying
    /// it is still audible.
    /// </summary>
    [Fact]
    public void TheFarEdgeOfTheBandStillRegisters()
    {
        NearnessAt(VIEW_RANGE).Should().BeGreaterThan(0f).And.BeLessThan(0.1f);
    }

    [Fact]
    public void NothingOutsideTheBandRegisters()
    {
        NearnessAt(VIEW_RANGE + 1).Should().Be(0f);
        ProximityMath.Nearness(2, 5, VIEW_RANGE).Should().Be(0f);
    }

    /// <summary>A band whose edges cross is not a band, and must claim nothing at all.</summary>
    [Fact]
    public void AnInvertedBandClaimsNothing()
    {
        ProximityMath.Nearness(5, 10, 4).Should().Be(0f);
    }

    /// <summary>The near edge of the band is full strength wherever it is put, so a rule that only
    /// answers to distant sounds still gets a full-strength occurrence at its own threshold.</summary>
    [Fact]
    public void TheNearEdgeIsFullStrengthWhereverItIsPut()
    {
        ProximityMath.Nearness(5, 5, 15).Should().Be(1f);
    }

    /// <summary>
    /// The squared falloff exists so the tiles nearest the player carry most of the scale. Linear
    /// would put the midpoint at 0.5; anything at or above that would mean the curve was lost.
    /// </summary>
    [Fact]
    public void QuadraticIsWeightedTowardsTheTilesClosestToThePlayer()
    {
        ShapedAt(VIEW_RANGE / 2).Should().BeLessThan(0.4f);
    }

    [Fact]
    public void DistanceIsMeasuredTheWayTheClientMeasuresIt()
    {
        ProximityMath.Distance(1003, 1003, 1000, 1000).Should().Be(3);
        ProximityMath.Distance(1003, 1000, 1000, 1000).Should().Be(3);
        ProximityMath.Distance(997, 1000, 1000, 1000).Should().Be(3);
    }

    /// <summary>
    /// Every curve has to agree at the ends, or switching between them would change what "at the
    /// player" and "at the edge" mean rather than only how the space between them is filled.
    /// </summary>
    [Theory]
    [InlineData(FalloffCurve.Linear)]
    [InlineData(FalloffCurve.Quadratic)]
    [InlineData(FalloffCurve.Cubic)]
    [InlineData(FalloffCurve.SquareRoot)]
    [InlineData(FalloffCurve.Custom)]
    public void EveryCurveAgreesAtFullNearness(FalloffCurve curve)
    {
        ProximityMath.Shape(1f, curve).Should().Be(1f);
    }

    /// <summary>Flat included: the band is what filters, and a curve must not smuggle something
    /// outside it back in at full strength.</summary>
    [Theory]
    [InlineData(FalloffCurve.Flat)]
    [InlineData(FalloffCurve.Linear)]
    [InlineData(FalloffCurve.Quadratic)]
    [InlineData(FalloffCurve.Cubic)]
    [InlineData(FalloffCurve.SquareRoot)]
    [InlineData(FalloffCurve.Custom)]
    public void NoCurveResurrectsSomethingOutsideTheBand(FalloffCurve curve)
    {
        ProximityMath.Shape(0f, curve).Should().Be(0f);
    }

    [Fact]
    public void FlatIgnoresDistanceInsideTheBand()
    {
        ProximityMath.Shape(0.01f, FalloffCurve.Flat).Should().Be(1f);
        ProximityMath.Shape(0.99f, FalloffCurve.Flat).Should().Be(1f);
    }

    [Fact]
    public void CurvesOrderThemselvesFromSlowestToSharpest()
    {
        const float NEARNESS = 0.5f;

        float[] sharpening =
        [
            ProximityMath.Shape(NEARNESS, FalloffCurve.SquareRoot),
            ProximityMath.Shape(NEARNESS, FalloffCurve.Linear),
            ProximityMath.Shape(NEARNESS, FalloffCurve.Quadratic),
            ProximityMath.Shape(NEARNESS, FalloffCurve.Cubic)
        ];

        sharpening.Should().BeInDescendingOrder();
    }

    [Fact]
    public void CustomWithATwoExponentMatchesQuadratic()
    {
        ProximityMath.Shape(0.37f, FalloffCurve.Custom, 2f)
            .Should()
            .BeApproximately(ProximityMath.Shape(0.37f, FalloffCurve.Quadratic), 1e-6f);
    }

    /// <summary>
    /// A zero or negative exponent would flatten every distance to full strength, or invert the
    /// curve outright. Both are worse than the sharpest real curve, so the floor holds it just above
    /// zero rather than rejecting the parameters.
    /// </summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-3f)]
    public void ACustomExponentCannotInvertTheCurve(float exponent)
    {
        ProximityMath.Shape(0.5f, FalloffCurve.Custom, exponent).Should().BeInRange(0f, 1f);
    }

    [Fact]
    public void LerpSpansTheGivenRange()
    {
        ProximityMath.Lerp(0.25f, 1f, 0f).Should().Be(0.25f);
        ProximityMath.Lerp(0.25f, 1f, 1f).Should().Be(1f);
        ProximityMath.Lerp(0f, 1f, 0.5f).Should().Be(0.5f);
    }
}
