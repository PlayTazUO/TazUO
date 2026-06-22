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
            TazLang.Get("assistant_pathfinding_zlevel", "Pathfinding Z level difference"),
            out MyraHSlider zLevelSlider,
            v => { ProfileManager.CurrentProfile?.PathfindingZLevelDiff = (int)v; },
            min: 1,
            max: 50,
            value: ProfileManager.CurrentProfile.PathfindingZLevelDiff);
        zLevelSlider.Tooltip = TazLang.Get("assistant_pathfinding_zlevel_tooltip", "Advanced setting: maximum Z (height) difference between pathfinding nodes. Adjust with care.");

        root.Widgets.Add(zLevelSliderWidget);

        HorizontalStackPanel maxNodesWidget = MyraInputBox.NumberWithLabel(
            TazLang.Get("assistant_pathfinding_maxnodes", "Pathfinding max nodes"),
            value: ProfileManager.CurrentProfile.PathfindingMaxNodes,
            min: 10000,
            max: 500000,
            onChanged: v => { ProfileManager.CurrentProfile?.PathfindingMaxNodes = v; },
            tooltip: TazLang.Get("assistant_pathfinding_maxnodes_tooltip", "Maximum number of tiles the in-game pathfinder will explore before giving up (10000-500000). Higher values find longer/harder paths at the cost of more CPU and memory."));

        root.Widgets.Add(maxNodesWidget);

        HorizontalStackPanel wmMaxNodesWidget = MyraInputBox.NumberWithLabel(
            TazLang.Get("assistant_pathfinding_worldmap_maxnodes", "World map pathfinding max nodes"),
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxNodes,
            min: 100000,
            max: 5000000,
            onChanged: v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxNodes = v; },
            tooltip: TazLang.Get("assistant_pathfinding_worldmap_maxnodes_tooltip", "Maximum number of tiles the long-distance world map pathfinder will explore (100000-5000000). Higher values reach farther destinations at the cost of more CPU and memory (~100MB per 1M nodes)."));

        root.Widgets.Add(wmMaxNodesWidget);

        HorizontalStackPanel wmTimeoutWidget = MyraInputBox.NumberWithLabel(
            TazLang.Get("assistant_pathfinding_worldmap_timeout", "World map pathfinding timeout (ms)"),
            value: ProfileManager.CurrentProfile.WorldMapPathfindingTimeout,
            min: 1000,
            max: 30000,
            onChanged: v => { ProfileManager.CurrentProfile?.WorldMapPathfindingTimeout = v; },
            tooltip: TazLang.Get("assistant_pathfinding_worldmap_timeout_tooltip", "Maximum time (in milliseconds) a single world map path search may run before it is abandoned (1000-30000)."));

        root.Widgets.Add(wmTimeoutWidget);

        HorizontalStackPanel wmRetriesSliderWidget = MyraHSlider.SliderWithLabel(
            TazLang.Get("assistant_pathfinding_worldmap_retries", "World map pathfinding retry attempts"),
            out MyraHSlider wmRetriesSlider,
            v => { ProfileManager.CurrentProfile?.WorldMapPathfindingMaxRetries = (int)v; },
            min: 0,
            max: 10,
            value: ProfileManager.CurrentProfile.WorldMapPathfindingMaxRetries);
        wmRetriesSlider.Tooltip = TazLang.Get("assistant_pathfinding_worldmap_retries_tooltip", "How many times world map navigation will replan around an unexpected obstacle (e.g. a placed door or lamp post) before giving up.");

        root.Widgets.Add(wmRetriesSliderWidget);

        return root;
    }
}
