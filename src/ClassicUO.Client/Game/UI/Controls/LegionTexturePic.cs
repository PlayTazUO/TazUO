using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class LegionTexturePic : Control
{
    private string _textureName;

    public string TextureName
    {
        get => _textureName;
        set
        {
            _textureName = value;
            TryApplyNaturalSize();
        }
    }

    public LegionTexturePic(string textureName, int width = 0, int height = 0)
    {
        _textureName = textureName;
        CanMove = true;
        AcceptMouseInput = false;

        if (width > 0) Width = width;
        if (height > 0) Height = height;

        if (width <= 0 || height <= 0)
            TryApplyNaturalSize();
    }

    private void TryApplyNaturalSize()
    {
        if (ExternalImageLoader.Instance.TryGetNamedZipTexture(_textureName, out Texture2D tex) && tex != null && !tex.IsDisposed)
        {
            if (Width <= 0)  Width  = tex.Width;
            if (Height <= 0) Height = tex.Height;
        }
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed) return false;

        if (!ExternalImageLoader.Instance.TryGetNamedZipTexture(_textureName, out Texture2D tex) || tex == null || tex.IsDisposed)
            return false;

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);
        batcher.Draw(tex, new Rectangle(x, y, Width, Height), tex.Bounds, hueVector);

        return base.Draw(batcher, x, y);
    }
}
