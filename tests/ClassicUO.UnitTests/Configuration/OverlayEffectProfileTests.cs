using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration.FeatureConfigs;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Configuration
{
    public class OverlayEffectProfileTests
    {
        private static string Serialize(ScreenDecorations config) =>
            JsonSerializer.Serialize(config, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations);

        private static ScreenDecorations Deserialize(string json) =>
            JsonSerializer.Deserialize(json, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations);

        private static ScreenDecorations RoundTrip(ScreenDecorations config) => Deserialize(Serialize(config));

        private static ScreenDecorations WithProfile(OverlayEffectProfile profile)
        {
            var config = new ScreenDecorations();
            config.Overlays.Bleed.AddProfile(profile);
            return config;
        }

        private static List<OverlayLayer> Bake(ScreenOverlayPreset preset)
        {
            var layers = new List<OverlayLayer>();
            preset.BakeClamped(layers);
            return layers;
        }

        /// <summary>
        /// The whole reason profiles store raw <see cref="OverlayLayer"/> values rather than a
        /// reduced set of knobs: every shader parameter has to survive the trip to disk and back
        /// exactly, or authoring silently loses tuning.
        /// </summary>
        [Fact]
        public void EveryLayerParameterSurvivesARoundTrip()
        {
            OverlayEffectProfile authored = new BleedOverlay().ToProfile("Bleed Copy");

            ScreenDecorations loaded = RoundTrip(WithProfile(authored));

            loaded.Overlays.Bleed.Profiles.Should().ContainSingle();
            loaded.Overlays.Bleed.Profiles[0].Layers.Should().BeEquivalentTo(authored.Layers);
        }

        [Fact]
        public void ProfileMetadataSurvivesARoundTrip()
        {
            OverlayEffectProfile authored = new BleedOverlay().ToProfile("Bleed Copy");
            authored.FadeInSeconds = 1.25f;
            authored.FadeOutSeconds = 2.5f;

            OverlayEffectProfile loaded = RoundTrip(WithProfile(authored)).Overlays.Bleed.Profiles[0];

            loaded.Name.Should().Be("Bleed Copy");
            loaded.Version.Should().Be(OverlayEffectProfile.CurrentVersion);
            loaded.BasePreset.Should().Be(nameof(BleedOverlay));
            loaded.FadeInSeconds.Should().Be(1.25f);
            loaded.FadeOutSeconds.Should().Be(2.5f);
        }

        /// <summary>
        /// Profiles are meant to be hand-editable, which they are not if a colour comes out as five
        /// redundant members or a noise channel comes out as an integer index.
        /// </summary>
        [Fact]
        public void JsonIsHandEditable()
        {
            OverlayParams p = OverlayParams.Default;
            p.Appearance.Tint = new Color(150, 16, 20);
            p.Noise.BaseChannel = NoiseChannel.Blue;

            string json = Serialize
            (
                WithProfile(new OverlayEffectProfile { Name = "x", Layers = [new OverlayLayer { Params = p, Blend = OverlayBlend.Additive }] })
            );

            json.Should().Contain("\"#961014\"");
            json.Should().Contain("\"Blue\"");
            json.Should().Contain("\"Additive\"");

            // Public fields on the nested parameter structs, which need IncludeFields to appear at all.
            json.Should().Contain("flat_floor");
            json.Should().Contain("corner_bias");
        }

        [Fact]
        public void TranslucentTintKeepsItsAlpha()
        {
            OverlayParams p = OverlayParams.Default;
            p.Appearance.Tint = new Color(1, 2, 3, 128);

            ScreenDecorations loaded = RoundTrip(WithProfile(new OverlayEffectProfile { Name = "x", Layers = [new OverlayLayer { Params = p }] }));

            loaded.Overlays.Bleed.Profiles[0].Layers[0].Params.Appearance.Tint.Should().Be(new Color(1, 2, 3, 128));
        }

        /// <summary>
        /// Profiles are untrusted input. Routing them through <see cref="ScreenOverlayPreset"/>
        /// rather than straight at the manager is what keeps the photosensitivity ceiling and the
        /// draw-call cap applying to them.
        /// </summary>
        [Fact]
        public void HandEditedProfileIsStillClamped()
        {
            OverlayParams p = OverlayParams.Default;
            p.Appearance.PulseFreq = 30f;
            p.Appearance.Opacity = 4f;
            p.Shape.Feather = 0f;

            var profile = new OverlayEffectProfile { Name = "hostile", Layers = [new OverlayLayer { Params = p }] };

            OverlayLayer baked = Bake(new CustomOverlayPreset(profile))[0];

            baked.Params.Appearance.PulseFreq.Should().Be(OverlayParams.MaxPulseFreqHz);
            baked.Params.Appearance.Opacity.Should().Be(1f);
            baked.Params.Shape.Feather.Should().BeGreaterThan(0f);
        }

        [Fact]
        public void OverlongProfileIsTruncatedToMaxLayers()
        {
            var profile = new OverlayEffectProfile { Name = "greedy" };

            for (int i = 0; i < ScreenOverlayPreset.MaxLayers * 2; i++)
                profile.Layers.Add(new OverlayLayer { Params = OverlayParams.Default });

            Bake(new CustomOverlayPreset(profile)).Should().HaveCount(ScreenOverlayPreset.MaxLayers);
        }

        [Fact]
        public void IntensityScalesEveryLayerWithoutMutatingTheProfile()
        {
            OverlayEffectProfile profile = new BleedOverlay().ToProfile("Bleed Copy");
            List<OverlayLayer> authored = [.. profile.Layers];

            List<OverlayLayer> baked = Bake(new CustomOverlayPreset(profile) { Intensity = 0.5f });

            for (int i = 0; i < baked.Count; i++)
                baked[i].Params.Appearance.Intensity.Should().BeApproximately(authored[i].Params.Appearance.Intensity * 0.5f, 1e-6f);

            profile.Layers.Should().BeEquivalentTo(authored);
        }

        [Fact]
        public void FadeTimingComesFromTheProfile()
        {
            var profile = new OverlayEffectProfile { Name = "slow", FadeInSeconds = 3f, FadeOutSeconds = 5f };

            var preset = new CustomOverlayPreset(profile);

            preset.FadeInSeconds.Should().Be(3f);
            preset.FadeOutSeconds.Should().Be(5f);
        }

        /// <summary>
        /// A profile is a value snapshot, so editing one can never reach through into the preset it
        /// was seeded from or into a copy taken from it.
        /// </summary>
        [Fact]
        public void CloneIsIndependent()
        {
            OverlayEffectProfile original = new BleedOverlay().ToProfile("Bleed Copy");
            OverlayEffectProfile copy = original.Clone();

            OverlayLayer edited = copy.Layers[0];
            edited.Params.Shape.Reach = 0.99f;
            copy.Layers[0] = edited;
            copy.Name = "Edited";

            original.Name.Should().Be("Bleed Copy");
            original.Layers[0].Params.Shape.Reach.Should().NotBe(0.99f);
        }

        [Fact]
        public void ResolveProfileFallsBackToNullWhenTheProfileIsGone()
        {
            var config = new ScreenDecorations();
            config.Overlays.Bleed.AddProfile(new OverlayEffectProfile { Name = "Wet" });
            config.Overlays.Bleed.EffectiveProfile = "Wet";

            config.Overlays.Bleed.ResolveProfile().Should().NotBeNull();

            config.Overlays.Bleed.Profiles.Clear();
            config.Overlays.Bleed.ResolveProfile().Should().BeNull();

            config.Overlays.Bleed.EffectiveProfile = null;
            config.Overlays.Bleed.ResolveProfile().Should().BeNull();
        }

        [Fact]
        public void AddProfileKeepsNamesUnique()
        {
            var config = new ScreenDecorations();

            config.Overlays.Bleed.AddProfile(new OverlayEffectProfile { Name = "Wet" }).Should().Be("Wet");
            config.Overlays.Bleed.AddProfile(new OverlayEffectProfile { Name = "Wet" }).Should().Be("Wet (2)");
            config.Overlays.Bleed.AddProfile(new OverlayEffectProfile { Name = "Wet" }).Should().Be("Wet (3)");
        }

        /// <summary>
        /// Each effect owns its own pool, so a profile authored for one is not offered for another.
        /// </summary>
        [Fact]
        public void ProfilePoolsAreIndependentPerEffect()
        {
            var config = new ScreenDecorations();
            config.Overlays.Bleed.AddProfile(new OverlayEffectProfile { Name = "Wet" });

            config.Overlays.Poison.Profiles.Should().BeEmpty();
            config.Overlays.Poison.FindProfile("Wet").Should().BeNull();

            ScreenDecorations loaded = RoundTrip(config);

            loaded.Overlays.Bleed.Profiles.Should().ContainSingle();
            loaded.Overlays.Poison.Profiles.Should().BeEmpty();
        }

        [Fact]
        public void GetSettingsReturnsTheEffectsOwnSettings()
        {
            var config = new ScreenDecorations();

            config.Overlays.GetSettings(OverlayEffect.Bleed).Should().BeSameAs(config.Overlays.Bleed);
            config.Overlays.GetSettings(OverlayEffect.Drunk).Should().BeSameAs(config.Overlays.Drunk);

            // Every effect must have a block of its own, so adding one to the enum without giving it
            // a home fails here rather than at runtime. Counting them instead would only fail when
            // the enum grows, which is the case that is already correct.
            OverlaySystemSettings.AllEffects
                                 .Select(config.Overlays.GetSettings)
                                 .Should()
                                 .OnlyHaveUniqueItems()
                                 .And.HaveSameCount(OverlaySystemSettings.AllEffects);
        }

        [Fact]
        public void BuiltInProfilesAreNotDeletable()
        {
            OverlayEffectProfile builtIn = new BleedOverlay().ToProfile("Bleed");
            builtIn.IsBuiltIn = true;

            builtIn.Deletable.Should().BeFalse();
            builtIn.Clone().Deletable.Should().BeTrue();
        }
    }
}
