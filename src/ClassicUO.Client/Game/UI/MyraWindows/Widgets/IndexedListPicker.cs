#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Builds a set of chosen values from a searchable <see cref="IndexedComboPicker" />: pick one, add
/// it, and it drops into a boxed list below, each row carrying its own remove glyph. A value already
/// picked drops out of the name list, so the search never offers a duplicate.
/// </summary>
public class IndexedListPicker : VerticalStackPanel
{
    #region Public events

    /// <summary>Raised whenever the picked set changes, from either button.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public accessors

    /// <summary>Every value currently picked.</summary>
    public int[] PickedItems => _pickedItemRows.Keys.ToArray();

    #endregion

    #region Private members

    private const string REMOVE_GLYPH = "\U0001F5D9";
    private const int SMALL_BUTTON_SIZE = 22;
    private const int SMALL_GLYPH_SIZE = 20;

    /// <summary>The glyph's ink sits a little low and right of centre at this size - the metrics
    /// <see cref="IconButton" /> reads off the font are close but not exact for it.</summary>
    private static readonly Point _removeGlyphNudge = new(0, -1);

    private const int SPACING = 4;

    private readonly List<(int Value, string Label)> _entries;
    private readonly Dictionary<int, string> _labels;

    private readonly PickedItemsBox _pickedItemsPanel;
    private readonly IndexedComboPicker _picker;
    private readonly IconButton _addButton;

    private readonly Dictionary<int, Widget> _pickedItemRows = [];

    #endregion

    #region Ctor

    /// <param name="value">The picker's starting value - not necessarily picked.</param>
    /// <param name="entries">
    /// Every known (value, label) pair the picker should offer. The label should already read as it
    /// will appear (e.g. "755 - Earthquake"), matching what <see cref="IndexedComboPicker" /> shows
    /// in its own search list.
    /// </param>
    /// <param name="numberWidth">Width for the raw-number field.</param>
    /// <param name="nameWidth">Width for the name list.</param>
    /// <param name="initialValues">Values already picked when the widget is built.</param>
    /// <param name="minValue">Lower bound the number field accepts.</param>
    /// <param name="maxValue">Upper bound the number field accepts.</param>
    public IndexedListPicker(
        int value,
        IEnumerable<(int Value, string Label)> entries,
        int numberWidth,
        int nameWidth,
        IEnumerable<int>? initialValues = null,
        int minValue = int.MinValue,
        int maxValue = int.MaxValue
    )
    {
        Spacing = SPACING;

        _entries = [..entries];
        _labels = _entries.ToDictionary(entry => entry.Value, entry => entry.Label);

        _picker = new IndexedComboPicker(value, _entries, minValue, maxValue) { VerticalAlignment = VerticalAlignment.Center };
        _picker.NumberInput.Width = numberWidth;
        _picker.NameList.Width = nameWidth;
        _picker.ValueChanged += OnPickerValueChanged;

        _addButton = new IconButton("+", OnAddClick, glyphSize: 34) { Enabled = false };

        var pickerRow = new HorizontalStackPanel { Spacing = SPACING };
        pickerRow.Widgets.Add(_picker);
        pickerRow.Widgets.Add(_addButton);

        // Fixed to the picker row's own width rather than the fill column it may sit in - a box
        // that spans the whole editor pane would strand its remove glyphs far from short labels.
        int boxWidth = numberWidth + nameWidth + _addButton.Width!.Value + SPACING * 2;

        _pickedItemsPanel = new PickedItemsBox(boxWidth);

        Widgets.Add(pickerRow);
        Widgets.Add(_pickedItemsPanel);

        foreach (int seeded in initialValues ?? [])
            AddItem(seeded);

        RefreshAddButton(value);
        RefreshNameListOptions();
    }

    #endregion

    #region Private methods

    private void OnPickerValueChanged(object? sender, int value) => RefreshAddButton(value);

    private void OnAddClick()
    {
        if (!AddItem(_picker.Value))
            return;

        _picker.Value = 0;

        RefreshAddButton(_picker.Value);
        RefreshNameListOptions();
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool AddItem(int value)
    {
        if (_pickedItemRows.ContainsKey(value))
            return false;

        var label = new MyraLabel(LabelFor(value), MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center };

        var remove = new IconButton(REMOVE_GLYPH, () => RemoveItem(value), size: SMALL_BUTTON_SIZE, glyphSize: SMALL_GLYPH_SIZE)
        {
            Nudge = _removeGlyphNudge
        };

        var row = new SpaceBetweenRow(label, remove, SPACING);

        _pickedItemRows.Add(value, row);
        _pickedItemsPanel.AddRow(row);

        return true;
    }

    private void RemoveItem(int value)
    {
        if (!_pickedItemRows.Remove(value, out Widget? row))
            return;

        _pickedItemsPanel.RemoveRow(row);

        RefreshAddButton(_picker.Value);
        RefreshNameListOptions();
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private string LabelFor(int value) => _labels.GetValueOrDefault(value, value.ToString());

    private void RefreshAddButton(int candidate) => _addButton.Enabled = !_pickedItemRows.ContainsKey(candidate);

    /// <summary>
    /// Drops every already-picked value's name out of the search list, so picking never offers a
    /// duplicate. Rebuilt from the full catalogue rather than trimmed incrementally - the
    /// catalogues this drives (sounds, buffs) are at most a few hundred entries, and this only runs
    /// on an add or remove click, not per frame.
    /// </summary>
    private void RefreshNameListOptions()
    {
        _picker.NameList.Items.Clear();

        foreach ((int entryValue, string label) in _entries)
        {
            if (!_pickedItemRows.ContainsKey(entryValue))
                _picker.NameList.Items.Add(label);
        }
    }

    #endregion
}
