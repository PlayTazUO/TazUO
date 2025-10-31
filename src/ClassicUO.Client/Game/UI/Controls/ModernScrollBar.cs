using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class ModernScrollBar : ScrollBarBase
    {
        private const int BUTTON_SIZE = 16;
        private const int SCROLLBAR_WIDTH = 12;
        private const int THUMB_MIN_SIZE = 20;

        private Rectangle _thumbRect;
        private Rectangle _trackRect;
        private bool _thumbHovered;
        private bool _thumbPressed;

        public Color TrackColor { get; set; } = new Color(40, 40, 40, 200);
        public Color ThumbColor { get; set; } = new Color(120, 120, 120, 240);
        public Color ThumbHoverColor { get; set; } = new Color(150, 150, 150, 255);
        public Color ThumbPressedColor { get; set; } = new Color(90, 90, 90, 255);
        public Color ButtonColor { get; set; } = new Color(70, 70, 70, 240);
        public Color ButtonHoverColor { get; set; } = new Color(100, 100, 100, 255);
        public Color ButtonPressedColor { get; set; } = new Color(50, 50, 50, 255);
        public Color ArrowColor { get; set; } = new Color(220, 220, 220, 255);

        public void UpdateHeight(int newHeight)
        {
            if (Height != newHeight)
            {
                Height = newHeight;
                UpdateScrollBarGeometry();
            }
        }

        public ModernScrollBar(int x, int y, int height)
        {
            X = x;
            Y = y;
            Height = height;
            Width = SCROLLBAR_WIDTH;
            AcceptMouseInput = true;

            UpdateScrollBarGeometry();
        }

        private void UpdateScrollBarGeometry()
        {
            _rectUpButton = new Rectangle(0, 0, Width, BUTTON_SIZE);
            _rectDownButton = new Rectangle(0, Height - BUTTON_SIZE, Width, BUTTON_SIZE);

            _trackRect = new Rectangle(0, BUTTON_SIZE, Width, Height - (2 * BUTTON_SIZE));

            CalculateThumbRect();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Height <= 0 || !IsVisible)
                return false;

            Color trackColor = TrackColor;
            Color upButtonColor = _btUpClicked ? ButtonPressedColor : ButtonColor;
            Color downButtonColor = _btDownClicked ? ButtonPressedColor : ButtonColor;
            Color thumbColor = _thumbPressed ? ThumbPressedColor :
                           (_thumbHovered ? ThumbHoverColor : ThumbColor);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(trackColor),
                x + _trackRect.X,
                y + _trackRect.Y,
                _trackRect.Width,
                _trackRect.Height,
                ShaderHueTranslator.GetHueVector(0)
            );

            if (MaxValue > MinValue)
            {
                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(thumbColor),
                    x + _thumbRect.X,
                    y + _thumbRect.Y,
                    _thumbRect.Width,
                    _thumbRect.Height,
                    ShaderHueTranslator.GetHueVector(0)
                );
            }

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(upButtonColor),
                x + _rectUpButton.X,
                y + _rectUpButton.Y,
                _rectUpButton.Width,
                _rectUpButton.Height,
                ShaderHueTranslator.GetHueVector(0)
            );

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(downButtonColor),
                x + _rectDownButton.X,
                y + _rectDownButton.Y,
                _rectDownButton.Width,
                _rectDownButton.Height,
                ShaderHueTranslator.GetHueVector(0)
            );

            DrawArrows(batcher, x, y);

            return base.Draw(batcher, x, y);
        }

        private void DrawArrows(UltimaBatcher2D batcher, int x, int y)
        {
            int arrowSize = 6;
            int centerX = x + Width / 2;

            int upArrowY = y + _rectUpButton.Y + _rectUpButton.Height / 2;
            DrawUpArrow(batcher, centerX, upArrowY, arrowSize);

            int downArrowY = y + _rectDownButton.Y + _rectDownButton.Height / 2;
            DrawDownArrow(batcher, centerX, downArrowY, arrowSize);
        }

        private void DrawUpArrow(UltimaBatcher2D batcher, int centerX, int centerY, int size)
        {
            int halfSize = size / 2;
            for (int i = 0; i < halfSize; i++)
            {
                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(ArrowColor),
                    centerX - i,
                    centerY - halfSize + i,
                    2 * i + 1,
                    1,
                    ShaderHueTranslator.GetHueVector(0)
                );
            }
        }

        private void DrawDownArrow(UltimaBatcher2D batcher, int centerX, int centerY, int size)
        {
            int halfSize = size / 2;
            for (int i = 0; i < halfSize; i++)
            {
                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(ArrowColor),
                    centerX - (halfSize - i - 1),
                    centerY + i - halfSize,
                    2 * (halfSize - i - 1) + 1,
                    1,
                    ShaderHueTranslator.GetHueVector(0)
                );
            }
        }

        protected override int GetScrollableArea() => _trackRect.Height - _thumbRect.Height;

        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                y -= _trackRect.Y + (_thumbRect.Height / 2);

                if (y < 0)
                    y = 0;

                int scrollableArea = GetScrollableArea();
                if (y > scrollableArea)
                    y = scrollableArea;

                _sliderPosition = y;
                _clickPosition.X = x;
                _clickPosition.Y = y;

                _value = (int)(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
                CalculateThumbRect();
            }
        }

        private void CalculateThumbRect()
        {
            if (MaxValue <= MinValue)
            {
                _thumbRect = new Rectangle(1, _trackRect.Y, Width - 2, 0);
                return;
            }

            int totalRange = MaxValue - MinValue;
            float visibleRatio = (float)Height / (Height + totalRange);
            int thumbHeight = (int)(_trackRect.Height * visibleRatio);

            if (thumbHeight < THUMB_MIN_SIZE)
                thumbHeight = THUMB_MIN_SIZE;

            if (thumbHeight > _trackRect.Height)
                thumbHeight = _trackRect.Height;

            int scrollableArea = _trackRect.Height - thumbHeight;
            int thumbY = _trackRect.Y + (scrollableArea * (Value - MinValue) / (MaxValue - MinValue));

            _thumbRect = new Rectangle(1, (int)thumbY, Width - 2, thumbHeight);
        }

        public override void Update()
        {
            base.Update();
            CalculateThumbRect();

            var mousePos = new Point(Mouse.Position.X - ParentX - X, Mouse.Position.Y - ParentY - Y);
            _thumbHovered = _thumbRect.Contains(mousePos) && !_btnSliderClicked;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return;

            base.OnMouseDown(x, y, button);

            if (_thumbRect.Contains(x, y))
            {
                _thumbPressed = true;
                _btnSliderClicked = true;
                CalculateByPosition(x, y);
            }
            else if (_trackRect.Contains(x, y))
            {
                int thumbCenter = _thumbRect.Y + (_thumbRect.Height / 2);
                if (y < thumbCenter)
                    Value -= ScrollStep;
                else
                    Value += ScrollStep;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            _thumbPressed = false;
        }

        public override bool Contains(int x, int y) => x >= 0 && x <= Width && y >= 0 && y <= Height;
    }
}
