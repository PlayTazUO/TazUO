using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Renderer;

public class OverlayParamsTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(3f, 3f)]
    [InlineData(3.0001f, OverlayParams.MaxPulseFreqHz)]
    [InlineData(50f, OverlayParams.MaxPulseFreqHz)]
    [InlineData(-1f, 0f)]
    public void Clamp_EnforcesPulseFrequencyCeiling(float input, float expected)
    {
        OverlayParams p = OverlayParams.Default;
        p.Appearance.PulseFreq = input;

        p.Clamp();

        p.Appearance.PulseFreq.Should().Be(expected);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.5f, 0.5f)]
    public void Clamp_ClampsUnitRangeFields(float input, float expected)
    {
        OverlayParams p = OverlayParams.Default;
        p.Appearance.Opacity = input;
        p.Appearance.Intensity = input;
        p.Appearance.PulseAmp = input;
        p.Noise.FlatFloor = input;
        p.Noise.RidgeAmount = input;
        p.Shape.FocusAmount = input;

        p.Clamp();

        p.Appearance.Opacity.Should().Be(expected);
        p.Appearance.Intensity.Should().Be(expected);
        p.Appearance.PulseAmp.Should().Be(expected);
        p.Noise.FlatFloor.Should().Be(expected);
        p.Noise.RidgeAmount.Should().Be(expected);
        p.Shape.FocusAmount.Should().Be(expected);
    }

    [Theory]
    [InlineData(-0.2f, 0f)]
    [InlineData(0.35f, 0.35f)]
    [InlineData(2f, 1f)]
    public void Clamp_BoundsShapeJitter(float input, float expected)
    {
        OverlayParams p = OverlayParams.Default;
        p.Shape.Jitter.ReachAmount = input;
        p.Shape.Jitter.FeatherAmount = input;

        p.Clamp();

        p.Shape.Jitter.ReachAmount.Should().Be(expected);
        p.Shape.Jitter.FeatherAmount.Should().Be(expected);
    }

    [Fact]
    public void Default_HasNoShapeJitter()
    {
        // Presets written before jitter existed must keep their exact straight-edged behaviour.
        OverlayParams.Default.Shape.Jitter.ReachAmount.Should().Be(0f);
    }

    [Fact]
    public void Clamp_ReplacesADegenerateJitterScale()
    {
        // A zero scale collapses the displacement lookup onto one texel, which is a uniform
        // offset rather than a varying boundary.
        OverlayParams p = default;

        p.Clamp();

        p.Shape.Jitter.Scale.Should().NotBe(Vector2.Zero);
    }

    [Fact]
    public void Clamp_RejectsZeroFeather()
    {
        OverlayParams p = OverlayParams.Default;
        p.Shape.Feather = 0f;

        p.Clamp();

        p.Shape.Feather.Should().BeGreaterThan(0f);
    }
}
