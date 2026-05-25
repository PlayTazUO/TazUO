using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class FiltersTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("图形", GraphicReplacementTabContent.Build);
        tabs.AddTab("日志过滤器", JournalFilterTabContent.Build);
        tabs.AddTab("声音过滤器", SoundFilterTabContent.Build);
        tabs.AddTab("音乐过滤器", MusicFilterTabContent.Build);
        tabs.AddTab("季节过滤器", SeasonFilterTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
