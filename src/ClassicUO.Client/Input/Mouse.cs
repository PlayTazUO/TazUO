// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Mouse
    {
        public const int MOUSE_DELAY_DOUBLE_CLICK = 350;

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

        public static bool LButtonPressed { get; set; }

        public static bool RButtonPressed { get; set; }

        public static bool MButtonPressed { get; set; }

        public static bool XButtonPressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point LDragOffset => LButtonPressed ? Position - LClickPosition : Point.Zero;

        public static Point RDragOffset => RButtonPressed ? Position - RClickPosition : Point.Zero;

        public static Point MDragOffset => MButtonPressed ? Position - MClickPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

        public static int ControllerSensativity { get; set; } = 10;

        public static void Update()
        {
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
                Microsoft.Xna.Framework.Input.GamePadState gamePadState = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One);

                if (gamePadState.IsConnected && gamePadState.ThumbSticks.Right != Vector2.Zero)
                {
                    Position.X += (int)(ControllerSensativity * gamePadState.ThumbSticks.Right.X);
                    Position.Y -= (int)(ControllerSensativity * gamePadState.ThumbSticks.Right.Y);
                    SDL.SDL_WarpMouseInWindow(Client.Game.Window.Handle, Position.X, Position.Y);
                }
            }

            // Scale the mouse coordinates for the faux-backbuffer
            Position.X = (int)((double)Position.X * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width);

            Position.Y = (int)((double)Position.Y * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height);

            if (Client.Game.UO.World != null && Client.Game.UO.World.InGame && ProfileManager.CurrentProfile.GlobalScaling)
            {
                Position.X = (int)(Position.X / ProfileManager.CurrentProfile.GlobalScale);
                Position.Y = (int)(Position.Y / ProfileManager.CurrentProfile.GlobalScale);
            }

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
        }
    }
}
