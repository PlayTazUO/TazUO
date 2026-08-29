#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Builds a set of chosen values from a searchable <see cref="IndexedComboPicker" />: pick one, add
/// it, and it moves into the list beside a remove button for whichever entry is selected there.
/// </summary>
public class IndexedListPicker : WrapPanel
{
    #region Public events

    /// <summary>Raised whenever the picked set changes, from either button.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public accessors

    /// <summary>Every value currently picked.</summary>
    public int[] PickedItems => _pickedItemsIndexToWidget.Keys.ToArray();

    #endregion

    #region Private members

    private readonly Dictionary<int, string> _labels;

    private readonly ListView _pickedItemsView;
    private readonly IndexedComboPicker _picker;
    private readonly IconButton _addButton;
    private readonly IconButton _removeButton;

    private readonly Dictionary<int, Widget> _pickedItemsIndexToWidget = [];
    private readonly Dictionary<Widget, int> _pickedItemsWidgetToIndex = [];

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
        List<(int Value, string Label)> entryList = [..entries];
        _labels = entryList.ToDictionary(entry => entry.Value, entry => entry.Label);

        _addButton = new IconButton("+", OnAddClick, glyphSize: 34) { Enabled = false };
        _removeButton = new IconButton("\U0001F5D9", OnRemoveClick, glyphSize: 34) { Enabled = false };

        _pickedItemsView = new ListView { VerticalAlignment = VerticalAlignment.Top };
        _pickedItemsView.SelectedIndexChanged += OnViewItemSelected;

        _picker = new IndexedComboPicker(value, entryList, minValue, maxValue) { VerticalAlignment = VerticalAlignment.Bottom };
        _picker.NumberInput.Width = numberWidth;
        _picker.NameList.Width = nameWidth;
        _picker.ValueChanged += OnPickerValueChanged;

        Children.Add(_pickedItemsView);
        Children.Add(_picker);
        Children.Add(_addButton);
        Children.Add(_removeButton);

        foreach (int seeded in initialValues ?? [])
            AddItem(seeded);

        RefreshAddButton(value);
    }

    #endregion

    #region Private methods

    private void OnPickerValueChanged(object? sender, int value) => RefreshAddButton(value);

    private void OnViewItemSelected(object? sender, EventArgs e) =>
        _removeButton.Enabled = _pickedItemsView.SelectedItem != null;

    private void OnAddClick()
    {
        if (!AddItem(_picker.Value))
            return;

        RefreshAddButton(_picker.Value);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnRemoveClick()
    {
        Widget? toRemove = _pickedItemsView.SelectedItem;

        if (toRemove == null || !_pickedItemsWidgetToIndex.Remove(toRemove, out int index))
            return;

        _pickedItemsIndexToWidget.Remove(index);
        _pickedItemsView.Widgets.Remove(toRemove);

        RefreshAddButton(_picker.Value);
        _removeButton.Enabled = false;
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool AddItem(int value)
    {
        if (_pickedItemsIndexToWidget.ContainsKey(value))
            return false;

        var listItem = new MyraLabel(LabelFor(value), MyraLabel.TextStyle.P);

        _pickedItemsIndexToWidget.Add(value, listItem);
        _pickedItemsWidgetToIndex.Add(listItem, value);
        _pickedItemsView.Widgets.Add(listItem);

        return true;
    }

    private string LabelFor(int value) => _labels.GetValueOrDefault(value, value.ToString());

    private void RefreshAddButton(int candidate) => _addButton.Enabled = !_pickedItemsIndexToWidget.ContainsKey(candidate);

    #endregion
}
