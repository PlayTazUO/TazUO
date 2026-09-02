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

        /// <summary>
        /// Invoked whenever any mouse button is pressed. Used by hotkey capture in the UI.
        /// </summary>
        public static event Action<MouseButtonType> ButtonDownEvent;

        /// <summary>
        /// Invoked on mouse wheel scroll; the argument is true when scrolled up. Used by hotkey capture.
        /// </summary>
        public static event Action<bool> WheelEvent;

        /// <summary>Raise <see cref="WheelEvent"/>. Called from the SDL wheel dispatch.</summary>
        public static void RaiseWheelEvent(bool up) => WheelEvent?.Invoke(up);

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
                    XButton1Pressed = true;
                    XButtonPressed = true;

                    break;

                case MouseButtonType.XButton2:
                    XButton2Pressed = true;
                    XButtonPressed = true;

                    break;
            }

            ButtonDownEvent?.Invoke(type);

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
                    XButton1Pressed = false;
                    XButtonPressed = XButton2Pressed;

                    break;

                case MouseButtonType.XButton2:
                    XButton2Pressed = false;
                    XButtonPressed = XButton1Pressed;

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

        public static bool XButton1Pressed { get; set; }

        public static bool XButton2Pressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point LDragOffset => LButtonPressed ? Position - LClickPosition : Point.Zero;

        public static Point RDragOffset => RButtonPressed ? Position - RClickPosition : Point.Zero;

        public static Point MDragOffset => MButtonPressed ? Position - MClickPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

        public static int ControllerSensitivity { get; set; } = 10;

        private static bool _isWarpingMouse = false;

        // Raw cursor position in window coordinates, as reported by SDL. <see cref="Position"/> is
        // this value transformed into game (backbuffer/RenderScale) coordinates by <see cref="FinalizePosition"/>.
        private static Point _sdlPosition;

        // SDL_GetWindowPosition is a P/Invoke; cached here and refreshed only on SDL_EVENT_WINDOW_MOVED.
        private static Point _windowPosition;
        private static bool _windowPositionCached;

        // Gates the gamepad warp path so GamePad.GetState (a P/Invoke) isn't hit every frame when no
        // pad is attached. Maintained from SDL_EVENT_GAMEPAD_ADDED/REMOVED.
        private static bool _gamepadConnected;

        private static MouseMovedEventArgs _mouseMovedEventArg = new(Position, Position);

        /// <summary>
        /// Refreshes the cached window position, used to convert global cursor coords to window
        /// coords while the cursor is outside the window. Called from SDL_EVENT_WINDOW_MOVED.
        /// </summary>
        public static void OnWindowMoved(int x, int y)
        {
            _windowPosition.X = x;
            _windowPosition.Y = y;
            _windowPositionCached = true;
        }

        /// <summary>
        /// Tracks gamepad connection state so <see cref="Update"/> skips <c>GamePad.GetState</c>
        /// entirely when no pad is attached. Called from SDL_EVENT_GAMEPAD_ADDED/REMOVED.
        /// </summary>
        public static void SetGamepadConnected(bool connected) => _gamepadConnected = connected;

        /// <summary>
        /// Updates the cursor position from SDL_EVENT_MOUSE_MOTION coordinates. While the cursor is
        /// inside the window position is fully event-driven, replacing the per-frame SDL_GetMouseState poll.
        /// </summary>
        public static void SetPositionFromEvent(float x, float y)
        {
            Point previous = Position;
            _sdlPosition.X = (int)x;
            _sdlPosition.Y = (int)y;
            FinalizePosition(previous);
        }

        /// <summary>
        /// Per-frame update. Re-syncs SDL's authoritative cursor state when <paramref name="resyncPosition"/>
        /// is set (button/wheel events, cursor re-entry) or while the cursor is outside the window (no motion
        /// events arrive there), and applies the gamepad right-stick mouse warp, gated on a connected pad.
        /// </summary>
        public static void Update(bool resyncPosition = false)
        {
            if (_isWarpingMouse)
                return;

            Point previous = Position;
            bool changed = false;

            if (resyncPosition || !MouseInWindow)
            {
                if (!MouseInWindow)
                {
                    if (!_windowPositionCached)
                    {
                        SDL.SDL_GetWindowPosition(Client.Game.Window.Handle, out int winX, out int winY);
                        _windowPosition.X = winX;
                        _windowPosition.Y = winY;
                        _windowPositionCached = true;
                    }

                    SDL.SDL_GetGlobalMouseState(out float gx, out float gy);
                    _sdlPosition.X = (int)gx - _windowPosition.X;
                    _sdlPosition.Y = (int)gy - _windowPosition.Y;
                }
                else
                {
                    SDL.SDL_GetMouseState(out float x, out float y);
                    _sdlPosition.X = (int)x;
                    _sdlPosition.Y = (int)y;
                }

                changed = true;
            }

            if (_gamepadConnected)
            {
                GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

                if (gamePadState.ThumbSticks.Right != Vector2.Zero)
                {
                    _sdlPosition.X += (int)(ControllerSensitivity * gamePadState.ThumbSticks.Right.X);
                    _sdlPosition.Y -= (int)(ControllerSensitivity * gamePadState.ThumbSticks.Right.Y);

                    _isWarpingMouse = true;
                    SDL.SDL_WarpMouseInWindow(Client.Game.Window.Handle, _sdlPosition.X, _sdlPosition.Y);
                    _isWarpingMouse = false;

                    changed = true;
                }
            }

            if (changed)
            {
                FinalizePosition(previous);
            }
        }

        /// <summary>
        /// Transforms <see cref="_sdlPosition"/> into game coordinates on <see cref="Position"/>,
        /// refreshes <see cref="IsDragging"/> and raises <see cref="Moved"/> when the position changed.
        /// </summary>
        private static void FinalizePosition(in Point previous)
        {
            Position.X = (int)(((double)_sdlPosition.X * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width) / Client.Game.RenderScale);

            Position.Y = (int)(((double)_sdlPosition.Y * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height) / Client.Game.RenderScale);

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;

            if (Moved != null && previous != Position){
                _mouseMovedEventArg.Previous = previous;
                _mouseMovedEventArg.Current = Position;
                Moved?.Invoke(null, _mouseMovedEventArg);
            }
        }
    }
}
