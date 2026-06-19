#nullable enable
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class PathfindingTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        HorizontalStackPanel zLevelSliderWidget = MyraHSlider.SliderWithLabel(
            "Pathfinding Z level difference",
            out MyraHSlider zLevelSlider,
            v => { ProfileManager.CurrentProfile?.PathfindingZLevelDiff = (int)v; },
            min: 1,
            max: 50,
            value: ProfileManager.CurrentProfile.PathfindingZLevelDiff);
        zLevelSlider.Tooltip = "Advanced setting: maximum Z (height) difference between pathfinding nodes. Adjust with care.";

        root.Widgets.Add(zLevelSliderWidget);

        HorizontalStackPanel maxNodesSliderWidget = MyraHSlider.SliderWithLabel(
            "Pathfinding max nodes",
            out MyraHSlider maxNodesSlider,
            v => { ProfileManager.CurrentProfile?.PathfindingMaxNodes = (int)v; },
            min: 10000,
            max: 500000,
            value: ProfileManager.CurrentProfile.PathfindingMaxNodes);
        maxNodesSlider.WheelStep = 10000;
        maxNodesSlider.Tooltip = "Maximum number of tiles the in-game pathfinder will explore before giving up. Higher values find longer/harder paths at the cost of more CPU and memory.";

        root.Widgets.Add(maxNodesSliderWidget);

        HorizontalStackPanel wmMaxNodesSliderWidget = MyraHSlider.SliderWithLabel(
            "World map pathfinding max nodes",
            out MyraHSlider wmMaxNodesSlider,
            v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxNodes = (int)v; },
            min: 100000,
            max: 5000000,
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxNodes);
        wmMaxNodesSlider.WheelStep = 100000;
        wmMaxNodesSlider.Tooltip = "Maximum number of tiles the long-distance world map pathfinder will explore. Higher values reach farther destinations at the cost of more CPU and memory (~100MB per 1M nodes).";

        root.Widgets.Add(wmMaxNodesSliderWidget);

        HorizontalStackPanel wmTimeoutSliderWidget = MyraHSlider.SliderWithLabel(
            "World map pathfinding timeout (ms)",
            out MyraHSlider wmTimeoutSlider,
            v => { ProfileManager.CurrentProfile?.WorldMapPathfindingTimeout = (int)v; },
            min: 1000,
            max: 30000,
            value: ProfileManager.CurrentProfile.WorldMapPathfindingTimeout);
        wmTimeoutSlider.WheelStep = 1000;
        wmTimeoutSlider.Tooltip = "Maximum time (in milliseconds) a single world map path search may run before it is abandoned.";

        root.Widgets.Add(wmTimeoutSliderWidget);

        HorizontalStackPanel wmRetriesSliderWidget = MyraHSlider.SliderWithLabel(
            "World map pathfinding retry attempts",
            out MyraHSlider wmRetriesSlider,
            v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxRetries = (int)v; },
            min: 0,
            max: 10,
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxRetries);
        wmRetriesSlider.Tooltip = "How many times world map navigation will replan around an unexpected obstacle (e.g. a placed door or lamp post) before giving up.";

        root.Widgets.Add(wmRetriesSliderWidget);

        return root;
    }
}
