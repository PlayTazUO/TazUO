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

        return root;
    }
}
