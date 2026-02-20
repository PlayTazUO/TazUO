using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraStyle
{
    public static void SetDefault()
    {
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = new SolidBrush(new Color(30, 29, 36, 200));
        style.Border = new SolidBrush(new Color(0.667f, 0.412f, 0.051f, 1f));
        style.Padding = new Thickness(0);
        style.BorderThickness = new Thickness(4);
    }

    public static WidgetStyle StandardStyle = new WidgetStyle()
    {
        Background = Stylesheet.Current.WindowStyle.Background,
    };
}
