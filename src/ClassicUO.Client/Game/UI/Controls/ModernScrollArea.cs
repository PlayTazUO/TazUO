using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    public class ModernScrollArea : Control
    {
        private readonly ModernScrollBar _scrollBar;
        private const int SCROLLBAR_WIDTH = 12;

        public ModernScrollArea(int x, int y, int w, int h, int scrollMaxHeight = -1)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _scrollBar = new ModernScrollBar(Width - SCROLLBAR_WIDTH, 0, Height)
            {
                Parent = this
            };

            ScrollMaxHeight = scrollMaxHeight;
            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scrollMaxHeight >= 0 ? scrollMaxHeight : Height;

            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = true;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
        }

        public int ScrollMaxHeight { get; set; } = -1;
        public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
        public int ScrollValue => _scrollBar.Value;
        public int ScrollMinValue => _scrollBar.MinValue;
        public int ScrollMaxValue => _scrollBar.MaxValue;
        public Rectangle ScissorRectangle;

        public Color TrackColor
        {
            get => _scrollBar.TrackColor;
            set => _scrollBar.TrackColor = value;
        }

        public Color ThumbColor
        {
            get => _scrollBar.ThumbColor;
            set => _scrollBar.ThumbColor = value;
        }

        public Color ThumbHoverColor
        {
            get => _scrollBar.ThumbHoverColor;
            set => _scrollBar.ThumbHoverColor = value;
        }

        public Color ThumbPressedColor
        {
            get => _scrollBar.ThumbPressedColor;
            set => _scrollBar.ThumbPressedColor = value;
        }

        public Color ButtonColor
        {
            get => _scrollBar.ButtonColor;
            set => _scrollBar.ButtonColor = value;
        }

        public Color ButtonHoverColor
        {
            get => _scrollBar.ButtonHoverColor;
            set => _scrollBar.ButtonHoverColor = value;
        }

        public Color ButtonPressedColor
        {
            get => _scrollBar.ButtonPressedColor;
            set => _scrollBar.ButtonPressedColor = value;
        }

        public Color ArrowColor
        {
            get => _scrollBar.ArrowColor;
            set => _scrollBar.ArrowColor = value;
        }

        public override void PreDraw()
        {
            base.PreDraw();
            CalculateScrollBarMaxValue();

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }
        }

        public void UpdateScrollbarPosition() => _scrollBar.X = Width - SCROLLBAR_WIDTH;

        public void UpdateHeight(int newHeight)
        {
            if (Height != newHeight)
            {
                Height = newHeight;
                _scrollBar.UpdateHeight(newHeight);
            }
        }

        public void UpdateWidth(int newWidth)
        {
            if (Width != newWidth)
            {
                Width = newWidth;
                _scrollBar.X = Width - SCROLLBAR_WIDTH;
            }
        }

        public void ResetScrollbarPosition() => _scrollBar?.ResetScrollPosition();

        public int ScrollBarWidth()
        {
            if (_scrollBar == null)
                return 0;
            return _scrollBar.Width;
        }

        public void Scroll(bool isup)
        {
            if (isup)
            {
                _scrollBar.Value -= _scrollBar.ScrollStep;
            }
            else
            {
                _scrollBar.Value += _scrollBar.ScrollStep;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

            int contentWidth = Width - (_scrollBar.IsVisible ? SCROLLBAR_WIDTH : 0);

            if (batcher.ClipBegin(
                x + ScissorRectangle.X,
                y + ScissorRectangle.Y,
                contentWidth + ScissorRectangle.Width,
                Height + ScissorRectangle.Height))
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (child == _scrollBar || !child.IsVisible)
                        continue;

                    int finalY = y + child.Y - _scrollBar.Value + ScissorRectangle.Y;
                    child.Draw(batcher, x + child.X, finalY);
                }

                batcher.ClipEnd();
            }

            return true;
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                    break;

                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;
                    break;
            }
        }

        public override void Clear()
        {
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i] != _scrollBar)
                {
                    Children[i].Dispose();
                }
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int startX = 0, startY = 0, endX = 0, endY = 0;

            for (int i = 0; i < Children.Count; i++)
            {
                Control c = Children[i];

                if (c == _scrollBar || !c.IsVisible || c.IsDisposed)
                    continue;

                if (c.X < startX)
                    startX = c.X;

                if (c.Y < startY)
                    startY = c.Y;

                if (c.Bounds.Right > endX)
                    endX = c.Bounds.Right;

                if (c.Bounds.Bottom > endY)
                    endY = c.Bounds.Bottom;
            }

            int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
            height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                    _scrollBar.Value = _scrollBar.MaxValue;
            }
            else
            {
                _scrollBar.Value = _scrollBar.MaxValue = 0;
            }

            _scrollBar.UpdateOffset(0, Offset.Y);

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] != _scrollBar)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }
        }
    }
}
