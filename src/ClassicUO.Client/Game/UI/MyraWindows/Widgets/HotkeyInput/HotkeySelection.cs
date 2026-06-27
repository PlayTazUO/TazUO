#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;

public enum MouseWheelEvent
{
    Click,
    ScrollUp,
    ScrollDown
}

public class HotkeySelection : IEquatable<HotkeySelection>
{
    public readonly SDL.SDL_Keycode Key;
    public readonly SDL.SDL_Keymod Modifiers;
    public readonly ImmutableArray<SDL.SDL_GamepadButton> GamepadButtons;
    public readonly MouseButtonType? MouseButton;
    public readonly MouseWheelEvent? Wheel;

    public bool Ctrl => (Modifiers & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
    public bool Shift => (Modifiers & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;
    public bool Alt => (Modifiers & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;

    public HotkeySelection(
        SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN,
        SDL.SDL_Keymod modifiers = SDL.SDL_Keymod.SDL_KMOD_NONE,
        IEnumerable<SDL.SDL_GamepadButton>? gamepadButtons = null,
        MouseButtonType? mouseButton = null,
        MouseWheelEvent? wheel = null
    )
    {
        Key = key;
        Modifiers = modifiers;
        // Always sort the buttons so equality checks are deterministic
        GamepadButtons = gamepadButtons?.OrderBy(b => b).ToImmutableArray() ?? ImmutableArray<SDL.SDL_GamepadButton>.Empty;
        MouseButton = mouseButton;
        Wheel = wheel;
    }

    public bool IsEmpty =>
        Key == SDL.SDL_Keycode.SDLK_UNKNOWN &&
        Modifiers == SDL.SDL_Keymod.SDL_KMOD_NONE &&
        GamepadButtons.IsEmpty &&
        !MouseButton.HasValue &&
        !Wheel.HasValue;

    public override string ToString()
    {
        if (IsEmpty) return "None";

        var sb = new StringBuilder();

        // 1. Modifiers
        if ((Modifiers & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0) sb.Append("Ctrl+");
        if ((Modifiers & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0) sb.Append("Alt+");
        if ((Modifiers & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0) sb.Append("Shift+");
        if ((Modifiers & SDL.SDL_Keymod.SDL_KMOD_GUI) != 0) sb.Append("Win+");

        // 2. Keyboard / Mouse / Wheel
        if (MouseButton.HasValue) sb.Append(MouseButton.Value.ToString());
        else if (Wheel.HasValue)
            switch (Wheel.Value)
            {
                case MouseWheelEvent.ScrollUp:
                    sb.Append("WheelUp");
                    break;
                case MouseWheelEvent.ScrollDown:
                    sb.Append("WheelDown");
                    break;
                case MouseWheelEvent.Click:
                    sb.Append("WheelClick");
                    break;
            }
        else if (Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
        {
            string keyName = Key.ToString();
            sb.Append(keyName.StartsWith("SDLK_") ? keyName.Substring(5).ToUpper() : keyName);
        }

        // 3. Gamepad (Append if present)
        foreach (SDL.SDL_GamepadButton btn in GamepadButtons)
        {
            if (sb.Length > 0 && sb[^1] != '+') sb.Append('+');
            sb.Append(btn.ToString());
        }

        return sb.ToString();
    }

    public static HotkeySelection FromString(string hotkeyString)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString) || hotkeyString == "None")
            return new HotkeySelection();

        // Matches the split logic used in Keyboard.NormalizeKeyString
        string[] parts = hotkeyString.ToUpperInvariant().Split('+');

        SDL.SDL_Keymod modifiers = SDL.SDL_Keymod.SDL_KMOD_NONE;
        SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        MouseButtonType? mouseButton = null;
        MouseWheelEvent? wheel = null;
        List<SDL.SDL_GamepadButton> gamepadButtons = [];

        foreach (string part in parts)
            switch (part)
            {
                case "CTRL":
                    modifiers |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                    break;
                case "SHIFT":
                    modifiers |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                    break;
                case "ALT":
                    modifiers |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                    break;
                case "WIN":
                    modifiers |= SDL.SDL_Keymod.SDL_KMOD_GUI;
                    break;
                case "WHEELUP":
                    wheel = MouseWheelEvent.ScrollUp;
                    break;
                case "WHEELDOWN":
                    wheel = MouseWheelEvent.ScrollDown;
                    break;
                case "WHEELCLICK":
                    wheel = MouseWheelEvent.Click;
                    break;
                case "NONE":
                    break;
                default:
                    // Mouse button names (Left, Middle, Right, XButton1, XButton2) match MouseButtonType exactly
                    if (Enum.TryParse(part, true, out MouseButtonType parsedMouse))
                    {
                        mouseButton = parsedMouse;
                        break;
                    }

                    // Gamepad button names already include the "SDL_GAMEPAD_BUTTON_" prefix from ToString()
                    if (Enum.TryParse(part, true, out SDL.SDL_GamepadButton parsedGamepad))
                    {
                        gamepadButtons.Add(parsedGamepad);
                        break;
                    }

                    // Ensure the key has the "SDLK_" prefix before parsing
                    string keyName = part.StartsWith("SDLK_") ? part : "SDLK_" + part;

                    if (Enum.TryParse(keyName, out SDL.SDL_Keycode parsedKey))
                        key = parsedKey;
                    break;
            }

        return new HotkeySelection(key, modifiers, gamepadButtons, mouseButton, wheel);
    }

    public bool Equals(HotkeySelection? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Key == other.Key &&
               Modifiers == other.Modifiers &&
               Enumerable.SequenceEqual(GamepadButtons, other.GamepadButtons) &&
               MouseButton == other.MouseButton &&
               Wheel == other.Wheel;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((HotkeySelection)obj);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add((int)Key);
        hash.Add((int)Modifiers);
        foreach (SDL.SDL_GamepadButton button in GamepadButtons)
            hash.Add(button);

        hash.Add(MouseButton);
        hash.Add(Wheel);
        return hash.ToHashCode();
    }
}
