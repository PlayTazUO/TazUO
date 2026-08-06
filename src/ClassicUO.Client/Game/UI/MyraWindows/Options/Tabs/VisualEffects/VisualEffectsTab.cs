#nullable enable

using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.VisualEffects;

/// <summary>
/// Options tab source for the screen decoration systems: the switches that gate them, the rulebase
/// that decides what runs, and the library of looks the rules point at.
/// </summary>
public static class VisualEffectsTab
{
    #region Private members

    /// <summary>
    /// Intensities are 0-1: without this the slider rounds to whole numbers and offers only its two
    /// ends.
    /// </summary>
    private const int INTENSITY_DECIMAL_PLACES = 2;

    private static string OverlayKeyword => TazLang.Get("visualeffects_kw_overlay", "overlay");
    private static string ShakeKeyword => TazLang.Get("visualeffects_kw_shake", "shake");
    private static string IntensityKeyword => TazLang.Get("visualeffects_kw_intensity", "intensity");
    private static string EffectsKeyword => TazLang.Get("visualeffects_kw_effects", "effects");

    #endregion

    #region Internal methods

    /// <summary>Returns the tab group: the system switches, the rules, and the profile library.</summary>
    /// <returns>The option source.</returns>
    internal static IOptionSource GetContent() =>
        new OptionTabGroup()
            .AddTab(TazLang.Get("visualeffects_general", "General"), GetGeneralSubTabContent)
            .AddTab(TazLang.Get("visualeffects_rules", "Rules"), OverlayRulesTab.GetContent, new SearchMetadata())
            .AddTab(TazLang.Get("visualeffects_profiles", "Profiles"), OverlayProfilesTab.GetContent, new SearchMetadata());

    #endregion

    #region Private methods

    /// <summary>
    /// The two systems' own switches, and the settings that apply across every effect. Kept apart
    /// from the rules and the looks because they gate work rather than describe an effect: with
    /// these off nothing is scheduled, drawn or shaken for.
    /// </summary>
    /// <returns>The option source.</returns>
    private static IOptionSource GetGeneralSubTabContent()
    {
        DecorationSettings settings = DecorationSettings.Current;

        string master = TazLang.Get("visualeffects_masterenabled", "Enable screen decorations");
        string overlays = TazLang.Get("visualeffects_overlaysenabled", "Enable screen overlays");
        string shake = TazLang.Get("visualeffects_shakeenabled", "Enable screen shake");

        // Nested groups: the master switch greys out both systems, and each system greys out its own
        // settings, so what a toggle governs is visible rather than merely documented.
        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => settings.Enabled), master),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => settings.Overlays.Enabled), overlays),
                IntensitySlider(
                    TazLang.Get("visualeffects_overlayintensity", "Master overlay intensity"),
                    new Accessor<float>(() => settings.Overlays.Intensity),
                    OverlayKeyword,
                    TazLang.Get(
                        "visualeffects_overlayintensity_tooltip",
                        "Scales every effect on top of whatever its profile\n"
                        + "already says, like a master volume. 1.00 draws each\n"
                        + "look exactly as authored; lower can only weaken it."
                    )
                ),
                MaxConcurrentInput(settings)
            ).WithSearch(new SearchMetadata(overlays, Keywords: [OverlayKeyword, EffectsKeyword])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => settings.Shake.Enabled), shake),
                IntensitySlider(
                    TazLang.Get("visualeffects_shakeintensity", "Master shake intensity"),
                    new Accessor<float>(() => settings.Shake.Intensity),
                    ShakeKeyword,
                    TazLang.Get(
                        "visualeffects_shakeintensity_tooltip",
                        "Scales every shake on top of whatever its profile\n"
                        + "already says. 1.00 hits exactly as authored; lower\n"
                        + "can only soften it."
                    )
                )
            ).WithSearch(new SearchMetadata(shake, Keywords: [ShakeKeyword, EffectsKeyword]))
        ).WithSearch(new SearchMetadata(master, Keywords: [OverlayKeyword, ShakeKeyword, EffectsKeyword]));
    }

    /// <summary>
    /// How many overlays may composite at once. A number rather than a slider: the useful range is
    /// only a handful of values, and each one is a decision about legibility rather than a dial to
    /// sweep.
    /// </summary>
    /// <param name="settings">The decoration settings to bind against.</param>
    /// <returns>The input entry.</returns>
    private static OptionEntry MaxConcurrentInput(DecorationSettings settings)
    {
        string label = TazLang.Get("visualeffects_maxconcurrent", "Maximum concurrent overlays");

        return Option.IntegerInput(
            label,
            new Accessor<int>(() => settings.Overlays.MaxConcurrent),
            OverlaySystemSettings.MinConcurrent,
            OverlaySystemSettings.MaxAllowedConcurrent,
            TazLang.Get(
                "visualeffects_maxconcurrent_tooltip",
                "How many effects may be drawn together before the least\n"
                + "important is dropped. Raising this costs a draw call per\n"
                + "layer per frame, and more than a few tinted fields at once\n"
                + "is hard to see through."
            ),
            new SearchMetadata(label, Keywords: [OverlayKeyword, EffectsKeyword])
        );
    }

    /// <summary>
    /// A 0-1 intensity slider. Whole-number rounding is the slider default, which would leave this
    /// range with nothing between off and full.
    /// </summary>
    /// <param name="label">The slider label.</param>
    /// <param name="setting">The intensity to bind to.</param>
    /// <param name="keyword">Extra search keyword naming the system it belongs to.</param>
    /// <param name="tooltip">Explains what the multiplier does to the profiles under it.</param>
    /// <returns>The slider entry.</returns>
    private static OptionEntry IntensitySlider(string label, Accessor<float> setting, string keyword, string tooltip) =>
        Option.Custom(
            () =>
            {
                OptionItem slider = OptionsFactory.PropBoundSliderOption(
                    label,
                    setting,
                    0f,
                    1f,
                    decimalPlaces: INTENSITY_DECIMAL_PLACES
                );

                slider.Tooltip = tooltip;

                return slider;
            },
            new SearchMetadata(label, Keywords: [keyword, IntensityKeyword, EffectsKeyword])
        );

    #endregion
}
