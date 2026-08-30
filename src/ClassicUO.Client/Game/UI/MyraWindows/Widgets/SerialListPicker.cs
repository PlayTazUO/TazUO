#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
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
    Hex = 1,
    Decimal = 2,
    Both = Hex | Decimal
}

/// <summary>
/// Builds a set of chosen object serials: type one in (decimal or hex) or target it in the world, and
/// it drops into a boxed list below - the same picked-list shape <see cref="IndexedListPicker" /> uses,
/// with a target button standing in for the name search a serial has none of.
/// </summary>
public sealed class SerialListPicker : VerticalStackPanel
{
    #region Public events

    /// <summary>Raised whenever the picked set changes, from either button or a target pick.</summary>
    public event EventHandler? ItemsChanged;

    #endregion

    #region Public accessors

    /// <summary>Every serial currently picked.</summary>
    public uint[] PickedItems => _pickedItemRows.Keys.ToArray();

    #endregion

    #region Private members

    private const string REMOVE_GLYPH = "\U0001F5D9";
    private const int SMALL_BUTTON_SIZE = 22;
    private const int SMALL_GLYPH_SIZE = 20;
    private const int TARGET_BUTTON_WIDTH = 90;
    private const int SPACING = 4;

    /// <summary>The glyph's ink sits a little low and right of centre at this size - the metrics
    /// <see cref="IconButton" /> reads off the font are close but not exact for it.</summary>
    private static readonly Point _removeGlyphNudge = new(0, -1);

    private readonly SerialDisplayFormat _displayFormat;

    private readonly PickedItemsBox _pickedItemsPanel;
    private readonly HexInputBox _serialInput;
    private readonly IconButton _addButton;

    private readonly Dictionary<uint, Widget> _pickedItemRows = [];

    #endregion

    #region Ctor

    /// <param name="inputWidth">Width for the raw-serial field. Serials are 32-bit, so this needs no
    /// more room than "0xFFFFFFFF" takes.</param>
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

        _serialInput = new HexInputBox
        {
            MinValue = 0,
            Width = inputWidth,
            VerticalAlignment = VerticalAlignment.Center,
            Tooltip = TazLang.Get(
                "seriallistpicker_input_tooltip",
                "Item serial to watch (e.g., 0xDEADBEEF or 3735928559)."
            )
        };
        _serialInput.ValueChanged += (_, args) => RefreshAddButton(unchecked((uint)args.NewValue));

        _addButton = new IconButton("+", OnAddClick, glyphSize: 34) { Enabled = false };

        var target = new TargetSelectionButton(
            OnTargeted,
            tooltip: TazLang.Get("seriallistpicker_target_tooltip", "Target an object in the world to add its serial.")
        ) { Width = TARGET_BUTTON_WIDTH };

        var pickerRow = new HorizontalStackPanel { Spacing = SPACING };
        pickerRow.Widgets.Add(_serialInput);
        pickerRow.Widgets.Add(target);
        pickerRow.Widgets.Add(_addButton);

        // Fixed to the picker row's own width rather than the fill column it may sit in - a box
        // that spans the whole editor pane would strand its remove glyphs far from short labels.
        int boxWidth = inputWidth + TARGET_BUTTON_WIDTH + _addButton.Width!.Value + SPACING * 2;

        _pickedItemsPanel = new PickedItemsBox(boxWidth);

        Widgets.Add(pickerRow);
        Widgets.Add(_pickedItemsPanel);

        foreach (uint seeded in initialValues ?? [])
            AddItem(seeded);
    }

    #endregion

    #region Private methods

    private void OnTargeted(uint? serial)
    {
        if (serial is not { } picked || !AddItem(picked))
            return;

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddClick()
    {
        uint candidate = unchecked((uint)_serialInput.Value);

        if (!AddItem(candidate))
            return;

        _serialInput.Value = 0;
        RefreshAddButton(0);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool AddItem(uint value)
    {
        if (value == 0 || _pickedItemRows.ContainsKey(value))
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

    private void RemoveItem(uint value)
    {
        if (!_pickedItemRows.Remove(value, out Widget? row))
            return;

        _pickedItemsPanel.RemoveRow(row);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private string LabelFor(uint value) => _displayFormat switch
    {
        SerialDisplayFormat.Decimal => value.ToString(),
        SerialDisplayFormat.Both => $"0x{value:X} ({value})",
        _ => $"0x{value:X}"
    };

    private void RefreshAddButton(uint candidate) => _addButton.Enabled = candidate != 0 && !_pickedItemRows.ContainsKey(candidate);

    #endregion
}
