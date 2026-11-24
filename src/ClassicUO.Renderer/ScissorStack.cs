// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    internal static class ScissorStack
    {
        private static readonly Stack<Rectangle> _scissors = new Stack<Rectangle>();

        public static bool HasScissors => _scissors.Count - 1 > 0;

        public static bool PushScissors(GraphicsDevice device, Rectangle scissor)
        {
            if (_scissors.Count > 0)
            {
                Rectangle parent = _scissors.Peek();
                int minX = Math.Max(parent.X, scissor.X);
                int maxX = Math.Min(parent.X + parent.Width, scissor.X + scissor.Width);

                if (maxX - minX < 1)
                {
                    return false;
                }

                int minY = Math.Max(parent.Y, scissor.Y);
                int maxY = Math.Min(parent.Y + parent.Height, scissor.Y + scissor.Height);

                if (maxY - minY < 1)
                {
                    return false;
                }

                scissor.X = minX;
                scissor.Y = minY;
                scissor.Width = maxX - minX;
                scissor.Height = Math.Max(1, maxY - minY);
            }

            _scissors.Push(scissor);
            device.ScissorRectangle = scissor;

            return true;
        }

        public static Rectangle PopScissors(GraphicsDevice device)
        {
            Rectangle scissors = _scissors.Pop();

            if (_scissors.Count == 0)
            {
                device.ScissorRectangle = device.Viewport.Bounds;
            }
            else
            {
                device.ScissorRectangle = _scissors.Peek();
            }

            return scissors;
        }

        /// <summary>
        ///     Drops every pushed scissor and restores the full-viewport scissor rectangle.
        /// </summary>
        /// <remarks>
        ///     Only for unwinding out of an exception that left a <c>ClipBegin</c>/<c>ClipEnd</c> pair
        ///     unfinished: the stack is process-wide, so a stale entry would clip everything drawn afterwards.
        /// </remarks>
        /// <param name="device">Device whose scissor rectangle is restored.</param>
        public static void Clear(GraphicsDevice device)
        {
            _scissors.Clear();
            device.ScissorRectangle = device.Viewport.Bounds;
        }

        public static Rectangle CalculateScissors(Matrix batchTransform, int sx, int sy, int sw, int sh)
        {
            var tmp = new Vector2(sx, sy);
            Vector2.Transform(ref tmp, ref batchTransform, out tmp);

            var newScissor = new Rectangle
            {
                X = (int) tmp.X, Y = (int) tmp.Y
            };

            tmp.X = sx + sw;
            tmp.Y = sy + sh;
            Vector2.Transform(ref tmp, ref batchTransform, out tmp);
            newScissor.Width = (int) tmp.X - newScissor.X;
            newScissor.Height = (int) tmp.Y - newScissor.Y;

            return newScissor;
        }
    }
}