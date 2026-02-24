using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraStyle
{
    public static Color GridBorderColor { get; } = Color.Gray;

    private static Color TazUO_Orange = new(0.667f, 0.412f, 0.051f, 1f);

    public static void SetDefault()
    {
        //Window style
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = new SolidBrush(new Color(30, 29, 36, 200));
        style.Border = new SolidBrush(TazUO_Orange);
        style.Padding = new Thickness(0);
        style.BorderThickness = new Thickness(2);

        //Labels
        Stylesheet.Current.LabelStyle.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 14);

        //Tabs
        TabControlStyle tabControlStyle = Stylesheet.Current.TabControlStyle;
        tabControlStyle.Background = new SolidBrush(Color.Transparent);
        tabControlStyle.Border = new SolidBrush(TazUO_Orange);
        tabControlStyle.BorderThickness = new Thickness(1);

        tabControlStyle.ContentStyle ??= new WidgetStyle();
        tabControlStyle.ContentStyle.Background = new SolidBrush(Color.Transparent);

        ImageTextButtonStyle tabItemStyle = tabControlStyle.TabItemStyle;
        tabItemStyle.Background = new SolidBrush(Color.Transparent);
        tabItemStyle.OverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        tabItemStyle.PressedBackground = new SolidBrush(new Color(170, 105, 13, 160));
        tabItemStyle.Border = new SolidBrush(TazUO_Orange);
        tabItemStyle.BorderThickness = new Thickness(1);

        //HSlider
        Stylesheet.Current.HorizontalSliderStyle.Background = new SolidBrush(new Color(50, 49, 56, 50));
        Stylesheet.Current.HorizontalSliderStyle.Width = 75;
        Stylesheet.Current.HorizontalSliderStyle.Height = 20;

        //Button
        ButtonStyle s = Stylesheet.Current.ButtonStyle;
        s.MinWidth = 1;
        s.MinHeight = 1;
        s.Padding = new Thickness(5);
    }
}
