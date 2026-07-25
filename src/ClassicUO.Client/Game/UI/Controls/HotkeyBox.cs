// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using SDL3;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// A compact label that shows the currently assigned hotkey and, when clicked, opens the shared
    /// <see cref="HotkeyCaptureWindow"/> to record a new one. All input capture happens inside that
    /// single window; this control only displays the result and raises <see cref="HotkeyChanged"/>
    /// (or <see cref="HotkeyCancelled"/> when the binding is cleared).
    /// </summary>
    public class HotkeyBox : Control
    {
        private readonly HoveredLabel _label;
        private HotkeyCaptureWindow _captureWindow;

        public HotkeyBox()
        {
            CanMove = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            Width = 150;
            Height = 25;

            ResizePic pic;

            Add
            (
                pic = new ResizePic(0x0BB8)
                {
                    Width = 150,
                    Height = Height
                }
            );

            pic.MouseUp += LabelOnMouseUp;

            Add
            (
                _label = new HoveredLabel
                (
                    string.Empty,
                    true,
                    1,
                    0x0021,
                    0x0021,
                    150,
                    1,
                    FontStyle.None,
                    TEXT_ALIGN_TYPE.TS_CENTER
                )
                {
                    Y = 5
                }
            );

            _label.MouseUp += LabelOnMouseUp;

            WantUpdateSize = false;
        }

        public SDL.SDL_Keycode Key { get; private set; }
        public SDL.SDL_GamepadButton[] Buttons { get; private set; }
        public MouseButtonType MouseButton { get; private set; }
        public bool WheelScroll { get; private set; }
        public bool WheelUp { get; private set; }
        public SDL.SDL_Keymod Mod { get; private set; }

        public event EventHandler HotkeyChanged, HotkeyCancelled;

        public void SetButtons(SDL.SDL_GamepadButton[] buttons)
        {
            ResetBinding();
            Buttons = buttons;
            _label.Text = Controller.GetButtonNames(buttons);
        }

        public void SetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_UNKNOWN && mod == SDL.SDL_Keymod.SDL_KMOD_NONE)
            {
                ResetBinding();

                Key = key;
                Mod = mod;
            }
            else
            {
                string newvalue = KeysTranslator.TryGetKey(key, mod);

                if (!string.IsNullOrEmpty(newvalue) && key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    ResetBinding();

                    Key = key;
                    Mod = mod;
                    _label.Text = newvalue;
                }
            }
        }

        public void SetMouseButton(MouseButtonType button, SDL.SDL_Keymod mod)
        {
            string newvalue = KeysTranslator.GetMouseButton(button, mod);

            if (!string.IsNullOrEmpty(newvalue) && button != MouseButtonType.None)
            {
                ResetBinding();

                MouseButton = button;
                Mod = mod;
                _label.Text = newvalue;
            }
        }

        public void SetMouseWheel(bool wheelUp, SDL.SDL_Keymod mod)
        {
            string newvalue = KeysTranslator.GetMouseWheel(wheelUp, mod);

            if (!string.IsNullOrEmpty(newvalue))
            {
                ResetBinding();

                WheelScroll = true;
                WheelUp = wheelUp;
                Mod = mod;
                _label.Text = newvalue;
            }
        }

        private void ResetBinding()
        {
            Key = 0;
            MouseButton = MouseButtonType.None;
            WheelScroll = false;
            WheelUp = false;
            Mod = 0;
            _label.Text = string.Empty;
            Buttons = null;
        }

        /// <summary>Builds a <see cref="HotkeyBinding"/> from the currently displayed binding.</summary>
        private HotkeyBinding ToBinding() => new()
        {
            Key = Key,
            Ctrl = (Mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0,
            Shift = (Mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0,
            Alt = (Mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0,
            MouseButton = MouseButton,
            WheelScroll = WheelScroll,
            WheelUp = WheelUp,
            ControllerButtons = Buttons
        };

        /// <summary>Applies a captured <see cref="HotkeyBinding"/> back into this control's fields.</summary>
        private void ApplyBinding(HotkeyBinding binding)
        {
            SDL.SDL_Keymod mod = binding.Mod;

            if (binding.HasController)
                SetButtons(binding.ControllerButtons);
            else if (binding.HasMouseButton)
                SetMouseButton(binding.MouseButton, mod);
            else if (binding.WheelScroll)
                SetMouseWheel(binding.WheelUp, mod);
            else if (binding.HasKey)
                SetKey(binding.Key, mod);
            else
                ResetBinding();
        }

        private void LabelOnMouseUp(object sender, MouseEventArgs e)
        {
            if (_captureWindow is { IsDisposed: false })
            {
                _captureWindow.BringOnTop();
                return;
            }

            _captureWindow = new HotkeyCaptureWindow(
                prompt: null,
                existing: ToBinding(),
                onSaved: OnBindingSaved,
                capturesMouseEvents: true
            );
        }

        private void OnBindingSaved(HotkeyBinding binding)
        {
            // This control only dispatches key, mouse, wheel or controller bindings; a bare
            // modifier-only capture (or an empty one) is treated as clearing the binding.
            bool usable = binding.HasController || binding.HasMouseButton || binding.WheelScroll || binding.HasKey;

            if (!usable)
            {
                ResetBinding();
                Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                Mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
                HotkeyCancelled.Raise(this);
                return;
            }

            ApplyBinding(binding);
            HotkeyChanged.Raise(this);
        }

        public override void Dispose()
        {
            if (_captureWindow is { IsDisposed: false })
                _captureWindow.Dispose();

            _captureWindow = null;
            base.Dispose();
        }
    }
}
