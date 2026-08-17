#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Picks a buff by its <see cref="BuffIconType" />: a searchable list of every type this client
///     knows, and a raw number field for one a shard sends that the enum has no name for.
///     <para>
///         Both inputs are needed for the same reason <see cref="SoundIndexPicker" /> carries two: the enum
///         only names what this client shipped with, but the id is what the server actually sends and is
///         what gets stored either way - the list only writes into it.
///     </para>
/// </summary>
public sealed class BuffTypePicker : HorizontalStackPanel
{
    #region Public events

    /// <summary>Raised when the chosen type changes, from either input.</summary>
    public event EventHandler<short>? TypeChanged;

    #endregion

    #region Public accessors

    /// <summary>The chosen buff type id. Setting it moves both inputs.</summary>
    public short BuffType
    {
        get => (short)_input.Value;
        set => _input.Value = value;
    }

    #endregion

    #region Private members

    private const int SPACING = 6;

    /// <summary>
    ///     Label for each known type, and the type each label names. Built once: the enum is
    ///     fixed at compile time, unlike the sound data a <see cref="SoundIndexPicker" /> reads.
    /// </summary>
    private static readonly Dictionary<short, string> _labels = [];

    private static readonly Dictionary<string, short> _values = [];

    private static readonly List<string> _orderedLabels = [];

    private readonly IntegerInputBox _input;

    private readonly ContainsLevenshteinComboBox _names;

    /// <summary>Set while one input is moving the other, so the echo back does not re-enter.</summary>
    private bool _syncing;

    #endregion

    #region Ctor

    static BuffTypePicker()
    {
        foreach (BuffIconType type in Enum.GetValues<BuffIconType>())
        {
            short id = (short)type;

            // A handful of enum members alias the same numeric id; the first name wins and the
            // rest still resolve to it through the number field.
            if (_labels.ContainsKey(id))
                continue;

            string label = $"{id} - {type}";

            _labels[id] = label;
            _values[label] = id;
            _orderedLabels.Add(label);
        }
    }

    /// <param name="buffType">The type to start on.</param>
    /// <param name="numberWidth">Width for the raw-number field.</param>
    /// <param name="nameWidth">Width for the name list.</param>
    public BuffTypePicker(short buffType, int numberWidth, int nameWidth)
    {
        Spacing = SPACING;

        _input = new IntegerInputBox
        {
            MinValue = short.MinValue,
            MaxValue = short.MaxValue,
            Width = numberWidth,
            VerticalAlignment = VerticalAlignment.Center,
            Tooltip = TazLang.Get(
                "overlaytrigger_buff_number_tooltip",
                "The buff's numeric ID.\n"
                + "Type one here for a buff the list has no name for."
            )
        };

        // addSelectedItemIfMissing is off: an id the enum has no name for is a real choice, but
        // adding a row for it would put a made-up entry in a list that otherwise mirrors the enum.
        // The number field carries it instead, and the list shows nothing selected.
        _names = new ContainsLevenshteinComboBox(
            LabelFor(buffType) ?? string.Empty,
            _orderedLabels,
            OnNameChosen,
            false
        ) { VerticalAlignment = VerticalAlignment.Center, TooltipSelector = name => name, Width = nameWidth };

        MyraStyle.ApplySearchComboBoxPopupBorder(_names);

        _input.Value = buffType;
        _input.ValueChanged += (_, args) => OnNumberTyped((short)args.NewValue);

        Widgets.Add(_input);
        Widgets.Add(_names);
    }

    #endregion

    #region Private methods

    private void OnNumberTyped(short buffType)
    {
        if (_syncing)
            return;

        _syncing = true;

        try
        {
            // Null where the id has no name, which clears the list rather than leaving it pointing
            // at whatever was chosen before.
            _names.SelectedIndex = PositionOf(buffType);
        }
        finally
        {
            _syncing = false;
        }

        TypeChanged?.Invoke(this, buffType);
    }

    private void OnNameChosen(string? label)
    {
        if (_syncing || label == null || !_values.TryGetValue(label, out short buffType))
            return;

        _syncing = true;

        try
        {
            _input.Value = buffType;
        }
        finally
        {
            _syncing = false;
        }

        TypeChanged?.Invoke(this, buffType);
    }

    private static string? LabelFor(short buffType) => _labels.GetValueOrDefault(buffType);

    private static int? PositionOf(short buffType)
    {
        string? label = LabelFor(buffType);

        if (label == null)
            return null;

        int position = _orderedLabels.IndexOf(label);

        return position < 0 ? null : position;
    }

    #endregion
}
