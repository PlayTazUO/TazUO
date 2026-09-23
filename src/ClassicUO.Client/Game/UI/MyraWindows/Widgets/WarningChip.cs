#nullable enable

using ClassicUO.Game.UI.MyraWindows.Theme;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     A warning glyph that says what it is warning about only on hover, for sitting beside a control whose
///     caveat is worth flagging but not worth a permanent line of body text. Built with no message it is
///     silent decoration, so give it one.
/// </summary>
/// <remarks>
///     Takes its colour from the theme at construction, so one built before a theme change keeps the old
///     palette until whatever holds it is rebuilt.
/// </remarks>
public sealed class WarningChip : MyraLabel
{
    /// <param name="tooltipMessage">Text shown when the chip is hovered.</param>
    public WarningChip(string? tooltipMessage) : base(StyleConstantsDefaults.WarningLabelIconText, StyleConstantsDefaults.WarningIconFontSize)
    {
        ApplySymbolStyle(StyleConstantsDefaults.WarningIconFontSize, MyraTheme.Current.Notice);
        Tooltip = tooltipMessage;
    }
}
