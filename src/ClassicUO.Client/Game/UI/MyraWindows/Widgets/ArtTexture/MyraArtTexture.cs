using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;

/// <summary>
///     A Myra Image widget that displays a UO art graphic by graphic ID.
///     Uses the correct UV sub-rectangle from the texture atlas so that only
///     the target sprite is rendered. The atlas Texture2D is NOT owned here and
///     must never be disposed — Myra's Image widget does not implement IDisposable,
///     so there is no disposal risk.
/// </summary>
public class MyraArtTexture : Image
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
}
