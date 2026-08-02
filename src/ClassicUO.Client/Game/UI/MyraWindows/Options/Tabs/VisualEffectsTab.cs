#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;
using ClassicUO.Game.ScreenOverlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Overlays;
using ClassicUO.Renderer.Effects;
using Myra.Graphics2D.UI;
using ScreenOverlaysConfig = ClassicUO.Configuration.FeatureConfigs.ScreenOverlays.ScreenOverlays;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for the full-screen overlay effects and their profiles</summary>
public static class VisualEffectsTab
{
    /// <summary>Returns the tab group containing one sub-tab per configurable effect</summary>
    internal static IOptionSource GetContent()
    {
        var group = new OptionTabGroup();

        foreach (OverlayEffect effect in ScreenOverlaysConfig.AllEffects)
        {
            OverlayEffect captured = effect;

            // Empty metadata to keep the profile editors out of search results, as the nameplate
            // profiles tab does - they don't render meaningfully there.
            group.AddTab(EffectLabel(captured), () => GetEffectSubTabContent(captured), new SearchMetadata());
        }

        return group;
    }

    private static string EffectLabel(OverlayEffect effect) =>
        effect switch
        {
            OverlayEffect.Bleed => TazLang.Get("visualeffects_bleed", "Bleed"),
            OverlayEffect.Poison => TazLang.Get("visualeffects_poison", "Poison"),
            OverlayEffect.MortalStrike => TazLang.Get("visualeffects_mortalstrike", "Mortal Strike"),
            OverlayEffect.Fog => TazLang.Get("visualeffects_fog", "Fog"),
            OverlayEffect.Drunk => TazLang.Get("visualeffects_drunk", "Drunk"),
            _ => effect.ToString()
        };

    private static IOptionSource GetEffectSubTabContent(OverlayEffect effect)
    {
        OverlayEffectGeneralSettings settings = ScreenOverlaysConfig.Current.GetSettings(effect);

        OptionFragment panel = OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("visualeffects_enabled", "Enable this effect"),
                new Accessor<bool>(() => settings.Enabled, b =>
                {
                    settings.Enabled = b;
                    ScreenOverlaysConfig.Current.Save();
                })
            ),
            Option.Checkbox(
                TazLang.Get("visualeffects_fullscreen", "Draw over the whole window"),
                new Accessor<bool>(() => settings.FullScreen, b =>
                {
                    settings.FullScreen = b;
                    ScreenOverlaysConfig.Current.Save();
                })
            ),
            Option.Custom(() => BuildProfileEditor(effect, settings))
        );

        // The profile editor doesn't fit the search results page, and the two checkboxes above it
        // aren't worth splitting the fragment for.
        panel.InheritsSearch = false;

        return panel;
    }

    private static Widget BuildProfileEditor(OverlayEffect effect, OverlayEffectGeneralSettings settings)
    {
        OverlayEffectProfile? builtIn = BuildBuiltInProfile(effect);

        List<OverlayEffectProfile> initial = builtIn == null
            ? [.. settings.Profiles]
            : [builtIn, .. settings.Profiles];

        // The editor always opens on the first entry and has no way to be told otherwise, so the
        // active profile is moved to the front - otherwise opening the options would silently
        // reassign the effect to whatever happened to be listed first.
        OverlayEffectProfile? active = settings.ResolveProfile();

        if (active != null && initial.Remove(active))
            initial.Insert(0, active);

        return new ProfileEditor<OverlayEffectProfile>(
            profile => BuildProfileUi(settings, profile, Save),
            name => CreateProfile(settings, builtIn, name),
            profile => DeleteProfile(settings, profile),
            initial,
            profile =>
            {
                // Rename only ever applies to the selected profile, and selecting a profile is what
                // activates it, so the effect is still pointing at this one.
                settings.EffectiveProfile = profile.Name;
                Save();
            }
        );

        void Save() => ScreenOverlaysConfig.Current.Save();
    }

    /// <summary>
    /// Selecting a profile is what assigns it to the effect; a built-in selection clears the
    /// assignment so the code preset is used.
    /// </summary>
    private static Widget BuildProfileUi(OverlayEffectGeneralSettings settings, OverlayEffectProfile profile, Action save)
    {
        string? assigned = profile.IsBuiltIn ? null : profile.Name;

        if (settings.EffectiveProfile != assigned)
        {
            settings.EffectiveProfile = assigned;
            save();
        }

        return new OverlayProfileEditor(profile, save);
    }

    /// <summary>
    /// Bakes the code preset for <paramref name="effect"/> into a profile so it can be inspected and
    /// copied from the editor. Null for effects that have no preset yet.
    /// </summary>
    private static OverlayEffectProfile? BuildBuiltInProfile(OverlayEffect effect)
    {
        // Creates a new instance of the hardcoded preset. Note this is an ephemeral object that is not actually saved anywhere.
        ScreenOverlayPreset preset = BuiltInOverlayPresets.Create(effect);

        if (preset == null)
            return null;

        OverlayEffectProfile profile = preset.ToProfile($"{EffectLabel(effect)} {TazLang.Get("visualeffects_builtinsuffix", "(built-in)")}");
        profile.IsBuiltIn = true;

        return profile;
    }

    private static OverlayEffectProfile CreateProfile(
        OverlayEffectGeneralSettings settings,
        OverlayEffectProfile? builtIn,
        string name
    )
    {
        OverlayEffectProfile created = builtIn?.Clone() ?? new OverlayEffectProfile
        {
            Layers = [new OverlayLayer { Params = OverlayParams.Default }]
        };

        created.Name = name;
        created.BasePreset = builtIn?.BasePreset;

        settings.AddProfile(created);
        settings.EffectiveProfile = created.Name;
        ScreenOverlaysConfig.Current.Save();

        return created;
    }

    private static void DeleteProfile(OverlayEffectGeneralSettings settings, OverlayEffectProfile profile)
    {
        settings.Profiles.Remove(profile);

        // Falls back to the built-in preset rather than to nothing.
        if (settings.EffectiveProfile == profile.Name)
            settings.EffectiveProfile = null;

        ScreenOverlaysConfig.Current.Save();
    }
}
