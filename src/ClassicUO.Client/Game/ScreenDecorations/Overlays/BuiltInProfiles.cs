#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>
/// The looks shipped with the client, as profiles.
/// <para>
/// Resolved from code every session rather than seeded into config: a shipped look retuned in a
/// later release should reach everyone who is using it, and a built-in the user cannot accidentally
/// delete is the durability the rules pointing at it rely on. Copying one produces an ordinary
/// user profile, which is how a shipped look gets customised.
/// </para>
/// <para>
/// The tuned <see cref="ScreenOverlayPreset" />s remain the authoring source. Their baked output is
/// recovered into narrow layer effects rather than retyped by hand, so the profiles here are exactly
/// the compositions those presets describe, and stay that way as the presets are tuned.
/// </para>
/// </summary>
public static class BuiltInProfiles
{
    #region Public accessors

    /// <summary>
    /// Stable identities, persisted by every rule that points at one. Never reuse or renumber these.
    /// </summary>
    public static class Ids
    {
        public static readonly Guid Bleed = new("0e778268-d89d-4538-80c6-621ca768a4b2");
        public static readonly Guid Poison = new("1c419034-7c0f-4e65-a3af-81f27a1e0efa");
        public static readonly Guid Fog = new("7195989d-5c02-47b8-bcd9-b91aaaafaa99");
        public static readonly Guid Drunk = new("271f1b8f-13b1-4a4a-acc4-a9784852398b");
        public static readonly Guid Concussion = new("b7b6a46c-9899-4d6f-8493-cd22d695c0fb");
        public static readonly Guid TunnelVision = new("b006e04f-3087-4ac7-bdbb-0c903d35792d");
        public static readonly Guid Death = new("bd6cf532-4dc4-46d7-94eb-bc061ef041da");
        public static readonly Guid EarthquakeRumble = new("7f67a87a-e30a-441b-9b5a-3a91da98d050");
    }

    /// <summary>Every shipped profile, in the order the library should list them.</summary>
    public static IReadOnlyList<EffectProfile> All => _all ??= Build();

    #endregion

    #region Private members

    /// <summary>
    /// Trauma the poison look asks for when it arrives. Onset shake belongs to the look rather than
    /// to the reason for it - a faint nudge, since poison is a state to notice rather than a hit.
    /// </summary>
    private const float POISON_TRAUMA = 0.1f;

    /// <summary>Trauma the struck-head look hits with on arrival.</summary>
    private const float CONCUSSION_TRAUMA = 0.65f;

    /// <summary>Longer than the default onset hit - a blow to the head rings for a moment.</summary>
    private const float CONCUSSION_SHAKE_SECONDS = 0.8f;

    /// <summary>Hardest a quake underfoot hits. Occurrence intensity scales it down with distance.</summary>
    private const float EARTHQUAKE_TRAUMA = 1f;

    /// <summary>Outlives the sound that raises it a little, so the ground settles rather than
    /// stopping with it.</summary>
    private const float EARTHQUAKE_SECONDS = 4f;

    /// <summary>Ground does not reach full violence instantly; it builds over about a second.</summary>
    private const float EARTHQUAKE_RAMP_UP_SECONDS = 0.9f;

    /// <summary>Longer than the build. A quake subsides rather than stopping.</summary>
    private const float EARTHQUAKE_RAMP_DOWN_SECONDS = 1.1f;

    /// <summary>Above the 20 Hz default: a tight tremor. Slower rates read as the screen swaying
    /// rather than as ground breaking up.</summary>
    private const float EARTHQUAKE_FREQUENCY_HZ = 25f;

    private static IReadOnlyList<EffectProfile>? _all;

    private static FrozenDictionary<Guid, EffectProfile>? _byId;

    #endregion

    #region Public methods

    /// <summary>
    /// The shipped profile with this id.
    /// </summary>
    /// <param name="id">The identity to look up.</param>
    /// <returns>The profile, or null if the id is not a shipped one.</returns>
    public static EffectProfile? Find(Guid id)
    {
        _byId ??= All.ToFrozenDictionary(profile => profile.Id);

        return _byId.TryGetValue(id, out EffectProfile? profile) ? profile : null;
    }

    #endregion

    #region Private methods

    private static IReadOnlyList<EffectProfile> Build() =>
    [
        FromPreset(Ids.Poison, BuiltInName(TazLang.Get("visualeffects_poison", "Poison")), new PoisonOverlay(), Shake(POISON_TRAUMA)),
        FromPreset(Ids.Bleed, BuiltInName(TazLang.Get("visualeffects_bleed", "Bleed")), new BleedOverlay()),
        FromPreset(Ids.Fog, BuiltInName(TazLang.Get("visualeffects_fog", "Fog")), new FogOverlay()),
        FromPreset(Ids.Drunk, BuiltInName(TazLang.Get("visualeffects_drunk", "Drunk")), new DrunkOverlay()),
        FromPreset(
            Ids.Concussion,
            BuiltInName(TazLang.Get("visualeffects_concussion", "Concussion")),
            new ConcussionOverlay(),
            // An impact that rings rather than one that only jolts: the ramp down spans the whole
            // duration, so it falls away instead of cutting off and the hit is still felt after the
            // screen has stopped visibly reacting.
            new ShakeSpec
            {
                Trauma = CONCUSSION_TRAUMA,
                DurationSeconds = CONCUSSION_SHAKE_SECONDS,
                RampDownSeconds = CONCUSSION_SHAKE_SECONDS,
                Curve = ShakeCurve.EaseIn
            }
        ),
        FromPreset(Ids.TunnelVision, BuiltInName(TazLang.Get("visualeffects_tunnelvision", "Tunnel vision")), new TunnelVisionOverlay()),
        FromPreset(Ids.Death, BuiltInName(TazLang.Get("visualeffects_death", "Death")), new DeathOverlay()),
        EarthquakeRumble()
    ];

    /// <summary>
    /// Recovers a preset's baked stack into an editable profile.
    /// </summary>
    /// <param name="id">The profile's stable identity.</param>
    /// <param name="name">Its display name.</param>
    /// <param name="preset">The tuned preset supplying the composition.</param>
    /// <param name="shake">Shake the look includes, or null for a purely visual one.</param>
    /// <returns>The profile.</returns>
    private static EffectProfile FromPreset(Guid id, string name, ScreenOverlayPreset preset, ShakeSpec? shake = null)
    {
        var layers = new List<OverlayLayer>();
        preset.BakeClamped(layers);

        var profile = new EffectProfile
        {
            Id = id,
            Name = name,
            IsBuiltIn = true,
            Fade = new FadeSpec { InSeconds = preset.FadeInSeconds, OutSeconds = preset.FadeOutSeconds },
            Shake = shake
        };

        foreach (OverlayLayer layer in layers)
        {
            profile.Layers.Add(
                new ProfileLayer
                {
                    Effect = LayerEffectFactory.FromParams(layer.Params),
                    Blend = layer.Blend
                }
            );
        }

        return profile;
    }

    /// <summary>
    /// Shake with nothing drawn. The one look that has no preset behind it, because there is nothing
    /// to compose: a quake is felt rather than seen, and jamming it into a visual effect is what the
    /// old single-slot model forced.
    /// </summary>
    /// <returns>The profile.</returns>
    private static EffectProfile EarthquakeRumble() =>
        new()
        {
            Id = Ids.EarthquakeRumble,
            Name = BuiltInName(TazLang.Get("visualeffects_earthquakerumble", "Earthquake rumble")),
            IsBuiltIn = true,
            // Builds, holds at strength, then subsides - which is what ground moving feels like. The
            // hold between the two ramps is the point: an envelope that peaks on the first frame and
            // falls from there reads as being hit rather than as the world shifting under you.
            Shake = new ShakeSpec
            {
                Trauma = EARTHQUAKE_TRAUMA,
                DurationSeconds = EARTHQUAKE_SECONDS,
                RampUpSeconds = EARTHQUAKE_RAMP_UP_SECONDS,
                RampDownSeconds = EARTHQUAKE_RAMP_DOWN_SECONDS,
                Curve = ShakeCurve.Smooth,
                Frequency = EARTHQUAKE_FREQUENCY_HZ
            }
        };

    /// <summary>
    /// Marks a shipped look in every list that shows a profile by name. Baked into the name rather
    /// than decorated at each call site: these are resolved from code and never persisted, so the
    /// suffix cannot leak into a user's config, and every list gets it for free.
    /// </summary>
    /// <param name="name">The look's own name.</param>
    /// <returns>The name as the library should show it.</returns>
    private static string BuiltInName(string name) =>
        $"{name} {TazLang.Get("visualeffects_builtinsuffix", "(built-in)")}";

    /// <summary>An onset hit: full strength immediately, falling away. The default envelope.</summary>
    private static ShakeSpec Shake(float trauma) => new() { Trauma = trauma };

    #endregion
}
