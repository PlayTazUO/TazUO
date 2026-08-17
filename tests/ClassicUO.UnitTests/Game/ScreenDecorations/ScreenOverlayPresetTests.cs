using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

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
        List<OverlayLayer> layers = Bake(new UnsafePreset(OverlayLayerStack.MaxLayers));

        layers.Should().HaveCount(OverlayLayerStack.MaxLayers);

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
        List<OverlayLayer> layers = Bake(new UnsafePreset(OverlayLayerStack.MaxLayers + 3));

        layers.Should().HaveCount(OverlayLayerStack.MaxLayers);
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
    public void SingleLayerPresetsBakeExactlyOneAlphaLayer(ShippedPreset preset)
    {
        List<OverlayLayer> layers = Bake(ShippedPresetCatalog.Create(preset));

        layers.Should().ContainSingle();
        layers[0].Blend.Should().Be(OverlayBlend.Alpha);
    }

    public static TheoryData<ShippedPreset> SingleLayerPresets()
    {
        return ShippedPresetCatalog.Only(ShippedPreset.TunnelVision, ShippedPreset.Death);
    }

    /// <summary>
    /// Selected by role rather than by index: the preset also bakes a distortion layer, and
    /// which position that occupies is a composition detail these assertions do not care about.
    /// </summary>
    private static List<OverlayLayer> PaintedLayers(ScreenOverlayPreset preset)
    {
        return Bake(preset).Where(l => !l.Params.Sampling.ReadsScene).ToList();
    }

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
    /// <para>
    /// Bleed and Fog are deliberate exceptions, covered by their own tests below instead: Bleed's
    /// sputter is a sparse accent allowed to outreach the streaks it rides on top of, and Fog's
    /// blur is asked to stop short of the tint so the out-of-focus band stays a rim rather than
    /// covering the whole view.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(NestedDepthPresets))]
    public void EveryPresetOrdersItsLayersFromDeepestToShallowest(ShippedPreset preset)
    {
        List<float> reaches = Bake(ShippedPresetCatalog.Create(preset))
            .Select(l => l.Params.Shape.Reach)
            .ToList();

        reaches.Should().BeInDescendingOrder();
    }

    /// <summary>Every shipped preset whose layers nest strictly deepest-first. Excludes Bleed and
    /// Fog - see <see cref="EveryPresetOrdersItsLayersFromDeepestToShallowest"/>.</summary>
    public static TheoryData<ShippedPreset> NestedDepthPresets()
    {
        return ShippedPresetCatalog.Only(
            ShippedPreset.Poison,
            ShippedPreset.Drunk,
            ShippedPreset.Concussion,
            ShippedPreset.TunnelVision,
            ShippedPreset.Death
        );
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
    }

    /// <summary>
    /// The sputter is meant to carry reach past both streak passes, not nest behind them - it is
    /// sparse enough that it never fully occludes what is under it, so this is not the boundary
    /// inversion the other presets have to avoid.
    /// </summary>
    [Theory]
    [InlineData(0.02f)]
    [InlineData(0.2f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void BleedSputterOutreachesBothStreaksAtEveryReach(float reach)
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay { Reach = reach });

        float thinStreak = layers[0].Params.Shape.Reach;
        float wideStreak = layers[1].Params.Shape.Reach;
        float sputter = layers[2].Params.Shape.Reach;

        wideStreak.Should().BeGreaterThan(0f).And.BeLessThanOrEqualTo(thinStreak);
        sputter.Should().BeGreaterThanOrEqualTo(thinStreak);
    }

    /// <summary>
    /// The inverse of the other sampling presets: the blur is asked to stop short of the tint so
    /// the colour reaches the centre while the out-of-focus band stays a rim around it.
    /// </summary>
    [Theory]
    [InlineData(0.02f)]
    [InlineData(0.2f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void FogBlurStopsShortOfTheTintAtEveryReach(float reach)
    {
        List<OverlayLayer> layers = Bake(new FogOverlay { Reach = reach });

        float blur = layers[0].Params.Shape.Reach;
        float wash = layers[1].Params.Shape.Reach;

        blur.Should().BeGreaterThan(0f).And.BeLessThanOrEqualTo(wash);
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
    public void BleedBakesThreeAlphaLayers()
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay());

        layers.Should().HaveCount(3);

        // No additive specular any more - every pass is the same dark fluid.
        layers.Select(l => l.Blend).Should().OnlyContain(b => b == OverlayBlend.Alpha);
    }

    [Fact]
    public void BleedWeightsTheFineStreakMost()
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay());

        float fineStreak = layers[0].Params.Appearance.Opacity;

        // The fine streak carries the caller's Opacity outright; the wide streak and the
        // sputter are both accents riding on top of it and are scaled down, or the pair would
        // read as twice the blood the caller asked for.
        layers[1].Params.Appearance.Opacity.Should().BeLessThan(fineStreak);
        layers[2].Params.Appearance.Opacity.Should().BeLessThan(fineStreak);
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
    /// Screen speed is scroll/scale, not scroll. The two streak passes are asked to stay close -
    /// travelling at wildly different speeds would stop them reading as one substance - but no
    /// longer identical: matching exactly is what made the pair look like one texture gliding in
    /// rigid lockstep.
    /// </summary>
    [Fact]
    public void BleedStreaksStayCloseInSpeedButNotIdentical()
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay());

        float thin = ScreenSpeed(layers[0].Params.Noise.BaseScroll, layers[0].Params.Noise.BaseScale);
        float wide = ScreenSpeed(layers[1].Params.Noise.BaseScroll, layers[1].Params.Noise.BaseScale);

        (wide / thin).Should().BeInRange(0.8f, 0.98f);
    }

    /// <summary>
    /// Verifies the fixed per-layer shift added so layers sharing a scale and scroll do not read
    /// as the same texture traced twice.
    /// </summary>
    [Fact]
    public void BleedLayersHaveDistinctNoiseOffsets()
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay());

        List<Microsoft.Xna.Framework.Vector2> offsets = layers.Select(l => l.Params.Noise.Offset).ToList();

        offsets.Distinct().Should().HaveCount(offsets.Count);
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
    public void BleedHueFlowsUnscaledIntoEveryLayer()
    {
        List<OverlayLayer> layers = Bake(new BleedOverlay { Hue = new Microsoft.Xna.Framework.Color(200, 40, 40) });

        // No separate highlight hue any more - every pass reads as the same fluid, so all three
        // must carry the caller's Hue at the same brightness.
        layers.Select(l => Brightness(l)).Should().OnlyContain(b => b == Brightness(layers[0]));
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

    private static float ScreenSpeed(Microsoft.Xna.Framework.Vector2 scroll, Microsoft.Xna.Framework.Vector2 scale)
    {
        return System.MathF.Abs(scroll.Y) / scale.Y;
    }

    private static float Brightness(OverlayLayer layer)
    {
        Microsoft.Xna.Framework.Color tint = layer.Params.Appearance.Tint;

        return tint.R + tint.G + tint.B;
    }
}
