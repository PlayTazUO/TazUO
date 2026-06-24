#nullable enable
using System;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// One-shot input capture for assigning a hotkey from the UI. While active it listens to
    /// keyboard, mouse button, mouse wheel and controller input; the first qualifying input is
    /// turned into a <see cref="HotkeyBinding"/>, reported via the onCaptured callback, and capture
    /// then stops automatically. Escape cancels.
    /// </summary>
    public sealed class HotkeyCapture
    {
        private Action<HotkeyBinding>? _onCaptured;
        private Action? _onCancelled;
        private bool _active;

        public bool IsActive => _active;

        public void Start(Action<HotkeyBinding> onCaptured, Action? onCancelled = null)
        {
            Stop();

            _onCaptured = onCaptured;
            _onCancelled = onCancelled;
            _active = true;

            Keyboard.KeyDownEvent += OnKey;
            Mouse.ButtonDownEvent += OnMouseButton;
            Mouse.WheelEvent += OnWheel;
            Controller.ButtonDownEvent += OnController;
        }

        public void Stop()
        {
            if (!_active)
                return;

            _active = false;
            Keyboard.KeyDownEvent -= OnKey;
            Mouse.ButtonDownEvent -= OnMouseButton;
            Mouse.WheelEvent -= OnWheel;
            Controller.ButtonDownEvent -= OnController;
            _onCaptured = null;
            _onCancelled = null;
        }

        private void OnKey(string hotkey)
        {
            (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) = HotkeyUtil.ParseHotKeyString(hotkey);

            if (key == SDL.SDL_Keycode.SDLK_ESCAPE)
            {
                Action? cancel = _onCancelled;
                Stop();
                cancel?.Invoke();
                return;
            }

            if (key == SDL.SDL_Keycode.SDLK_UNKNOWN)
                return;

            Capture(new HotkeyBinding(key, mod));
        }

        private void OnMouseButton(MouseButtonType button)
        {
            // Left/Right operate the UI (including the "Set" button that started capture), so only the
            // middle and extra buttons can be bound — matching the legacy HotkeyBox.
            if (button != MouseButtonType.Middle && button != MouseButtonType.XButton1 && button != MouseButtonType.XButton2)
                return;

            Capture(new HotkeyBinding
            {
                MouseButton = button,
                Ctrl = Keyboard.Ctrl,
                Shift = Keyboard.Shift,
                Alt = Keyboard.Alt
            });
        }

        private void OnWheel(bool up)
        {
            Capture(new HotkeyBinding
            {
                WheelScroll = true,
                WheelUp = up,
                Ctrl = Keyboard.Ctrl,
                Shift = Keyboard.Shift,
                Alt = Keyboard.Alt
            });
        }

        private void OnController(SDL.SDL_GamepadButton button)
        {
            // Capture every button held at this instant so chords (e.g. LB + A) can be bound.
            SDL.SDL_GamepadButton[] pressed = Controller.PressedButtons();
            if (pressed.Length == 0)
                pressed = new[] { button };

            Capture(new HotkeyBinding { ControllerButtons = pressed });
        }

        private void Capture(HotkeyBinding binding)
        {
            Action<HotkeyBinding>? cb = _onCaptured;
            Stop();
            cb?.Invoke(binding);
        }
    }
}
