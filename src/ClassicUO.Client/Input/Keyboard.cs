// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Keyboard
    {
        public static SDL.SDL_Keymod IgnoreKeyMod = SDL.SDL_Keymod.SDL_KMOD_CAPS | SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_MODE;

        public static bool Alt { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Ctrl { get; private set; }
        public static event Action<string> KeyDownEvent;
        public static event Action<string> KeyUpEvent;

        /// <summary>
        /// Fired when a lone modifier key is pressed or released, carrying the current generic
        /// CTRL/SHIFT/ALT mask. Bare modifiers are intentionally not broadcast via
        /// <see cref="KeyDownEvent"/> (so tapping Ctrl doesn't trigger hotkeys), so hotkey capture
        /// uses this to let a modifier-only binding be recorded.
        /// </summary>
        public static event Action<SDL.SDL_Keymod> BareModifierEvent;

        public static void ClearModifiers()
        {
            Alt = Shift = Ctrl = false;
        }

        public static string NormalizeKeyString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string[] parts = input.ToUpperInvariant().Replace(" ", "").Split('+');
            bool ctrl = false, shift = false, alt = false;
            string key = null;

            foreach (string p in parts)
            {
                if (string.IsNullOrEmpty(p))
                    continue;

                if (p == "CTRL") ctrl = true;
                else if (p == "SHIFT") shift = true;
                else if (p == "ALT") alt = true;
                else key = p.StartsWith("SDLK_") ? p : "SDLK_" + p;
            }

            List<string> normalized = new();
            if (ctrl) normalized.Add("CTRL");
            if (shift) normalized.Add("SHIFT");
            if (alt) normalized.Add("ALT");
            if (key != null) normalized.Add(key);

            return string.Join("+", normalized);
        }

        public static bool IgnoreBareModifierKey(SDL.SDL_KeyboardEvent e)
        {
            var keycode = (SDL.SDL_Keycode)e.key;
            return keycode is SDL.SDL_Keycode.SDLK_LSHIFT
                        or SDL.SDL_Keycode.SDLK_RSHIFT
                        or SDL.SDL_Keycode.SDLK_LCTRL
                        or SDL.SDL_Keycode.SDLK_RCTRL
                        or SDL.SDL_Keycode.SDLK_LALT
                        or SDL.SDL_Keycode.SDLK_RALT;
        }

        public static string BuildHotKeyString(SDL.SDL_KeyboardEvent e)
        {
            List<string> parts = new();
            if (Ctrl) parts.Add("CTRL");
            if (Shift) parts.Add("SHIFT");
            if (Alt) parts.Add("ALT");

            string keyName = ((SDL.SDL_Keycode)e.key).ToString().ToUpperInvariant();
            parts.Add(keyName);

            return string.Join("+", parts);
        }

        public static void OnKeyUp(SDL.SDL_KeyboardEvent e) => OnKeyEvent(e, KeyUpEvent);

        public static void OnKeyDown(SDL.SDL_KeyboardEvent e) => OnKeyEvent(e, KeyDownEvent);

        private static void OnKeyEvent(SDL.SDL_KeyboardEvent e, Action<string> keyboardEvent)
        {
            UpdateModifiers(e.mod);

            if (IgnoreBareModifierKey(e))
            {
                BareModifierEvent?.Invoke(CurrentMods());
                return;
            }

            if (keyboardEvent == null)
                return;

            string hotkey = BuildHotKeyString(e);
            keyboardEvent?.Invoke(hotkey);
        }

        private static SDL.SDL_Keymod CurrentMods()
        {
            SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (Ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (Shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            if (Alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            return mod;
        }

        private static void UpdateModifiers(SDL.SDL_Keymod e)
        {
            SDL.SDL_Keymod mod = e & ~IgnoreKeyMod;
            SDL.SDL_Keymod filtered = mod;

            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL)) == (SDL.SDL_Keymod.SDL_KMOD_RALT | SDL.SDL_Keymod.SDL_KMOD_LCTRL))
            {
                filtered = SDL.SDL_Keymod.SDL_KMOD_NONE;
            }

            Shift = (filtered & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Alt = (filtered & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
            Ctrl = (filtered & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;
        }
    }
}
