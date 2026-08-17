using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;

using ClassicUO.Game.UI.MyraWindows.Theme;

namespace ClassicUO.Game.UI.MyraWindows;

public static class StyleConstantsDefaults
{
    public static readonly Color ModernUiCorpus = new(38, 43, 68, 255);
    public static readonly Color ModernUiBorderDark = new(24, 20, 37, 255);
    public static readonly Color ModernUiBorderLight = new(58, 68, 102, 255);

    public const int WINDOW_MIN_WIDTH = 200;
    public const int WINDOW_MIN_HEIGHT = 200;
    public const int WINDOW_MAX_WIDTH = 1200;
    public const int WINDOW_MAX_HEIGHT = 1200;

    #region Resize Handle

    public const int RESIZE_HANDLE_FONT_SIZE = 20;
    public const string BOTTOM_RIGHT_HANDLE_TEXT = "🭿";
    public const string TOP_RIGHT_HANDLE_TEXT = "🭾";
    public const string TOP_LEFT_HANDLE_TEXT = "🭽";
    public const string BOTTOM_LEFT_HANDLE_TEXT = "🭼";

    #endregion

    /// <summary>
    /// A standard icon for 'reset' type operations.
    /// Must be used with a supported font such as <see cref="ClassicUO.Assets.EmbeddedFontNames.NOTO_SANS_2_SYMBOLS"/>
    /// </summary>
    public const string RESET_LABEL_ICON_TEXT = "⭯";

    /// <summary>
    /// Point size the reset glyph is drawn at inside a <see cref="TOOLBAR_BUTTON_SIZE"/> button.
    /// </summary>
    public const int RESET_ICON_FONT_SIZE = 24;

    public const int TOOLBAR_BUTTON_SIZE = 28;

    #region Inputs

    public const int NUMERIC_INPUT_BOX_WIDTH = 80;

    #endregion

    #region Containers

    /// <summary>
    /// Fill and outline of a framed area. Properties rather than fields: a field would be built once
    /// from whichever palette happened to be current at type load, and would go on drawing that one
    /// after a theme change.
    /// </summary>
    public static IBrush BorderBackgroundBrush => new SolidBrush(MyraTheme.Current.PanelFill);

    /// <inheritdoc cref="BorderBackgroundBrush" />
    public static IBrush BorderLineBrush => new SolidBrush(MyraTheme.Current.PanelBorder);
    public static readonly Thickness BorderThickness = new(2);

    #endregion
}
