using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Theme;
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

    /// <summary>
    /// Outline for grids and framed areas. Read through the palette on every access rather than
    /// captured once, so a theme change reaches everything rebuilt after it.
    /// </summary>
    public static Color GridBorderColor => MyraTheme.Current.PanelBorder;
    public static SpriteFontBase UiFont => _uiFont;

    public static int UiFontSize => ProfileManager.CurrentProfile == null ? 16 : ProfileManager.CurrentProfile.OptionsFontSize;
    public static SpriteFontBase GetUiFont(int sizeOffset) =>
        TrueTypeLoader.Instance.GetFont(ProfileManager.CurrentProfile == null ? EmbeddedFontNames.IBM_PLEX : ProfileManager.CurrentProfile.OptionsFont, UiFontSize + sizeOffset);

    private static Color TazUO_Orange = new (0.306f, 0.271f, 0.251f, 0.9f);

    private static SpriteFontBase _uiFont;
    private static NinePatchRegion _ninePatchPanel;
    public static NinePatchRegion NinePatchButtonUp;
    public static NinePatchRegion NinePatchButtonDown;
    private static NinePatchRegion _ninePatchButtonDangerUp;
    private static NinePatchRegion _ninePatchButtonDangerDown;
    private static TextureRegion _skillUpButton;
    private static TextureRegion _skillDownButton;
    private static TextureRegion _skillLockBtn;

    /// <summary>
    /// The generated radio marks, held as swappable images so a restyle can resize them in place - see
    /// <see cref="SwappableImage" /> for why handing the stylesheet new ones does not work. Built on
    /// the first <see cref="ApplyRadioStyle" /> and kept for the session.
    /// </summary>
    private static SwappableImage _radioMarkOff, _radioMarkOver, _radioMarkOn;

    /// <summary>
    /// Size the radio marks were generated at, so a restyle that does not change it is a no-op.
    /// 0 until the first <see cref="ApplyRadioStyle" />.
    /// </summary>
    private static int _radioMarkSize;

    /// <summary>
    /// Radio mark size for the default font size, matching the check box art it sits beside. Scaled
    /// from there so the mark keeps step with the text rather than shrinking against it.
    /// </summary>
    private const int RADIO_MARK_SIZE_AT_DEFAULT_FONT = 17;
    private const int DEFAULT_UI_FONT_SIZE = 16;
    private const int RADIO_MARK_MIN_SIZE = 10;

    /// <summary>
    /// Ceiling on the generated mark. Drawing is quadratic in the size, so the three faces together
    /// cost roughly 9ms at this size against well under a millisecond at the default - affordable
    /// because it is paid only on a style rebuild, but not worth letting grow unbounded. The three
    /// textures also sit in GPU memory for the session: 4 bytes a texel, so 48KiB at this size.
    /// </summary>
    private const int RADIO_MARK_MAX_SIZE = 64;

    public static void SetDefault()
    {
        _ninePatchPanel = new NinePatchRegion(
            ModernUIConstants.ModernUIPanel,
            ModernUIConstants.ModernUIPanel.Bounds,
            new Thickness(ModernUIConstants.ModernUIPanel_BorderSize)
        );
        NinePatchButtonUp = new NinePatchRegion(
            ModernUIConstants.ModernUIButtonUp,
            ModernUIConstants.ModernUIButtonUp.Bounds,
            new Thickness(ModernUIConstants.ModernUIButton_BorderSize)
        );
        NinePatchButtonDown = new NinePatchRegion(
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

        _uiFont = TrueTypeLoader.Instance.GetFont(ProfileManager.CurrentProfile == null ? EmbeddedFontNames.IBM_PLEX : ProfileManager.CurrentProfile.OptionsFont, UiFontSize);

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
        tabItemStyle.OverBackground = new SolidBrush(new Color(129, 120, 115, 150)); // lighter hover tone, matches combo/menu hover; selected uses the darker TazUO_Orange
        tabItemStyle.PressedBackground = new SolidBrush(TazUO_Orange);
        tabItemStyle.Border = new SolidBrush(new Color(0, 0, 0, STANDARD_BORDER_ALPHA));
        tabItemStyle.BorderThickness = new Thickness(1, 1, 1, 0); // remove bottom border to avoid overlap
        tabItemStyle.Margin = new Thickness(1, 0);
        tabItemStyle.Padding = new Thickness(10, 2);

        //HSlider
        SliderStyle sStyle = Stylesheet.Current.HorizontalSliderStyle;
        sStyle.Background = new SolidBrush(new Color(50, 49, 56, 50));
        sStyle.OverBackground = new SolidBrush(new Color(50, 49, 56, 150));

        Color sliderMainColor = new(0.506f, 0.471f, 0.451f, 0.9f);
        sStyle.KnobStyle.ImageStyle.Background = new SolidBrush(sliderMainColor);
        sStyle.KnobStyle.ImageStyle.OverBackground = new SolidBrush(sliderMainColor);
        sStyle.KnobStyle.ImageStyle.FocusedBackground = new SolidBrush(sliderMainColor);
        sStyle.KnobStyle.ImageStyle.PressedImage = null;
        sStyle.KnobStyle.ImageStyle.Image = null;
        sStyle.KnobStyle.ImageStyle.Height = 30;
        sStyle.Width = 175;
        sStyle.Height = 30;

        //Button
        ButtonStyle s = Stylesheet.Current.ButtonStyle;
        //s.Background = new SolidBrush(TazUO_Orange);
        s.Background = NinePatchButtonUp;
        s.OverBackground = NinePatchButtonDown;
        s.PressedBackground = NinePatchButtonDown;
        s.MinWidth = 1;
        s.MinHeight = 1;
        s.Padding = new Thickness(5);
        s.LabelStyle.Font = _uiFont;

        //Checkbox style
        ImageTextButtonStyle cbStyle = Stylesheet.Current.CheckBoxStyle;
        cbStyle.ImageStyle.PressedImage = new TextureRegion(ModernUIConstants.ModernUICheckBoxChecked);
        cbStyle.ImageStyle.Image = new TextureRegion(ModernUIConstants.ModernUICheckBoxUnChecked);
        cbStyle.ImageStyle.Background = null;

        ApplyRadioStyle();

        TextBoxStyle inputStyle = Stylesheet.Current.TextBoxStyle;
        inputStyle.Background = new SolidBrush(new Color(21, 21, 21, 75));
        inputStyle.Border = new SolidBrush(new Color(21, 21, 21, STANDARD_BORDER_ALPHA));
        inputStyle.BorderThickness = new Thickness(1);
        inputStyle.Padding = new Thickness(3);
        inputStyle.Font = _uiFont;

        ScrollViewerStyle svStyle = Stylesheet.Current.ScrollViewerStyle;
        svStyle.VerticalScrollBackground = new NinePatchRegion(ModernUIConstants.ModernUIVerticalScrollbar, ModernUIConstants.ModernUIVerticalScrollbar.Bounds, new Thickness(1));

        svStyle.VerticalScrollKnob = new TextureRegion(ModernUIConstants.ModernUIVerticalScrollbarKnob);

        svStyle.HorizontalScrollBackground = new NinePatchRegion(ModernUIConstants.ModernUIHorizontalScrollbar, ModernUIConstants.ModernUIHorizontalScrollbar.Bounds, new Thickness(1));
        svStyle.HorizontalScrollKnob = new TextureRegion(ModernUIConstants.ModernUIHorizontalScrollbarKnob);

        ComboBoxStyle comboStyle = Stylesheet.Current.ComboBoxStyle;
        comboStyle.Padding = new Thickness(3);
        comboStyle.Background = new SolidBrush(new Color(21, 21, 21, 75));
        comboStyle.OverBackground = new SolidBrush(new Color(0.506f, 0.471f, 0.451f, 0.9f));
        comboStyle.ListBoxStyle.Background = new SolidBrush(TazUO_Orange);
        comboStyle.LabelStyle.Font = _uiFont;

        ImageTextButtonStyle comboItemStyle = comboStyle.ListBoxStyle.ListItemStyle;
        comboItemStyle.Background = new SolidBrush(Color.Transparent);
        comboItemStyle.OverBackground = new SolidBrush(new Color(0.306f, 0.271f, 0.251f, 0.7f));
        comboItemStyle.PressedBackground = new SolidBrush(new Color(0.506f, 0.471f, 0.451f, 0.9f));

        comboItemStyle.Padding = new Thickness(2);
        comboItemStyle.LabelStyle.Font = _uiFont;

        // Drives PropertyGrid as well as Tree; without this its labels keep the Myra default font
        // and sit at a different size from every other label in the options window.
        TreeStyle treeStyle = Stylesheet.Current.TreeStyle;
        treeStyle.LabelStyle ??= new LabelStyle();
        treeStyle.LabelStyle.Font = _uiFont;
        treeStyle.SelectionBackground = new SolidBrush(TazUO_Orange);
        treeStyle.SelectionHoverBackground = new SolidBrush(new Color(129, 120, 115, 150));

        MenuStyle menuStyle = Stylesheet.Current.VerticalMenuStyle;
        menuStyle.Padding = new Thickness(0);
        menuStyle.Margin = new Thickness(0);
        menuStyle.Background = new SolidBrush(TazUO_Orange);
        menuStyle.Border = new SolidBrush(new Color(11, 11, 11, 230));
        menuStyle.SelectionBackground = new SolidBrush(new Color(0.306f, 0.271f, 0.251f, 0.9f));
        menuStyle.SelectionHoverBackground = new SolidBrush(new Color(0.506f, 0.471f, 0.451f, 0.9f));
        menuStyle.LabelStyle.Font = _uiFont;
        menuStyle.LabelStyle.Margin = new Thickness(2);

        // Last: it fills in gaps the styles above leave, so anything they set for themselves stands.
        ApplyDisabledStyling();
    }

    /// <summary>
    /// Fills in the disabled half of every style the options UI uses.
    /// <para>
    /// Myra already draws these - <c>Widget.GetCurrentBackground</c> reaches for
    /// <c>DisabledBackground</c> and <c>Label</c> for <c>DisabledTextColor</c> - but the default
    /// stylesheet leaves both null, so a disabled check box, button or input looks exactly like an
    /// enabled one and merely refuses to respond. Setting them here is what makes
    /// <c>Enabled = false</c> visible, and it costs nothing at draw time.
    /// </para>
    /// <para>
    /// Worse than merely identical wherever a style defines a hover brush: the disabled branch is
    /// skipped entirely when <c>DisabledBackground</c> is null, and the next one tested is the hover
    /// brush - which <c>IsMouseInside</c> reports regardless of <c>Enabled</c>. A dead control then
    /// lights up under the pointer, looking live while refusing every input.
    /// </para>
    /// <para>
    /// Runs last, so a style that sets its own disabled brush above keeps it. Every style and
    /// sub-style is null-checked: the default stylesheet leaves several of them unset - the tree's
    /// label style among them - and this runs during content load, where a null reference is a
    /// startup crash rather than a missing tint.
    /// </para>
    /// </summary>
    private static void ApplyDisabledStyling()
    {
        MyraPalette palette = MyraTheme.Current;
        Stylesheet sheet = Stylesheet.Current;

        if (sheet == null)
            return;

        var disabledFill = new SolidBrush(palette.DisabledFill);

        ApplyDisabledText(sheet.LabelStyle, palette);

        // The caption of a button or a check box is a label of its own, held on that button's style
        // rather than on the shared one - so each has to be told separately.
        ApplyDisabled(sheet.ButtonStyle, disabledFill, palette);
        ApplyDisabled(sheet.CheckBoxStyle, disabledFill, palette);
        ApplyDisabled(sheet.RadioButtonStyle, disabledFill, palette);
        ApplyDisabled(sheet.ComboBoxStyle, disabledFill, palette);

        if (sheet.TextBoxStyle is { } textBoxStyle)
        {
            textBoxStyle.DisabledBackground ??= disabledFill;
            textBoxStyle.DisabledTextColor ??= palette.DisabledText;
        }

        if (sheet.HorizontalSliderStyle is { } sliderStyle)
        {
            sliderStyle.DisabledBackground ??= disabledFill;

            // The knob's fill sits on its image rather than on the button around it, so a disabled
            // brush on the button alone would never be the one drawn.
            if (sliderStyle.KnobStyle?.ImageStyle is { } knobImage)
                knobImage.DisabledBackground ??= disabledFill;
        }

        if (sheet.TreeStyle is { } treeStyle)
        {
            treeStyle.LabelStyle ??= new LabelStyle();
            ApplyDisabledText(treeStyle.LabelStyle, palette);
        }
    }

    /// <summary>Gives a button-shaped style its disabled backing and caption colour.</summary>
    /// <param name="style">The style, or null where the sheet does not define one.</param>
    /// <param name="disabledFill">Backing for a control that cannot be used.</param>
    /// <param name="palette">The palette in force.</param>
    private static void ApplyDisabled(ButtonStyle style, IBrush disabledFill, MyraPalette palette)
    {
        if (style == null)
            return;

        style.DisabledBackground ??= disabledFill;

        ApplyDisabledText(style.LabelStyle, palette);
    }

    /// <summary>Gives a label style its disabled text colour, if it has none and there is one to
    /// give.</summary>
    /// <param name="style">The style, or null.</param>
    /// <param name="palette">The palette in force.</param>
    private static void ApplyDisabledText(LabelStyle style, MyraPalette palette)
    {
        if (style != null)
            style.DisabledTextColor ??= palette.DisabledText;
    }


    /// <summary>
    /// Builds the radio button style. Left on Myra's default this draws a bare tick that reads as a
    /// bullet point, saying nothing about the options being exclusive.
    /// </summary>
    /// <remarks>
    /// The marks are generated at the current font's size rather than loaded as art, so they stay
    /// crisp at any UI scale; see <see cref="RadioMark" />. A resize reaches the radios already on
    /// screen because the stylesheet holds <see cref="SwappableImage" />s, not the textures.
    /// </remarks>
    private static void ApplyRadioStyle()
    {
        ImageTextButtonStyle rbStyle = Stylesheet.Current.RadioButtonStyle;

        if (rbStyle?.ImageStyle == null || Client.Game?.GraphicsDevice == null)
            return;

        rbStyle.ImageStyle.Background = null;
        rbStyle.LabelStyle.Font = _uiFont;

        int size = Math.Clamp(
            (int)MathF.Round(RADIO_MARK_SIZE_AT_DEFAULT_FONT * (UiFontSize / (float)DEFAULT_UI_FONT_SIZE)),
            RADIO_MARK_MIN_SIZE,
            RADIO_MARK_MAX_SIZE
        );

        // A re-style for another reason - a font family change, a profile load - leaves the marks at the
        // size they are already drawn at, so there is nothing to regenerate.
        if (size == _radioMarkSize)
            return;

        _radioMarkSize = size;

        if (_radioMarkOff == null)
        {
            _radioMarkOff = RadioMarkImage(RadioMarkState.Off);
            _radioMarkOver = RadioMarkImage(RadioMarkState.Over);
            _radioMarkOn = RadioMarkImage(RadioMarkState.On);

            rbStyle.ImageStyle.Image = _radioMarkOff;
            rbStyle.ImageStyle.OverImage = _radioMarkOver;
            rbStyle.ImageStyle.PressedImage = _radioMarkOn;

            return;
        }

        _radioMarkOff.Regenerate();
        _radioMarkOver.Regenerate();
        _radioMarkOn.Regenerate();
    }

    /// <summary>
    /// A mark that redraws itself at whatever <see cref="_radioMarkSize" /> holds when asked, so a
    /// resize does not have to reach the radios already built from it.
    /// </summary>
    /// <param name="state">Which of the mark's three looks to draw.</param>
    private static SwappableImage RadioMarkImage(RadioMarkState state) =>
        new(() => RadioMark.Create(Client.Game.GraphicsDevice, _radioMarkSize, state));

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

    /// <summary>
    /// The combo box skin only gives ListBoxStyle a background, not a border (unlike the
    /// menu skin), so a SearchableComboBox's popup - which reads like a menu - looks
    /// unstyled next to one. Give it the same border as VerticalMenuStyle.
    /// </summary>
    public static void ApplySearchComboBoxPopupBorder<T>(SearchableComboBox<T> combo)
    {
        combo.PopupBorder = new SolidBrush(TazUO_Orange);
        combo.PopupBorderThickness = new Thickness(1);
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
