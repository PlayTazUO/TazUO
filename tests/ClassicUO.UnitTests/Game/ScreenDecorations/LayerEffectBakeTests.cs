using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The narrow layer types are an authoring model over the flat struct the shader is uploaded
/// from, and <see cref="LayerEffect.Bake"/> is the whole boundary between them. If that boundary
/// is not exact then the shipped looks change the day they stop being presets, which is a
/// regression nothing else would catch - the effects are tuned by eye.
/// </summary>
public class LayerEffectBakeTests
{
    private static List<OverlayLayer> BakePreset(ScreenOverlayPreset preset)
    {
        var layers = new List<OverlayLayer>();
        preset.BakeClamped(layers);

        return layers;
    }

    private static List<OverlayLayer> BakeProfile(EffectProfile profile)
    {
        var layers = new List<OverlayLayer>();
        profile.BakeClamped(layers);

        return layers;
    }

    /// <summary>
    /// Recovering a flat layer into its narrow effect and baking it again has to be the identity,
    /// or the built-in profiles are not the presets they were seeded from.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShippedPresets))]
    public void RecoveringAndRebakingALayerChangesNothing(ShippedPreset preset)
    {
        foreach (OverlayLayer layer in BakePreset(ShippedPresetCatalog.Create(preset)))
        {
            OverlayParams rebaked = LayerEffectFactory.FromParams(layer.Params).Bake();

            rebaked.Should().BeEquivalentTo(layer.Params);
        }
    }

    [Theory]
    [MemberData(nameof(ShippedPresets))]
    public void EveryBuiltInProfileBakesExactlyLikeItsPreset(ShippedPreset preset)
    {
        EffectProfile profile = BuiltInProfiles.Find(ShippedPresetCatalog.ProfileId(preset));

        profile.Should().NotBeNull();
        BakeProfile(profile).Should().BeEquivalentTo(BakePreset(ShippedPresetCatalog.Create(preset)));
    }

    /// <summary>Each technique must reach its own sampling mode and no other's, since the mode is
    /// what selects the compiled pixel shader.</summary>
    [Fact]
    public void EachTechniqueBakesToItsOwnSamplingMode()
    {
        new TintEffect().Bake().Sampling.Mode.Should().Be(OverlaySampleMode.None);
        new BlurEffect().Bake().Sampling.Mode.Should().Be(OverlaySampleMode.Blur);
        new RadialBlurEffect().Bake().Sampling.Mode.Should().Be(OverlaySampleMode.Radial);
        new ChromaticEffect().Bake().Sampling.Mode.Should().Be(OverlaySampleMode.Chromatic);
    }

    /// <summary>
    /// The authored strength is the layer's own opacity; everything that scales it at runtime -
    /// occurrence intensity, the fade envelope, the global setting - is applied by the compositor
    /// instead. Baking it in would apply it twice.
    /// </summary>
    [Fact]
    public void BakingLeavesTheRuntimeIntensityDialAlone()
    {
        OverlayParams baked = new TintEffect { Strength = 0.4f }.Bake();

        baked.Appearance.Opacity.Should().Be(0.4f);
        baked.Appearance.Intensity.Should().Be(1f);
    }

    /// <summary>
    /// The photosensitivity ceiling is not the caller's to skip. Clamping inside Bake is what
    /// stops an authored profile - which is hand-editable JSON - from reaching the shader with a
    /// value no code path checked.
    /// </summary>
    [Fact]
    public void BakingClampsTheSafetyCeilings()
    {
        OverlayParams tint = new TintEffect
        {
            Strength = 4f,
            Pulse = new PulseSpec { Frequency = 30f, Amplitude = 5f }
        }.Bake();

        tint.Appearance.PulseFreq.Should().Be(OverlayParams.MaxPulseFreqHz);
        tint.Appearance.PulseAmp.Should().Be(1f);
        tint.Appearance.Opacity.Should().Be(1f);

        new BlurEffect { Radius = 10f }.Bake().Sampling.Radius.Should().Be(OverlayParams.MaxSampleRadius);
        new ChromaticEffect { Aberration = 10f }.Bake().Sampling.Aberration.Should().Be(OverlayParams.MaxSampleAberration);
    }

    /// <summary>
    /// A technique's knobs exist only on the technique that reads them, so the fields another one
    /// would have read must bake to nothing rather than to whatever was left lying in the struct.
    /// </summary>
    [Fact]
    public void ATechniqueLeavesNoOtherTechniquesFieldsSet()
    {
        OverlaySampling blur = new BlurEffect { Radius = 0.01f }.Bake().Sampling;

        blur.Zoom.Should().Be(0f);
        blur.Aberration.Should().Be(0f);
    }

    [Fact]
    public void CloningAnEffectLeavesTheOriginalAlone()
    {
        var original = new BlurEffect { Radius = 0.02f, Strength = 0.5f };
        original.Shape = original.Shape with { Reach = 0.3f };

        var copy = (BlurEffect)original.Clone();
        copy.Radius = 0.05f;
        copy.Strength = 0.9f;
        copy.Shape = copy.Shape with { Reach = 0.9f };

        original.Radius.Should().Be(0.02f);
        original.Strength.Should().Be(0.5f);
        original.Shape.Reach.Should().Be(0.3f);
    }

    /// <summary>Every preset the client ships, which is exactly the set the built-in profiles are
    /// seeded from.</summary>
    public static TheoryData<ShippedPreset> ShippedPresets()
    {
        return ShippedPresetCatalog.All();
    }
}
