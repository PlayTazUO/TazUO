using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations
{
    /// <summary>
    /// Overcast is the one shipped look that covers the frame rather than framing it, and the one
    /// whose properties are load-bearing for that: a vignette-shaped or fast-arriving overcast reads
    /// as something wrong with the player instead of as weather.
    /// </summary>
    public class OvercastOverlayTests
    {
        private static List<OverlayLayer> Bake()
        {
            var layers = new List<OverlayLayer>();
            new OvercastOverlay().BakeClamped(layers);

            return layers;
        }

        /// <summary>
        /// A radial mask is normalised to screen width, so at full reach it still falls away before
        /// the top and bottom edges of a widescreen display and leaves the sky lighter than the
        /// middle of the screen.
        /// </summary>
        [Fact]
        public void EveryLayerCoversTheWholeFrame()
        {
            foreach (OverlayLayer layer in Bake())
            {
                layer.Params.Shape.Reach.Should().Be(1f);
                layer.Params.Shape.EdgeBlend.Should().Be(1f);
            }
        }

        /// <summary>Daylight under cloud varies over tens of seconds. Anything fast enough to notice
        /// reads as a flicker fault, and at the top end is a photosensitivity hazard.</summary>
        [Fact]
        public void NothingPulses()
        {
            foreach (OverlayLayer layer in Bake())
                layer.Params.Appearance.PulseAmp.Should().Be(0f);
        }

        /// <summary>
        /// Ridged and Worley both draw outlines, which on cloud resolve into a visible cell lattice.
        /// </summary>
        [Fact]
        public void NoLayerDrawsOutlines()
        {
            foreach (OverlayLayer layer in Bake())
            {
                layer.Params.Noise.RidgeAmount.Should().Be(0f);
                layer.Params.Noise.BaseChannel.Should().BeOneOf(NoiseChannel.Red, NoiseChannel.Green);
                layer.Params.Noise.DetailChannel.Should().BeOneOf(NoiseChannel.Red, NoiseChannel.Green);
            }
        }

        /// <summary>
        /// The base layer carries the light level, so it must never reach zero alpha: a hole in it
        /// punches through to the unlit frame.
        /// </summary>
        [Fact]
        public void TheBaseLayerNeverOpensAHole()
        {
            Bake()[0].Params.Noise.FlatFloor.Should().BeGreaterThan(0.5f);
        }

        /// <summary>
        /// Structure and motion belong to the shadow pass. A textured base reads as dirt on the lens,
        /// and the two passes churning at the same rate collapses into one substance.
        /// </summary>
        [Fact]
        public void TheShadowPassCarriesTheTexture()
        {
            List<OverlayLayer> layers = Bake();

            layers[1].Params.Noise.FlatFloor.Should().Be(0f);
            layers[1].Params.Noise.WarpStrength.Should().BeGreaterThan(layers[0].Params.Noise.WarpStrength);
        }

        /// <summary>A preset that touches nothing, to read the shipped defaults off.</summary>
        private sealed class DefaultPreset : ScreenOverlayPreset
        {
            protected override void Bake(List<OverlayLayer> layers)
            {
            }
        }

        /// <summary>
        /// Weather arrives and clears over longer than an injury does. Measured against the shared
        /// defaults rather than against fixed seconds, since the exact timing is a tuning knob and
        /// only the relationship is a design decision.
        /// </summary>
        [Fact]
        public void ItArrivesAndClearsSlowly()
        {
            var preset = new OvercastOverlay();
            var shared = new DefaultPreset();

            preset.FadeInSeconds.Should().BeGreaterThan(shared.FadeInSeconds);
            preset.FadeOutSeconds.Should().BeGreaterThan(shared.FadeOutSeconds);
            preset.FadeOutSeconds.Should().BeGreaterThan(preset.FadeInSeconds);
        }

        /// <summary>
        /// Nothing distorts the frame, so the look costs one draw call per layer and no scene taps -
        /// which is what lets it run for as long as weather lasts.
        /// </summary>
        [Fact]
        public void NothingSamplesTheScene()
        {
            Bake().Should().OnlyContain(layer => layer.Params.Sampling.Mode == OverlaySampleMode.None);
        }

        [Fact]
        public void TheShippedProfileIsRegistered()
        {
            BuiltInProfiles.Find(BuiltInProfiles.Ids.Overcast).Should().NotBeNull();
        }
    }
}
