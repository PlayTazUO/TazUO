using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for tooltip appearance and override settings</summary>
public static class TooltipsTab
{
    /// <summary>Returns the option group for tooltip enable/disable, delay, background, font, and override rules</summary>
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.UseTooltip), TazLang.Get("mog_tooltips_enabletooltips")),
            Option.Slider(
                TazLang.Get("mog_tooltips_tooltipdelay"),
                0,
                1000,
                new Accessor<float>(() => profile.TooltipDelayBeforeDisplay, f => profile.TooltipDelayBeforeDisplay = (int)f),
                search: new SearchMetadata(TazLang.Get("mog_tooltips_tooltipdelay"), Keywords: [TazLang.Get("mog_kw_delay")])
            ),
            Option.Slider(
                TazLang.Get("mog_tooltips_tooltipbg"),
                0,
                100,
                new Accessor<float>(() => profile.TooltipBackgroundOpacity, f => profile.TooltipBackgroundOpacity = (int)f),
                search: new SearchMetadata(TazLang.Get("mog_tooltips_tooltipbg"), Keywords: [TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_opacity")])
            ),
            Option.HuePicker(
                TazLang.Get("mog_tooltips_tooltipfont"),
                new Accessor<ushort>(() => profile.TooltipTextHue),
                search: new SearchMetadata(TazLang.Get("mog_tooltips_tooltipfont"), Keywords: [TazLang.Get("mog_kw_font"), TazLang.Get("mog_kw_color")])
            ),
            Option.HuePicker(
                TazLang.Get("mog_tazuo_backgroundhue"),
                new Accessor<ushort>(() => profile.ToolTipBGHue),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_backgroundhue"), Keywords: [TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_color")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_aligntooltipstotheleftside"),
                new Accessor<bool>(() => profile.LeftAlignToolTips),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_aligntooltipstotheleftside"), Keywords: [TazLang.Get("mog_kw_align"), TazLang.Get("mog_kw_left")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_alignmobiletooltipstocenter"),
                new Accessor<bool>(() => profile.ForceCenterAlignTooltipMobiles),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_alignmobiletooltipstocenter"), Keywords: [TazLang.Get("mog_kw_align"), TazLang.Get("mog_kw_mobile"), TazLang.Get("mog_kw_center")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_forcedtooltips"),
                new Accessor<bool>(() => profile.ForceTooltipsOnOldClients),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_forcedtooltips"), Keywords: [TazLang.Get("mog_kw_force")])
            ),
            Option.InputField(
                TazLang.Get("mog_tazuo_headerformatitemname"),
                new Accessor<string>(() => profile.TooltipHeaderFormat),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_headerformatitemname"), Keywords: [TazLang.Get("mog_kw_format"), TazLang.Get("mog_kw_name")])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps
                {
                    LabelText = TazLang.Get("mog_tooltips_labeltooltipoverrides"),
                    LabelLink = "https://tazuo.org/wiki/tooltip-override/"
                },
                Option.Checkbox(
                    TazLang.Get("mog_tazuo_ignoretooltipoverridesformobiles"),
                    new Accessor<bool>(() => profile.ToolTipOverride_IgnoreMobiles),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_ignoretooltipoverridesformobiles"), Keywords: [TazLang.Get("mog_kw_override"), TazLang.Get("mog_kw_mobile")])
                ),
                Option.Button(
                    TazLang.Get("mog_tooltips_labelopenoverridesconfig"),
                    () => TooltipOverrideWindow.Show(World.Instance),
                    search: new SearchMetadata(TazLang.Get("mog_tooltips_labelopenoverridesconfig"), Keywords: [TazLang.Get("mog_kw_override"), TazLang.Get("mog_kw_config")])
                )
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_labeltooltips"), Tags: [TazLang.Get("mog_kw_tooltip"), TazLang.Get("mog_kw_hover")]));
    }
}
