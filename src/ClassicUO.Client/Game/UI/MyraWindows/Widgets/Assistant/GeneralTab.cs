using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class GeneralTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("选项", GeneralTabContent.Build);
        tabs.AddTab("HUD", HudTabContent.Build);
        tabs.AddTab("法术条", SpellBarTabContent.Build);
        tabs.AddTab("标题栏", TitleBarTabContent.Build);
        tabs.AddTab("法术指示器", SpellIndicatorTabContent.Build);
        tabs.AddTab("好友", FriendsListTabContent.Build);
        tabs.AddTab("寻路", PathfindingTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
