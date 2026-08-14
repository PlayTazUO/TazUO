using System.Collections.Generic;
using System.Text.Json;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Profiles are the pool rules point at, and they are stored as hand-editable JSON. Two things
/// therefore have to hold: every authored value survives the trip to disk and back exactly, and
/// nothing a file can say gets past the safety clamps.
/// </summary>
public class EffectProfileTests
{
    private static string Serialize(ScreenDecorations config) =>
        JsonSerializer.Serialize(config, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations);

    private static ScreenDecorations Deserialize(string json) =>
        JsonSerializer.Deserialize(json, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations);

    private static ScreenDecorations RoundTrip(ScreenDecorations config) => Deserialize(Serialize(config));

    private static ScreenDecorations WithProfile(EffectProfile profile)
    {
        var config = new ScreenDecorations();
        config.Overlays.AddProfile(profile);

        return config;
    }

    private static EffectProfile AuthoredCopyOfBleed() =>
        BuiltInProfiles.Find(BuiltInProfiles.Ids.Bleed).Clone("Bleed Copy");

    private static List<OverlayLayer> Bake(EffectProfile profile)
    {
        var layers = new List<OverlayLayer>();
        profile.BakeClamped(layers);

        return layers;
    }

    /// <summary>
    /// The point of the polymorphic layer model: a stored layer has to come back as the same
    /// technique carrying the same values, or authoring silently loses tuning.
    /// </summary>
    [Fact]
    public void EveryLayerParameterSurvivesARoundTrip()
    {
        EffectProfile authored = AuthoredCopyOfBleed();

        ScreenDecorations loaded = RoundTrip(WithProfile(authored));

        loaded.Overlays.Profiles.Should().ContainSingle();
        Bake(loaded.Overlays.Profiles[0]).Should().BeEquivalentTo(Bake(authored));
    }

    [Fact]
    public void ProfileMetadataSurvivesARoundTrip()
    {
        EffectProfile authored = AuthoredCopyOfBleed();
        authored.Fade.InSeconds = 1.25f;
        authored.Fade.OutSeconds = 2.5f;
        authored.FullScreen = true;
        authored.Shake = new ShakeSpec { Trauma = 0.4f, DurationSeconds = 1.5f };

        EffectProfile loaded = RoundTrip(WithProfile(authored)).Overlays.Profiles[0];

        loaded.Id.Should().Be(authored.Id);
        loaded.Name.Should().Be("Bleed Copy");
        loaded.FullScreen.Should().BeTrue();
        loaded.Fade.InSeconds.Should().Be(1.25f);
        loaded.Fade.OutSeconds.Should().Be(2.5f);
        loaded.Shake!.Trauma.Should().Be(0.4f);
        loaded.Shake.DurationSeconds.Should().Be(1.5f);
    }

    /// <summary>
    /// The shipped looks live in code so that they stay correct as the client is retuned. Storing
    /// a copy of one would freeze it at whatever the release that wrote it happened to think.
    /// </summary>
    [Fact]
    public void BuiltInProfilesAreNeverStored()
    {
        var config = new ScreenDecorations();

        config.Overlays.Profiles.Should().BeEmpty();
        config.Overlays.AllProfiles().Should().NotBeEmpty();

        RoundTrip(config).Overlays.Profiles.Should().BeEmpty();
    }

    /// <summary>
    /// Profiles are meant to be hand-editable, which they are not if a colour comes out as five
    /// redundant members or a noise channel comes out as an integer index. The technique
    /// discriminator has to be legible for the same reason.
    /// </summary>
    [Fact]
    public void JsonIsHandEditable()
    {
        var tint = new TintEffect { Tint = new Color(150, 16, 20) };
        tint.Noise = tint.Noise with { BaseChannel = NoiseChannel.Blue };

        string json = Serialize(
            WithProfile(
                new EffectProfile
                {
                    Name = "x",
                    Layers = [new ProfileLayer { Effect = tint, Blend = OverlayBlend.Additive }]
                }
            )
        );

        json.Should().Contain("\"#961014\"");
        json.Should().Contain("\"Blue\"");
        json.Should().Contain("\"Additive\"");
        json.Should().Contain("\"tint\"");

        // Public fields on the nested parameter structs, which need IncludeFields to appear at all.
        json.Should().Contain("flat_floor");
        json.Should().Contain("corner_bias");
    }

    [Fact]
    public void TranslucentTintKeepsItsAlpha()
    {
        var profile = new EffectProfile
        {
            Name = "x",
            Layers = [new ProfileLayer { Effect = new TintEffect { Tint = new Color(1, 2, 3, 128) } }]
        };

        EffectProfile loaded = RoundTrip(WithProfile(profile)).Overlays.Profiles[0];

        ((TintEffect)loaded.Layers[0].Effect).Tint.Should().Be(new Color(1, 2, 3, 128));
    }

    /// <summary>
    /// Profiles are untrusted input. Clamping as they bake is what keeps the photosensitivity
    /// ceiling and the draw-call cap applying to a hand-written file.
    /// </summary>
    [Fact]
    public void HandEditedProfileIsStillClamped()
    {
        var hostile = new TintEffect
        {
            Strength = 4f,
            Pulse = new PulseSpec { Frequency = 30f }
        };

        hostile.Shape = hostile.Shape with { Feather = 0f };

        var profile = new EffectProfile { Name = "hostile", Layers = [new ProfileLayer { Effect = hostile }] };

        OverlayLayer baked = Bake(profile)[0];

        baked.Params.Appearance.PulseFreq.Should().Be(OverlayParams.MaxPulseFreqHz);
        baked.Params.Appearance.Opacity.Should().Be(1f);
        baked.Params.Shape.Feather.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void OverlongProfileIsTruncatedToMaxLayers()
    {
        var profile = new EffectProfile { Name = "greedy" };

        for (int i = 0; i < OverlayLayerStack.MaxLayers * 2; i++)
            profile.Layers.Add(new ProfileLayer { Effect = new TintEffect() });

        Bake(profile).Should().HaveCount(OverlayLayerStack.MaxLayers);
    }

    /// <summary>
    /// A copy is the only way to customise a shipped look, so it must share nothing with the
    /// original - including the identity every rule points at.
    /// </summary>
    [Fact]
    public void CloneIsIndependent()
    {
        EffectProfile original = BuiltInProfiles.Find(BuiltInProfiles.Ids.Bleed);
        EffectProfile copy = original.Clone("Edited");

        copy.Id.Should().NotBe(original.Id);
        copy.IsBuiltIn.Should().BeFalse();
        copy.Deletable.Should().BeTrue();

        LayerEffect edited = copy.Layers[0].Effect;
        edited.Shape = edited.Shape with { Reach = 0.99f };
        copy.Fade.InSeconds = 9f;

        original.Name.Should().NotBe("Edited");
        original.Layers[0].Effect.Shape.Reach.Should().NotBe(0.99f);
        original.Fade.InSeconds.Should().NotBe(9f);
    }

    [Fact]
    public void BuiltInProfilesAreNotDeletable()
    {
        BuiltInProfiles.Find(BuiltInProfiles.Ids.Bleed).Deletable.Should().BeFalse();
    }

    /// <summary>Names are for people; the id is what identifies a profile. Duplicates are still
    /// worth avoiding, since the library lists by name.</summary>
    [Fact]
    public void AddProfileKeepsNamesUnique()
    {
        var config = new ScreenDecorations();

        config.Overlays.AddProfile(new EffectProfile { Name = "Wet" }).Should().Be("Wet");
        config.Overlays.AddProfile(new EffectProfile { Name = "Wet" }).Should().Be("Wet (2)");
        config.Overlays.AddProfile(new EffectProfile { Name = "Wet" }).Should().Be("Wet (3)");
    }

    /// <summary>The pool is flat and shared: one look serves as many rules as point at it.</summary>
    [Fact]
    public void ProfilesAreFoundByIdAcrossBothPools()
    {
        var config = new ScreenDecorations();
        var authored = new EffectProfile { Name = "Wet" };

        config.Overlays.AddProfile(authored);

        config.Overlays.FindProfile(authored.Id).Should().BeSameAs(authored);
        config.Overlays.FindProfile(BuiltInProfiles.Ids.Poison).Should().NotBeNull();
        config.Overlays.FindProfile(System.Guid.NewGuid()).Should().BeNull();
    }

    /// <summary>Raising the concurrency cap past what the compositor will honour would silently
    /// promise overlays that are dropped anyway.</summary>
    [Fact]
    public void ConcurrencyCapIsHeldWithinItsRange()
    {
        var settings = new OverlaySystemSettings();

        settings.MaxConcurrent.Should().Be(OverlaySystemSettings.DefaultConcurrent);

        settings.MaxConcurrent = 999;
        settings.MaxConcurrent.Should().Be(OverlaySystemSettings.MaxAllowedConcurrent);

        settings.MaxConcurrent = -5;
        settings.MaxConcurrent.Should().Be(OverlaySystemSettings.MinConcurrent);
    }
}
