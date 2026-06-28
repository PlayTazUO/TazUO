using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Binds Legion scripts to hotkeys. Bindings are keyed by each script's
    /// <see cref="ScriptFile.RelativePath"/> (so identically named scripts in different groups don't
    /// collide) and persist globally in <see cref="LScriptSettings.ScriptHotkeys"/>.
    ///
    /// Reuses the central <see cref="HotkeyBinding"/> for capture/describe; dispatch is edge-triggered
    /// from the game scene input handler via <see cref="HandleKeyDown"/> and toggles the script.
    /// </summary>
    internal static class ScriptHotkeysManager
    {
        // Runtime cache of relative-path -> binding, rebuilt from settings on Load.
        private static readonly Dictionary<string, HotkeyBinding> _bindings = new();

        /// <summary>
        /// Rebuild the runtime bindings from settings and prune any whose script no longer exists.
        /// Call after scripts and settings have loaded.
        /// </summary>
        public static void Load()
        {
            _bindings.Clear();

            LScriptSettings settings = LegionScripting.LScriptSettings;
            if (settings?.ScriptHotkeys == null)
                return;

            var existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (ScriptFile script in LegionScripting.LoadedScripts)
                existing.Add(script.RelativePath);

            // Drop hotkeys whose target script is gone so they don't linger forever.
            List<string> stale = settings.ScriptHotkeys.Keys.Where(k => !existing.Contains(k)).ToList();
            foreach (string id in stale)
                settings.ScriptHotkeys.Remove(id);

            foreach (KeyValuePair<string, ScriptHotkeyData> kvp in settings.ScriptHotkeys)
                _bindings[kvp.Key] = FromData(kvp.Value);
        }

        /// <summary>Current binding for <paramref name="script"/>, or an empty binding when unset.</summary>
        public static HotkeyBinding GetBinding(ScriptFile script)
        {
            if (script != null && _bindings.TryGetValue(script.RelativePath, out HotkeyBinding binding))
                return binding.Clone();

            return new HotkeyBinding();
        }

        /// <summary>
        /// Set (or, when <paramref name="binding"/> is empty/null, clear) the hotkey for a script.
        /// Updates both the runtime cache and the persisted settings.
        /// </summary>
        public static void SetBinding(ScriptFile script, HotkeyBinding binding)
        {
            if (script == null)
                return;

            if (binding == null || binding.IsEmpty)
            {
                ClearBinding(script);
                return;
            }

            string id = script.RelativePath;
            _bindings[id] = binding.Clone();

            if (LegionScripting.LScriptSettings != null)
                LegionScripting.LScriptSettings.ScriptHotkeys[id] = ToData(binding);
        }

        /// <summary>Remove the hotkey bound to <paramref name="script"/>.</summary>
        public static void ClearBinding(ScriptFile script)
        {
            if (script == null)
                return;

            string id = script.RelativePath;
            _bindings.Remove(id);
            LegionScripting.LScriptSettings?.ScriptHotkeys.Remove(id);
        }

        /// <summary>
        /// Edge-triggered dispatch. Called from the game scene input handler (already focus-gated and
        /// CanExecuteMacro-gated). Toggles the matching script when its key+modifiers are pressed.
        /// </summary>
        public static void HandleKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, bool repeat)
        {
            if (repeat || key == SDL.SDL_Keycode.SDLK_UNKNOWN || _bindings.Count == 0)
                return;

            // Script hotkeys honor the global hotkey shutoff just like the central registry.
            if (HotKeys.GloballyDisabled)
                return;

            SDL.SDL_Keymod normalized = HotkeyUtil.NormalizeMods(mod);

            // Snapshot in case toggling a script mutates anything during enumeration.
            foreach (KeyValuePair<string, HotkeyBinding> kvp in _bindings.ToArray())
            {
                HotkeyBinding b = kvp.Value;
                if (b == null || !b.HasKey || b.HasMouseButton || b.HasController || b.WheelScroll)
                    continue;

                if (b.Key == key && b.Mod == normalized)
                {
                    Toggle(kvp.Key);
                    break;
                }
            }
        }

        private static void Toggle(string relativePath)
        {
            ScriptFile script = LegionScripting.LoadedScripts.FirstOrDefault(s => s.RelativePath == relativePath);
            if (script == null)
                return;

            if (script.IsPlaying)
                LegionScripting.StopScript(script);
            else
                LegionScripting.PlayScript(script);
        }

        private static HotkeyBinding FromData(ScriptHotkeyData data) => new()
        {
            Key = (SDL.SDL_Keycode)data.Key,
            Ctrl = data.Ctrl,
            Shift = data.Shift,
            Alt = data.Alt,
            MouseButton = (MouseButtonType)data.MouseButton,
            WheelScroll = data.WheelScroll,
            WheelUp = data.WheelUp,
            ControllerButtons = data.ControllerButtons?.Select(x => (SDL.SDL_GamepadButton)x).ToArray()
        };

        private static ScriptHotkeyData ToData(HotkeyBinding binding) => new()
        {
            Key = (int)binding.Key,
            Ctrl = binding.Ctrl,
            Shift = binding.Shift,
            Alt = binding.Alt,
            MouseButton = (int)binding.MouseButton,
            WheelScroll = binding.WheelScroll,
            WheelUp = binding.WheelUp,
            ControllerButtons = binding.ControllerButtons?.Select(x => (int)x).ToArray()
        };
    }
}
