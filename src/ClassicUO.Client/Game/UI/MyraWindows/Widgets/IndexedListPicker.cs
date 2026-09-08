#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// An extra widget for a picker row, and the width to reserve for it.
/// </summary>
/// <param name="Build">
///     Builds the widget appended after the add button - e.g. a preview button. Handed the picker it
///     will sit in, so an accessory acting on what the inputs currently hold needs no back-reference
///     assigned around construction.
/// </param>
/// <param name="Width">
///     What it takes horizontally, so the picked-items box below still matches the row's width. The
///     box is sized before the widget exists, so its width cannot be measured off it.
/// </param>
public readonly record struct PickerAccessory(Func<IndexedListPicker, Widget> Build, int Width);

/// <summary>
/// How an <see cref="IndexedListPicker" />'s two inputs are sized, and what its number field accepts.
/// </summary>
/// <param name="NumberWidth">Width for the raw-number field.</param>
/// <param name="NameWidth">Width for the name list.</param>
/// <param name="MinValue">Lower bound the number field accepts.</param>
/// <param name="MaxValue">Upper bound the number field accepts.</param>
public readonly record struct IndexedPickerLayout(
    int NumberWidth,
    int NameWidth,
    int MinValue = int.MinValue,
    int MaxValue = int.MaxValue
);

/// <summary>
/// Builds a set of chosen values from a searchable <see cref="IndexedComboPicker" />. A picked value
/// leaves the name list, so the search never offers a duplicate.
/// </summary>
public class IndexedListPicker : VerticalStackPanel
{
    #region Public events

    /// <summary>Raised whenever the picked set changes, from either button.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public accessors

    /// <summary>Every value currently picked.</summary>
    public int[] PickedItems => _picked.PickedItems;

    /// <summary>
    ///     What the picker's inputs currently hold - the candidate for the next add, not a picked value.
    ///     Read by an accessory acting on whatever is being looked at, such as a preview button.
    /// </summary>
    public int Value => _picker.Value;

    #endregion

    #region Private members

    private const int SPACING = PickedItemsController<int>.SPACING;

    private readonly List<(int Value, string Label)> _entries;
    private readonly Dictionary<int, string> _labels;

    private readonly PickedItemsController<int> _picked;
    private readonly IndexedComboPicker _picker;

    #endregion

    #region Ctor

    /// <summary>Builds the picker row and its picked-items box.</summary>
    /// <param name="value">The picker's starting value - not necessarily picked.</param>
    /// <param name="entries">Every known (value, label) pair to offer. Labels arrive display-ready
    /// (e.g. "755 - Earthquake").</param>
    /// <param name="layout">How the two inputs are sized, and what the number field accepts.</param>
    /// <param name="initialValues">Values already picked when the widget is built.</param>
    /// <param name="accessory">An extra widget for the picker row, or null for none.</param>
    public IndexedListPicker(
        int value,
        IEnumerable<(int Value, string Label)> entries,
        IndexedPickerLayout layout,
        IEnumerable<int>? initialValues = null,
        PickerAccessory? accessory = null
    )
    {
        Spacing = SPACING;

        _entries = [..entries];
        _labels = _entries.ToDictionary(entry => entry.Value, entry => entry.Label);

        _picker = new IndexedComboPicker(value, _entries, layout.MinValue, layout.MaxValue)
        {
            VerticalAlignment = VerticalAlignment.Center, NumberInput = { Width = layout.NumberWidth }, NameList = { Width = layout.NameWidth }
        };

        // Fixed to the picker row's width, not the fill column it sits in - a box spanning the whole
        // editor pane would strand its remove glyphs far from short labels. Two gaps to account for:
        // the picker's own between its inputs, this row's before the add button.
        int boxWidth = layout.NumberWidth
            + IndexedComboPicker.SPACING
            + layout.NameWidth
            + SPACING
            + PickedItemsController<int>.ADD_BUTTON_SIZE;

        if (accessory is { } extra)
            boxWidth += SPACING + extra.Width;

        _picked = new PickedItemsController<int>(boxWidth, LabelFor, OnAddClick);
        _picked.ItemsChanged += OnPickedItemsChanged;

        // Subscribed only now: the handler reads _picked.
        _picker.ValueChanged += OnPickerValueChanged;

        var pickerRow = new HorizontalStackPanel { Spacing = SPACING };
        pickerRow.Widgets.Add(_picker);
        pickerRow.Widgets.Add(_picked.AddButton);

        // Built last, and handed this picker: an accessory reading Value needs _picker in place.
        if (accessory is { } trailing)
            pickerRow.Widgets.Add(trailing.Build(this));

        Children.Add(pickerRow);
        Children.Add(_picked.Box);

        _picked.Seed(initialValues);
        _picked.SetCandidate(value);

        RefreshNameListOptions();
    }

    #endregion

    #region Private methods

    private void OnPickerValueChanged(object? sender, int value) => _picked.SetCandidate(value);

    /// <summary>Every change retrims the name list - a removed value becomes offerable again.</summary>
    private void OnPickedItemsChanged(object? sender, EventArgs e)
    {
        RefreshNameListOptions();

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddClick()
    {
        if (!_picked.Add(_picker.Value))
            return;

        // Re-enters through OnPickerValueChanged to re-read the add button. After the retrim above, so
        // the list it looks the cleared value up in is the current one.
        _picker.Value = 0;
    }

    private string LabelFor(int value) => _labels.GetValueOrDefault(value, value.ToString());

    /// <summary>Drops every picked value's name out of the search list. Rebuilt whole rather than
    /// trimmed: a few thousand entries, but only on an add or remove click.</summary>
    private void RefreshNameListOptions()
    {
        _picker.NameList.Items.Clear();

        foreach ((int entryValue, string label) in _entries)
        {
            if (!_picked.Contains(entryValue))
                _picker.NameList.Items.Add(label);
        }
    }

    #endregion
}
