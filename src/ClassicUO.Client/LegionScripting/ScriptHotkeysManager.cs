using System.Linq;
using ClassicUO.Game.Managers.Hotkeys;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Bridges Legion scripts onto the central <see cref="HotKeys"/> registry. Each bound script is a
    /// normal <see cref="HotKeyEntry"/> (id <c>lscript:&lt;relativePath&gt;</c>) whose OnPressed toggles
    /// the script, so dispatch, conflict detection and persistence are all handled by the shared
    /// hotkey system (and the binding shows up in the central Hotkeys tab).
    ///
    /// The set of scripts that have a hotkey is recorded in <see cref="LScriptSettings.ScriptHotkeys"/>
    /// (by relative path) so the entries can be re-registered each session; the key binding itself
    /// lives in the hotkey system's hotkeys.json.
    /// </summary>
    internal static class ScriptHotkeysManager
    {
        private const string IdPrefix = "lscript:";
        private const string Category = "Legion Scripts";

        /// <summary>
        /// Re-register a hotkey entry for every tracked script, pruning any whose script no longer
        /// exists. Call after <see cref="HotKeys.Load"/> so saved bindings are re-applied.
        /// </summary>
        public static void RegisterAll()
        {
            LScriptSettings settings = LegionScripting.LScriptSettings;
            if (settings?.ScriptHotkeys == null)
                return;

            // Drop hotkeys whose target script is gone so they don't linger or get re-saved.
            settings.ScriptHotkeys.RemoveAll(rel => LegionScripting.LoadedScripts.All(s => s.RelativePath != rel));

            foreach (string rel in settings.ScriptHotkeys)
            {
                ScriptFile script = LegionScripting.LoadedScripts.FirstOrDefault(s => s.RelativePath == rel);
                if (script != null)
                    Register(script, new HotkeyBinding());
            }
        }

        /// <summary>Current binding for <paramref name="script"/>, or an empty binding when unset.</summary>
        public static HotkeyBinding GetBinding(ScriptFile script)
        {
            if (script == null)
                return new HotkeyBinding();

            HotKeyEntry entry = HotKeys.Get(IdPrefix + script.RelativePath);
            return entry?.Binding?.Clone() ?? new HotkeyBinding();
        }

        /// <summary>
        /// Set (or, when <paramref name="binding"/> is empty/null, clear) the hotkey for a script.
        /// Registers the entry with the central hotkey system and records the script as tracked.
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

            string rel = script.RelativePath;
            LScriptSettings settings = LegionScripting.LScriptSettings;
            if (settings != null && !settings.ScriptHotkeys.Contains(rel))
                settings.ScriptHotkeys.Add(rel);

            HotKeyEntry entry = Register(script, binding);
            // The just-captured binding should win over any stale value loaded from hotkeys.json.
            entry.Binding = binding.Clone();
        }

        /// <summary>Remove the hotkey bound to <paramref name="script"/>.</summary>
        public static void ClearBinding(ScriptFile script)
        {
            if (script == null)
                return;

            string rel = script.RelativePath;
            LegionScripting.LScriptSettings?.ScriptHotkeys.Remove(rel);
            HotKeys.Unregister(IdPrefix + rel);
        }

        private static HotKeyEntry Register(ScriptFile script, HotkeyBinding defaults)
        {
            string rel = script.RelativePath;
            return HotKeys.Register(IdPrefix + rel, ScriptName(script), defaults, Category, () => Toggle(rel));
        }

        private static string ScriptName(ScriptFile script) => script.FileName;

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
    }
}
