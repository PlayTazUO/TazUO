using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class RenderedMapArea : Control
{
    private readonly Texture2D _texture;
    private readonly bool _disposeTexture;

    public RenderedMapArea(Texture2D texture, int x, int y, int width, int height, bool disposeTexture = true)
    {
        _texture = texture;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        _disposeTexture = disposeTexture;
        CanMove = true;
        AcceptMouseInput = true;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y)) return false;

        if (_texture == null) return false;

        batcher.Draw(_texture, new Rectangle(x, y, Width, Height), new Rectangle(0, 0, _texture.Width, _texture.Height), ShaderHueTranslator.GetHueVector(0, false, Alpha));

        return true;
    }

    public override void Dispose()
    {
        base.Dispose();
        if(_disposeTexture)
            _texture?.Dispose();
    }
}
