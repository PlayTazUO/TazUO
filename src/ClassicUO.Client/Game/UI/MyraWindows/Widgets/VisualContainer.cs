using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public enum VisualContainerSpacing
{
    Standard,
    Dense,
    Comfortable,
    Sparse
}

public record struct VisualContainerProps()
{
    public Orientation Orientation { get; init; } = Orientation.Vertical;
    public string LabelText { get; init; } = null;
    public HorizontalAlignment LabelHorizontalAlightment { get; init; } = HorizontalAlignment.Left;
    public string LabelLink { get; init; } = null;
    public VisualContainerSpacing? Spacing { get; init; } = VisualContainerSpacing.Comfortable;
}

public class VisualContainer : Container
{
    public VisualContainer(VisualContainerProps props, params Widget[] widgets)
    {
        if (!string.IsNullOrWhiteSpace(props.LabelText))
        {
            Widget label;
            if (string.IsNullOrWhiteSpace(props.LabelLink))
                label = new MyraLabel(props.LabelText, MyraLabel.TextStyle.P);
            else
                label = new LinkLabel(props.LabelText, props.LabelLink);

            label.HorizontalAlignment = props.LabelHorizontalAlightment;

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

    public void Add(params Widget[] widgets)
    {
        foreach (Widget widget in widgets)
            Children.Add(widget);
    }
}
