#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Builds a set of chosen values from a searchable <see cref="IndexedComboPicker" />: pick one, add
/// it, and it drops into a boxed list below with its own remove glyph. A picked value leaves the name
/// list, so the search never offers a duplicate.
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

    #endregion

    #region Private members

    private const int SPACING = PickedItemsController<int>.SPACING;

    private readonly List<(int Value, string Label)> _entries;
    private readonly Dictionary<int, string> _labels;

    private readonly PickedItemsController<int> _picked;
    private readonly IndexedComboPicker _picker;

    #endregion

    #region Ctor

    /// <param name="value">The picker's starting value - not necessarily picked.</param>
    /// <param name="entries">Every known (value, label) pair to offer. Labels arrive display-ready
    /// (e.g. "755 - Earthquake").</param>
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

        // Fixed to the picker row's own width rather than the fill column it may sit in - a box
        // that spans the whole editor pane would strand its remove glyphs far from short labels.
        int boxWidth = numberWidth + nameWidth + PickedItemsController<int>.ADD_BUTTON_SIZE + SPACING * 2;

        _picked = new PickedItemsController<int>(boxWidth, LabelFor, OnAddClick);
        _picked.ItemsChanged += OnPickedItemsChanged;

        // Subscribed only now: the handler reads _picked, so it must not be reachable before it exists.
        _picker.ValueChanged += OnPickerValueChanged;

        var pickerRow = new HorizontalStackPanel { Spacing = SPACING };
        pickerRow.Widgets.Add(_picker);
        pickerRow.Widgets.Add(_picked.AddButton);

        Widgets.Add(pickerRow);
        Widgets.Add(_picked.Box);

        _picked.Seed(initialValues);
        _picked.SetCandidate(value);

        RefreshNameListOptions();
    }

    #endregion

    #region Private methods

    private void OnPickerValueChanged(object? sender, int value) => _picked.SetCandidate(value);

    /// <summary>Every change to the set, from either button, has to retrim the name list - a removed
    /// value becomes offerable again.</summary>
    private void OnPickedItemsChanged(object? sender, EventArgs e)
    {
        RefreshNameListOptions();

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddClick()
    {
        if (!_picked.Add(_picker.Value))
            return;

        // Clearing the field re-enters through OnPickerValueChanged, which re-reads the add button.
        // After the retrim above, so the list it looks the cleared value up in is the current one.
        _picker.Value = 0;
    }

    private string LabelFor(int value) => _labels.GetValueOrDefault(value, value.ToString());

    /// <summary>
    /// Drops every already-picked value's name out of the search list, so picking never offers a
    /// duplicate. Rebuilt from the full catalogue rather than trimmed incrementally: the sound
    /// catalogue runs to a few thousand entries, but this fires on an add or remove click only.
    /// </summary>
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
