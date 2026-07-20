using System;
using ClassicUO.Input;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls;

public class GridScrollArea : Control
{
    /// <summary>Width reserved on the right edge for the scroll bar.</summary>
    public const int SCROLLBAR_WIDTH = 14;

    // The scroll bar is always Children[0]; content controls start at index 1. The loops
    // below iterate from index 1 to skip the scroll bar itself.
    private const int FIRST_CONTENT_CHILD = 1;

    private readonly ScrollBarBase _scrollBar;
    private int _lastWidth;
    private int _lastHeight;

    public GridScrollArea
    (
        int x,
        int y,
        int w,
        int h,
        int scrollMaxHeight = -1
    )
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
        _lastWidth = w;
        _lastHeight = h;

        _scrollBar = new ScrollBar(Width - SCROLLBAR_WIDTH, 0, Height);


        ScrollMaxHeight = scrollMaxHeight;

        _scrollBar.MinValue = 0;
        _scrollBar.MaxValue = scrollMaxHeight >= 0 ? scrollMaxHeight : Height;
        _scrollBar.Parent = this;

        AcceptMouseInput = true;
        WantUpdateSize = false;
        CanMove = true;
        ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
    }


    public int ScrollMaxHeight { get; set; } = -1;
    public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
    public int ScrollValue => _scrollBar.Value;
    public int ScrollMinValue => _scrollBar.MinValue;
    public int ScrollMaxValue => _scrollBar.MaxValue;

    public override void Update()
    {
        base.Update();

        CalculateScrollBarMaxValue();

        if (Width != _lastWidth || Height != _lastHeight)
        {
            _scrollBar.X = Width - SCROLLBAR_WIDTH;
            _scrollBar.Height = Height;
            _lastWidth = Width;
            _lastHeight = Height;
        }

        if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
        {
            _scrollBar.IsVisible = true;
        }
        else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
        {
            _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
        }
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
        _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

        if (batcher.ClipBegin(x, y, Width - SCROLLBAR_WIDTH, Height))
        {
            for (int i = FIRST_CONTENT_CHILD; i < Children.Count; i++)
            {
                IGui child = Children[i];

                if (!child.IsVisible)
                {
                    continue;
                }

                int finalY = y + child.Y - _scrollBar.Value;

                child.Draw(batcher, x + child.X, finalY);
            }

            batcher.ClipEnd();
        }

        return true;
    }

    public override void OnMouseWheel(MouseEventType delta)
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
        for (int i = FIRST_CONTENT_CHILD; i < Children.Count; i++)
        {
            Children[i].Dispose();
        }
    }

    private void CalculateScrollBarMaxValue()
    {
        _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
        bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

        int startX = 0, startY = 0, endX = 0, endY = 0;

        for (int i = FIRST_CONTENT_CHILD; i < Children.Count; i++)
        {
            IGui c = Children[i];

            if (c.IsVisible && !c.IsDisposed)
            {
                if (c.X < startX)
                {
                    startX = c.X;
                }

                if (c.Y < startY)
                {
                    startY = c.Y;
                }

                if (c.Bounds.Right > endX)
                {
                    endX = c.Bounds.Right;
                }

                if (c.Bounds.Bottom > endY)
                {
                    endY = c.Bounds.Bottom;
                }
            }
        }

        //int width = Math.Abs(startX) + Math.Abs(endX);
        int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
        height = Math.Max(0, height);

        if (height > 0)
        {
            _scrollBar.MaxValue = height;

            if (maxValue)
            {
                _scrollBar.Value = _scrollBar.MaxValue;
            }
        }
        else
        {
            _scrollBar.Value = _scrollBar.MaxValue = 0;
        }

        _scrollBar.UpdateOffset(0, Offset.Y);

        for (int i = FIRST_CONTENT_CHILD; i < Children.Count; i++)
        {
            Children[i].UpdateOffset(0, -_scrollBar.Value);
        }
    }
}
