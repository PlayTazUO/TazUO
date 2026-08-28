#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class IndexedListPicker : WrapPanel
{
    private readonly ListView _pickedItemsView;
    private readonly IndexedComboPicker _picker;

    private IconButton _addButton;
    private bool _addButtonEnabled = false;

    private IconButton _removeButton;
    private bool _removeButtonEnabled = false;

    private readonly Dictionary<int, Widget> _pickedItemsIndexToWidget = [];
    private readonly Dictionary<Widget, int> _pickedItemsWidgetToIndex = [];

    public int[] PickedItems => _pickedItemsIndexToWidget.Keys.ToArray();

    public IndexedListPicker(
        int value,
        IEnumerable<(int Value, string Label)> entries,
        int minValue = int.MinValue,
        int maxValue = int.MaxValue
    )
    {
        _addButton = new IconButton("+", OnAddClick, glyphSize: 34);
        _removeButton = new IconButton("\U0001F5D9", OnRemoveClick, glyphSize: 34);

        _pickedItemsView = new ListView { VerticalAlignment = VerticalAlignment.Top };
        _pickedItemsView.SelectedIndexChanged += OnViewItemSelected;

        _picker = new IndexedComboPicker(value, entries, minValue, maxValue) { VerticalAlignment = VerticalAlignment.Bottom};
        _picker.ValueChanged += OnPickerValueChanged;

        Children.Add(_pickedItemsView);
        Children.Add(_picker);
    }

    private void OnPickerValueChanged(object? sender, int value) => _addButtonEnabled = !_pickedItemsIndexToWidget.ContainsKey(value);

    private void OnViewItemSelected(object? sender, EventArgs e) => _removeButtonEnabled = _pickedItemsView.SelectedItem != null;

    private void OnAddClick()
    {
        (int, string) selectedEntry = _picker.SelectedEntry;
        if (_pickedItemsIndexToWidget.ContainsKey(selectedEntry.Item1))
            return;

        var listItem = new MyraLabel($"{selectedEntry.Item1} - {selectedEntry.Item2}", MyraLabel.TextStyle.P);
        _pickedItemsIndexToWidget.Add(selectedEntry.Item1, listItem);
        _pickedItemsWidgetToIndex.Add(listItem, selectedEntry.Item1);

        _pickedItemsView.Widgets.Add(listItem);
    }

    private void OnRemoveClick()
    {
        Widget? toRemove = _pickedItemsView.SelectedItem;
        if (toRemove == null)
            return;

        if (!_pickedItemsWidgetToIndex.Remove(toRemove, out int index))
            return;

        _pickedItemsIndexToWidget.Remove(index);
        _pickedItemsView.Widgets.Remove(toRemove);
    }
}
