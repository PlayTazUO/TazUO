#nullable enable

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Theme;
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

    private const int GLYPH_FONT_SIZE = StyleConstantsDefaults.RESET_ICON_FONT_SIZE;

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

        // Every LocalizedDisplayName/LocalizedDescription in the grid resolves through here.
        // Per-grid rather than global: only the screens built on this pay for it, and TazLang's
        // key-plus-fallback shape is exactly what the hook asks for.
        Localizer = TazLang.Get;

        // Hovering a parameter and scrolling the panel would otherwise edit it. These grids are
        // long enough to need scrolling and dense enough that the pointer is nearly always over
        // something.
        MouseWheelRequiresFocusOnEditors = true;

        if (pristine == null)
            return;

        DefaultValueProvider = DefaultValueOf;

        // With a default to compare against, a parameter can say whether it has been touched: the
        // reset button goes dead where there is nothing to undo, and the name is tinted where there
        // is. Between them they answer "what have I actually changed here", which a grid of thirty
        // five knobs otherwise cannot.
        ResetOnlyWhenModified = true;
        ModifiedNameColor = MyraTheme.Current.ModifiedValue;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The grid's own reset button is drawn from the skin's tree glyphs. Supplied from here instead,
    /// out of the symbol font the rest of the UI uses.
    /// <para>
    /// Built through the shared icon-button factory rather than assembled here, so it carries the
    /// same zeroed padding and margins as every other glyph button. <see cref="MyraLabel.Symbol"/>
    /// is the wrong tool for this: its nudge exists to seat a glyph on a line of text, and inside a
    /// fixed-size button - where the label is explicitly sized and centred - it only pushes the icon
    /// low.
    /// </para>
    /// </summary>
    private static Widget CreateResetButton(Record record, Action reset)
    {
        var button = new IconButton(
            StyleConstantsDefaults.RESET_LABEL_ICON_TEXT,
            reset,
            TazLang.Get("visualeffects_resettodefault", "Reset to default"),
            GLYPH_BUTTON_SIZE,
            GLYPH_FONT_SIZE
        )
        {
            Margin = new Thickness(RESET_BUTTON_GAP, 0, 0, 0)
        };

        return button;
    }

    private static Widget CreateExpanderMark(bool expanded) =>
        MyraLabel.Symbol(expanded ? "⮟" : "⮞", GLYPH_FONT_SIZE);

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
