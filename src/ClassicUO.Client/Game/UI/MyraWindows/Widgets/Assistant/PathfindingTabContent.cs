#nullable enable
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class PathfindingTabContent
{
    public static Widget Build()
    {
        var root = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        #region LeftSide
        var leftStack = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        leftStack.Widgets.Add(
            MyraCheckButton.CreateWithCallback(
                World.Instance?.Player?.Pathfinder.UseLongDistancePathfinding ?? false,
                b =>
                {
                    if (World.Instance?.Player != null)
                        World.Instance.Player.Pathfinder.UseLongDistancePathfinding = b;
                    Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.USE_LONG_DISTANCE_PATHING, b);
                },
                "长距离寻路",
                "此功能目前处于测试阶段。"));

        HorizontalStackPanel genTimeRow = MyraHSlider.SliderWithLabel(
            "寻路生成时间（毫秒）",
            out MyraHSlider genTimeSlider,
            v =>
            {
                int ms = (int)v;
                Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.LONG_DISTANCE_PATHING_SPEED, ms);
                if (WalkableManager.Instance != null)
                    WalkableManager.Instance.TargetGenerationTimeMs = ms;
            },
            min: 1,
            max: 50,
            value: Client.Settings.Get(SettingsScope.Global, Constants.SqlSettings.LONG_DISTANCE_PATHING_SPEED, 2));
        genTimeSlider.Tooltip = "每周期寻路缓存生成的目标时间（毫秒）。较高的值能更快生成缓存，但可能导致性能问题。";
        leftStack.Widgets.Add(genTimeRow);

        var progressLabel = new MyraLabel("缓存进度: N/A", MyraLabel.TextStyle.P)
        {
            Tooltip = "当前地图缓存生成进度"
        };

        void RefreshProgress()
        {
            if (WalkableManager.Instance != null)
            {
                var (current, total) = WalkableManager.Instance.GetCurrentMapGenerationProgress();
                if (total > 0)
                    progressLabel.Text = $"缓存进度: {current}/{total} 块 ({(float)current / total * 100f:F1}%)";
                else
                    progressLabel.Text = "缓存进度: N/A";
            }
            else
            {
                progressLabel.Text = "Cache Progress: N/A";
            }
        }

        RefreshProgress();

        var progressRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        progressRow.Widgets.Add(progressLabel);
        progressRow.Widgets.Add(new MyraButton("刷新", RefreshProgress));
        leftStack.Widgets.Add(progressRow);

        leftStack.Widgets.Add(new MyraButton("重置当前地图缓存", () =>
        {
            if (World.Instance != null)
                WalkableManager.Instance?.StartFreshGeneration(World.Instance.MapIndex);
            RefreshProgress();
        })
        { Tooltip = "这将重新生成当前地图缓存。" });

        root.Widgets.Add(leftStack);
        #endregion

        #region RightSide

        var rightSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        HorizontalStackPanel zLevelSliderWidget = MyraHSlider.SliderWithLabel(
            "寻路Z轴差异",
            out MyraHSlider zLevelSlider, v
                => { ProfileManager.CurrentProfile?.PathfindingZLevelDiff = (int)v; },
            1,
            50,
            ProfileManager.CurrentProfile.PathfindingZLevelDiff);
        zLevelSlider.Tooltip = "这是一个高级设置，请自行承担风险调整。\n此设置调整寻路节点之间的最大Z轴（高度）差异。";

        rightSide.Widgets.Add(zLevelSliderWidget);

        root.Widgets.Add(rightSide);
        #endregion

        return root;
    }
}
