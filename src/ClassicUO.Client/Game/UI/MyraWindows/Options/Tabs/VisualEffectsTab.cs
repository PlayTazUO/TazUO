#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Renderer.Effects;
using Myra.Graphics2D.UI;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for the full-screen overlay effects and their profiles</summary>
public static class VisualEffectsTab
{
    /// <summary>
    ///     Intensities are 0-1: without this the slider rounds to whole numbers and offers only
    ///     its two ends.
    /// </summary>
    private const int INTENSITY_DECIMAL_PLACES = 2;

    private static string OverlayKeyword => TazLang.Get("visualeffects_kw_overlay", "overlay");
    private static string ShakeKeyword => TazLang.Get("visualeffects_kw_shake", "shake");
    private static string IntensityKeyword => TazLang.Get("visualeffects_kw_intensity", "intensity");
    private static string EffectsKeyword => TazLang.Get("visualeffects_kw_effects", "effects");

    /// <summary>Returns the tab group containing one sub-tab per configurable effect</summary>
    internal static IOptionSource GetContent()
    {
        var group = new OptionTabGroup();

        group.AddTab(TazLang.Get("visualeffects_general", "General"), GetGeneralSubTabContent);

        foreach (OverlayEffect effect in OverlaySystemSettings.AllEffects)
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
            OverlayEffect.Concussion => TazLang.Get("visualeffects_concussion", "Concussion"),
            _ => effect.ToString()
        };

    /// <summary>
    ///     The two systems' own switches. Kept apart from the per-effect tabs because they gate work
    ///     rather than describe an effect: with these off nothing is scheduled, drawn or shaken for.
    /// </summary>
    private static IOptionSource GetGeneralSubTabContent()
    {
        DecorationSettings settings = DecorationSettings.Current;

        string master = TazLang.Get("visualeffects_masterenabled", "Enable screen decorations");
        string overlays = TazLang.Get("visualeffects_overlaysenabled", "Enable screen overlays");
        string shake = TazLang.Get("visualeffects_shakeenabled", "Enable screen shake");

        // Nested groups: the master switch greys out both systems, and each system greys out its own
        // intensity, so what a toggle governs is visible rather than merely documented.
        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => settings.Enabled), master),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => settings.Overlays.Enabled), overlays),
                IntensitySlider(
                    TazLang.Get("visualeffects_overlayintensity", "Overlay intensity"),
                    new Accessor<float>(() => settings.Overlays.Intensity),
                    TazLang.Get("visualeffects_kw_overlay", "overlay")
                )
            ).WithSearch(new SearchMetadata(overlays, Keywords: [OverlayKeyword, EffectsKeyword])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => settings.Shake.Enabled), shake),
                IntensitySlider(
                    TazLang.Get("visualeffects_shakeintensity", "Shake intensity"),
                    new Accessor<float>(() => settings.Shake.Intensity),
                    ShakeKeyword
                ),
                ShakeFullScreenCheckbox(settings)
            ).WithSearch(new SearchMetadata(shake, Keywords: [ShakeKeyword, EffectsKeyword]))
        ).WithSearch(new SearchMetadata(master, Keywords: [OverlayKeyword, ShakeKeyword, EffectsKeyword]));
    }

    /// <summary>
    ///     Whether shake displaces the whole window or only the game world. The per-effect tabs carry
    ///     the same choice for overlays; shake has one switch because it is one effect.
    /// </summary>
    /// <param name="settings">The decoration settings to bind against.</param>
    /// <returns>The checkbox entry.</returns>
    private static OptionEntry ShakeFullScreenCheckbox(DecorationSettings settings)
    {
        string label = TazLang.Get("visualeffects_shakefullscreen", "Shake the whole window");

        return Option.Checkbox(
            label,
            new Accessor<bool>(() => settings.Shake.FullScreen),
            search: new SearchMetadata(label, Keywords: [ShakeKeyword, EffectsKeyword])
        );
    }

    /// <summary>
    ///     A 0-1 intensity slider. Whole-number rounding is the slider default, which would leave this
    ///     range with nothing between off and full.
    /// </summary>
    /// <param name="label">The slider label.</param>
    /// <param name="setting">The intensity to bind to.</param>
    /// <param name="keyword">Extra search keyword naming the system it belongs to.</param>
    /// <returns>The slider entry.</returns>
    private static OptionEntry IntensitySlider(string label, Accessor<float> setting, string keyword) =>
        Option.Slider(
            label,
            0f,
            1f,
            setting,
            search: new SearchMetadata(label, Keywords: [keyword, IntensityKeyword, EffectsKeyword]),
            decimalPlaces: INTENSITY_DECIMAL_PLACES
        );

    private static IOptionSource GetEffectSubTabContent(OverlayEffect effect)
    {
        OverlayEffectGeneralSettings settings = DecorationSettings.Current.Overlays.GetSettings(effect);

        // Switches first, then the profile editor under a rule: the editor is a panel of its own with
        // its own toolbar, and without a break the two read as one undifferentiated column.
        OptionFragment panel = OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("visualeffects_enabled", "Enable this effect"),
                new Accessor<bool>(() => settings.Enabled)
            ),
            Option.Checkbox(
                TazLang.Get("visualeffects_fullscreen", "Draw over the whole window"),
                new Accessor<bool>(() => settings.FullScreen)
            ),
            PreviewCheckbox(effect),
            Option.Custom(OptionTabCommons.StyledHorizontalSeparator),
            Option.Custom(() => BuildProfileEditor(effect, settings))
        );

        // The profile editor doesn't fit the search results page, and the switches above it aren't
        // worth splitting the fragment for.
        panel.InheritsSearch = false;

        return panel;
    }

    /// <summary>
    ///     Shows the effect on demand, so it can be tuned without waiting to be poisoned. Ignores both
    ///     the effect's own enabled switch and the player's state - the usual reason to preview one is
    ///     to decide whether to turn it on at all.
    /// </summary>
    /// <param name="effect">The effect this tab configures.</param>
    /// <returns>The checkbox entry.</returns>
    private static OptionEntry PreviewCheckbox(OverlayEffect effect)
    {
        // Read at build time rather than bound: a preview can also be ended from elsewhere - closing
        // the options, or leaving the world - and the tab is rebuilt on every visit anyway.
        bool previewing = ScreenOverlayManager.Instance.IsPreviewing(effect);

        return Option.Checkbox(
            TazLang.Get("visualeffects_preview", "Preview this effect"),
            previewing,
            on => ScreenOverlayManager.Instance.SetPreview(effect, on),
            TazLang.Get(
                "visualeffects_preview_tooltip",
                "Shows this effect regardless of your character's state. One at a time, and still "
                + "subject to the switches on the General tab. Ends when the options are closed."
            )
        );
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

        void Save()
        {
            DecorationSettings.Current.Save();
        }
    }

    /// <summary>
    ///     Selecting a profile is what assigns it to the effect; a built-in selection clears the
    ///     assignment so the code preset is used.
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
    ///     Bakes the code preset for <paramref name="effect" /> into a profile so it can be inspected and
    ///     copied from the editor. Null for effects that have no preset yet.
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
        OverlayEffectProfile created = builtIn?.Clone() ?? new OverlayEffectProfile { Layers = [new OverlayLayer { Params = OverlayParams.Default }] };

        created.Name = name;
        created.BasePreset = builtIn?.BasePreset;

        settings.AddProfile(created);
        settings.EffectiveProfile = created.Name;
        DecorationSettings.Current.Save();

        return created;
    }

    private static void DeleteProfile(OverlayEffectGeneralSettings settings, OverlayEffectProfile profile)
    {
        settings.Profiles.Remove(profile);

        // Falls back to the built-in preset rather than to nothing.
        if (settings.EffectiveProfile == profile.Name)
            settings.EffectiveProfile = null;

        DecorationSettings.Current.Save();
    }
}
