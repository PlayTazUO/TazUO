using ClassicUO.Assets;
using ClassicUO.Game.Data;
using FontStashSharp;
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

    private static SpriteFontBase _uiFont;
    private static NinePatchRegion _ninePatchPanel;
    private static NinePatchRegion _ninePatchButtonUp;
    private static NinePatchRegion _ninePatchButtonDown;
    private static NinePatchRegion _ninePatchButtonDangerUp;
    private static NinePatchRegion _ninePatchButtonDangerDown;
    private static TextureRegion _skillUpButton;
    private static TextureRegion _skillDownButton;
    private static TextureRegion _skillLockBtn;

    public static void SetDefault()
    {
        _ninePatchPanel = new NinePatchRegion(
            ModernUIConstants.ModernUIPanel,
            ModernUIConstants.ModernUIPanel.Bounds,
            new Thickness(ModernUIConstants.ModernUIPanel_BorderSize)
        );
        _ninePatchButtonUp = new NinePatchRegion(
            ModernUIConstants.ModernUIButtonUp,
            ModernUIConstants.ModernUIButtonUp.Bounds,
            new Thickness(ModernUIConstants.ModernUIButton_BorderSize)
        );
        _ninePatchButtonDown = new NinePatchRegion(
            ModernUIConstants.ModernUIButtonDown,
            ModernUIConstants.ModernUIButtonUp.Bounds,
            new Thickness(ModernUIConstants.ModernUIButton_BorderSize)
        );
        _ninePatchButtonDangerUp = new NinePatchRegion(
            ModernUIConstants.ModernUIButtonDangerUp,
            ModernUIConstants.ModernUIButtonDangerUp.Bounds,
            new Thickness(ModernUIConstants.ModernUIButton_BorderSize)
        );
        _ninePatchButtonDangerDown = new NinePatchRegion(
            ModernUIConstants.ModernUIButtonDangerDown,
            ModernUIConstants.ModernUIButtonDangerUp.Bounds,
            new Thickness(ModernUIConstants.ModernUIButton_BorderSize)
        );

        _skillUpButton = new TextureRegion(ModernUIConstants.ModernUISkillUp);
        _skillDownButton = new TextureRegion(ModernUIConstants.ModernUISkillDown);
        _skillLockBtn = new TextureRegion(ModernUIConstants.ModernUISkillLock);

        _uiFont = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.IBM_PLEX, 16);

        //Window style
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = _ninePatchPanel;
        style.Padding = new Thickness(6);
        style.TitleStyle.Padding = new Thickness(3);
        style.TitleStyle.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.IBM_PLEX, 18);

        //Labels
        Stylesheet.Current.LabelStyle.Font = _uiFont;

        //Tabs
        TabControlStyle tabControlStyle = Stylesheet.Current.TabControlStyle;
        tabControlStyle.ContentStyle ??= new WidgetStyle();
        tabControlStyle.ContentStyle.Background = new SolidBrush(Color.Transparent);
        tabControlStyle.ContentStyle.Border = new SolidBrush(new Color(0, 0, 0, STANDARD_BORDER_ALPHA));
        tabControlStyle.ContentStyle.BorderThickness = new Thickness(1);
        tabControlStyle.TabItemStyle.LabelStyle.Font = _uiFont;

        ImageTextButtonStyle tabItemStyle = tabControlStyle.TabItemStyle;
        tabItemStyle.Background = new SolidBrush(Color.Transparent);
        tabItemStyle.OverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        tabItemStyle.PressedBackground = new SolidBrush(new Color(170, 105, 13, 160));
        tabItemStyle.Border = new SolidBrush(new Color(0, 0, 0, STANDARD_BORDER_ALPHA));
        tabItemStyle.BorderThickness = new Thickness(1, 1, 1, 0); // remove bottom border to avoid overlap
        tabItemStyle.Margin = new Thickness(1, 0);
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
        s.LabelStyle.Font = _uiFont;

        //Checkbox style
        ImageTextButtonStyle cbStyle = Stylesheet.Current.CheckBoxStyle;
        cbStyle.ImageStyle.PressedImage = new TextureRegion(ModernUIConstants.ModernUICheckBoxChecked);
        cbStyle.ImageStyle.Image = new TextureRegion(ModernUIConstants.ModernUICheckBoxUnChecked);
        cbStyle.ImageStyle.Background = null;

        TextBoxStyle inputStyle = Stylesheet.Current.TextBoxStyle;
        inputStyle.Background = new SolidBrush(new Color(21, 21, 21, 75));
        inputStyle.Border = new SolidBrush(new Color(21, 21, 21, STANDARD_BORDER_ALPHA));
        inputStyle.BorderThickness = new Thickness(1);
        inputStyle.Padding = new Thickness(3);
        inputStyle.Font = _uiFont;

        ScrollViewerStyle svStyle = Stylesheet.Current.ScrollViewerStyle;
        svStyle.VerticalScrollBackground = new TextureRegion(ModernUIConstants.ModernUIVerticalScrollbar);
        svStyle.VerticalScrollKnob = new TextureRegion(ModernUIConstants.ModernUIVerticalScrollbarKnob);

        svStyle.HorizontalScrollBackground = new TextureRegion(ModernUIConstants.ModernUIHorizontalScrollbar);
        svStyle.HorizontalScrollKnob = new TextureRegion(ModernUIConstants.ModernUIHorizontalScrollbarKnob);

        ComboBoxStyle comboStyle = Stylesheet.Current.ComboBoxStyle;
        comboStyle.Padding = new Thickness(3);
        comboStyle.Background = new SolidBrush(new Color(21, 21, 21, 75));
        comboStyle.OverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        comboStyle.ListBoxStyle.Background = new SolidBrush("#242941");
        comboStyle.LabelStyle.Font = _uiFont;

        ImageTextButtonStyle comboItemStyle = comboStyle.ListBoxStyle.ListItemStyle;
        comboItemStyle.Background = new SolidBrush(Color.Transparent);
        comboItemStyle.OverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        comboItemStyle.PressedBackground = new SolidBrush(new Color(170, 105, 13, 160));

        comboItemStyle.Padding = new Thickness(2);
        comboItemStyle.LabelStyle.Font = _uiFont;

        MenuStyle menuStyle = Stylesheet.Current.VerticalMenuStyle;
        menuStyle.Padding = new Thickness(0);
        menuStyle.Margin = new Thickness(0);
        menuStyle.Background = new SolidBrush("#242941");
        menuStyle.Border = new SolidBrush(TazUO_Orange);
        menuStyle.SelectionBackground = new SolidBrush(new Color(170, 105, 13, 160));
        menuStyle.SelectionHoverBackground = new SolidBrush(new Color(170, 105, 13, 80));
        menuStyle.LabelStyle.Font = _uiFont;
        menuStyle.LabelStyle.Margin = new Thickness(2);
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
        grid.Background = new SolidBrush(new Color(0, 0, 0, 25));
        grid.ColumnSpacing = 4;
        grid.RowSpacing = 1;
    }

    public static Button ApplyButtonDangerStyle(Button button)
    {
        button.Background = _ninePatchButtonDangerUp;
        button.OverBackground = _ninePatchButtonDangerDown;
        button.PressedBackground = _ninePatchButtonDangerDown;

        return button;
    }

    public static Button ApplySkillButtonStyle(Button button, Lock skillLock)
    {
        var img = new Image()
        {
            Renderable = skillLock switch
            {
                Lock.Up => _skillUpButton,
                Lock.Down => _skillDownButton,
                Lock.Locked => _skillLockBtn,
                _ => _skillLockBtn,
            },
        };

        button.Content = img;
        button.HorizontalAlignment = HorizontalAlignment.Center;
        return button;
    }
}
