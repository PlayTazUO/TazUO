// SPDX-License-Identifier: BSD-2-Clause

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Mouse
    {
        public const int MOUSE_DELAY_DOUBLE_CLICK = 350;

        /// <summary>
        /// Invoked whenever the mouse position changes
        /// </summary>
        public static event EventHandler<MouseMovedEventArgs> Moved;

        /// <summary>
        /// Invoked whenever the left mouse button is pressed or released
        /// </summary>
        public static event EventHandler<MouseLeftButtonClickStateChangedEventArgs> LeftButtonClickStateChanged;

        public static MouseInfo GetMyraMouseInfo()
        {
            var info = new MouseInfo();

            info.IsLeftButtonDown = LButtonPressed;
            info.IsRightButtonDown = RButtonPressed;
            info.IsMiddleButtonDown = MButtonPressed;
            info.Position = Position;

            MouseState fnaMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            info.Wheel = fnaMouseState.ScrollWheelValue;

            return info;
        }

        /* Log a button press event at the given time. */
        public static void ButtonPress(MouseButtonType type)
        {
            CancelDoubleClick = false;

            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = true;
                    LClickPosition = Position;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = true;
                    MClickPosition = Position;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = true;
                    RClickPosition = Position;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = true;

                    break;
            }

            SDL.SDL_CaptureMouse(true);
        }

        /* Log a button release event at the given time */
        public static void ButtonRelease(MouseButtonType type)
        {
            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = false;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = false;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = false;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = false;

                    break;
            }

            if (!(LButtonPressed || RButtonPressed || MButtonPressed))
            {
                SDL.SDL_CaptureMouse(false);
            }
        }

        public static Point Position;

        public static Point LClickPosition;

        public static Point RClickPosition;

        public static Point MClickPosition;

        public static uint LastLeftButtonClickTime { get; set; }

        public static uint LastMidButtonClickTime { get; set; }

        public static uint LastRightButtonClickTime { get; set; }

        public static bool CancelDoubleClick { get; set; }

        public static bool LButtonPressed
        {
            get;
            set
            {
                if (field == value)
                    return;

                var eArgs = new MouseLeftButtonClickStateChangedEventArgs(field, value);

                field = value;
                LeftButtonClickStateChanged?.Invoke(null, eArgs);
            }
        }

        public static bool RButtonPressed { get; set; }

        public static bool MButtonPressed { get; set; }

        public static bool XButtonPressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point LDragOffset => LButtonPressed ? Position - LClickPosition : Point.Zero;

        public static Point RDragOffset => RButtonPressed ? Position - RClickPosition : Point.Zero;

        public static Point MDragOffset => MButtonPressed ? Position - MClickPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

        public static int ControllerSensitivity { get; set; } = 10;

        private static bool _isWarpingMouse = false;

        public static void Update()
        {
            if (_isWarpingMouse)
                return;

            Point previous = Position;

            if (!MouseInWindow)
            {
                SDL.SDL_GetGlobalMouseState(out float x, out float y);
                SDL.SDL_GetWindowPosition(Client.Game.Window.Handle, out int winX, out int winY);
                Position.X = (int)x - winX;
                Position.Y = (int)y - winY;
            }
            else
            {
                SDL.SDL_GetMouseState(out float x, out float y);
                Position.X = (int)x;
                Position.Y = (int)y;
                GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

                if (gamePadState.IsConnected && gamePadState.ThumbSticks.Right != Vector2.Zero)
                {
                    Position.X += (int)(ControllerSensitivity * gamePadState.ThumbSticks.Right.X);
                    Position.Y -= (int)(ControllerSensitivity * gamePadState.ThumbSticks.Right.Y);

                    _isWarpingMouse = true;
                    SDL.SDL_WarpMouseInWindow(Client.Game.Window.Handle, Position.X, Position.Y);
                    _isWarpingMouse = false;
                }
            }

            Position.X = (int)(((double)Position.X * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width) / Client.Game.RenderScale);

            Position.Y = (int)(((double)Position.Y * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height) / Client.Game.RenderScale);

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;

            // Check for null first;
            // While a point comparison is not a 'heavy' operation, a null check should generally be quicker.
            if (Moved != null && previous != Position)
                Moved?.Invoke(null, new MouseMovedEventArgs(previous, Position));
        }
    }
}
