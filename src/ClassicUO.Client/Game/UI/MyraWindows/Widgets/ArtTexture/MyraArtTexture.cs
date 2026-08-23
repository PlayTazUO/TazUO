using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;

/// <summary>
///     Displays a UO art graphic by graphic ID.
///     <para>
///         A hued graphic holds shared GPU memory while the widget is on a desktop; leaving gives it back.
///         <see cref="Dispose" /> only matters for retiring one that is still placed.
///     </para>
/// </summary>
public class MyraArtTexture : Image, IDisposable
{
    private readonly HuedTexture _texture;

    /// <summary>
    ///     Creates the widget for a graphic.
    /// </summary>
    /// <param name="graphic">Art graphic ID, without the 0x4000 offset.</param>
    /// <param name="hue">UO hue to display the graphic in. 0 renders it unhued.</param>
    /// <param name="maxSize">Upper bound on both dimensions; the sprite scales down to fit.</param>
    public MyraArtTexture(uint graphic, ushort hue = 0, int maxSize = 36)
    {
        _texture = new HuedTexture(graphic, hue);

        if (_texture.IsValid)
            Renderable = _texture;

        MaxWidth = maxSize;
        MaxHeight = maxSize;
    }

    /// <summary>
    ///     Switches the displayed hue.
    /// </summary>
    /// <param name="hue">UO hue to apply. 0 restores the unhued sprite.</param>
    /// <param name="alpha">Opacity multiplier, 0-1.</param>
    public void SetColorByHue(ushort hue, float alpha = 1f) => _texture.SetColorByHue(hue, alpha);

    /// <inheritdoc />
    /// <remarks>Placement is the only lifetime signal Myra gives a widget, and the baked texture needs one.</remarks>
    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();

        _texture.SetPlaced(IsPlaced);
    }

    /// <summary>
    ///     Gives up the graphic's share of any baked texture. Idempotent; the widget still draws unhued after.
    /// </summary>
    public void Dispose()
    {
        _texture.Dispose();
        GC.SuppressFinalize(this);
    }
}
