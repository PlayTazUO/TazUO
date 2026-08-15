#nullable enable

using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Theme;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
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

    /// <summary>High voltage sign, U+26A1. Present in Noto Sans Symbols 2, absent from the body font.</summary>
    private const string PSE_WARNING_GLYPH = "⚡";

    private const int PSE_BANNER_GLYPH_SIZE = 34;

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
        return OptionsUi.Vertical(
            Option.Custom(BuildPseWarningBanner),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => settings.Enabled), master) { Gate = PseWarningGate },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => settings.Overlays.Enabled), overlays),
                    IntensitySlider(
                        TazLang.Get("visualeffects_overlayintensity", "Master overlay intensity"),
                        new Accessor<float>(() => settings.Overlays.Intensity),
                        OverlayKeyword,
                        TazLang.Get(
                            "visualeffects_overlayintensity_tooltip",
                            "Scales every effect on top of whatever its profile already says,\n"
                            + "like a master volume. 1.00 draws each look exactly as authored;\n"
                            + "lower can only weaken it."
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
                            "Scales every shake on top of whatever its profile already says.\n"
                            + "1.00 hits exactly as authored; lower can only soften it."
                        )
                    )
                ).WithSearch(new SearchMetadata(shake, Keywords: [ShakeKeyword, EffectsKeyword]))
            ).WithSearch(new SearchMetadata(master, Keywords: [OverlayKeyword, ShakeKeyword, EffectsKeyword]))
        );
    }

    /// <summary>
    /// Short standing reminder that these effects flash, shake and tint the whole screen. Shown
    /// regardless of the master switch's state, unlike <see cref="ConfirmEnableScreenDecorations"/>'s
    /// one-time dialog, which only fires the first time the switch is turned on.
    /// </summary>
    /// <returns>The banner widget.</returns>
    private static Widget BuildPseWarningBanner()
    {
        MyraPalette palette = MyraTheme.Current;

        var banner = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            Padding = new Thickness(8, 6),
            Background = new SolidBrush(palette.PanelFill),
            Border = new SolidBrush(palette.Notice * palette.NoticeBorderAlpha),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        banner.Widgets.Add(MyraLabel.Symbol(PSE_WARNING_GLYPH, PSE_BANNER_GLYPH_SIZE, palette.Notice));

        banner.Widgets.Add(
            new MyraLabel(
                TazLang.Get(
                    "visualeffects_pse_banner",
                    "PHOTOSENSITIVE SEIZURE WARNING\n"
                    + "The following visual effects may contain flashing lights or rapid color changes.\n"
                    + "Viewer discretion is advised."
                ),
                MyraLabel.TextStyle.P
            )
            {
                TextColor = palette.Notice,
                VerticalAlignment = VerticalAlignment.Center
            }
        );

        return banner;
    }

    /// <summary>
    /// Gate for the master switch: lets a disable through unconditionally, but an enable only once
    /// the photosensitivity warning has been acknowledged, prompting for it first if not.
    /// </summary>
    /// <param name="newRequestedValue">The value the user tried to set the switch to.</param>
    /// <param name="commit">Callback that actually applies the value once the gate is satisfied.</param>
    private static void PseWarningGate(bool newRequestedValue, Action<bool> commit)
    {
        if (!newRequestedValue)
        {
            commit(false);
            return;
        }

        Profile profile = ProfileManager.CurrentProfile;

        if (profile.ScreenDecorationsPseAcknowledged)
        {
            commit(true);
            return;
        }

        MainThreadQueue.InvokeOnMainThread(() => UIManager.Add(new ConfirmationModal(
                TazLang.Get("visualeffects_pse_warningtitle", "PHOTOSENSITIVE SEIZURE WARNING"),
                TazLang.Get(
                    "visualeffects_pse_warning",
                    "Some visual effects may flash, shake, tint, distort, or otherwise affect large portions of the screen.\n" +
                    "If you have an epileptic condition or have had seizures of any kind,\n" +
                    "consult your physician before enabling this feature.\n" +
                    "Do you with to enable visual effects?"
                ),
                confirmed =>
                {
                    if (confirmed)
                        profile.ScreenDecorationsPseAcknowledged = true;

                    commit(confirmed);
                }
            ))
        );
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
                "How many effects may be drawn together before the least important\n"
                + "is dropped. Raising this costs a draw call per layer per frame,\n"
                + "and more than a few tinted fields at once is hard to see through."
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
