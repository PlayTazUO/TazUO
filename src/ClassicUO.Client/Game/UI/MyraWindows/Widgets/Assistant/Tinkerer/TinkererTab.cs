using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

public static class TinkererTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("Clilocs", CililocsTabContent.Build);
        tabs.AddTab("Hue View", HueViewTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
