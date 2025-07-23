using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class NineSliceControl : Control
{
    private Texture2D _customTexture;
    private Rectangle[] _slices = new Rectangle[9];
    private int _borderSize;
    private ushort _hue;

    public Texture2D Texture
    {
        get => _customTexture;
        set
        {
            _customTexture = value;
            CalculateSlices();
        }
    }
    public ushort Hue
    {
        get => _hue;
        set
        {
            _hue = value;
        }
    }

    public int BorderSize
    {
        get => _borderSize;
        set
        {
            _borderSize = value;
            CalculateSlices();
        }
    }

    /// <summary>
    /// Creates a 9-slice resizable control
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Initial width</param>
    /// <param name="height">Initial height</param>
    /// <param name="texture">Texture to 9-slice (does not dispose, handle elsewhere)</param>
    /// <param name="borderSize">Size of the border slices</param>
    public NineSliceControl(int width, int height, Texture2D texture, int borderSize)
    {
        Width = width;
        Height = height;
        _customTexture = texture;
        _borderSize = borderSize;

        CalculateSlices();
        AcceptMouseInput = true;
        CanMove = true;
        WantUpdateSize = false;
    }

    private void CalculateSlices()
    {
        if (_customTexture == null) return;

        int texWidth = _customTexture.Width;
        int texHeight = _customTexture.Height;

        // Top row
        _slices[0] = new Rectangle(0, 0, _borderSize, _borderSize); // Top-left
        _slices[1] = new Rectangle(_borderSize, 0, texWidth - _borderSize * 2, _borderSize); // Top-center
        _slices[2] = new Rectangle(texWidth - _borderSize, 0, _borderSize, _borderSize); // Top-right

        // Middle row
        _slices[3] = new Rectangle(0, _borderSize, _borderSize, texHeight - _borderSize * 2); // Middle-left
        _slices[4] =
            new Rectangle(_borderSize, _borderSize, texWidth - _borderSize * 2,
                texHeight - _borderSize * 2); // Middle-center
        _slices[5] =
            new Rectangle(texWidth - _borderSize, _borderSize, _borderSize,
                texHeight - _borderSize * 2); // Middle-right

        // Bottom row
        _slices[6] = new Rectangle(0, texHeight - _borderSize, _borderSize, _borderSize); // Bottom-left
        _slices[7] =
            new Rectangle(_borderSize, texHeight - _borderSize, texWidth - _borderSize * 2,
                _borderSize); // Bottom-center
        _slices[8] =
            new Rectangle(texWidth - _borderSize, texHeight - _borderSize, _borderSize, _borderSize); // Bottom-right
    }

    protected virtual void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed || _customTexture == null || _customTexture.IsDisposed)
        {
            return false;
        }

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

        DrawNineSlice(batcher, x, y, hueVector);

        return base.Draw(batcher, x, y);
    }

    private void DrawNineSlice(UltimaBatcher2D batcher, int x, int y, Vector3 hueVector)
    {
        // Top row
        batcher.Draw(_customTexture, new Rectangle(x, y, _borderSize, _borderSize), _slices[0], hueVector); // Top-left
        batcher.Draw(_customTexture, new Rectangle(x + _borderSize, y, Width - _borderSize * 2, _borderSize),
            _slices[1], hueVector); // Top-center
        batcher.Draw(_customTexture, new Rectangle(x + Width - _borderSize, y, _borderSize, _borderSize), _slices[2],
            hueVector); // Top-right

        // Middle row
        batcher.Draw(_customTexture, new Rectangle(x, y + _borderSize, _borderSize, Height - _borderSize * 2),
            _slices[3], hueVector); // Middle-left
        batcher.Draw(_customTexture,
            new Rectangle(x + _borderSize, y + _borderSize, Width - _borderSize * 2, Height - _borderSize * 2),
            _slices[4], hueVector); // Middle-center
        batcher.Draw(_customTexture,
            new Rectangle(x + Width - _borderSize, y + _borderSize, _borderSize, Height - _borderSize * 2), _slices[5],
            hueVector); // Middle-right

        // Bottom row
        batcher.Draw(_customTexture, new Rectangle(x, y + Height - _borderSize, _borderSize, _borderSize), _slices[6],
            hueVector); // Bottom-left
        batcher.Draw(_customTexture,
            new Rectangle(x + _borderSize, y + Height - _borderSize, Width - _borderSize * 2, _borderSize), _slices[7],
            hueVector); // Bottom-center
        batcher.Draw(_customTexture,
            new Rectangle(x + Width - _borderSize, y + Height - _borderSize, _borderSize, _borderSize), _slices[8],
            hueVector); // Bottom-right
    }
}
