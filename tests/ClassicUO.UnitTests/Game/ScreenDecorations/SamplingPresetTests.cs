using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class SamplingPresetTests
{
    private static List<OverlayLayer> Bake(ScreenOverlayPreset preset)
    {
        var layers = new List<OverlayLayer>();
        preset.BakeClamped(layers);
        return layers;
    }

    public static TheoryData<ShippedPreset, OverlaySampleMode> SamplingPresets()
    {
        return new TheoryData<ShippedPreset, OverlaySampleMode>
        {
            { ShippedPreset.Fog, OverlaySampleMode.Blur },
            { ShippedPreset.Drunk, OverlaySampleMode.Radial },
            { ShippedPreset.Concussion, OverlaySampleMode.Chromatic }
        };
    }

    [Theory]
    [MemberData(nameof(SamplingPresets))]
    public void EachSamplingPresetUsesItsOwnMode(ShippedPreset preset, OverlaySampleMode mode)
    {
        List<OverlayLayer> layers = Bake(ShippedPresetCatalog.Create(preset));

        layers.Should().Contain(l => l.Params.Sampling.Mode == mode);
    }

    /// <summary>
    /// A sampling layer reads the frame from before the overlay pass, so anything this preset
    /// drew underneath it is absent from what it samples - and it composites at its own alpha,
    /// which overwrites those layers rather than distorting them.
    /// </summary>
    [Theory]
    [MemberData(nameof(SamplingPresets))]
    public void TheSamplingLayerIsAtTheBottomOfTheStack(ShippedPreset preset, OverlaySampleMode mode)
    {
        List<OverlayLayer> layers = Bake(ShippedPresetCatalog.Create(preset));

        layers[0].Params.Sampling.Mode.Should().Be(mode);
    }

    /// <summary>Two sampling layers both read the same pre-pass frame, so the upper one
    /// overwrites the lower instead of compounding with it.</summary>
    [Theory]
    [MemberData(nameof(SamplingPresets))]
    public void NoPresetStacksTwoSamplingLayers(ShippedPreset preset, OverlaySampleMode mode)
    {
        _ = mode;

        Bake(ShippedPresetCatalog.Create(preset)).Count(l => l.Params.Sampling.ReadsScene).Should().Be(1);
    }

    /// <summary>The distortion alone has nothing to say about color, so every one of these
    /// presets pairs it with a painted layer.</summary>
    [Theory]
    [MemberData(nameof(SamplingPresets))]
    public void EachSamplingPresetPaintsOverItsDistortion(ShippedPreset preset, OverlaySampleMode mode)
    {
        _ = mode;

        Bake(ShippedPresetCatalog.Create(preset)).Should().Contain(l => !l.Params.Sampling.ReadsScene);
    }

    [Fact]
    public void SwimIsTakenOffTheFlatFloorSoStrengthVaries()
    {
        OverlayLayer steady = SamplingLayers.Blur(SamplingShape.Vignette(1f, 0.4f, 1f), 0.01f);
        OverlayLayer swimming = SamplingLayers.Blur(SamplingShape.Vignette(1f, 0.4f, 1f, 0.5f), 0.01f);

        steady.Params.Noise.FlatFloor.Should().Be(1f);
        swimming.Params.Noise.FlatFloor.Should().BeApproximately(0.5f, 1e-5f);
    }

    [Fact]
    public void ShapeStrengthBecomesTheLayerOpacity()
    {
        SamplingLayers.Radial(SamplingShape.Vignette(0.8f, 0.4f, 0.65f), 0.1f)
                      .Params.Appearance.Opacity
                      .Should()
                      .BeApproximately(0.65f, 1e-5f);
    }

    /// <summary>Clamped by Clamp(), but the presets should be shipping usable values in the
    /// first place rather than relying on being caught.</summary>
    [Theory]
    [MemberData(nameof(SamplingPresets))]
    public void ShippedSamplingValuesAreWithinTheirCeilings(ShippedPreset preset, OverlaySampleMode mode)
    {
        _ = mode;

        OverlaySampling sampling = Bake(ShippedPresetCatalog.Create(preset)).First(l => l.Params.Sampling.ReadsScene).Params.Sampling;

        Enum.IsDefined(sampling.Taps).Should().BeTrue();
        sampling.Radius.Should().BeLessThanOrEqualTo(OverlayParams.MaxSampleRadius);
        sampling.Aberration.Should().BeLessThanOrEqualTo(OverlayParams.MaxSampleAberration);
        sampling.Zoom.Should().BeInRange(0f, 1f);
    }
}
