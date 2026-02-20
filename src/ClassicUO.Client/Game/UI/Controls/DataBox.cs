// SPDX-License-Identifier: BSD-2-Clause


using System;

namespace ClassicUO.Game.UI.Controls
{
    public class DataBox : Control
    {
        public DataBox(int x, int y, int w, int h)
        {
            CanMove = false;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }

        public bool ContainsByBounds { get; set; }

        public override T Add<T>(T c, int page = 0)
        {
            base.Add(c, page);
            c.UpdateOffset(0, Offset.Y);
            return c;
        }

        public void ReArrangeChildrenGridStyle(int vspacing = 0, int hspacing = 0)
        {
            // Grid layout: left to right, top to bottom
            int currentX = 0;
            int currentY = 0;
            int rowHeight = 0;

            for (int i = 0; i < Children.Count; ++i)
            {
                IGui c = Children[i];

                if (!c.IsVisible || c.IsDisposed)
                    continue;

                // If adding this control would exceed the width, move to next row
                if (currentX + c.Width > Width && currentX > 0)
                {
                    currentX = 0;
                    currentY += rowHeight + vspacing;
                    rowHeight = 0;
                }

                // Position the control
                c.X = currentX;
                c.Y = currentY;

                // Update position for next control
                currentX += c.Width + hspacing;

                // Keep track of tallest control in this row to determine next row's Y position
                rowHeight = Math.Max(rowHeight, c.Height);
            }
        }

        public void ReArrangeChildren(int vspacing = 0)
        {
            for (int i = 0, height = 0; i < Children.Count; ++i)
            {
                IGui c = Children[i];

                if (c.IsVisible && !c.IsDisposed)
                {
                    c.Y = height;

                    height += c.Height + vspacing;
                }
            }

            WantUpdateSize = true;
        }

        public override bool Contains(int x, int y)
        {
            if (ContainsByBounds)
            {
                return true;
            }

            IGui t = null;
            x += ScreenCoordinateX;
            y += ScreenCoordinateY;

            foreach (IGui child in Children)
            {
                child.HitTest(x, y, ref t);

                if (t != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
