#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Marks a <c>List&lt;uint&gt;</c> property as a set of object serials, so the rule editor offers a
/// multi-serial picker for it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerialListEditorAttribute : Attribute;

/// <summary>Which base(s) a <see cref="SerialListPicker" /> shows a picked serial in.</summary>
[Flags]
public enum SerialDisplayFormat
{
    /// <summary>Hex only, e.g. <c>0x4000BEEF</c>.</summary>
    Hex = 1,

    /// <summary>Decimal only.</summary>
    Decimal = 2,

    /// <summary>Both bases, e.g. <c>0x4000BEEF (1073790703)</c>.</summary>
    Both = Hex | Decimal
}

/// <summary>
/// Builds a set of chosen object serials: type one in (decimal or hex) or target it in the world.
/// <see cref="IndexedListPicker" />'s shape, with a target button where its name search would be.
/// </summary>
public sealed class SerialListPicker : VerticalStackPanel
{
    #region Public events

    /// <summary>Raised whenever the picked set changes, from either button or a target pick.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public accessors

    /// <summary>Every serial currently picked.</summary>
    public uint[] PickedItems => _picked.PickedItems;

    #endregion

    #region Private members

    private const int TARGET_BUTTON_WIDTH = 90;
    private const int SPACING = PickedItemsController<uint>.SPACING;

    /// <summary>Serial zero is "nothing", not an object, so it is never a pick.</summary>
    private const uint NO_SERIAL = 0;

    private readonly SerialDisplayFormat _displayFormat;

    private readonly PickedItemsController<uint> _picked;
    private readonly HexIntInputBox _serialInput;

    #endregion

    #region Ctor

    /// <summary>Builds the picker row and its picked-items box.</summary>
    /// <param name="inputWidth">Width for the raw-serial field - no wider than "0xFFFFFFFF".</param>
    /// <param name="initialValues">Serials already picked when the widget is built.</param>
    /// <param name="displayFormat">Which base(s) a picked row is shown in.</param>
    public SerialListPicker(
        int inputWidth,
        IEnumerable<uint>? initialValues = null,
        SerialDisplayFormat displayFormat = SerialDisplayFormat.Hex
    )
    {
        Spacing = SPACING;
        _displayFormat = displayFormat;

        _serialInput = new HexIntInputBox
        {
            MinValue = 0,
            Width = inputWidth,
            VerticalAlignment = VerticalAlignment.Center,
            Tooltip = TazLang.Get(
                "seriallistpicker_input_tooltip",
                "Item serial to watch (e.g., 0x4000BEEF or 1073790703)."
            )
        };

        var target = new TargetSelectionButton(
            OnTargeted,
            tooltip: TazLang.Get("seriallistpicker_target_tooltip", "Target an object in the world to add its serial.")
        ) { Width = TARGET_BUTTON_WIDTH };

        // Fixed to the picker row's width, not the fill column it sits in - a box spanning the whole
        // editor pane would strand its remove glyphs far from short labels.
        int boxWidth = inputWidth + TARGET_BUTTON_WIDTH + PickedItemsController<uint>.ADD_BUTTON_SIZE + SPACING * 2;

        _picked = new PickedItemsController<uint>(boxWidth, LabelFor, OnAddClick, serial => serial != NO_SERIAL);
        _picked.ItemsChanged += (_, _) => ItemsChanged?.Invoke(this, EventArgs.Empty);

        // Subscribed only now: the handler reads _picked.
        _serialInput.ValueChanged += (_, args) => _picked.SetCandidate(unchecked((uint)args.NewValue));

        var pickerRow = new HorizontalStackPanel { Spacing = SPACING };
        pickerRow.Widgets.Add(_serialInput);
        pickerRow.Widgets.Add(target);
        pickerRow.Widgets.Add(_picked.AddButton);

        Widgets.Add(pickerRow);
        Widgets.Add(_picked.Box);

        _picked.Seed(initialValues);
        _picked.SetCandidate(NO_SERIAL);
    }

    #endregion

    #region Private methods

    private void OnTargeted(uint? serial)
    {
        if (serial is { } picked)
            _picked.Add(picked);
    }

    private void OnAddClick()
    {
        if (_picked.Add(unchecked((uint)_serialInput.Value)))
            _serialInput.Value = 0;
    }

    private string LabelFor(uint value) => _displayFormat switch
    {
        SerialDisplayFormat.Decimal => value.ToString(),
        SerialDisplayFormat.Both => $"0x{value:X} ({value})",
        _ => $"0x{value:X}"
    };

    #endregion
}
