using ClassicUO.Assets;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraStyle
{
    public const int STANDARD_SPACING = 3;
    public const int STANDARD_BORDER_ALPHA = 125;
    public static Color GridBorderColor { get; } = new Color(0, 0, 0, STANDARD_BORDER_ALPHA);

    private static Color TazUO_Orange = new(0.667f, 0.412f, 0.051f, 1f);

    private static NinePatchRegion _ninePatchPanel;
    private static NinePatchRegion _ninePatchButtonUp;
    private static NinePatchRegion _ninePatchButtonDown;
    private static NinePatchRegion _ninePatchButtonDangerUp;
    private static NinePatchRegion _ninePatchButtonDangerDown;

    public static void SetDefault()
    {
        _ninePatchPanel = new NinePatchRegion(ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel.Bounds, new Thickness(ModernUIConstants.ModernUIPanel_BorderSize));
        _ninePatchButtonUp = new NinePatchRegion(ModernUIConstants.ModernUIButtonUp,
            ModernUIConstants.ModernUIButtonUp.Bounds, new Thickness(ModernUIConstants.ModernUIButton_BorderSize));
        _ninePatchButtonDown = new NinePatchRegion(ModernUIConstants.ModernUIButtonDown,
            ModernUIConstants.ModernUIButtonUp.Bounds, new Thickness(ModernUIConstants.ModernUIButton_BorderSize));
        _ninePatchButtonDangerUp = new NinePatchRegion(ModernUIConstants.ModernUIButtonDangerUp,
            ModernUIConstants.ModernUIButtonDangerUp.Bounds, new Thickness(ModernUIConstants.ModernUIButton_BorderSize));
        _ninePatchButtonDangerDown = new NinePatchRegion(ModernUIConstants.ModernUIButtonDangerDown,
            ModernUIConstants.ModernUIButtonDangerUp.Bounds, new Thickness(ModernUIConstants.ModernUIButton_BorderSize));

        //Window style
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = _ninePatchPanel;
        //style.Border = _ninePatchRegion;
        //style.BorderThickness = new Thickness(ModernUIConstants.ModernUIPanel_BorderSize);
        //style.Background = new SolidBrush(new Color(12, 12, 12, 220));
        //style.Border = new SolidBrush(TazUO_Orange);
        //style.BorderThickness = new Thickness(2);

        style.Padding = new Thickness(6);
        style.TitleStyle.Padding = new Thickness(2);

        //Labels
        Stylesheet.Current.LabelStyle.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 16);

        //Tabs
        TabControlStyle tabControlStyle = Stylesheet.Current.TabControlStyle;
        tabControlStyle.Background = new SolidBrush(Color.Transparent);
        tabControlStyle.Border = new SolidBrush(new Color(0, 0, 0, STANDARD_BORDER_ALPHA));
        tabControlStyle.BorderThickness = new Thickness(1);

        tabControlStyle.ContentStyle ??= new WidgetStyle();
        tabControlStyle.ContentStyle.Background = new SolidBrush(Color.Transparent);

        ImageTextButtonStyle tabItemStyle = tabControlStyle.TabItemStyle;
        tabItemStyle.Background = new SolidBrush(Color.Transparent);
        tabItemStyle.OverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        tabItemStyle.PressedBackground = new SolidBrush(new Color(170, 105, 13, 160));
        tabItemStyle.Border = new SolidBrush(new Color(0, 0, 0, STANDARD_BORDER_ALPHA));
        tabItemStyle.BorderThickness = new Thickness(1);
        tabItemStyle.Padding = new Thickness(10, 2);

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
        //s.Background = new SolidBrush(TazUO_Orange);
        s.Background = _ninePatchButtonUp;
        s.OverBackground = _ninePatchButtonDown;
        s.PressedBackground = _ninePatchButtonDown;
        s.MinWidth = 1;
        s.MinHeight = 1;
        s.Padding = new Thickness(5);

        TextBoxStyle inputS = Stylesheet.Current.TextBoxStyle;
        inputS.Padding = new Thickness(3);


        //Checkbox style
        ImageTextButtonStyle cbStyle = Stylesheet.Current.CheckBoxStyle;
        cbStyle.ImageStyle.PressedImage = new TextureRegion(ModernUIConstants.ModernUICheckBoxChecked);
        cbStyle.ImageStyle.Image = new TextureRegion(ModernUIConstants.ModernUICheckBoxUnChecked);
        cbStyle.ImageStyle.Background = null;

        TextBoxStyle inputStyle = Stylesheet.Current.TextBoxStyle;
        inputStyle.Background = new SolidBrush(new Color(21, 21, 21, 75));
        inputStyle.Border = new SolidBrush(new Color(21, 21, 21, STANDARD_BORDER_ALPHA));
        inputStyle.BorderThickness = new Thickness(1);
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
        button.Background = _ninePatchButtonDangerUp;
        button.OverBackground = _ninePatchButtonDangerDown;
        button.PressedBackground = _ninePatchButtonDangerDown;

        return button;
    }
}
