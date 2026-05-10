using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class FiltersTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("Graphics", GraphicReplacementTabContent.Build);
        tabs.AddTab("Journal Filter", JournalFilterTabContent.Build);
        tabs.AddTab("Sound Filter", SoundFilterTabContent.Build);
        tabs.AddTab("Music Filter", MusicFilterTabContent.Build);
        tabs.AddTab("Season Filter", SeasonFilterTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
