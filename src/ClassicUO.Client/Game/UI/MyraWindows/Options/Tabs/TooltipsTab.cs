using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for tooltip appearance and override settings</summary>
public static class TooltipsTab
{
    /// <summary>Returns the option group for tooltip enable/disable, delay, background, font, and override rules</summary>
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ToolTips tooltipLang = lang.GetToolTips;
        ModernOptionsGumpLanguage.TazUO tuoMiscLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.UseTooltip), tooltipLang.EnableToolTips),
            Option.Slider(
                tooltipLang.ToolTipDelay,
                0,
                1000,
                new Accessor<float>(() => profile.TooltipDelayBeforeDisplay, f => profile.TooltipDelayBeforeDisplay = (int)f),
                search: new SearchMetadata(tooltipLang.ToolTipDelay, Keywords: [kw.Delay])
            ),
            Option.Slider(
                tooltipLang.ToolTipBG,
                0,
                100,
                new Accessor<float>(() => profile.TooltipBackgroundOpacity, f => profile.TooltipBackgroundOpacity = (int)f),
                search: new SearchMetadata(tooltipLang.ToolTipBG, Keywords: [kw.Background, kw.Opacity])
            ),
            Option.HuePicker(
                tooltipLang.ToolTipFont,
                new Accessor<ushort>(() => profile.TooltipTextHue),
                search: new SearchMetadata(tooltipLang.ToolTipFont, Keywords: [kw.Font, kw.Color])
            ),
            Option.HuePicker(
                tuoMiscLang.BackgroundHue,
                new Accessor<ushort>(() => profile.ToolTipBGHue),
                search: new SearchMetadata(tuoMiscLang.BackgroundHue, Keywords: [kw.Background, kw.Color])
            ),
            Option.Checkbox(
                tuoMiscLang.AlignTooltipsToTheLeftSide,
                new Accessor<bool>(() => profile.LeftAlignToolTips),
                search: new SearchMetadata(tuoMiscLang.AlignTooltipsToTheLeftSide, Keywords: [kw.Align, kw.Left])
            ),
            Option.Checkbox(
                tuoMiscLang.AlignMobileTooltipsToCenter,
                new Accessor<bool>(() => profile.ForceCenterAlignTooltipMobiles),
                search: new SearchMetadata(tuoMiscLang.AlignMobileTooltipsToCenter, Keywords: [kw.Align, kw.Mobile, kw.Center])
            ),
            Option.Checkbox(
                tuoMiscLang.ForcedTooltips,
                new Accessor<bool>(() => profile.ForceTooltipsOnOldClients),
                search: new SearchMetadata(tuoMiscLang.ForcedTooltips, Keywords: [kw.Force])
            ),
            Option.InputField(
                tuoMiscLang.HeaderFormatItemName,
                new Accessor<string>(() => profile.TooltipHeaderFormat),
                search: new SearchMetadata(tuoMiscLang.HeaderFormatItemName, Keywords: [kw.Format, kw.Name])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps
                {
                    LabelText = tooltipLang.LabelTooltipOverrides,
                    LabelLink = "https://tazuo.org/wiki/tooltip-override/"
                },
                Option.Button(
                    tooltipLang.LabelOpenOverridesConfig,
                    () => UIManager.Add(new TooltipConfigGump()),
                    search: new SearchMetadata(tooltipLang.LabelOpenOverridesConfig, Keywords: [kw.Override, kw.Config])
                )
            )
        ).WithSearch(new SearchMetadata(lang.LabelTooltips, Tags: [kw.Tooltip, kw.Hover]));
    }
}
