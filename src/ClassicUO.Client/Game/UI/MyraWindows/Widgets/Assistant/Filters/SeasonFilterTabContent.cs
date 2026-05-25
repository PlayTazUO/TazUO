#nullable enable
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

public static class SeasonFilterTabContent
{
    private static readonly string[] SeasonNames = { "春", "夏", "秋", "冬", "荒芜" };
    private static readonly Season[] AllSeasons =
    {
        Season.Spring,
        Season.Summer,
        Season.Fall,
        Season.Winter,
        Season.Desolation
    };

    // Display options: "None" followed by each season
    private static readonly string[] DisplayOptions;

    static SeasonFilterTabContent()
    {
        DisplayOptions = new string[AllSeasons.Length + 1];
        DisplayOptions[0] = "无";
        for (int j = 0; j < SeasonNames.Length; j++)
            DisplayOptions[j + 1] = SeasonNames[j];
    }

    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel(
            "覆盖服务器发送的季节。例如，如果服务器发送冬天，您可以显示秋天。",
            MyraLabel.TextStyle.H3) { MaxWidth = 500 });

        // Collect BuildCycleBtn delegates so Clear can refresh all wrappers
        var rebuildActions = new System.Collections.Generic.List<System.Action>();

        root.Widgets.Add(new MyraButton("清除所有过滤器", () =>
        {
            SeasonFilter.Instance.Clear();
            foreach (System.Action rebuild in rebuildActions) rebuild();
        }) { Tooltip = "移除所有季节过滤器并按服务器发送的方式显示季节" });

        root.Widgets.Add(new MyraLabel("季节过滤器:", MyraLabel.TextStyle.H3));

        var grid = new MyraGrid();
        grid.SetupWithHeaders(GridColumnInfo.Auto("服务器发送"), GridColumnInfo.Auto("显示为"));

        for (int i = 0; i < AllSeasons.Length; i++)
        {
            Season incoming = AllSeasons[i];
            string incomingName = SeasonNames[i];

            grid.AddWidget(new MyraLabel(incomingName, MyraLabel.TextStyle.P), i + 1, 0);

            var cycleWrapper = new HorizontalStackPanel();

            void BuildCycleBtn()
            {
                cycleWrapper.Widgets.Clear();

                string currentLabel = "无";
                int currentIdx = 0;
                if (SeasonFilter.Instance.Filters.TryGetValue(incoming, out Season replacement))
                {
                    for (int k = 0; k < AllSeasons.Length; k++)
                    {
                        if (AllSeasons[k] == replacement)
                        {
                            currentIdx = k + 1;
                            currentLabel = SeasonNames[k];
                            break;
                        }
                    }
                }

                cycleWrapper.Widgets.Add(new MyraButton(currentLabel, () =>
                {
                    int nextIdx = (currentIdx + 1) % DisplayOptions.Length;
                    if (nextIdx == 0)
                        SeasonFilter.Instance.RemoveFilter(incoming);
                    else
                        SeasonFilter.Instance.SetFilter(incoming, AllSeasons[nextIdx - 1]);
                    BuildCycleBtn();
                }) { Tooltip = $"点击循环切换 {incomingName} 的季节覆盖" });
            }

            rebuildActions.Add(BuildCycleBtn);
            BuildCycleBtn();
            grid.AddWidget(cycleWrapper, i + 1, 1);
        }

        root.Widgets.Add(grid);
        root.Widgets.Add(new MyraLabel(
            "点击按钮循环切换选项。'无' 将禁用过滤器。",
            MyraLabel.TextStyle.P));

        return root;
    }
}
