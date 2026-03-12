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
                "Long-Distance Pathfinding",
                "This is currently in beta."));

        HorizontalStackPanel genTimeRow = MyraHSlider.SliderWithLabel(
            "Pathfinding Gen Time (ms)",
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
        genTimeSlider.Tooltip = "Target time in milliseconds for pathfinding cache generation per cycle. Higher values generate cache faster but may cause performance issues.";
        leftStack.Widgets.Add(genTimeRow);

        var progressLabel = new MyraLabel("Cache Progress: N/A", MyraLabel.TextStyle.P)
        {
            Tooltip = "Current map cache generation progress"
        };

        void RefreshProgress()
        {
            if (WalkableManager.Instance != null)
            {
                var (current, total) = WalkableManager.Instance.GetCurrentMapGenerationProgress();
                if (total > 0)
                    progressLabel.Text = $"Cache Progress: {current}/{total} chunks ({(float)current / total * 100f:F1}%)";
                else
                    progressLabel.Text = "Cache Progress: N/A";
            }
            else
            {
                progressLabel.Text = "Cache Progress: N/A";
            }
        }

        RefreshProgress();

        var progressRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        progressRow.Widgets.Add(progressLabel);
        progressRow.Widgets.Add(new MyraButton("Refresh", RefreshProgress));
        leftStack.Widgets.Add(progressRow);

        leftStack.Widgets.Add(new MyraButton("Reset current map cache", () =>
        {
            if (World.Instance != null)
                WalkableManager.Instance?.StartFreshGeneration(World.Instance.MapIndex);
            RefreshProgress();
        })
        { Tooltip = "This will start regeneration of the current map cache." });

        root.Widgets.Add(leftStack);
        #endregion

        #region RightSide

        var rightSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        HorizontalStackPanel zLevelSliderWidget = MyraHSlider.SliderWithLabel(
            "Pathfinding Z level difference",
            out MyraHSlider zLevelSlider, v
                => { ProfileManager.CurrentProfile?.PathfindingZLevelDiff = (int)v; },
            1,
            50,
            ProfileManager.CurrentProfile.PathfindingZLevelDiff);
        zLevelSlider.Tooltip = "This is an advanced setting, adjust at your own peril.\nThis adjusts the maximum z level(height) difference between pathfinding nodes.";

        rightSide.Widgets.Add(zLevelSliderWidget);

        root.Widgets.Add(rightSide);
        #endregion

        return root;
    }
}
