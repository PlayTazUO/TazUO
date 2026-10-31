using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Guards the predicate that selects the shader's flat-alpha passes (tint, chromatic, saturation).
/// Those passes drop the three noise fetches outright, so anything that widens
/// <see cref="OverlayParams.HasFlatAlpha" /> past exact equivalence shows up as a layer rendering
/// differently depending on which technique it happened to land on - not as a failure anywhere near
/// the change.
/// <para>
/// Technique selection itself needs a GraphicsDevice and so is not reachable from here; these cover
/// the contract it reads.
/// </para>
/// </summary>
public class OverlayFlatAlphaTests
{
    /// <summary>Flat params with no shortcut taken, for each test to break one field of.</summary>
    private static OverlayParams Flat()
    {
        OverlayParams p = OverlayParams.Default;

        p.Noise.FlatFloor = 1f;
        p.Shape.Jitter.ReachAmount = 0f;
        p.Shape.Jitter.FeatherAmount = 0f;

        return p;
    }

    [Fact]
    public void FullFloorWithoutJitterIsFlat()
    {
        Flat().HasFlatAlpha.Should().BeTrue();
    }

    /// <summary>
    /// The editor's starting point is a noisy layer, so a fresh one must not pick a pass that
    /// ignores the field it is about to be given.
    /// </summary>
    [Fact]
    public void DefaultParamsAreNotFlat()
    {
        OverlayParams.Default.HasFlatAlpha.Should().BeFalse();
    }

    /// <summary>
    /// Each condition alone is disqualifying: the floor leaves the noise field live, and either
    /// jitter amount leaves the boundary or the falloff riding on the jitter fetch.
    /// </summary>
    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0.99f, 0f, 0f)]
    [InlineData(1f, 0.01f, 0f)]
    [InlineData(1f, 0f, 0.01f)]
    public void AnyLiveNoiseTermDisqualifies(float flatFloor, float jitterReach, float jitterFeather)
    {
        OverlayParams p = Flat();

        p.Noise.FlatFloor = flatFloor;
        p.Shape.Jitter.ReachAmount = jitterReach;
        p.Shape.Jitter.FeatherAmount = jitterFeather;

        p.HasFlatAlpha.Should().BeFalse();
    }

    /// <summary>
    /// The flat passes hand <c>Feather</c> to <c>ShapeMask</c> raw, while <c>OverlayAlpha</c> runs it
    /// through <c>max(Feather * (1 + flux * JitterFeather), 0.01)</c>. With the jitter term gone
    /// those agree only while every clamped layer already clears that hardcoded 0.01 - so this is
    /// the coupling between <c>Clamp</c>'s feather floor and a literal in the shader.
    /// </summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(0.001f)]
    public void ClampFloorsFeatherAtTheShadersOwnMinimum(float authored)
    {
        OverlayParams p = Flat();
        p.Shape.Feather = authored;

        p.Clamp();

        // The 0.01 in OverlayAlpha's feather guard. Lowering Clamp's floor below it silently splits
        // the two alpha paths apart.
        p.Shape.Feather.Should().BeGreaterThanOrEqualTo(0.01f);
    }

    /// <summary>
    /// Flatness is a property of the mask, not of what the layer draws, so <c>Clamp</c> must not
    /// disturb it for any mode - each of the three flat passes is picked after clamping.
    /// </summary>
    [Theory]
    [InlineData(OverlaySampleMode.None)]
    [InlineData(OverlaySampleMode.Chromatic)]
    [InlineData(OverlaySampleMode.Saturation)]
    public void ClampPreservesFlatness(OverlaySampleMode mode)
    {
        OverlayParams p = Flat();
        p.Sampling.Mode = mode;

        p.Clamp();

        p.HasFlatAlpha.Should().BeTrue();
    }

    /// <summary>
    /// A zero jitter scale is repaired by <c>Clamp</c> rather than left to collapse the lookup onto
    /// one texel. That repair must not read as live jitter: the amounts are what gate the fetch.
    /// </summary>
    [Fact]
    public void RepairedJitterScaleStaysFlat()
    {
        OverlayParams p = Flat();
        p.Shape.Jitter.Scale = Vector2.Zero;

        p.Clamp();

        p.Shape.Jitter.Scale.Should().NotBe(Vector2.Zero);
        p.HasFlatAlpha.Should().BeTrue();
    }
}
