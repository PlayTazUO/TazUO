using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class FiltersTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab(TazLang.Get("assistant_filter_tab_graphics", "Graphics"), GraphicReplacementTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_filter_tab_journal", "Journal Filter"), JournalFilterTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_filter_tab_sound", "Sound Filter"), SoundFilterTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_filter_tab_music", "Music Filter"), MusicFilterTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_filter_tab_season", "Season Filter"), SeasonFilterTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
