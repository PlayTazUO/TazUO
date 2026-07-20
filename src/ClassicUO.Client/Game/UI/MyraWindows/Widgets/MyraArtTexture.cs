#nullable enable
using System;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

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

    public MyraArtTexture(uint graphic, ushort hue = 0, int maxSize = 36)
    {
        _texture = new HuedTexture(graphic, hue);

        if (_texture.IsValid)
            Renderable = _texture;

        MaxWidth = maxSize;
        MaxHeight = maxSize;
    }

    public void SetColor(Color color) => _texture.SetColor(color);

    public void SetColorByHue(ushort hue, float alpha = 1f) => _texture.SetColorByHue(hue, alpha);
}

internal class HuedTexture : IImage
{
    private TextureRegion? _region;

    public Color RenderColor { get; set; }

    public Point Size { get; private set; }

    public bool IsValid => _region != null;

    public HuedTexture(uint graphic, ushort hue)
    {
        SpriteInfo artInfo = Client.Game.UO.Arts.GetArt(graphic);

        if (artInfo.Texture == null)
        {
            // Throw in debug, warn and return empty in release.
#if DEBUG
            throw new ArgumentException($@"Could not find texture for graphic '{graphic}'", nameof(graphic));
#else
            Utility.Logging.Log.Warn($"Could not find texture for graphic '{graphic}'");
            return;
#endif
        }

        // artInfo.UV is the sub-rectangle within the shared atlas texture.
        // Passing just the Texture2D would render the entire atlas page;
        // supplying artInfo.UV scopes it to only this sprite.

        // That said, the actual relevant bounds may be smaller than the sprite suggests, so another step is required here.
        Rectangle actualUv = Client.Game.UO.Arts.GetRealArtBounds(graphic);
        Init(artInfo.Texture, artInfo.UV, actualUv, hue);
    }

    public HuedTexture(Texture2D texture, Rectangle textureUv, Rectangle bounds, ushort hue)
    {
        Init(texture, textureUv, bounds, hue);
    }

    protected void Init(Texture2D texture, Rectangle textureUv, Rectangle bounds, ushort hue)
    {
        Size = new Point(bounds.Width, bounds.Height);
        _region = new TextureRegion(new TextureRegion(texture, textureUv), bounds);
        SetColorByHue(hue);
    }

    public void SetColor(Color color) => RenderColor = color;

    public void SetColorByHue(ushort hue, float alpha = 1f)
    {
        if (hue == 0)
        {
            RenderColor = new Color(255, 255, 255, (byte)(MathHelper.Clamp(alpha, 0f, 1f) * 255f));
            return;
        }

        uint rgba = Client.Game.UO.FileManager.Hues.GetHueColorRgba8888(31, hue);
        byte a = (byte)(MathHelper.Clamp(alpha, 0f, 1f) * 255f);

        RenderColor = new Color(
            (byte)(rgba & 0xFF),
            (byte)((rgba >> 8) & 0xFF),
            (byte)((rgba >> 16) & 0xFF),
            a
        );
    }

    public void Draw(RenderContext context, Rectangle dest, Color color) => _region.Draw(context, dest, RenderColor);
}
