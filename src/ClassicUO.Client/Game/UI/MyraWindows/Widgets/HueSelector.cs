#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;
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

    /// <summary>The dye tub every hue picker in the UI uses for its swatch.</summary>
    private const ushort SWATCH_GRAPHIC = 0x0FAB;

    private readonly MyraArtTexture _swatch;
    private readonly IntegerInputBox _numberInput;

    private ushort _hue;

    /// <summary>Set while one input is moving the other, so the echo back does not re-enter.</summary>
    private bool _syncing;

    #endregion

    #region Ctor

    /// <param name="hue">The hue to start on. 0 is unhued.</param>
    /// <param name="swatchSize">Maximum pixel size of the swatch.</param>
    /// <param name="inputWidth">Width of the number field.</param>
    public HueSelector(ushort hue, int swatchSize = 20, int inputWidth = 70)
    {
        _hue = hue;

        Spacing = MyraStyle.STANDARD_SPACING;
        VerticalAlignment = VerticalAlignment.Center;

        _swatch = new MyraArtTexture(SWATCH_GRAPHIC, hue, swatchSize)
        {
            Tooltip = TazLang.Get("hueselector_swatch_tooltip", "Click to pick a hue.")
        };

        _swatch.TouchUp += (_, _) => OpenPicker();

        _numberInput = new IntegerInputBox
        {
            MinValue = 0,
            MaxValue = ushort.MaxValue,
            Width = inputWidth,
            Value = hue,
            Tooltip = TazLang.Get("hueselector_input_tooltip", "Hue index, 0 - 65535. 0 is unhued.")
        };

        _numberInput.ValueChanged += (_, args) =>
        {
            if (_syncing)
                return;

            // The number field is the one the user is typing in, so leave its text alone.
            Apply((ushort)Math.Clamp(args.NewValue, 0, ushort.MaxValue), raise: true, moveNumberInput: false);
        };

        Widgets.Add(_swatch);
        Widgets.Add(_numberInput);
    }

    #endregion

    #region Private methods

    private void OpenPicker()
    {
        // Enabled is propagated down by Myra, so this also covers the whole selector being gated off.
        if (!_swatch.Enabled)
            return;

        UIManager.GetGump<ModernColorPicker>()?.Dispose();
        UIManager.Add(new ModernColorPicker(
            World.Instance,
            picked => Apply(picked, raise: true, moveNumberInput: true),
            isClickable: true
        ));
    }

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
        _swatch.SetColorByHue(hue);

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
