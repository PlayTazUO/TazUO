using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraStyle
{
    public const int STANDARD_SPACING = 3;
    public static Color GridBorderColor { get; } = Color.Gray;

    private static Color TazUO_Orange = new(0.667f, 0.412f, 0.051f, 1f);

    public static void SetDefault()
    {
        //Window style
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = new SolidBrush(new Color(12, 12, 12, 220));
        style.Border = new SolidBrush(TazUO_Orange);
        style.Padding = new Thickness(0);
        style.BorderThickness = new Thickness(2);
        style.TitleStyle.Padding = new Thickness(2);

        //Labels
        Stylesheet.Current.LabelStyle.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 16);

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
        SliderStyle sStyle = Stylesheet.Current.HorizontalSliderStyle;
        sStyle.Background = new SolidBrush(new Color(50, 49, 56, 50));
        sStyle.OverBackground = new SolidBrush(new Color(50, 49, 56, 150));
        sStyle.KnobStyle.ImageStyle.Background = new SolidBrush(TazUO_Orange);
        sStyle.KnobStyle.ImageStyle.OverBackground = new SolidBrush(TazUO_Orange);
        sStyle.KnobStyle.ImageStyle.FocusedBackground = new SolidBrush(TazUO_Orange);

        sStyle.KnobStyle.ImageStyle.PressedImage = null;
        sStyle.KnobStyle.ImageStyle.Image = null;
        sStyle.Width = 100;
        sStyle.Height = 20;

        //Button
        ButtonStyle s = Stylesheet.Current.ButtonStyle;
        s.Background = new SolidBrush(TazUO_Orange);
        s.MinWidth = 1;
        s.MinHeight = 1;
        s.Padding = new Thickness(5);

        TextBoxStyle inputS = Stylesheet.Current.TextBoxStyle;
        inputS.Padding = new Thickness(3);
    }

    /// <summary>
    /// Various properties that cannot be applied by default in Myra for grids.
    /// </summary>
    /// <param name="grid"></param>
    public static void ApplyStandardGridStyling(Grid grid)
    {
        grid.Border = new SolidBrush(GridBorderColor);
        grid.BorderThickness = new Thickness(1);
        grid.GridLinesColor = GridBorderColor;
        grid.ShowGridLines = true;
    }

    public static Button ApplyButtonDangerStyle(Button button)
    {
        button.Background = new SolidBrush(new Color(155, 0, 0, 255));
        button.DisabledBackground = new SolidBrush(new Color(155, 0, 0, 155));
        button.OverBackground = new SolidBrush(new Color(100, 0, 0, 255));
        button.PressedBackground = new SolidBrush(new Color(55, 0, 0, 255));

        return button;
    }
}
