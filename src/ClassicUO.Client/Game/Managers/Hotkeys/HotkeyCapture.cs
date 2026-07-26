#nullable enable
using System;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// Input capture for assigning a hotkey from the UI. While active it listens to keyboard, mouse
    /// button, mouse wheel and controller input; each qualifying input is turned into a
    /// <see cref="HotkeyBinding"/> and reported via the onCaptured callback.
    ///
    /// In the default one-shot mode (<see cref="AutoStop"/> is <see langword="true"/>) the first
    /// qualifying input stops the capture automatically and Escape cancels. When <see cref="AutoStop"/>
    /// is <see langword="false"/> the capture keeps listening after every input and never cancels on
    /// its own; the owner (e.g. the hotkey capture window) is responsible for calling <see cref="Stop"/>.
    /// </summary>
    public sealed class HotkeyCapture
    {
        private Action<HotkeyBinding>? _onCaptured;
        private Action? _onCancelled;
        private SDL.SDL_Keymod _modAccum;

        public bool IsActive { get; private set; }

        public bool CapturesMouseEvents { get; set; } = true;

        /// <summary>
        /// When <see langword="true"/> (default) the capture stops after the first input and Escape
        /// cancels. When <see langword="false"/> the capture keeps listening after each captured input
        /// and Escape is ignored, leaving the owner to stop it explicitly.
        /// </summary>
        public bool AutoStop { get; set; } = true;

        public void Start(Action<HotkeyBinding> onCaptured, Action? onCancelled = null)
        {
            Stop();

            _onCaptured = onCaptured;
            _onCancelled = onCancelled;
            _modAccum = SDL.SDL_Keymod.SDL_KMOD_NONE;
            IsActive = true;

            // Suppress hotkeys globally until the capture is stopped
            HotKeys.RequestDisableHotkeys();

            Keyboard.KeyDownEvent += OnKey;
            Keyboard.BareModifierEvent += OnBareModifier;

            if (CapturesMouseEvents)
            {
                Mouse.ButtonDownEvent += OnMouseButton;
                Mouse.WheelEvent += OnWheel;
            }

            Controller.ButtonDownEvent += OnController;
        }

        public void Stop()
        {
            if (!IsActive)
                return;

            IsActive = false;
            Keyboard.KeyDownEvent -= OnKey;
            Keyboard.BareModifierEvent -= OnBareModifier;
            Mouse.ButtonDownEvent -= OnMouseButton;
            Mouse.WheelEvent -= OnWheel;
            Controller.ButtonDownEvent -= OnController;
            _onCaptured = null;
            _onCancelled = null;
            _modAccum = SDL.SDL_Keymod.SDL_KMOD_NONE;

            HotKeys.ReleaseDisableHotkeys();
        }

        private void OnKey(string hotkey)
        {
            (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) = HotkeyUtil.ParseHotKeyString(hotkey);

            if (key == SDL.SDL_Keycode.SDLK_ESCAPE)
            {
                // In continuous mode the owning window handles cancellation via its own buttons,
                // so Escape is simply ignored rather than tearing down the capture.
                if (!AutoStop)
                    return;

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
            if (button == MouseButtonType.Left || button == MouseButtonType.Right)
                return;

            Capture(new HotkeyBinding
            {
                MouseButton = button,
                Ctrl = Keyboard.Ctrl,
                Shift = Keyboard.Shift,
                Alt = Keyboard.Alt
            });
        }

        private void OnWheel(bool up) =>
            Capture(new HotkeyBinding
            {
                WheelScroll = true,
                WheelUp = up,
                Ctrl = Keyboard.Ctrl,
                Shift = Keyboard.Shift,
                Alt = Keyboard.Alt
            });

        private void OnBareModifier(SDL.SDL_Keymod mods)
        {
            if (mods != SDL.SDL_Keymod.SDL_KMOD_NONE)
            {
                // Remember the largest combo held so chords (e.g. Ctrl+Shift) can be captured;
                // commit only once everything is released.
                _modAccum |= mods;
                return;
            }

            if (_modAccum == SDL.SDL_Keymod.SDL_KMOD_NONE)
                return;

            // Reset the accumulator before reporting so continuous (non-auto-stop) capture starts
            // fresh on the next chord instead of unioning it with the one just committed.
            SDL.SDL_Keymod accumulated = _modAccum;
            _modAccum = SDL.SDL_Keymod.SDL_KMOD_NONE;

            Capture(new HotkeyBinding
            {
                Ctrl = (accumulated & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0,
                Shift = (accumulated & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0,
                Alt = (accumulated & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0
            });
        }

        private void OnController(SDL.SDL_GamepadButton button)
        {
            // Capture every button held at this instant so chords (e.g. LB + A) can be bound.
            SDL.SDL_GamepadButton[] pressed = Controller.PressedButtons();
            if (pressed.Length == 0)
                pressed = [button];

            Capture(new HotkeyBinding { ControllerButtons = pressed });
        }

        private void Capture(HotkeyBinding binding)
        {
            if (!AutoStop)
            {
                // Continuous mode: report the binding but keep listening so the user can keep
                // adjusting until they explicitly save or cancel.
                _onCaptured?.Invoke(binding);
                return;
            }

            Action<HotkeyBinding>? cb = _onCaptured;
            Stop();
            cb?.Invoke(binding);
        }
    }
}
