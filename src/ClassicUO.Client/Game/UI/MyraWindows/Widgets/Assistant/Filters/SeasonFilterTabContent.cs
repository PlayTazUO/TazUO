#nullable enable
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class SeasonFilterTabContent
{
    private static readonly Season[] AllSeasons =
    {
        Season.Spring,
        Season.Summer,
        Season.Fall,
        Season.Winter,
        Season.Desolation
    };

    private static string GetSeasonName(Season s) => s switch
    {
        Season.Spring => TazLang.Get("season_filter_tabs_season_spring", "Spring"),
        Season.Summer => TazLang.Get("season_filter_tabs_season_summer", "Summer"),
        Season.Fall => TazLang.Get("season_filter_tabs_season_fall", "Fall"),
        Season.Winter => TazLang.Get("season_filter_tabs_season_winter", "Winter"),
        Season.Desolation => TazLang.Get("season_filter_tabs_season_desolation", "Desolation"),
        _ => "Unknown"
    };

    private static string GetDisplayOption(int index)
    {
        if (index == 0)
            return TazLang.Get("season_filter_tabs_option_none", "None");
        return GetSeasonName(AllSeasons[index - 1]);
    }

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            TazLang.Get("season_filter_tabs_desc", "Override seasons sent by the server. For example, if the server sends Winter, you can display Fall instead."),
            MyraLabel.TextStyle.H3) { MaxWidth = 500 });

        // Collect BuildCycleBtn delegates so Clear can refresh all wrappers
        var rebuildActions = new System.Collections.Generic.List<System.Action>();

        root.Widgets.Add(new MyraButton(TazLang.Get("season_filter_tabs_btn_clear_all", "Clear All Filters"), () =>
        {
            SeasonFilter.Instance.Clear();
            foreach (System.Action rebuild in rebuildActions) rebuild();
        }) { Tooltip = TazLang.Get("season_filter_tabs_tooltip_clear_all", "Remove all season filters and display seasons as sent by the server") });

        root.Widgets.Add(new MyraLabel(TazLang.Get("season_filter_tabs_label_season_filters", "Season Filters:"), MyraLabel.TextStyle.H3));

        var grid = new MyraGrid();
        grid.SetupWithHeaders(
            GridColumnInfo.Auto(TazLang.Get("season_filter_tabs_col_server_sends", "When Server Sends")),
            GridColumnInfo.Auto(TazLang.Get("season_filter_tabs_col_show_as", "Show As")));

        int displayOptionCount = AllSeasons.Length + 1;

        for (int i = 0; i < AllSeasons.Length; i++)
        {
            Season incoming = AllSeasons[i];
            string incomingName = GetSeasonName(incoming);

            grid.AddWidget(new MyraLabel(incomingName, MyraLabel.TextStyle.P), i + 1, 0);

            var cycleWrapper = new HorizontalStackPanel();

            void BuildCycleBtn()
            {
                cycleWrapper.Widgets.Clear();

                string currentLabel = GetDisplayOption(0);
                int currentIdx = 0;
                if (SeasonFilter.Instance.Filters.TryGetValue(incoming, out Season replacement))
                {
                    for (int k = 0; k < AllSeasons.Length; k++)
                    {
                        if (AllSeasons[k] == replacement)
                        {
                            currentIdx = k + 1;
                            currentLabel = GetSeasonName(AllSeasons[k]);
                            break;
                        }
                    }
                }

                cycleWrapper.Widgets.Add(new MyraButton(currentLabel, () =>
                {
                    int nextIdx = (currentIdx + 1) % displayOptionCount;
                    if (nextIdx == 0)
                        SeasonFilter.Instance.RemoveFilter(incoming);
                    else
                        SeasonFilter.Instance.SetFilter(incoming, AllSeasons[nextIdx - 1]);
                    BuildCycleBtn();
                }) { Tooltip = TazLang.Get("season_filter_tabs_tooltip_cycle_season_fmt", new[] { incomingName }) });
            }

            rebuildActions.Add(BuildCycleBtn);
            BuildCycleBtn();
            grid.AddWidget(cycleWrapper, i + 1, 1);
        }

        root.Widgets.Add(grid);
        root.Widgets.Add(new MyraLabel(
            TazLang.Get("season_filter_tabs_help", "Click the button to cycle through options. 'None' disables the filter."),
            MyraLabel.TextStyle.P));

        return root;
    }
}
