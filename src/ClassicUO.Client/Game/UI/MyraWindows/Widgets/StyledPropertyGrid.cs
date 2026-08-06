#nullable enable

using System;
using ClassicUO.Configuration;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A <see cref="PropertyGrid"/> dressed to match the rest of the options UI: the skin's 8px tree
/// glyphs replaced with the symbol font everything else uses, and the spacing the option panels are
/// laid out on.
/// <para>
/// Reset buttons need somewhere to reset <em>to</em>. Rather than each caller duplicating the walk,
/// this takes a factory for a pristine instance of whatever is being edited and follows the grid's
/// own record path down it, so every parameter gets its default from the type that declares it
/// instead of from a table of constants kept in step by hand.
/// </para>
/// </summary>
internal sealed class StyledPropertyGrid : PropertyGrid
{
    #region Private members

    private const int ROW_SPACING = 12;
    private const int COLUMN_SPACING = 12;
    private const int GROUP_SPACING = 10;

    private const int GLYPH_BUTTON_SIZE = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE;

    /// <summary>Extra gap between an editor and its reset button, on top of the row's own spacing.</summary>
    private const int RESET_BUTTON_GAP = 2;

    private static readonly int _glyphFontSize = MyraLabel.SymbolFontSize;

    private readonly Func<object?>? _pristine;

    #endregion

    #region Ctor

    /// <summary>
    /// Builds a styled grid.
    /// </summary>
    /// <param name="pristine">Supplies an untouched instance of the edited type, for the reset
    /// buttons. Null hides them, for a grid whose defaults are not knowable.</param>
    public StyledPropertyGrid(Func<object?>? pristine = null)
    {
        _pristine = pristine;

        IgnoreCollections = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        RowSpacing = ROW_SPACING;
        ColumnSpacing = COLUMN_SPACING;
        GroupSpacing = GROUP_SPACING;
        GroupSeparators = true;
        ToggleGroupsOnSingleClick = true;
        ResetButtonFactory = CreateResetButton;
        MarkContentFactory = CreateExpanderMark;

        if (pristine != null)
            DefaultValueProvider = DefaultValueOf;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The grid's own reset button is drawn from the skin's tree glyphs. Supplied from here instead,
    /// out of the symbol font the rest of the UI uses.
    /// </summary>
    private static Widget CreateResetButton(Record record, Action reset)
    {
        MyraLabel glyph = MyraLabel.Symbol(StyleConstantsDefaults.RESET_LABEL_ICON_TEXT, _glyphFontSize);

        // Sized to fill its button, whose own padding is what keeps the glyph centred.
        glyph.Width = GLYPH_BUTTON_SIZE;
        glyph.Height = GLYPH_BUTTON_SIZE;

        var button = new Button
        {
            Width = GLYPH_BUTTON_SIZE,
            Height = GLYPH_BUTTON_SIZE,
            Tooltip = TazLang.Get("visualeffects_resettodefault", "Reset to default"),
            Content = glyph,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(RESET_BUTTON_GAP, 0, 0, 0)
        };

        button.Click += (_, _) => reset();

        return button;
    }

    private static Widget CreateExpanderMark(bool expanded) =>
        MyraLabel.Symbol(expanded ? "⮟" : "⮞", _glyphFontSize);

    /// <summary>
    /// Walks the pristine instance down the same path the grid is showing.
    /// </summary>
    /// <param name="grid">The grid asking, which carries the path in its parent records.</param>
    /// <param name="record">The property being reset.</param>
    /// <returns>Its default value, or null where the path does not resolve.</returns>
    private object? DefaultValueOf(PropertyGrid grid, Record record)
    {
        object? owner = _pristine?.Invoke();

        if (owner == null)
            return null;

        foreach (Record step in grid.ParentRecords)
        {
            owner = step.GetValue(owner);

            if (owner == null)
                return null;
        }

        return record.GetValue(owner);
    }

    #endregion
}
