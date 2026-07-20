using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>
/// Top-level interface options tab that groups containers, nameplates, tooltips, info bars,
/// cooldown bars, gumps, and counter sub-tabs.
/// </summary>
public static class InterfaceTab
{
    /// <summary>Returns the tab group containing all interface sub-tabs</summary>
    internal static IOptionSource GetContent()
    {

        return new OptionTabGroup(search: new SearchMetadata(Tags: [TazLang.Get("mog_kw_interface")]))
            .AddTab(
                TazLang.Get("mog_buttoncontainers"),
                ContainersTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttoncontainers"))
            )
            .AddTab(
                TazLang.Get("mog_buttonnameplates"),
                NameplatesTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttonnameplates"), [TazLang.Get("mog_kw_nameplate"), TazLang.Get("mog_kw_name")])
            )
            .AddTab(
                TazLang.Get("mog_labeltooltips"),
                TooltipsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_labeltooltips"), [TazLang.Get("mog_kw_tooltip"), TazLang.Get("mog_kw_hover")])
            )
            .AddTab(
                TazLang.Get("mog_buttoninfobar"),
                InfoBarsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttoninfobar"), [TazLang.Get("mog_kw_infobarspaced"), TazLang.Get("mog_kw_infobar"), TazLang.Get("mog_kw_stat")])
            )
            .AddTab(
                TazLang.Get("mog_buttongumps"),
                GumpsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttongumps"), [TazLang.Get("mog_kw_gump"), TazLang.Get("mog_kw_window")])
            )
            .AddTab(
                TazLang.Get("mog_buttoncounters"),
                CountersTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttoncounters"), [TazLang.Get("mog_kw_counter"), TazLang.Get("mog_kw_item"), TazLang.Get("mog_kw_reagent")])
            )
            .AddTab(
                TazLang.Get("mog_buttonpaperdoll"),
                PaperdollTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttonpaperdoll"), [TazLang.Get("mog_kw_paperdoll"), TazLang.Get("mog_kw_character"), TazLang.Get("mog_kw_equipment")])
            )
            .AddTab(
                TazLang.Get("mog_cooldownstab_cooldownbarslabel"),
                CooldownBarsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_cooldownstab_cooldownbarslabel"), [TazLang.Get("mog_kw_cooldown"), TazLang.Get("mog_kw_timer")])
            );
    }
}
