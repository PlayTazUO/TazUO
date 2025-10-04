// SPDX-License-Identifier: BSD-2-Clause

using System;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Keyboard
    {
        public static SDL.SDL_Keymod IgnoreKeyMod = SDL.SDL_Keymod.SDL_KMOD_CAPS | SDL.SDL_Keymod.SDL_KMOD_NUM | SDL.SDL_Keymod.SDL_KMOD_MODE;

        public static bool Alt { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Ctrl { get; private set; }
        public static event Action<SDL.SDL_KeyboardEvent> KeyDownEvent;
        public static event Action<SDL.SDL_KeyboardEvent> KeyUpEvent;

        public static void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
            UpdateModifiers(e.mod);
            KeyUpEvent?.Invoke(e);
        }

        public static void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            UpdateModifiers(e.mod);
            KeyDownEvent?.Invoke(e);
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
