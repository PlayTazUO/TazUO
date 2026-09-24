using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for paperdoll settings, including equip-on-drop behavior and modern paperdoll appearance</summary>
public class PaperdollTab
{
    /// <summary>Returns the option fragments for equip-on-drop behavior and modern-paperdoll enable/disable and styling</summary>
    internal static IOptionSource GetContent()
    {
        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_tazuo_swapequippedonpaperdolldrop"),
                new Accessor<bool>(() => ProfileManager.GlobalSettings.SwapEquippedOnPaperdollDrop),
                TazLang.Get("mog_tazuo_swapequippedonpaperdolldroptooltip"),
                new SearchMetadata(
                    TazLang.Get("mog_tazuo_swapequippedonpaperdolldrop"),
                    Keywords: [TazLang.Get("mog_kw_paperdoll"), TazLang.Get("mog_kw_equipment"), TazLang.Get("mog_kw_item"), TazLang.Get("mog_kw_drop")]
                )
            ),
            GetModernPaperdollSection()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_buttonpaperdoll"), [TazLang.Get("mog_kw_paperdoll"), TazLang.Get("mog_kw_character"), TazLang.Get("mog_kw_equipment")]));
    }

    private static OptionFragment GetModernPaperdollSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_tazuo_modernpaperdoll"), LabelLink = "https://tazuo.org/wiki/alternate-paperdoll/" },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseModernPaperdoll), TazLang.Get("mog_tazuo_enablemodernpaperdoll")),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_paperdollhue"),
                    new Accessor<ushort>(() => profile.ModernPaperDollHue, newHue =>
                    {
                        profile.ModernPaperDollHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    new SearchMetadata(TazLang.Get("mog_tazuo_paperdollhue"), Keywords: [TazLang.Get("mog_kw_hue"), TazLang.Get("mog_kw_color")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_durabilitybarhue"),
                    new Accessor<ushort>(() => profile.ModernPaperDollDurabilityHue, newHue =>
                    {
                        profile.ModernPaperDollDurabilityHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    new SearchMetadata(TazLang.Get("mog_tazuo_durabilitybarhue"), Keywords: [TazLang.Get("mog_kw_durability"), TazLang.Get("mog_kw_bar"), TazLang.Get("mog_kw_hue"), TazLang.Get("mog_kw_color")])
                ),
                Option.Slider(
                    TazLang.Get("mog_tazuo_showdurabilitybarbelow"),
                    1,
                    100,
                    new Accessor<float>(() => profile.ModernPaperDoll_DurabilityPercent, f => profile.ModernPaperDoll_DurabilityPercent = (int)f),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_showdurabilitybarbelow"), Keywords: [TazLang.Get("mog_kw_durability"), TazLang.Get("mog_kw_bar"), TazLang.Get("mog_kw_below")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_tazuo_paperdollanchor"),
                    new Accessor<bool>(() => profile.ModernPaperdollAnchorEnabled, newValue =>
                    {
                        profile.ModernPaperdollAnchorEnabled = newValue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_paperdollanchor"), Keywords: [TazLang.Get("mog_kw_anchor")])
                )
            ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_paperdoll")], Keywords: [TazLang.Get("mog_kw_enable")]))
        );
    }
}
