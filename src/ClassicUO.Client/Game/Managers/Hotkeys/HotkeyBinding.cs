using System.Linq;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// A single input binding for a hotkey. Mirrors the binding fields of <c>Macro</c> (key+mods,
    /// mouse button, mouse wheel, controller buttons) so the two can be compared for conflicts and,
    /// eventually, macros can be folded into this system without a data reshape.
    ///
    /// A binding represents exactly one kind of input at a time; whichever field is set determines
    /// the kind (controller &gt; mouse button &gt; mouse wheel &gt; keyboard).
    /// </summary>
    public sealed class HotkeyBinding
    {
        public SDL.SDL_Keycode Key { get; set; } = SDL.SDL_Keycode.SDLK_UNKNOWN;
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }

        public MouseButtonType MouseButton { get; set; } = MouseButtonType.None;
        public bool WheelScroll { get; set; }
        public bool WheelUp { get; set; }

        public SDL.SDL_GamepadButton[] ControllerButtons { get; set; }

        public HotkeyBinding() { }

        public HotkeyBinding(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            Key = key;
            SDL.SDL_Keymod n = HotkeyUtil.NormalizeMods(mod);
            Ctrl = (n & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
            Shift = (n & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;
            Alt = (n & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;
        }

        /// <summary>The generic CTRL/SHIFT/ALT mask for this binding's modifiers.</summary>
        public SDL.SDL_Keymod Mod => HotkeyUtil.ModsFromFlags(Ctrl, Shift, Alt);

        public bool HasController => ControllerButtons != null && ControllerButtons.Length > 0;
        public bool HasMouseButton => MouseButton != MouseButtonType.None;
        public bool HasKey => Key != SDL.SDL_Keycode.SDLK_UNKNOWN && Key != 0;

        /// <summary>True when this binding is a bare modifier (Ctrl/Shift/Alt) with no other input.</summary>
        public bool HasModifiers => Ctrl || Shift || Alt;

        public bool IsEmpty => !HasController && !HasMouseButton && !WheelScroll && !HasKey && !HasModifiers;

        public void Clear()
        {
            Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            Ctrl = Shift = Alt = false;
            MouseButton = MouseButtonType.None;
            WheelScroll = false;
            WheelUp = false;
            ControllerButtons = null;
        }

        public HotkeyBinding Clone() => new()
        {
            Key = Key,
            Ctrl = Ctrl,
            Shift = Shift,
            Alt = Alt,
            MouseButton = MouseButton,
            WheelScroll = WheelScroll,
            WheelUp = WheelUp,
            ControllerButtons = ControllerButtons == null ? null : (SDL.SDL_GamepadButton[])ControllerButtons.Clone()
        };

        private bool SameMods(HotkeyBinding other) => Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt;

        /// <summary>
        /// True when the two bindings would be triggered by the same input. Used for conflict
        /// detection in the Hotkeys tab. Empty bindings never conflict.
        /// </summary>
        public bool Matches(HotkeyBinding other)
        {
            if (other == null || IsEmpty || other.IsEmpty)
                return false;

            if (HasController || other.HasController)
            {
                if (!HasController || !other.HasController)
                    return false;
                if (ControllerButtons.Length != other.ControllerButtons.Length)
                    return false;
                // Order-insensitive set comparison.
                return !ControllerButtons.Except(other.ControllerButtons).Any();
            }

            if (HasMouseButton || other.HasMouseButton)
                return HasMouseButton && other.HasMouseButton && MouseButton == other.MouseButton && SameMods(other);

            if (WheelScroll || other.WheelScroll)
                return WheelScroll && other.WheelScroll && WheelUp == other.WheelUp && SameMods(other);

            // Both are now key-only or modifier-only (no controller/mouse/wheel).
            if (HasKey || other.HasKey)
                return HasKey && other.HasKey && Key == other.Key && SameMods(other);

            // Modifier-only on both sides.
            return SameMods(other);
        }

        /// <summary>
        /// Polls live input state to decide whether this binding is currently active.
        /// </summary>
        /// <param name="allowAdditionalModifiers">
        /// When true (default), extra held modifiers beyond those required still count as a match.
        /// When false, the held modifiers must match exactly.
        /// </param>
        public bool IsPressed(bool allowAdditionalModifiers = true)
        {
            if (IsEmpty)
                return false;

            if (HasController)
                return Controller.AreButtonsPressed(ControllerButtons, exact: !allowAdditionalModifiers);

            if (!ModifiersSatisfied(allowAdditionalModifiers))
                return false;

            if (HasMouseButton)
                return IsMouseButtonDown(MouseButton);

            // Mouse wheel is a transient event with no held state, so it can't be polled.
            if (WheelScroll)
                return false;

            if (HasKey)
                return HotKeys.IsKeyHeld(Key);

            // Modifier-only binding: active whenever the required modifiers (checked above) are held.
            return HasModifiers;
        }

        private bool ModifiersSatisfied(bool allowAdditionalModifiers)
        {
            if (allowAdditionalModifiers)
            {
                if (Ctrl && !Keyboard.Ctrl) return false;
                if (Shift && !Keyboard.Shift) return false;
                if (Alt && !Keyboard.Alt) return false;
                return true;
            }

            return Ctrl == Keyboard.Ctrl && Shift == Keyboard.Shift && Alt == Keyboard.Alt;
        }

        private static bool IsMouseButtonDown(MouseButtonType button) => button switch
        {
            MouseButtonType.Left => Mouse.LButtonPressed,
            MouseButtonType.Right => Mouse.RButtonPressed,
            MouseButtonType.Middle => Mouse.MButtonPressed,
            MouseButtonType.XButton1 => Mouse.XButton1Pressed,
            MouseButtonType.XButton2 => Mouse.XButton2Pressed,
            _ => false
        };

        /// <summary>Human-readable description for display in the UI.</summary>
        public string Describe()
        {
            if (HasController)
                return Controller.GetButtonNames(ControllerButtons);

            if (HasMouseButton)
                return KeysTranslator.GetMouseButton(MouseButton, Mod);

            if (WheelScroll)
                return KeysTranslator.GetMouseWheel(WheelUp, Mod);

            if (HasKey)
                return KeysTranslator.TryGetKey(Key, Mod);

            if (HasModifiers)
                return ModifierText();

            return "None";
        }

        private string ModifierText()
        {
            string s = string.Empty;
            if (Ctrl) s += "Ctrl";
            if (Shift) s += (s.Length > 0 ? " + " : string.Empty) + "Shift";
            if (Alt) s += (s.Length > 0 ? " + " : string.Empty) + "Alt";
            return s.Length == 0 ? "None" : s;
        }

        public override string ToString() => Describe();
    }
}
