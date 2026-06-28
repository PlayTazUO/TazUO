#nullable enable

using System.Linq;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Convenience factories for composing common option layout primitives: vertical/horizontal
/// groups, visual containers, and checkbox groups with dependent children
/// </summary>
internal static class OptionsUi
{
    /// <summary>
    /// Creates an <see cref="OptionFragment"/> whose children are stacked vertically
    /// </summary>
    /// <param name="children">The option slots to include</param>
    /// <returns>A vertically-arranged option fragment</returns>
    public static OptionFragment Vertical(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                return OptionTabCommons.StyledVerticalWrapPanel(widgets);
            },
            children
        );

    /// <summary>
    /// Creates an <see cref="OptionFragment"/> whose children are arranged horizontally
    /// </summary>
    /// <param name="children">The option slots to include</param>
    /// <returns>A horizontally-arranged option fragment</returns>
    public static OptionFragment Horizontal(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                return OptionTabCommons.StyledHorizontalWrapPanel(widgets);
            },
            children
        );

    /// <summary>
    /// Creates an <see cref="OptionFragment"/> that wraps its children in a
    /// <see cref="VisualContainer"/> with a styled border and optional label
    /// </summary>
    /// <param name="props">Visual container display properties (label, link, spacing)</param>
    /// <param name="children">The option slots to display inside the container</param>
    /// <returns>An option fragment backed by a <see cref="VisualContainer"/></returns>
    public static OptionFragment VisualContainer(
        VisualContainerProps props,
        params OptionContent[] children
    ) =>
        new(
            () => new VisualContainer(props, children.Select(c => c.Render()).ToArray()),
            children
        );

    /// <summary>
    /// Creates an <see cref="OptionFragment"/> that wraps its children in a
    /// <see cref="Widgets.CheckBoxGroup"/>, which enables or disables all children
    /// based on a controlling checkbox property
    /// </summary>
    /// <param name="controlProp">The property that drives the group's enabled state via its checkbox</param>
    /// <param name="children">The option slots that are enabled/disabled as a group</param>
    /// <returns>An option fragment backed by a <see cref="Widgets.CheckBoxGroup"/></returns>
    public static OptionFragment CheckBoxGroup(
        PropertyBinder controlProp,
        params OptionContent[] children
    ) =>
        new OptionFragment(
            () => new CheckBoxGroup(controlProp, children.Select(c => c.Render()).ToArray()),
            children
        ).AsSearchGroup();
}
