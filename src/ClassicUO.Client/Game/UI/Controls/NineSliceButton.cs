using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class NineSliceButton : NineSliceControl
{
    private readonly Texture2D _textureUp;
    private readonly int _borderSizeUp;
    private readonly Texture2D _textureDown;
    private readonly int _borderSizeDown;
    private readonly ushort _hoverHue;
    private bool _hover;
    private ushort _oldHue;

    /// <summary>
    /// Textures are not disposed here.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="textureUp"></param>
    /// <param name="borderSizeUp"></param>
    /// <param name="textureDown"></param>
    /// <param name="borderSizeDown"></param>
    /// <param name="hoverHue"></param>
    public NineSliceButton(int width, int height, Texture2D textureUp, int borderSizeUp, Texture2D textureDown, int borderSizeDown, ushort hoverHue = 0) : base(width, height, textureUp, borderSizeUp)
    {
        _textureUp = textureUp;
        _borderSizeUp = borderSizeUp;
        _textureDown = textureDown;
        _borderSizeDown = borderSizeDown;
        _hoverHue = hoverHue;
    }

    protected override void OnMouseEnter(int x, int y)
    {
        base.OnMouseEnter(x, y);
        _oldHue = Hue;
        Hue = _hoverHue;
    }

    protected override void OnMouseExit(int x, int y)
    {
        base.OnMouseExit(x, y);
        Hue = _oldHue;
    }

    protected override void OnMouseDown(int x, int y, MouseButtonType button)
    {
        base.OnMouseDown(x, y, button);
        Texture = _textureDown;
        BorderSize = _borderSizeDown;
    }

    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);
        Texture = _textureUp;
        BorderSize = _borderSizeUp;
    }
}
