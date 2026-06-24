using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Data;

public enum HotKeyTriggerKind
{
    None = 0,
    Keyboard = 1,
    MouseButton = 2,
    MouseWheel = 3,
}

public class HotKeyTrigger
{
    public HotKeyTriggerKind Kind { get; set; } = HotKeyTriggerKind.None;
    public int Key { get; set; }        // (int)SDL.SDL_Keycode
    public int Button { get; set; }     // (int)MouseButtonType
    public bool WheelUp { get; set; }
    public int Mod { get; set; }        // (int)SDL.SDL_Keymod (normalized)

    // Strip lock keys and consolidate left/right modifiers — identical rules to
    // SpellBarManager / SelfHealManager so all three systems agree on a combo.
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

    public bool MatchesKeyboard(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        => Kind == HotKeyTriggerKind.Keyboard
           && Key == (int)key
           && (int)NormalizeMods(mod) == Mod;

    public bool MatchesMouseButton(MouseButtonType button, SDL.SDL_Keymod mod)
        => Kind == HotKeyTriggerKind.MouseButton
           && Button == (int)button
           && (int)NormalizeMods(mod) == Mod;

    public bool MatchesWheel(bool up, SDL.SDL_Keymod mod)
        => Kind == HotKeyTriggerKind.MouseWheel
           && WheelUp == up
           && (int)NormalizeMods(mod) == Mod;

    public string Describe()
    {
        switch (Kind)
        {
            case HotKeyTriggerKind.Keyboard:
                return KeysTranslator.TryGetKey((SDL.SDL_Keycode)Key, (SDL.SDL_Keymod)Mod);
            case HotKeyTriggerKind.MouseButton:
                string mods = ModPrefix();
                string btn = (MouseButtonType)Button switch
                {
                    MouseButtonType.Middle => "Mouse3",
                    MouseButtonType.XButton1 => "Mouse4",
                    MouseButtonType.XButton2 => "Mouse5",
                    _ => "Mouse",
                };
                return mods + btn;
            case HotKeyTriggerKind.MouseWheel:
                return ModPrefix() + (WheelUp ? "Wheel Up" : "Wheel Down");
            default:
                return "";
        }
    }

    private string ModPrefix()
    {
        var m = (SDL.SDL_Keymod)Mod;
        string s = "";
        if ((m & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0) s += "Ctrl+";
        if ((m & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0) s += "Shift+";
        if ((m & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0) s += "Alt+";
        return s;
    }
}
