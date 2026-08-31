#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// The picked-set half of a multi-value picker: the chosen values, the rows showing them, the box
/// they sit in, and the add button's enabled state. A picker supplies only how a value is entered and
/// how it reads; everything after "the user chose this" is the same either way.
/// <para>
/// The add button is owned here rather than by the picker because its enabled state has to be re-read
/// after every change to the set, not only after typing - removing the value currently in the input
/// makes it addable again.
/// </para>
/// </summary>
/// <typeparam name="TValue">The picked value - an index, a serial.</typeparam>
public sealed class PickedItemsController<TValue> where TValue : notnull
{
    #region Public events

    /// <summary>Raised whenever the picked set changes. Not raised while seeding.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public constants

    /// <summary>Gap between a picker's own widgets, so a caller laying out the row above the box
    /// matches what the rows inside it use.</summary>
    public const int SPACING = 4;

    /// <summary>Width the add button takes in the picker row. Exposed because a caller has to size the
    /// box to that row before the controller - and so the button - exists.</summary>
    public const int ADD_BUTTON_SIZE = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE;

    #endregion

    #region Public accessors

    /// <summary>The box the picked rows are shown in. Add it to the picker's own layout.</summary>
    public PickedItemsBox Box { get; }

    /// <summary>The add button. Enabled only while the current candidate can actually be added.</summary>
    public IconButton AddButton { get; }

    /// <summary>Every value currently picked.</summary>
    public TValue[] PickedItems => _rows.Keys.ToArray();

    #endregion

    #region Private members

    private const string REMOVE_GLYPH = "\U0001F5D9";
    private const int REMOVE_BUTTON_SIZE = 22;
    private const int REMOVE_GLYPH_SIZE = 20;
    private const int ADD_GLYPH_SIZE = 34;

    /// <summary>The metrics <see cref="IconButton" /> reads off the font are close but not exact for
    /// this glyph at this size.</summary>
    private static readonly Point _removeGlyphNudge = new(0, -1);

    private readonly Dictionary<TValue, Widget> _rows = [];

    private readonly Func<TValue, string> _labelFor;
    private readonly Func<TValue, bool>? _isAddable;

    /// <summary>Whatever the picker's input currently holds, kept so a change to the set can re-read
    /// the add button without the picker having to remember to say what is in the box. Flagged rather
    /// than compared against null, which says nothing useful for a value type.</summary>
    private TValue _candidate = default!;

    private bool _hasCandidate;

    #endregion

    #region Ctor

    /// <param name="boxWidth">Fixed width for the picked-items box, matching the picker row above it.</param>
    /// <param name="labelFor">How a picked value reads on its row.</param>
    /// <param name="onAddRequested">Invoked when the add button is clicked.</param>
    /// <param name="isAddable">Rejects a value the picker considers no value at all - serial zero,
    /// say. Anything not already picked is addable if omitted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="labelFor" /> or
    /// <paramref name="onAddRequested" /> is null.</exception>
    public PickedItemsController(
        int boxWidth,
        Func<TValue, string> labelFor,
        Action onAddRequested,
        Func<TValue, bool>? isAddable = null
    )
    {
        ArgumentNullException.ThrowIfNull(labelFor);
        ArgumentNullException.ThrowIfNull(onAddRequested);

        _labelFor = labelFor;
        _isAddable = isAddable;

        Box = new PickedItemsBox(boxWidth);
        AddButton = new IconButton("+", onAddRequested, size: ADD_BUTTON_SIZE, glyphSize: ADD_GLYPH_SIZE) { Enabled = false };
    }

    #endregion

    #region Public methods

    /// <summary>Whether <paramref name="value" /> is already picked.</summary>
    public bool Contains(TValue value) => _rows.ContainsKey(value);

    /// <summary>Fills the set in without raising <see cref="ItemsChanged" />, for construction time
    /// where the values came from the config being edited rather than from the user.</summary>
    /// <param name="values">Values to pick. Null is treated as none.</param>
    public void Seed(IEnumerable<TValue>? values)
    {
        foreach (TValue value in values ?? [])
            AddRow(value);

        RefreshAddButton();
    }

    /// <summary>Picks <paramref name="value" />, unless it is already picked or not addable.</summary>
    /// <returns>Whether the set changed.</returns>
    public bool Add(TValue value)
    {
        if (!AddRow(value))
            return false;

        Changed();

        return true;
    }

    /// <summary>Un-picks <paramref name="value" />.</summary>
    /// <returns>Whether the set changed.</returns>
    public bool Remove(TValue value)
    {
        if (!_rows.Remove(value, out Widget? row))
            return false;

        Box.RemoveRow(row);
        Changed();

        return true;
    }

    /// <summary>
    /// Tells the controller what the picker's input now holds, so the add button can answer for it.
    /// Call on every change to that input.
    /// </summary>
    public void SetCandidate(TValue candidate)
    {
        _candidate = candidate;
        _hasCandidate = true;

        RefreshAddButton();
    }

    #endregion

    #region Private methods

    private bool CanAdd(TValue value) => !_rows.ContainsKey(value) && (_isAddable == null || _isAddable(value));

    private bool AddRow(TValue value)
    {
        if (!CanAdd(value))
            return false;

        var label = new MyraLabel(_labelFor(value), MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center };

        var remove = new IconButton(
            REMOVE_GLYPH,
            () => Remove(value),
            size: REMOVE_BUTTON_SIZE,
            glyphSize: REMOVE_GLYPH_SIZE
        ) { Nudge = _removeGlyphNudge };

        var row = new SpaceBetweenRow(label, remove, SPACING);

        _rows.Add(value, row);
        Box.AddRow(row);

        return true;
    }

    private void Changed()
    {
        RefreshAddButton();

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshAddButton() => AddButton.Enabled = _hasCandidate && CanAdd(_candidate);

    #endregion
}
