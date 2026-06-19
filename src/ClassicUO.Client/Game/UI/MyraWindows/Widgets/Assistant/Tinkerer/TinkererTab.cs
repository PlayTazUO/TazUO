using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

public static class TinkererTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab(TazLang.Get("tinkerer_tab_clilocs", "Clilocs"), CililocsTabContent.Build);
        tabs.AddTab(TazLang.Get("tinkerer_tab_hues", "Hues"), HueViewTabContent.Build);
        tabs.AddTab(TazLang.Get("tinkerer_tab_art", "Art"), ArtBrowserTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
