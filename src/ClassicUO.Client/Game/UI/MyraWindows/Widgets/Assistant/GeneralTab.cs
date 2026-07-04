using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class GeneralTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab(TazLang.Get("assistant_general_tab_options", "Options"), GeneralTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_hud", "HUD"), HudTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_spellbar", "Spell Bar"), SpellBarTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_titlebar", "Title Bar"), TitleBarTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_spellindicators", "Spell Indicators"), SpellIndicatorTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_friends", "Friends"), FriendsListTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_general_tab_pathfinding", "Pathfinding"), PathfindingTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
