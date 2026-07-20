using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Controls inter-widget spacing inside a <see cref="VisualContainer"/>.
/// </summary>
public enum VisualContainerSpacing
{
    /// <summary>Uses the shared <see cref="MyraStyle.STANDARD_SPACING"/> value.</summary>
    Standard,
    /// <summary>2 px between children — tightest layout.</summary>
    Dense,
    /// <summary>4 px between children — default for most containers.</summary>
    Comfortable,
    /// <summary>8 px between children — loosest layout.</summary>
    Sparse
}

/// <summary>
/// Immutable configuration record for <see cref="VisualContainer"/>. All properties have
/// sensible defaults so callers only need to override what differs.
/// </summary>
public record struct VisualContainerProps()
{
    /// <summary>Gets the stack direction of the container's children.</summary>
    public Orientation Orientation { get; init; } = Orientation.Vertical;

    /// <summary>
    /// Gets optional header text rendered above the children. When <see langword="null"/> or
    /// whitespace, no label widget is added.
    /// </summary>
    public string LabelText { get; init; } = null;

    /// <summary>
    /// A tooltip to be displayed on the label, if one is present
    /// </summary>
    public string LabelTooltip { get; init; } = null;

    /// <summary>Gets the horizontal alignment applied to the label widget.</summary>
    public HorizontalAlignment LabelHorizontalAlignment { get; init; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets an optional URL or anchor for the label. When set, the label is rendered as a
    /// <see cref="LinkLabel"/> instead of a plain <see cref="MyraLabel"/>.
    /// </summary>
    public string LabelLink { get; init; } = null;

    /// <summary>Gets the spacing preset applied between children.</summary>
    public VisualContainerSpacing? Spacing { get; init; } = VisualContainerSpacing.Comfortable;
}

/// <summary>
/// A styled Myra <see cref="Container"/> with a consistent visual frame (background, border,
/// margin, padding) and an optional header label. Children are stacked using a
/// <see cref="StackPanelLayout"/> whose orientation and spacing are driven by
/// <see cref="VisualContainerProps"/>.
/// </summary>
public class VisualContainer : Container
{
    /// <summary>
    /// Initializes a new <see cref="VisualContainer"/> with the given props and optional initial
    /// child widgets.
    /// </summary>
    /// <param name="props">Layout and style configuration.</param>
    /// <param name="widgets">Zero or more widgets added as children after the optional label.</param>
    public VisualContainer(VisualContainerProps props, params Widget[] widgets)
    {
        if (!string.IsNullOrWhiteSpace(props.LabelText))
        {
            Widget label;
            if (string.IsNullOrWhiteSpace(props.LabelLink))
                label = new MyraLabel(props.LabelText, MyraLabel.TextStyle.P) { Tooltip = props.LabelTooltip};
            else
                label = new LinkLabel(props.LabelText, props.LabelLink) { Tooltip = props.LabelTooltip};

            label.HorizontalAlignment = props.LabelHorizontalAlignment;

            Children.Add(label);
            Children.Add(new MyraSpacer(0, 2));
        }

        if (widgets?.Length > 0)
            Add(widgets);

        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);
        VerticalAlignment = VerticalAlignment.Top;

        int spacing = props.Spacing switch
        {
            VisualContainerSpacing.Dense => 2,
            VisualContainerSpacing.Comfortable => 4,
            VisualContainerSpacing.Sparse => 8,
            _ => MyraStyle.STANDARD_SPACING
        };

        ChildrenLayout = new StackPanelLayout(props.Orientation) { Spacing = spacing };
    }

    /// <summary>Appends one or more widgets to the container's children.</summary>
    /// <param name="widgets">Widgets to add.</param>
    public void Add(params Widget[] widgets)
    {
        foreach (Widget widget in widgets)
            Children.Add(widget);
    }
}
