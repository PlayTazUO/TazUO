using Microsoft.Xna.Framework;

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

    public const int TOOLBAR_BUTTON_SIZE = 28;
}
