#nullable enable
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class PathfindingTabContent
{
    public static Widget Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        HorizontalStackPanel zLevelSliderWidget = LabeledHorizontalSlider.SliderWithLabel(
            "Pathfinding Z level difference",
            out LabeledHorizontalSlider zLevelSlider,
            v => { ProfileManager.CurrentProfile?.PathfindingZLevelDiff = (int)v; },
            min: 1,
            max: 50,
            value: ProfileManager.CurrentProfile.PathfindingZLevelDiff);
        zLevelSlider.Tooltip = "Advanced setting: maximum Z (height) difference between pathfinding nodes. Adjust with care.";

        root.Widgets.Add(zLevelSliderWidget);

        HorizontalStackPanel maxNodesWidget = MyraInputBox.NumberWithLabel(
            "Pathfinding max nodes",
            value: ProfileManager.CurrentProfile.PathfindingMaxNodes,
            min: 10000,
            max: 500000,
            onChanged: v => { ProfileManager.CurrentProfile?.PathfindingMaxNodes = v; },
            tooltip: "Maximum number of tiles the in-game pathfinder will explore before giving up (10000-500000). Higher values find longer/harder paths at the cost of more CPU and memory.");

        root.Widgets.Add(maxNodesWidget);

        HorizontalStackPanel multiBufferSliderWidget = LabeledHorizontalSlider.SliderWithLabel(
            "Pathfinding house buffer",
            out LabeledHorizontalSlider multiBufferSlider,
            v => { ProfileManager.CurrentProfile?.PathfindingMultiBuffer = (int)v; },
            min: 0,
            max: 50,
            value: ProfileManager.CurrentProfile.PathfindingMultiBuffer);
        multiBufferSlider.Tooltip = "Extra pathfinding cost for tiles next to a house/multi wall, keeping paths a tile away from houses when possible. 0 disables it; higher values avoid houses more strongly.";

        root.Widgets.Add(multiBufferSliderWidget);

        HorizontalStackPanel wmMaxNodesWidget = MyraInputBox.NumberWithLabel(
            "World map pathfinding max nodes",
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxNodes,
            min: 100000,
            max: 5000000,
            onChanged: v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxNodes = v; },
            tooltip: "Maximum number of tiles the long-distance world map pathfinder will explore (100000-5000000). Higher values reach farther destinations at the cost of more CPU and memory (~100MB per 1M nodes).");

        root.Widgets.Add(wmMaxNodesWidget);

        HorizontalStackPanel wmTimeoutWidget = MyraInputBox.NumberWithLabel(
            "World map pathfinding timeout (ms)",
            value: ProfileManager.CurrentProfile.WorldMapPathfindingTimeout,
            min: 1000,
            max: 30000,
            onChanged: v => { ProfileManager.CurrentProfile?.WorldMapPathfindingTimeout = v; },
            tooltip: "Maximum time (in milliseconds) a single world map path search may run before it is abandoned (1000-30000).");

        root.Widgets.Add(wmTimeoutWidget);

        HorizontalStackPanel wmRetriesSliderWidget = LabeledHorizontalSlider.SliderWithLabel(
            "World map pathfinding retry attempts",
            out LabeledHorizontalSlider wmRetriesSlider,
            v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxRetries = (int)v; },
            min: 0,
            max: 10,
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxRetries);
        wmRetriesSlider.Tooltip = "How many times world map navigation will replan around an unexpected obstacle (e.g. a placed door or lamp post) before giving up.";

        root.Widgets.Add(wmRetriesSliderWidget);

        return root;
    }
}
