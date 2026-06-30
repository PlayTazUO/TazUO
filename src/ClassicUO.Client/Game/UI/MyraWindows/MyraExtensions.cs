using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraExtensions
{
    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = VerticalAlignment.Center};
        panel.Widgets.Add(widget);
        panel.Widgets.Add(rightSide);
        return panel;
    }

    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget[] rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = VerticalAlignment.Center };
        panel.Widgets.Add(widget);
        foreach (Widget w in rightSide)
            panel.Widgets.Add(w);
        return panel;
    }

    public static ScrollViewer WrapInScroll(this Widget widget, int? maxHeight = null)
    {
        var scroll = new ScrollViewer();
        scroll.Content = widget;

        if(maxHeight != null)
            scroll.MaxHeight = maxHeight.Value;

        return scroll;
    }

    public static WrapPanel AddRange(this WrapPanel panel, params Widget[] children)
    {
        foreach (Widget child in children)
            panel.Widgets.Add(child);
        return panel;
    }
}
