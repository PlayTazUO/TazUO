using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations
{
    public class OverlaySamplingTests
    {
        [Fact]
        public void DefaultParamsPaintRatherThanSample()
        {
            OverlayParams.Default.Sampling.Mode.Should().Be(OverlaySampleMode.None);
            OverlayParams.Default.Sampling.ReadsScene.Should().BeFalse();
        }

        [Theory]
        [InlineData(OverlaySampleMode.Blur)]
        [InlineData(OverlaySampleMode.Radial)]
        [InlineData(OverlaySampleMode.Chromatic)]
        public void EveryDistortionModeReadsTheScene(OverlaySampleMode mode)
        {
            new OverlaySampling { Mode = mode }.ReadsScene.Should().BeTrue();
        }

        /// <summary>Zero taps would collapse the disk and radial loops onto the centre sample, which
        /// draws the undistorted frame over itself.</summary>
        [Fact]
        public void ClampFloorsTapsAtOne()
        {
            OverlayParams p = OverlayParams.Default;
            p.Sampling.Taps = 0;
            p.Clamp();

            p.Sampling.Taps.Should().Be(1);
        }

        [Fact]
        public void ClampCapsTapsAndRadiiAtTheirCeilings()
        {
            OverlayParams p = OverlayParams.Default;
            p.Sampling.Taps = OverlayParams.MaxSampleTaps * 4;
            p.Sampling.Radius = 10f;
            p.Sampling.Aberration = 10f;
            p.Sampling.Zoom = 10f;
            p.Clamp();

            p.Sampling.Taps.Should().Be(OverlayParams.MaxSampleTaps);
            p.Sampling.Radius.Should().Be(OverlayParams.MaxSampleRadius);
            p.Sampling.Aberration.Should().Be(OverlayParams.MaxSampleAberration);
            p.Sampling.Zoom.Should().Be(1f);
        }

        [Fact]
        public void ClampRejectsNegativeSamplingValues()
        {
            OverlayParams p = OverlayParams.Default;
            p.Sampling.Radius = -1f;
            p.Sampling.Aberration = -1f;
            p.Sampling.Zoom = -1f;
            p.Clamp();

            p.Sampling.Radius.Should().Be(0f);
            p.Sampling.Aberration.Should().Be(0f);
            p.Sampling.Zoom.Should().Be(0f);
        }

        [Fact]
        public void SceneMapOfAWholeTextureIsTheIdentity()
        {
            OverlaySceneMap.Full.Offset.Should().Be(Vector2.Zero);
            OverlaySceneMap.Full.Scale.Should().Be(Vector2.One);
        }

        /// <summary>The viewport pass draws a rectangle that covers only part of the world target,
        /// so the mapping has to place it rather than assume it fills the texture.</summary>
        [Fact]
        public void SceneMapPlacesACropWithinItsTexture()
        {
            OverlaySceneMap map = OverlaySceneMap.From(null, new Rectangle(100, 50, 400, 200));

            // A null texture cannot be measured, so the mapping falls back rather than dividing by
            // an unknown size.
            map.Should().Be(OverlaySceneMap.Full);
        }
    }
}
