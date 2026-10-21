using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.ApiClasses.VisualEffects;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The DTO tier has to be lossless in both directions: a script fetching a shipped look, changing
/// nothing and starting it must get that look, and anything it does change must survive. Parity of
/// field <em>names</em> is asserted elsewhere; this is parity of values.
/// </summary>
public class ApiEffectDtoConversionTests
{
    private static List<OverlayLayer> Bake(EffectProfile profile)
    {
        var layers = new List<OverlayLayer>();
        profile.BakeClamped(layers);

        return layers;
    }

    /// <summary>Only the shipped looks with something drawn; the quake is shake alone.</summary>
    public static TheoryData<string> DrawnProfileIds()
    {
        var ids = new TheoryData<string>();

        foreach (EffectProfile profile in BuiltInProfiles.All.Where(profile => profile.Layers.Count > 0))
            ids.Add(profile.Id.ToString());

        return ids;
    }

    [Theory]
    [MemberData(nameof(DrawnProfileIds))]
    public void ProjectingAShippedLookAndFlatteningItBakesIdentically(string id)
    {
        EffectProfile shipped = BuiltInProfiles.Find(Guid.Parse(id));

        shipped.Should().NotBeNull();

        EffectProfile roundTripped = ApiEffectProfile.From(shipped).ToInternal();

        Bake(roundTripped).Should().BeEquivalentTo(Bake(shipped));
        roundTripped.FullScreen.Should().Be(shipped.FullScreen);
        roundTripped.Fade.InSeconds.Should().Be(shipped.Fade.InSeconds);
        roundTripped.Fade.OutSeconds.Should().Be(shipped.Fade.OutSeconds);
    }

    /// <summary>
    /// The shipped compositions are a cached singleton list shared by every rule pointing at one, so
    /// a DTO that kept a reference into them would let one script retune the client for the session.
    /// </summary>
    [Fact]
    public void ProjectingALookCopiesItRatherThanSharingIt()
    {
        EffectProfile shipped = BuiltInProfiles.Find(BuiltInProfiles.Ids.Poison);

        shipped.Should().NotBeNull();

        float authored = shipped.Layers[0].Effect!.Strength;

        ApiEffectProfile projected = ApiEffectProfile.From(shipped);
        projected.Layers[0].Effect!.Strength = 0.123;
        projected.Name = "mine";

        shipped.Layers[0].Effect!.Strength.Should().Be(authored);
        shipped.Name.Should().NotBe("mine");
    }

    /// <summary>
    /// Flattening is the hand-off, so a DTO edited after the effect was started must not reach it.
    /// </summary>
    [Fact]
    public void FlatteningProducesAnInstanceLaterEditsCannotReach()
    {
        ApiEffectProfile config = NewTintEffect(0.5);
        EffectProfile flattened = config.ToInternal();

        config.Layers[0].Effect!.Strength = 1.0;
        config.Shake = new ApiShakeSpec();

        flattened.Layers[0].Effect!.Strength.Should().Be(0.5f);
        flattened.Shake.Should().BeNull();
    }

    [Fact]
    public void AVisualEffectWithNoLayersIsRejected()
    {
        var config = new ApiEffectProfile();

        config.Invoking(effect => effect.ToInternal())
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*at least one layer*");
    }

    [Fact]
    public void AVisualEffectWithMoreLayersThanTheStackAllowsIsRejected()
    {
        var config = new ApiEffectProfile();

        for (int i = 0; i <= OverlayLayerStack.MaxLayers; i++)
            config.AddLayer(new ApiTintLayer());

        config.Invoking(effect => effect.ToInternal()).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ALayerWithNothingToDrawIsRejected()
    {
        var config = new ApiEffectProfile();
        config.Layers.Add(new ApiProfileLayer());

        config.Invoking(effect => effect.ToInternal())
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*must have an Effect*");
    }

    /// <summary>
    /// A cast would carry an invented enum value through to the shader as a technique or channel
    /// index nothing handles. The ones that index an unrolled loop in the shader matter most.
    /// </summary>
    [Fact]
    public void AnInventedEnumValueIsRejected()
    {
        var blur = new ApiBlurLayer { Taps = (LegionAPI.OverlaySampleTaps)7 };
        var config = new ApiEffectProfile();
        config.AddLayer(blur);

        config.Invoking(effect => effect.ToInternal())
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Taps*");
    }

    [Fact]
    public void AnInventedBlendIsRejected()
    {
        var config = new ApiEffectProfile();
        config.AddLayer(new ApiTintLayer(), (LegionAPI.OverlayBlend)9);

        config.Invoking(effect => effect.ToInternal()).Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Channels are clamped rather than rejected: an arithmetic slip in a script is better answered
    /// with the nearest colour than by refusing to draw.
    /// </summary>
    [Fact]
    public void ColourChannelsOutOfRangeAreClampedNotRejected()
    {
        var config = new ApiEffectProfile();
        config.AddLayer(new ApiTintLayer { R = 300, G = -5, B = 128, A = 255 });

        OverlayParams baked = config.ToInternal().Layers[0].Effect!.Bake();

        baked.Appearance.Tint.R.Should().Be(255);
        baked.Appearance.Tint.G.Should().Be(0);
        baked.Appearance.Tint.B.Should().Be(128);
    }

    /// <summary>
    /// Clearing a per-axis knob is a legitimate thing to want, and in Python it reads as assigning
    /// None. Faulting on that would make the obvious spelling the wrong one.
    /// </summary>
    [Fact]
    public void ClearingAPerAxisKnobReadsAsZeroRatherThanFaulting()
    {
        var tint = new ApiTintLayer();
        tint.Noise!.BaseScroll = null;
        tint.Shape!.Jitter = null;

        var config = new ApiEffectProfile();
        config.AddLayer(tint);

        OverlayParams baked = config.ToInternal().Layers[0].Effect!.Bake();

        baked.Noise.BaseScroll.Should().Be(Microsoft.Xna.Framework.Vector2.Zero);
        baked.Shape.Jitter.ReachAmount.Should().Be(0f);
    }

    /// <summary>A shake with no duration could never stop on its own, so it is not schedulable.</summary>
    [Fact]
    public void AShakeWithNoDurationIsRejected()
    {
        var shake = new ApiShakeConfig { Seconds = 0 };

        shake.Invoking(config => config.ToInternal()).Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Ramps are capped at the duration, so a value that could never be reached cannot quietly do
    /// nothing. Overlapping ramps are left alone - they multiply, which is a legitimate short shake.
    /// </summary>
    [Fact]
    public void ShakeRampsAreCappedAtTheDuration()
    {
        var shake = new ApiShakeConfig { Seconds = 1, RampUpSeconds = 10, RampDownSeconds = 10 };

        ClassicUO.Game.ScreenDecorations.Shake.ShakeRequest request = shake.ToInternal();

        request.RampUp.Should().Be(TimeSpan.FromSeconds(1));
        request.RampDown.Should().Be(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// The factories exist so a script does not start from zeros: a composition built from them and
    /// started unchanged has to draw something.
    /// </summary>
    [Fact]
    public void AFreshLayerCarriesTheClientsOwnDefaults()
    {
        OverlayParams baked = new ApiTintLayer().ToInternal().Bake();

        baked.Appearance.Opacity.Should().BeGreaterThan(0f);
        baked.Shape.Reach.Should().Be(OverlayParams.Default.Shape.Reach);
        baked.Noise.BaseScale.Should().Be(OverlayParams.Default.Noise.BaseScale);
    }

    [Fact]
    public void AFreshCompositionCarriesTheClientsOwnFadeTiming()
    {
        var expected = new FadeSpec();
        ApiFadeSpec fade = new ApiEffectProfile().Fade;

        fade.Should().NotBeNull();
        fade!.InSeconds.Should().Be(expected.InSeconds);
        fade.OutSeconds.Should().Be(expected.OutSeconds);
    }

    private static ApiEffectProfile NewTintEffect(double strength)
    {
        var config = new ApiEffectProfile();
        config.AddLayer(new ApiTintLayer { Strength = strength });

        return config;
    }
}
