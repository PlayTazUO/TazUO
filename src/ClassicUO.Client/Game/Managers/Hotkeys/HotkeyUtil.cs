using System;
using SDL3;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// Shared helpers for the central hotkey system. Modifier normalization and hotkey-string
    /// parsing used to be duplicated in <see cref="SelfHealManager"/> and SelfHealTabContent;
    /// they live here now so every consumer agrees on the canonical form.
    /// </summary>
    internal static class HotkeyUtil
    {
        /// <summary>
        /// Strip lock keys (Caps/Num/Scroll/Mode) and collapse left/right modifiers down to the
        /// generic CTRL/SHIFT/ALT masks, so a binding matches regardless of which physical
        /// modifier key was used.
        /// </summary>
        public static SDL.SDL_Keymod NormalizeMods(SDL.SDL_Keymod mod)
        {
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_NUM;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_CAPS;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_SCROLL;
            mod &= ~SDL.SDL_Keymod.SDL_KMOD_MODE;

            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL);
                mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            }
            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT);
                mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            }
            if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT)) != 0)
            {
                mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT);
                mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            }
            return mod;
        }

        /// <summary>Build the generic CTRL/SHIFT/ALT mask from individual modifier flags.</summary>
        public static SDL.SDL_Keymod ModsFromFlags(bool ctrl, bool shift, bool alt)
        {
            SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            if (ctrl) mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            if (shift) mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            if (alt) mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            return mod;
        }

        /// <summary>
        /// Keyboard event strings look like "CTRL+SHIFT+SDLK_F1". Pull out the SDLK_ token.
        /// </summary>
        public static bool TryParseKeycode(string hotkey, out SDL.SDL_Keycode key)
        {
            key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            if (string.IsNullOrEmpty(hotkey))
                return false;

            foreach (string part in hotkey.Split('+'))
            {
                if (part.StartsWith("SDLK_", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse(part, true, out SDL.SDL_Keycode parsed))
                {
                    key = parsed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Parse a full "CTRL+SHIFT+SDLK_F1" event string into a key and a normalized modifier mask.
        /// </summary>
        public static (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) ParseHotKeyString(string hotkey)
        {
            SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

            if (string.IsNullOrEmpty(hotkey))
                return (key, mod);

            foreach (string part in hotkey.Split('+'))
            {
                switch (part.ToUpperInvariant())
                {
                    case "CTRL": mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL; break;
                    case "SHIFT": mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT; break;
                    case "ALT": mod |= SDL.SDL_Keymod.SDL_KMOD_ALT; break;
                    default:
                        if (Enum.TryParse(part, true, out SDL.SDL_Keycode parsed))
                            key = parsed;
                        break;
                }
            }

            return (key, mod);
        }
    }
}
