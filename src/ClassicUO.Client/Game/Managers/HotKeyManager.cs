using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.Managers;

public static class HotKeyManager
{
    public const int MaxCustomConsumables = 4;

    public static List<HotKeyEntry> Entries { get; private set; } = new();
    public static List<CustomConsumable> CustomConsumables { get; private set; } = new();

    /// <summary>
    /// Raised after any persisted mutation (always at the end of <see cref="Save"/>).
    /// UI tabs subscribe to live-refresh their already-built widgets when data changes
    /// in another context. Subscribers MUST unsubscribe when their window closes.
    /// </summary>
    public static event System.Action Changed;

    public static bool AddCustomConsumable(CustomConsumable c)
    {
        if (CustomConsumables.Count >= MaxCustomConsumables)
            return false;
        CustomConsumables.Add(c);
        return true;
    }

    private static string FilePath()
    {
        string dir = ProfileManager.ProfilePath;
        return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, "HotKeys.json");
    }

    public static void Load()
    {
        Entries = new();
        CustomConsumables = new();

        string path = FilePath();
        if (path == null || !File.Exists(path))
            return;

        try
        {
            var s = JsonSerializer.Deserialize(File.ReadAllText(path), HotKeySettingsContext.Default.HotKeySettings);
            if (s != null)
            {
                Entries = s.Entries ?? new();
                CustomConsumables = s.CustomConsumables ?? new();
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"Failed to load HotKeys.json: {e}");
        }
    }

    public static void Save()
    {
        string path = FilePath();
        if (path == null)
            return;

        try
        {
            var s = new HotKeySettings { Entries = Entries, CustomConsumables = CustomConsumables };
            File.WriteAllText(path, JsonSerializer.Serialize(s, HotKeySettingsContext.Default.HotKeySettings));
        }
        catch (System.Exception e)
        {
            Log.Error($"Failed to save HotKeys.json: {e}");
        }

        Changed?.Invoke();
    }

    private static bool HotkeysDisabled => ProfileManager.CurrentProfile?.DisableHotkeys ?? false;

    // Pure matcher (no Activate) so dispatch + gating are unit-testable.
    public static bool TestDispatch(HotKeyTrigger incoming, SDL.SDL_Keymod mod, out HotKeyEntry matched)
    {
        matched = null;
        bool disabled = HotkeysDisabled;

        foreach (var e in Entries)
        {
            if (!e.Enabled || e.Trigger == null)
                continue;
            // While hotkeys are disabled, only a ToggleHotkeys binding may fire (so it can re-enable).
            if (disabled && e.Action?.Type != HotKeyActionType.ToggleHotkeys)
                continue;

            bool hit = incoming.Kind switch
            {
                HotKeyTriggerKind.Keyboard    => e.Trigger.MatchesKeyboard((SDL.SDL_Keycode)incoming.Key, mod),
                HotKeyTriggerKind.MouseButton => e.Trigger.MatchesMouseButton((MouseButtonType)incoming.Button, mod),
                HotKeyTriggerKind.MouseWheel  => e.Trigger.MatchesWheel(incoming.WheelUp, mod),
                _ => false,
            };
            if (hit)
            {
                matched = e;
                return true;
            }
        }
        return false;
    }

    public static bool KeyPress(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        => Dispatch(new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)key }, mod);

    public static bool MousePress(MouseButtonType button, SDL.SDL_Keymod mod)
        => Dispatch(new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseButton, Button = (int)button }, mod);

    public static bool WheelPress(bool up, SDL.SDL_Keymod mod)
        => Dispatch(new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseWheel, WheelUp = up }, mod);

    private static bool Dispatch(HotKeyTrigger incoming, SDL.SDL_Keymod mod)
    {
        if (!TestDispatch(incoming, mod, out var matched))
            return false;
        matched.Action?.Activate(Client.Game?.UO?.World);
        return true;
    }

    // Returns human-readable descriptions of any existing binding that uses this trigger.
    public static List<string> FindConflicts(HotKeyTrigger trigger, HotKeyEntry ignore = null)
    {
        var conflicts = new List<string>();
        var world = Client.Game?.UO?.World;
        var mod = (SDL.SDL_Keymod)trigger.Mod;

        foreach (var e in Entries)
        {
            if (ReferenceEquals(e, ignore))
                continue;
            if (e.Trigger == null)
                continue;
            bool hit = trigger.Kind switch
            {
                HotKeyTriggerKind.Keyboard    => e.Trigger.MatchesKeyboard((SDL.SDL_Keycode)trigger.Key, mod),
                HotKeyTriggerKind.MouseButton => e.Trigger.MatchesMouseButton((MouseButtonType)trigger.Button, mod),
                HotKeyTriggerKind.MouseWheel  => e.Trigger.MatchesWheel(trigger.WheelUp, mod),
                _ => false,
            };
            if (hit)
                conflicts.Add($"HotKey: {e.Action?.DisplayName(world)}");
        }

        // Macros (same trigger kinds the macro system supports)
        var macros = world?.Macros;
        if (macros != null)
        {
            bool alt = (mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;
            bool ctrl = (mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
            bool shift = (mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;

            Macro m = trigger.Kind switch
            {
                HotKeyTriggerKind.Keyboard    => macros.FindMacro((SDL.SDL_Keycode)trigger.Key, alt, ctrl, shift),
                HotKeyTriggerKind.MouseButton => macros.FindMacro((MouseButtonType)trigger.Button, alt, ctrl, shift),
                HotKeyTriggerKind.MouseWheel  => macros.FindMacro(trigger.WheelUp, alt, ctrl, shift),
                _ => null,
            };
            if (m != null)
                conflicts.Add($"Macro: {m.Name}");
        }

        if (trigger.Kind == HotKeyTriggerKind.Keyboard)
        {
            SDL.SDL_Keycode[] keys = SpellBarManager.GetHotKeys();
            SDL.SDL_Keymod[] mods = SpellBarManager.GetModKeys();
            if (keys != null && mods != null)
            {
                for (int i = 0; i < keys.Length && i < mods.Length; i++)
                {
                    if ((int)keys[i] == trigger.Key &&
                        (int)HotKeyTrigger.NormalizeMods(mods[i]) == (int)HotKeyTrigger.NormalizeMods((SDL.SDL_Keymod)trigger.Mod))
                    {
                        conflicts.Add($"SpellBar slot {i}");
                    }
                }
            }
        }

        return conflicts;
    }

    private static SDL.SDL_Keymod ModsFromBools(bool alt, bool ctrl, bool shift)
    {
        SDL.SDL_Keymod m = SDL.SDL_Keymod.SDL_KMOD_NONE;
        if (alt) m |= SDL.SDL_Keymod.SDL_KMOD_ALT;
        if (ctrl) m |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
        if (shift) m |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
        return m;
    }

    public static HotKeyTrigger TriggerFromMacro(Macro m)
    {
        if (m == null)
            return new HotKeyTrigger { Kind = HotKeyTriggerKind.None };

        int mod = (int)HotKeyTrigger.NormalizeMods(ModsFromBools(m.Alt, m.Ctrl, m.Shift));

        if (m.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            return new HotKeyTrigger { Kind = HotKeyTriggerKind.Keyboard, Key = (int)m.Key, Mod = mod };
        if (m.MouseButton != MouseButtonType.None)
            return new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseButton, Button = (int)m.MouseButton, Mod = mod };
        if (m.WheelScroll)
            return new HotKeyTrigger { Kind = HotKeyTriggerKind.MouseWheel, WheelUp = m.WheelUp, Mod = mod };

        return new HotKeyTrigger { Kind = HotKeyTriggerKind.None };
    }

    public static void ApplyTriggerToMacro(Macro m, HotKeyTrigger t)
    {
        if (m == null || t == null)
            return;

        m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        m.MouseButton = MouseButtonType.None;
        m.WheelScroll = false;
        m.WheelUp = false;

        var mod = (SDL.SDL_Keymod)t.Mod;
        m.Alt = (mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;
        m.Ctrl = (mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
        m.Shift = (mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;

        switch (t.Kind)
        {
            case HotKeyTriggerKind.Keyboard:    m.Key = (SDL.SDL_Keycode)t.Key; break;
            case HotKeyTriggerKind.MouseButton: m.MouseButton = (MouseButtonType)t.Button; break;
            case HotKeyTriggerKind.MouseWheel:  m.WheelScroll = true; m.WheelUp = t.WheelUp; break;
        }
    }

    /// <summary>
    /// "Assign anyway": removes the old binding for <paramref name="trigger"/> everywhere it
    /// promised — conflicting HotKey entries (disabling Profile.SelfHeal if a SelfHeal entry
    /// was among them), the bound macro, and the SpellBar slot (keyboard only).
    /// The entry being edited (<paramref name="keepEntry"/>) and the entry's own macro
    /// (<paramref name="keepMacroName"/>) are left intact. Does NOT call Save() — the caller's
    /// Finalize persists the HotKey entries; macro/SpellBar are persisted here directly.
    /// </summary>
    public static void ClearTriggerEverywhere(HotKeyTrigger trigger, HotKeyEntry keepEntry, string keepMacroName)
    {
        if (trigger == null)
            return;

        var mod = (SDL.SDL_Keymod)trigger.Mod;

        // ── HotKey entries ──────────────────────────────────────────────────
        var matches = new List<HotKeyEntry>();
        foreach (var e in Entries)
        {
            if (ReferenceEquals(e, keepEntry))
                continue;
            if (e.Trigger == null)
                continue;
            bool hit = trigger.Kind switch
            {
                HotKeyTriggerKind.Keyboard    => e.Trigger.MatchesKeyboard((SDL.SDL_Keycode)trigger.Key, mod),
                HotKeyTriggerKind.MouseButton => e.Trigger.MatchesMouseButton((MouseButtonType)trigger.Button, mod),
                HotKeyTriggerKind.MouseWheel  => e.Trigger.MatchesWheel(trigger.WheelUp, mod),
                _ => false,
            };
            if (hit)
                matches.Add(e);
        }

        foreach (var e in matches)
        {
            if (e.Action?.Type == HotKeyActionType.SelfHeal)
            {
                var p = ProfileManager.CurrentProfile;
                if (p != null)
                {
                    p.SelfHeal_Key = 0;
                    p.SelfHeal_Enabled = false;
                }
            }
        }
        Entries.RemoveAll(e => matches.Contains(e));

        // ── Macro ───────────────────────────────────────────────────────────
        var macros = Client.Game?.UO?.World?.Macros;
        if (macros != null)
        {
            bool alt = (mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != 0;
            bool ctrl = (mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != 0;
            bool shift = (mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != 0;

            Macro FindBound() => trigger.Kind switch
            {
                HotKeyTriggerKind.Keyboard    => macros.FindMacro((SDL.SDL_Keycode)trigger.Key, alt, ctrl, shift),
                HotKeyTriggerKind.MouseButton => macros.FindMacro((MouseButtonType)trigger.Button, alt, ctrl, shift),
                HotKeyTriggerKind.MouseWheel  => macros.FindMacro(trigger.WheelUp, alt, ctrl, shift),
                _ => null,
            };

            Macro m = FindBound();
            while (m != null)
            {
                // Guard against an infinite loop: never clear (and thus never re-find)
                // the entry's own macro — stop when that's the only match left.
                if (m.Name == keepMacroName)
                    break;

                m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                m.MouseButton = MouseButtonType.None;
                m.WheelScroll = false;
                m.WheelUp = false;
                m.Alt = false;
                m.Ctrl = false;
                m.Shift = false;
                m.ControllerButtons = null;

                macros.Save();
                OnMacroKeyChanged(m);

                m = FindBound();
            }
        }

        // ── SpellBar (keyboard only) ────────────────────────────────────────
        if (trigger.Kind == HotKeyTriggerKind.Keyboard)
            SpellBarManager.ClearHotKeyForKey((SDL.SDL_Keycode)trigger.Key, (SDL.SDL_Keymod)trigger.Mod);
    }

    public static void OnMacroKeyChanged(Macro m)
    {
        if (m == null)
            return;

        bool changed = false;
        foreach (var e in Entries)
        {
            if (e.Action?.Type == HotKeyActionType.Macro && e.Action.MacroName == m.Name)
            {
                e.Trigger = TriggerFromMacro(m); // fresh instance per entry (no aliasing)
                changed = true;
            }
        }
        if (changed)
            Save();
    }
}
