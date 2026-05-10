using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraExtensions
{
    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING };
        panel.Widgets.Add(widget);
        panel.Widgets.Add(rightSide);
        return panel;
    }

    public static HorizontalStackPanel PlaceBefore(this Widget widget, Widget[] rightSide)
    {
        var panel = new HorizontalStackPanel() { Spacing = MyraStyle.STANDARD_SPACING };
        panel.Widgets.Add(widget);
        foreach (Widget w in rightSide)
            panel.Widgets.Add(w);
        return panel;
    }
}
