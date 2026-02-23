using ClassicUO.Assets;
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

        Stylesheet.Current.LabelStyle.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 14);

        Stylesheet.Current.TabControlStyle.Background = new SolidBrush(Color.Transparent);
        Stylesheet.Current.TabControlStyle.TabItemStyle.Background = new SolidBrush(Color.Transparent);
    }

    public static WidgetStyle GridStyle = new()
    {
        Background = Stylesheet.Current.WindowStyle.Background
    };

    public static SliderStyle HorizontalSlider = new()
    {
        Background = new SolidBrush(new Color(50, 49, 56, 200)),
        Width = 75,
        Height = 20,
    };

    public static ButtonStyle ButtonStyle = GetButonStyle();
    private static ButtonStyle GetButonStyle()
    {
        var s = Stylesheet.Current.ButtonStyle.Clone() as ButtonStyle;
        s?.MinWidth = 1;
        s?.MinHeight = 1;
        s?.Padding = new Thickness(5);
        return s;
    }
}
