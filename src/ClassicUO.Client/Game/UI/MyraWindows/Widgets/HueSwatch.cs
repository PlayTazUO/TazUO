#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     A dye tub showing a UO hue, which opens the shared colour picker when clicked.
/// </summary>
/// <remarks>
///     The whole of what a hue picker is, so that the several places offering one agree on its look,
///     its tooltip and its click behaviour. Compose it with whatever else a given spot needs - a label,
///     a number field, a reset button - rather than re-hanging a picker off a bare
///     <see cref="MyraArtTexture" />.
/// </remarks>
public class HueSwatch : MyraArtTexture
{
    #region Public events

    /// <summary>Raised when the user picks a hue. Not raised by <see cref="SetHue" />.</summary>
    public event EventHandler<ushort>? HueChanged;

    #endregion

    #region Public constants

    /// <summary>The dye tub art every hue picker in the UI shows.</summary>
    public const ushort SWATCH_GRAPHIC = 0x0FAB;

    /// <summary>Size that sits level with a line of UI text.</summary>
    public const int DEFAULT_SIZE = 20;

    #endregion

    #region Public accessors

    /// <summary>The hue on display.</summary>
    public ushort Hue { get; private set; }

    #endregion

    #region Ctor

    /// <summary>Builds a swatch showing a hue, which opens the colour picker when clicked.</summary>
    /// <param name="hue">The hue to start on. 0 shows the tub undyed.</param>
    /// <param name="size">Maximum pixel size of the swatch.</param>
    public HueSwatch(ushort hue, int size = DEFAULT_SIZE) : base(SWATCH_GRAPHIC, hue, size)
    {
        Hue = hue;

        // Myra defaults every widget to top alignment, which leaves the swatch riding high against
        // the label and inputs it shares a row with.
        VerticalAlignment = VerticalAlignment.Center;

        RefreshTooltip();

        TouchUp += (_, _) => OpenPicker();
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Shows a hue without reporting it, for a caller whose value changed by some other route.
    /// </summary>
    /// <param name="hue">The hue to show.</param>
    public void SetHue(ushort hue)
    {
        Hue = hue;
        SetColorByHue(hue);
        RefreshTooltip();
    }

    #endregion

    #region Private methods

    /// <summary>Opens the shared colour picker, replacing one already open.</summary>
    private void OpenPicker()
    {
        // Enabled is propagated down by Myra, so this also covers an ancestor being gated off.
        if (!Enabled)
            return;

        UIManager.GetGump<ModernColorPicker>()?.Dispose();
        UIManager.Add(new ModernColorPicker(
            World.Instance,
            picked =>
            {
                SetHue(picked);
                HueChanged?.Invoke(this, picked);
            },
            isClickable: true
        ));
    }

    /// <summary>Restates the tooltip for the hue on display, which changes with every pick.</summary>
    private void RefreshTooltip() =>
        Tooltip = TazLang.GetEx("hueswatch_tooltip", "Click to pick a hue.\nCurrent hue: {0}", [Hue.ToString()]);

    #endregion
}
