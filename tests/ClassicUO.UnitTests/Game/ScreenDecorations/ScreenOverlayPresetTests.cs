using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations
{
    public class ScreenOverlayPresetTests
    {
        /// <summary>
        /// Bakes <paramref name="count"/> layers all asking for an unsafe pulse frequency, to prove
        /// the clamp is applied per layer rather than once for the overlay.
        /// </summary>
        private sealed class UnsafePreset : ScreenOverlayPreset
        {
            private readonly int _count;

            public UnsafePreset(int count) => _count = count;

            protected override void Bake(List<OverlayLayer> layers)
            {
                for (int i = 0; i < _count; i++)
                {
                    OverlayParams p = OverlayParams.Default;
                    p.Appearance.PulseFreq = 30f;
                    p.Appearance.Opacity = 4f;
                    p.Shape.Feather = 0f;

                    layers.Add(new OverlayLayer { Params = p });
                }
            }
        }

        private static List<OverlayLayer> Bake(ScreenOverlayPreset preset)
        {
            var layers = new List<OverlayLayer>();
            preset.BakeClamped(layers);
            return layers;
        }

        [Fact]
        public void EveryLayerIsClampedIndependently()
        {
            List<OverlayLayer> layers = Bake(new UnsafePreset(ScreenOverlayPreset.MaxLayers));

            layers.Should().HaveCount(ScreenOverlayPreset.MaxLayers);

            foreach (OverlayLayer layer in layers)
            {
                layer.Params.Appearance.PulseFreq.Should().Be(OverlayParams.MaxPulseFreqHz);
                layer.Params.Appearance.Opacity.Should().Be(1f);
                layer.Params.Shape.Feather.Should().BeGreaterThan(0f);
            }
        }

        [Fact]
        public void LayerCountIsCappedAtMaxLayers()
        {
            List<OverlayLayer> layers = Bake(new UnsafePreset(ScreenOverlayPreset.MaxLayers + 3));

            layers.Should().HaveCount(ScreenOverlayPreset.MaxLayers);
        }

        [Fact]
        public void RebakingReplacesRatherThanAppends()
        {
            var preset = new UnsafePreset(2);
            var layers = new List<OverlayLayer>();

            preset.BakeClamped(layers);
            preset.BakeClamped(layers);

            layers.Should().HaveCount(2);
        }

        [Theory]
        [MemberData(nameof(SingleLayerPresets))]
        public void SingleLayerPresetsBakeExactlyOneAlphaLayer(ScreenOverlayPreset preset)
        {
            List<OverlayLayer> layers = Bake(preset);

            layers.Should().ContainSingle();
            layers[0].Blend.Should().Be(OverlayBlend.Alpha);
        }

        public static IEnumerable<object[]> SingleLayerPresets() =>
            new[]
            {
                new object[] { new TunnelVisionOverlay() },
                new object[] { new FractureOverlay() }
            };

        /// <summary>
        /// Selected by role rather than by index: the preset also bakes a distortion layer, and
        /// which position that occupies is a composition detail these assertions do not care about.
        /// </summary>
        private static List<OverlayLayer> PaintedLayers(ScreenOverlayPreset preset) =>
            Bake(preset).Where(l => !l.Params.Sampling.ReadsScene).ToList();

        /// <summary>
        /// The gas layer alone tints without ever obscuring, which reads as a colour filter rather
        /// than as being poisoned. The dark wash under it is what does the occluding.
        /// </summary>
        [Fact]
        public void PoisonBakesAGasLayerOverADarkerWash()
        {
            List<OverlayLayer> painted = PaintedLayers(new PoisonOverlay());

            painted.Should().HaveCount(2);
            Brightness(painted[0]).Should().BeLessThan(Brightness(painted[1]));

            // Mostly floored, so it is a field rather than a pattern - anything legible in it would
            // fight the gas above it.
            painted[0].Params.Noise.FlatFloor.Should().BeGreaterThan(0.5f);
        }

        /// <summary>
        /// Every stacked preset draws deepest first, so reach has to fall monotonically through the
        /// stack. An inversion puts a layer's boundary inside one it is meant to sit behind, which is
        /// the one arrangement that looks broken rather than merely mistuned - and for the sampling
        /// presets it also means the distortion stops short of the colour it exists to soften.
        /// </summary>
        [Theory]
        [MemberData(nameof(BuiltInEffects))]
        public void EveryPresetOrdersItsLayersFromDeepestToShallowest(OverlayEffectSlot effectSlot)
        {
            ScreenOverlayPreset? preset = BuiltInOverlayPresets.Create(effectSlot);

            if (preset == null)
                return;

            List<float> reaches = Bake(preset).Select(l => l.Params.Shape.Reach).ToList();

            reaches.Should().BeInDescendingOrder();
        }

        /// <summary>
        /// The stagger has to survive the whole range of the knob, not merely its default. Deriving
        /// the layers by multiplying Reach satisfies the default and fails at both ends: proportional
        /// margins collapse toward zero as Reach falls, and the deepest layer saturates first as it
        /// rises, so the shallower ones climb to meet a pinned one.
        /// </summary>
        [Theory]
        [InlineData(0.02f)]
        [InlineData(0.2f)]
        [InlineData(0.5f)]
        [InlineData(1f)]
        public void StackedPresetsStayStaggeredAtEveryReach(float reach)
        {
            AssertStaggered(Bake(new PoisonOverlay { Reach = reach }));
            AssertStaggered(Bake(new BleedOverlay { Reach = reach }));
            AssertStaggered(Bake(new FogOverlay { Reach = reach }));
        }

        /// <summary>
        /// Ordered, with no layer collapsed to zero reach - a layer that is not drawn at all.
        /// <para>
        /// Boundaries must also stay distinct, but only while any of them is still on screen.
        /// Converging at full reach is the accepted outcome of asking for an effect that covers
        /// everything: the masks saturate, so no boundary is visible for them to coincide on. The
        /// arrangement that actually reads as a hard ring is several boundaries landing together in
        /// the middle of the screen, which is what the margins prevent.
        /// </para>
        /// </summary>
        private static void AssertStaggered(List<OverlayLayer> layers)
        {
            List<float> reaches = layers.Select(l => l.Params.Shape.Reach).ToList();

            reaches.Should().BeInDescendingOrder();
            reaches.Should().OnlyContain(r => r > 0f);

            if (reaches[0] < LayerReach.Max)
                reaches.Distinct().Should().HaveCount(reaches.Count);
        }

        public static TheoryData<OverlayEffectSlot> BuiltInEffects()
        {
            var data = new TheoryData<OverlayEffectSlot>();

            foreach (OverlayEffectSlot effect in Enum.GetValues<OverlayEffectSlot>())
                data.Add(effect);

            return data;
        }

        /// <summary>
        /// Gas rises. Positive V scroll walks the sample point down the texture, which carries the
        /// pattern up the screen - the opposite sign from the bleed preset's flow.
        /// </summary>
        [Fact]
        public void PoisonFlowsUpwardWithTheGasOutrunningTheWash()
        {
            List<OverlayLayer> painted = PaintedLayers(new PoisonOverlay());

            foreach (OverlayLayer layer in painted)
                layer.Params.Noise.BaseScroll.Y.Should().BePositive();

            // Parallax: the near, lighter layer has to move faster than the heavy one behind it, or
            // the pair reads as flat.
            ScreenSpeed(painted[1].Params.Noise.BaseScroll, painted[1].Params.Noise.BaseScale)
                .Should()
                .BeGreaterThan(ScreenSpeed(painted[0].Params.Noise.BaseScroll, painted[0].Params.Noise.BaseScale));
        }

        [Fact]
        public void BleedBakesDarkBasalLayersUnderASingleAdditiveHighlight()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay());

            layers.Should().HaveCount(3);

            // Exactly one additive pass, and it must be last: an additive layer is the only thing
            // that lightens the composite, so anything drawn after it would be washed out by it.
            layers.Select(l => l.Blend)
                  .Should()
                  .Equal(OverlayBlend.Alpha, OverlayBlend.Alpha, OverlayBlend.Additive);
        }

        [Fact]
        public void BleedIsWeightedTowardItsBasalLayers()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay());

            float highlight = layers[2].Params.Appearance.Opacity;

            // The specular exists to define a surface, not to be seen. Every dark pass under it has
            // to carry more weight, or the effect reads as mostly bright.
            for (int i = 0; i < 2; i++)
                layers[i].Params.Appearance.Opacity.Should().BeGreaterThan(highlight * 2f);
        }

        [Fact]
        public void BleedHighlightIsTheBrightestAndSparsestPass()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay());

            OverlayLayer highlight = layers[2];

            foreach (OverlayLayer basal in layers.Take(2))
            {
                Brightness(highlight).Should().BeGreaterThan(Brightness(basal));
                highlight.Params.Noise.Threshold.Should().BeGreaterThan(basal.Params.Noise.Threshold);
            }
        }

        /// <summary>
        /// The ridge transform outlines the field's median and the Worley/ridged channels pack cell
        /// edges and crack filaments. Any of the three turns this preset into a microscope slide.
        /// </summary>
        [Fact]
        public void BleedUsesNoCellularOrOutliningNoise()
        {
            foreach (OverlayLayer layer in Bake(new BleedOverlay()))
            {
                layer.Params.Noise.RidgeAmount.Should().Be(0f);
                layer.Params.Noise.BaseChannel.Should().BeOneOf(NoiseChannel.Red, NoiseChannel.Green);
                layer.Params.Noise.DetailChannel.Should().BeOneOf(NoiseChannel.Red, NoiseChannel.Green);
            }
        }

        /// <summary>
        /// The radial term is normalised to screen width, so on a widescreen display it reaches far
        /// less at the top and bottom than at the sides. Any radial component in a border trim shows
        /// up as a left/right bias; corner weighting has to come from CornerBias, which is measured
        /// per axis.
        /// </summary>
        [Fact]
        public void BleedHasNoAspectBiasedRadialComponent()
        {
            foreach (OverlayLayer layer in Bake(new BleedOverlay()))
            {
                layer.Params.Shape.EdgeBlend.Should().Be(1f);
                layer.Params.Shape.CornerBias.Should().BeGreaterThan(0f);
            }
        }

        /// <summary>
        /// Any flat floor makes the shape mask render its own geometry - a soft-edged rectangle -
        /// instead of letting the noise carve streaks out of it.
        /// </summary>
        [Fact]
        public void BleedIsFullyNoiseDriven()
        {
            foreach (OverlayLayer layer in Bake(new BleedOverlay()))
                layer.Params.Noise.FlatFloor.Should().Be(0f);
        }

        /// <summary>
        /// Screen speed is scroll/scale, not scroll. Two dark layers travelling at different speeds
        /// stop reading as one substance and start reading as cloud shadow drifting over terrain.
        /// </summary>
        [Fact]
        public void BleedBasalLayersTravelAtOneSpeed()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay());

            float reference = ScreenSpeed(layers[0].Params.Noise.BaseScroll, layers[0].Params.Noise.BaseScale);

            foreach (OverlayLayer basal in layers.Take(2))
            {
                ScreenSpeed(basal.Params.Noise.BaseScroll, basal.Params.Noise.BaseScale).Should().BeApproximately(reference, 1e-5f);
                ScreenSpeed(basal.Params.Noise.DetailScroll, basal.Params.Noise.DetailScale).Should().BeApproximately(reference, 1e-5f);
            }
        }

        [Fact]
        public void BleedHighlightDriftsOverTheRunsItRidesOn()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay());
            OverlayNoise runs = layers[1].Params.Noise;
            OverlayNoise highlight = layers[2].Params.Noise;

            // Same scale and channel, so it sits on the same rivulets...
            highlight.BaseScale.Should().Be(runs.BaseScale);
            highlight.BaseChannel.Should().Be(runs.BaseChannel);

            // ...but faster, so it slides along them instead of being locked to them, which would
            // look like one texture drawn twice. Only slightly faster: at a large ratio it stops
            // being a specular and becomes a second thing moving over the fluid.
            float ratio = ScreenSpeed(highlight.BaseScroll, highlight.BaseScale)
                        / ScreenSpeed(runs.BaseScroll, runs.BaseScale);

            ratio.Should().BeInRange(1.05f, 1.5f);
        }

        [Fact]
        public void BleedOpacityScalesEveryLayer()
        {
            List<OverlayLayer> quiet = Bake(new BleedOverlay { Opacity = 0.2f });
            List<OverlayLayer> loud = Bake(new BleedOverlay { Opacity = 0.8f });

            for (int i = 0; i < quiet.Count; i++)
            {
                loud[i].Params.Appearance.Opacity
                       .Should()
                       .BeGreaterThan(quiet[i].Params.Appearance.Opacity);
            }
        }

        [Fact]
        public void BleedHueDrivesTheDerivedFilmTint()
        {
            List<OverlayLayer> layers = Bake(new BleedOverlay { Hue = new Microsoft.Xna.Framework.Color(200, 40, 40) });

            // The film tint is scaled from Hue rather than exposed separately, so changing the blood
            // colour has to move it too or the layers stop being one substance.
            Brightness(layers[0]).Should().BeLessThan(Brightness(layers[1]));
        }

        /// <summary>
        /// The shape mask is a function of distance to the nearest screen edge alone, so with no
        /// jitter every column of a border trim terminates at exactly the same depth and the effect
        /// reads as a rectangle.
        /// </summary>
        [Fact]
        public void BleedLayersAllDisplaceTheirShapeBoundary()
        {
            foreach (OverlayLayer layer in Bake(new BleedOverlay()))
            {
                OverlayJitter jitter = layer.Params.Shape.Jitter;

                jitter.ReachAmount.Should().BeGreaterThan(0f);

                // Displacing the boundary alone still gives every column the same gradient profile
                // at a different offset. Varying the falloff width alongside it is what makes a
                // deep run taper and a shallow one end bluntly.
                jitter.FeatherAmount.Should().BeGreaterThan(0f);

                // The displacement has to be coarser than the detail it is displacing, or the
                // boundary just buzzes at the same rate as the texture instead of making some
                // columns reach deeper than others.
                jitter.Scale.X.Should().BeLessThan(layer.Params.Noise.BaseScale.X);
            }
        }

        private static float ScreenSpeed(Microsoft.Xna.Framework.Vector2 scroll, Microsoft.Xna.Framework.Vector2 scale) =>
            System.MathF.Abs(scroll.Y) / scale.Y;

        private static float Brightness(OverlayLayer layer)
        {
            Microsoft.Xna.Framework.Color tint = layer.Params.Appearance.Tint;

            return tint.R + tint.G + tint.B;
        }
    }
}
