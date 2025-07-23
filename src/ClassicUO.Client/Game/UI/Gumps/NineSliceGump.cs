using System;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps;

public class NineSliceGump : Gump
{
    private Texture2D _customTexture;
    private Rectangle[] _slices = new Rectangle[9];
    private bool _resizable;
    private int _borderSize;
    private bool _isDragging;
    private Point _dragStartMousePos;
    private Point _dragStartPosition;
    private Point _dragStartSize;
    private ResizeCorner _dragCorner;
    private ResizeCorner _hoveredCorner;
    private int _cornerSize = 10;
    private ushort _hue;

    public ushort Hue
    {
        get => _hue;
        set
        {
            _hue = value;
        }
    }

    public bool Resizable
    {
        get => _resizable;
        set => _resizable = value;
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

    private enum ResizeCorner
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// Creates a 9-slice resizable gump
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Initial width</param>
    /// <param name="height">Initial height</param>
    /// <param name="texture">Texture to 9-slice (does not dispose, handle elsewhere)</param>
    /// <param name="borderSize">Size of the border slices</param>
    /// <param name="resizable">Whether the control can be resized by dragging corners</param>
    public NineSliceGump(int x, int y, int width, int height, Texture2D texture, int borderSize, bool resizable = true) : base(0, 0)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        _customTexture = texture;
        _borderSize = borderSize;
        _resizable = resizable;

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

    private ResizeCorner GetCornerAtPosition(int localX, int localY)
    {
        if (!_resizable) return ResizeCorner.None;

        // Top-left corner
        if (localX <= _cornerSize && localY <= _cornerSize)
            return ResizeCorner.TopLeft;

        // Top-right corner
        if (localX >= Width - _cornerSize && localY <= _cornerSize)
            return ResizeCorner.TopRight;

        // Bottom-left corner
        if (localX <= _cornerSize && localY >= Height - _cornerSize)
            return ResizeCorner.BottomLeft;

        // Bottom-right corner
        if (localX >= Width - _cornerSize && localY >= Height - _cornerSize)
            return ResizeCorner.BottomRight;

        return ResizeCorner.None;
    }

    protected override void OnMouseEnter(int x, int y)
    {
        base.OnMouseEnter(x, y);
        if (!_resizable) return;

        _hoveredCorner = GetCornerAtPosition(x, y);
    }

    protected override void OnMouseOver(int x, int y)
    {
        base.OnMouseOver(x, y);
        if (!_resizable) return;

        _hoveredCorner = GetCornerAtPosition(x, y);
    }

    protected override void OnMouseExit(int x, int y)
    {
        _hoveredCorner = ResizeCorner.None;
        base.OnMouseExit(x, y);
    }

    protected override void OnMouseDown(int x, int y, MouseButtonType button)
    {
        base.OnMouseDown(x, y, button);

        if (!_resizable || button != MouseButtonType.Left) return;

        _dragCorner = GetCornerAtPosition(x, y);

        if (_dragCorner != ResizeCorner.None && Mouse.LButtonPressed && MouseIsOver)
        {
            _isDragging = true;
            _dragStartMousePos = Mouse.Position; // Store absolute mouse position
            _dragStartSize = new Point(Width, Height);
            _dragStartPosition = new Point(X, Y);
        }
    }

    protected override void OnMouseUp(int x, int y, MouseButtonType button)
    {
        base.OnMouseUp(x, y, button);

        // Fixed: Properly reset drag state
        if (button == MouseButtonType.Left)
        {
            _isDragging = false;
            _dragCorner = ResizeCorner.None;
        }
    }

    public override void Update()
    {
        base.Update();

        if (_isDragging && _dragCorner != ResizeCorner.None && Mouse.LButtonPressed)
        {
            Point currentMousePos = Mouse.Position;

            // Calculate delta from absolute mouse positions
            int deltaX = currentMousePos.X - _dragStartMousePos.X;
            int deltaY = currentMousePos.Y - _dragStartMousePos.Y;

            int oldWidth = Width;
            int oldHeight = Height;
            int newWidth = Width;
            int newHeight = Height;
            int newX = X;
            int newY = Y;

            switch (_dragCorner)
            {
                case ResizeCorner.TopLeft:
                    // Top-left: position moves, size shrinks in both directions
                    newWidth = Math.Max(_borderSize * 2, _dragStartSize.X - deltaX);
                    newHeight = Math.Max(_borderSize * 2, _dragStartSize.Y - deltaY);
                    // Only move position if we're actually resizing (not at minimum size)
                    if (newWidth > _borderSize * 2 || deltaX < 0)
                        newX = _dragStartPosition.X + (_dragStartSize.X - newWidth);
                    if (newHeight > _borderSize * 2 || deltaY < 0)
                        newY = _dragStartPosition.Y + (_dragStartSize.Y - newHeight);
                    break;

                case ResizeCorner.TopRight:
                    // Top-right: X position stays fixed, width grows, height shrinks with Y moving
                    newWidth = Math.Max(_borderSize * 2, _dragStartSize.X + deltaX);
                    newHeight = Math.Max(_borderSize * 2, _dragStartSize.Y - deltaY);
                    newX = _dragStartPosition.X; // X position doesn't change
                    // Only move Y position to keep top-right corner fixed
                    if (newHeight > _borderSize * 2 || deltaY < 0)
                        newY = _dragStartPosition.Y + (_dragStartSize.Y - newHeight);
                    break;

                case ResizeCorner.BottomLeft:
                    // Bottom-left: Y position stays fixed, width shrinks with X moving, height grows
                    newWidth = Math.Max(_borderSize * 2, _dragStartSize.X - deltaX);
                    newHeight = Math.Max(_borderSize * 2, _dragStartSize.Y + deltaY);
                    newY = _dragStartPosition.Y; // Y position doesn't change
                    // Only move X position to keep bottom-left corner fixed
                    if (newWidth > _borderSize * 2 || deltaX < 0)
                        newX = _dragStartPosition.X + (_dragStartSize.X - newWidth);
                    break;

                case ResizeCorner.BottomRight:
                    // Bottom-right: position stays fixed, size grows in both directions
                    newWidth = Math.Max(_borderSize * 2, _dragStartSize.X + deltaX);
                    newHeight = Math.Max(_borderSize * 2, _dragStartSize.Y + deltaY);
                    // Position doesn't change for bottom-right
                    newX = _dragStartPosition.X;
                    newY = _dragStartPosition.Y;
                    break;
            }

            // Apply changes
            if (newWidth != Width || newHeight != Height || newX != X || newY != Y)
            {
                X = newX;
                Y = newY;
                Width = newWidth;
                Height = newHeight;
                OnResize(oldWidth, oldHeight, newWidth, newHeight);
            }
        }

        // Fixed: Reset drag state if mouse button is released (backup check)
        if (_isDragging && !Mouse.LButtonPressed)
        {
            _isDragging = false;
            _dragCorner = ResizeCorner.None;
        }
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (IsDisposed || _customTexture == null || _customTexture.IsDisposed)
        {
            return false;
        }

        Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

        DrawNineSlice(batcher, x, y, hueVector);

        if (_resizable && _hoveredCorner != ResizeCorner.None)
        {
            DrawCornerHighlight(batcher, x, y);
        }

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

    private void DrawCornerHighlight(UltimaBatcher2D batcher, int x, int y)
    {
        Vector3 whiteHue = ShaderHueTranslator.GetHueVector(0, false, 0.7f, true); // White with some transparency
        Rectangle highlightRect = Rectangle.Empty;

        switch (_hoveredCorner)
        {
            case ResizeCorner.TopLeft:
                highlightRect = new Rectangle(x, y, _cornerSize, _cornerSize);
                break;
            case ResizeCorner.TopRight:
                highlightRect = new Rectangle(x + Width - _cornerSize, y, _cornerSize, _cornerSize);
                break;
            case ResizeCorner.BottomLeft:
                highlightRect = new Rectangle(x, y + Height - _cornerSize, _cornerSize, _cornerSize);
                break;
            case ResizeCorner.BottomRight:
                highlightRect = new Rectangle(x + Width - _cornerSize, y + Height - _cornerSize, _cornerSize,
                    _cornerSize);
                break;
        }

        if (highlightRect != Rectangle.Empty)
        {
            // Draw a simple white rectangle as highlight - you might want to use a specific highlight texture
            batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), highlightRect, _slices[4], whiteHue); // Use center slice for highlight
        }
    }
}
