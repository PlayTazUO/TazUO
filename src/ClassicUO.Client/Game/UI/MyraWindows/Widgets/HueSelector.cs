#nullable enable

using System;
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Picks a UO hue: a clickable swatch that opens the shared color picker, beside a number field
///     showing the hue index, either one driving the other.
/// </summary>
/// <remarks>
///     The number field accepts any two-byte value, not just the hues the client's hue file defines -
///     a shard can key its own meaning to an index, and a value past the file simply renders unhued,
///     which the swatch shows plainly enough to need no validation message.
/// </remarks>
public sealed class HueSelector : HorizontalStackPanel
{
    #region Public events

    /// <summary>Raised when the hue changes, from either input.</summary>
    public event EventHandler<ushort>? HueChanged;

    #endregion

    #region Public accessors

    /// <summary>The chosen hue. Setting it moves both inputs without raising <see cref="HueChanged" />.</summary>
    public ushort Hue
    {
        get => _hue;
        set => Apply(value, raise: false, moveNumberInput: true);
    }

    #endregion

    #region Private members

    /// <summary>Gap between the swatch and the index field.</summary>
    private const int SWATCH_INPUT_SPACING = 8;

    /// <summary>The clickable swatch, which owns the colour picker itself.</summary>
    private readonly HueSwatch _swatch;

    /// <summary>The hue index, for reaching a value the picker's grid does not offer.</summary>
    private readonly IntegerInputBox _numberInput;

    private ushort _hue;

    /// <summary>Set while one input is moving the other, so the echo back does not re-enter.</summary>
    private bool _syncing;

    #endregion

    #region Ctor

    /// <summary>Builds a swatch and number field, both showing the one hue.</summary>
    /// <param name="hue">The hue to start on. 0 is unhued.</param>
    /// <param name="swatchSize">Maximum pixel size of the swatch.</param>
    /// <param name="inputWidth">Width of the number field.</param>
    public HueSelector(ushort hue, int swatchSize = 20, int inputWidth = 70)
    {
        _hue = hue;

        // Wider than the shared spacing: the swatch is a picture and the field is a box, and butted
        // together they read as one control rather than two.
        Spacing = SWATCH_INPUT_SPACING;
        VerticalAlignment = VerticalAlignment.Center;

        _swatch = new HueSwatch(hue, swatchSize);
        _swatch.HueChanged += (_, picked) => Apply(picked, raise: true, moveNumberInput: true);

        _numberInput = new IntegerInputBox
        {
            MinValue = 0,
            MaxValue = ushort.MaxValue,
            Width = inputWidth,
            Value = hue,
            Tooltip = TazLang.Get("hueselector_input_tooltip", "Hue index, 0 - 65535. 0 is unhued")
        };

        _numberInput.ValueChanged += (_, args) =>
        {
            if (_syncing)
                return;

            // The number field is the one the user is typing in, so leave its text alone.
            Apply((ushort)Math.Clamp(args.NewValue, 0, ushort.MaxValue), raise: true, moveNumberInput: false);
        };

        // Index first, matching GumpGraphicPicker's number-then-chooser order, so the two picker
        // types scan the same way down a column of settings.
        Widgets.Add(_numberInput);
        Widgets.Add(_swatch);
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Commits a hue to both inputs.
    /// </summary>
    /// <param name="hue">The hue to show.</param>
    /// <param name="raise">Whether to notify listeners; false for a programmatic set.</param>
    /// <param name="moveNumberInput">
    ///     False when the number field is the source of the change, so its text is not rewritten under
    ///     the caret mid-edit.
    /// </param>
    private void Apply(ushort hue, bool raise, bool moveNumberInput)
    {
        _hue = hue;
        _swatch.SetHue(hue);

        if (moveNumberInput)
        {
            _syncing = true;

            try
            {
                _numberInput.Value = hue;
            }
            finally
            {
                _syncing = false;
            }
        }

        if (raise)
            HueChanged?.Invoke(this, hue);
    }

    #endregion
}
