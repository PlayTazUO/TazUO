using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Renderer
{
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
            p.Noise.Amount = input;
            p.Noise.RidgeAmount = input;
            p.Shape.FocusAmount = input;

            p.Clamp();

            p.Appearance.Opacity.Should().Be(expected);
            p.Appearance.Intensity.Should().Be(expected);
            p.Appearance.PulseAmp.Should().Be(expected);
            p.Noise.Amount.Should().Be(expected);
            p.Noise.RidgeAmount.Should().Be(expected);
            p.Shape.FocusAmount.Should().Be(expected);
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
}
