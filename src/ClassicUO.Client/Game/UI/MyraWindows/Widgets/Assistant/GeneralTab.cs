using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class GeneralTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("Options", GeneralTabContent.Build);
        tabs.AddTab("HUD", HudTabContent.Build);
        tabs.AddTab("Spell Bar", SpellBarTabContent.Build);
        tabs.AddTab("Title Bar", TitleBarTabContent.Build);
        tabs.AddTab("Spell Indicators", SpellIndicatorTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
