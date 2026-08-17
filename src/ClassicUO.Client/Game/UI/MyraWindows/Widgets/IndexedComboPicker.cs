#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Numeric field and searchable name list kept in sync, so a value can be reached either by typing
///     its raw number or by finding it in a browsable list of names.
///     <para>
///         Generalizes the pattern <see cref="SoundIndexPicker" /> and <see cref="BuffTypePicker" /> each hand-roll:
///         a bare index means nothing to read, so it needs a searchable list of names beside it, but the list
///         only names what the caller's data covers - the number field is what actually gets stored, and is
///         what a value outside that data still has to go through.
///     </para>
/// </summary>
public class IndexedComboPicker : HorizontalStackPanel
{
    #region Public events

    /// <summary>Raised when the chosen value changes, from either input.</summary>
    public event EventHandler<int>? ValueChanged;

    #endregion

    #region Public accessors

    /// <summary>The chosen value. Setting it moves both inputs.</summary>
    public int Value
    {
        get => NumberInput.Value;
        set => NumberInput.Value = value;
    }

    /// <summary>The number field, exposed so callers can set a tooltip/hint beyond the ctor's reach.</summary>
    public IntegerInputBox NumberInput { get; }

    /// <summary>The name list, exposed so callers can set a tooltip selector beyond the ctor's reach.</summary>
    public ContainsLevenshteinComboBox NameList { get; }

    #endregion

    #region Private members

    private const int SPACING = 6;

    private readonly Dictionary<int, string> _labels = [];
    private readonly Dictionary<string, int> _values = [];
    private readonly List<string> _orderedLabels = [];

    /// <summary>Set while one input is moving the other, so the echo back does not re-enter.</summary>
    private bool _syncing;

    #endregion

    #region Ctor

    /// <param name="value">The value to start on.</param>
    /// <param name="entries">Every known (value, label) pair the name list should offer.</param>
    /// <param name="minValue">Lower bound the number field accepts.</param>
    /// <param name="maxValue">Upper bound the number field accepts.</param>
    /// <param name="numberInput">
    ///     The number field to drive, when a caller needs one that parses more than plain decimal (e.g., a
    ///     hex-capable <see cref="IntegerInputBox" /> subclass). Defaults to a plain one.
    /// </param>
    /// <remarks>
    ///     Width and hint text are cosmetic, not structural - set them on <see cref="NumberInput" /> and
    ///     <see cref="NameList" /> after construction instead of through the ctor.
    /// </remarks>
    public IndexedComboPicker(
        int value,
        IEnumerable<(int Value, string Label)> entries,
        int minValue = int.MinValue,
        int maxValue = int.MaxValue,
        IntegerInputBox? numberInput = null
    )
    {
        Spacing = SPACING;

        foreach ((int entryValue, string label) in entries)
        {
            // First name wins if entries repeat a value; the rest still resolve to it through the
            // number field.
            if (!_labels.TryAdd(entryValue, label))
                continue;

            _values[label] = entryValue;
            _orderedLabels.Add(label);
        }

        NumberInput = numberInput ?? new IntegerInputBox();
        NumberInput.MinValue = minValue;
        NumberInput.MaxValue = maxValue;
        NumberInput.VerticalAlignment = VerticalAlignment.Center;

        // addSelectedItemIfMissing is off: a value entries have no name for is a real choice, but
        // adding a row for it would put a made-up entry in a list that otherwise mirrors entries. The
        // number field carries it instead, and the list shows nothing selected.
        NameList = new ContainsLevenshteinComboBox(
            LabelFor(value) ?? string.Empty,
            _orderedLabels,
            OnNameChosen,
            false
        ) { VerticalAlignment = VerticalAlignment.Center, TooltipSelector = name => name };

        MyraStyle.ApplySearchComboBoxPopupBorder(NameList);

        NumberInput.Value = value;
        NumberInput.ValueChanged += (_, args) => OnNumberTyped(args.NewValue);

        Children.Add(NumberInput);
        Children.Add(NameList);
    }

    #endregion

    #region Private methods

    private void OnNumberTyped(int value)
    {
        if (_syncing)
            return;

        _syncing = true;

        try
        {
            // Null where the value has no name, which clears the list rather than leaving it pointing
            // at whatever was chosen before.
            NameList.SelectedIndex = PositionOf(value);
        }
        finally
        {
            _syncing = false;
        }

        ValueChanged?.Invoke(this, value);
    }

    private void OnNameChosen(string? label)
    {
        if (_syncing || label == null || !_values.TryGetValue(label, out int value))
            return;

        _syncing = true;

        try
        {
            NumberInput.Value = value;
        }
        finally
        {
            _syncing = false;
        }

        ValueChanged?.Invoke(this, value);
    }

    private string? LabelFor(int value) => _labels.GetValueOrDefault(value);

    private int? PositionOf(int value)
    {
        string? label = LabelFor(value);

        if (label == null)
            return null;

        int position = _orderedLabels.IndexOf(label);

        return position < 0 ? null : position;
    }

    #endregion
}
