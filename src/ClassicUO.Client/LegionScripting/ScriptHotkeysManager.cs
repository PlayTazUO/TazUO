using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.Hotkeys;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Bridges Legion scripts onto the central <see cref="HotKeys"/> registry. Each bound script is a
    /// normal <see cref="HotKeyEntry"/> (id <c>lscript:&lt;relativePath&gt;</c>) so dispatch, conflict
    /// detection and binding persistence are all handled by the shared hotkey system (and the binding
    /// shows up in the central Hotkeys tab).
    ///
    /// Which scripts have a hotkey is recorded per-profile in <see cref="Profile.ScriptHotkeys"/> (by
    /// relative path) so the entries can be re-registered each session; the key binding itself lives in
    /// the hotkey system's hotkeys.json.
    ///
    /// Keyboard bindings toggle through the entry's focus-gated OnPressed dispatch; mouse and
    /// controller bindings have no key-down event to dispatch, so <see cref="Update"/> polls
    /// <c>HotKeys.Get(id).IsPressed()</c> for them and toggles on the rising edge.
    /// </summary>
    internal static class ScriptHotkeysManager
    {
        private const string IdPrefix = "lscript:";
        private const string Category = "Legion Scripts";

        // Previous IsPressed state per hotkey id, for edge-detecting non-key bindings in Update.
        private static readonly Dictionary<string, bool> _wasPressed = new();

        /// <summary>
        /// Re-register a hotkey entry for every tracked script, pruning any whose script no longer
        /// exists. Call after <see cref="HotKeys.Load"/> so saved bindings are re-applied.
        /// </summary>
        public static void RegisterAll()
        {
            _wasPressed.Clear();

            Profile profile = ProfileManager.CurrentProfile;
            if (profile?.ScriptHotkeys == null)
                return;

            // Drop hotkeys whose target script is gone so they don't linger or get re-saved.
            profile.ScriptHotkeys.RemoveAll(rel => LegionScripting.LoadedScripts.All(s => s.RelativePath != rel));

            foreach (string rel in profile.ScriptHotkeys)
            {
                ScriptFile script = LegionScripting.LoadedScripts.FirstOrDefault(s => s.RelativePath == rel);
                if (script != null)
                    Register(script);
            }
        }

        /// <summary>
        /// Poll non-key (mouse / controller) bindings and toggle the script on the rising edge.
        /// Keyboard bindings are handled by the focus-gated OnPressed dispatch instead.
        /// </summary>
        public static void Update()
        {
            Profile profile = ProfileManager.CurrentProfile;
            if (profile?.ScriptHotkeys == null || profile.ScriptHotkeys.Count == 0)
                return;

            foreach (string rel in profile.ScriptHotkeys.ToArray())
            {
                string id = IdPrefix + rel;
                HotKeyEntry entry = HotKeys.Get(id);

                // Skip empty and key bindings (the latter toggle via OnPressed); wheel bindings are
                // transient and IsPressed always reports them as not held.
                bool eligible = entry?.Binding != null && !entry.Binding.HasKey && !entry.Binding.IsEmpty;
                bool pressed = eligible && entry.IsPressed();

                bool was = _wasPressed.TryGetValue(id, out bool w) && w;
                if (pressed && !was)
                    Toggle(rel);

                _wasPressed[id] = pressed;
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
        /// Registers the entry with the central hotkey system and records the script in the profile.
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
            Profile profile = ProfileManager.CurrentProfile;
            if (profile != null && !profile.ScriptHotkeys.Contains(rel))
                profile.ScriptHotkeys.Add(rel);

            HotKeyEntry entry = Register(script);
            // The just-captured binding should win over any stale value loaded from hotkeys.json.
            entry.Binding = binding.Clone();
        }

        /// <summary>Remove the hotkey bound to <paramref name="script"/>.</summary>
        public static void ClearBinding(ScriptFile script)
        {
            if (script == null)
                return;

            string rel = script.RelativePath;
            string id = IdPrefix + rel;

            ProfileManager.CurrentProfile?.ScriptHotkeys.Remove(rel);
            HotKeys.Unregister(id);
            _wasPressed.Remove(id);
        }

        private static HotKeyEntry Register(ScriptFile script)
        {
            string rel = script.RelativePath;
            return HotKeys.Register(IdPrefix + rel, script.FileName, new HotkeyBinding(), Category, () => Toggle(rel));
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
    }
}
