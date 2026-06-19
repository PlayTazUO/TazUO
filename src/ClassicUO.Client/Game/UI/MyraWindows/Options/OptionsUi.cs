#nullable enable

using System.Linq;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal static class OptionsUi
{
    public static OptionFragment Vertical(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                return OptionTabCommons.StyledVerticalWrapPanel(widgets);
            },
            children
        );

    public static OptionFragment Horizontal(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                return OptionTabCommons.StyledHorizontalWrapPanel(widgets);
            },
            children
        );

    public static OptionFragment VisualContainer(
        VisualContainerProps props,
        params OptionContent[] children
    ) =>
        new(
            () => new VisualContainer(props, children.Select(c => c.Render()).ToArray()),
            children
        );
}
