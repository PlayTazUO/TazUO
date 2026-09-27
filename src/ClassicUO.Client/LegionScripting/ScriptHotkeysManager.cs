using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.Hotkeys;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// Bridges Legion scripts onto the central <see cref="HotKeys"/> registry. Each bound script is a
    /// normal <see cref="HotKeyEntry"/> (id <c>lscript:&lt;relativePath&gt;</c>) so dispatch, conflict
    /// detection and binding persistence are all handled by the shared hotkey system (and the binding
    /// shows up in the central Hotkeys tab). Keyboard, mouse-button and controller-button bindings all
    /// fire the entry's OnPressed callback, dispatched centrally by <see cref="HotKeys"/>.
    ///
    /// Which scripts have a hotkey is recorded per-profile in <see cref="Profile.ScriptHotkeys"/> (by
    /// relative path) so the entries can be re-registered each session; the key binding itself lives in
    /// the hotkey system's hotkeys.json.
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
            // Registrations live for the process lifetime, so drop the previous profile's script
            // hotkeys before re-applying the active one — otherwise they keep participating in
            // conflicts and get written into the next profile's hotkeys.json.
            foreach (string id in HotKeys.AllRegistered()
                         .Where(e => e.Id.StartsWith(IdPrefix, StringComparison.Ordinal))
                         .Select(e => e.Id)
                         .ToArray())
            {
                HotKeys.Unregister(id);
            }

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

        /// <summary>Current binding for <paramref name="script"/>, or an empty binding when unset.</summary>
        public static HotkeyBinding GetBinding(ScriptFile script)
        {
            if (script == null)
                return new HotkeyBinding();

            HotKeyEntry entry = HotKeys.Get(IdPrefix + script.RelativePath);
            return entry?.Binding?.Clone() ?? new HotkeyBinding();
        }

        /// <summary>
        /// Set the hotkey for a script (or clear it when <paramref name="binding"/> isn't toggleable).
        /// Registers the entry with the central hotkey system and records the script in the profile.
        /// </summary>
        public static void SetBinding(ScriptFile script, HotkeyBinding binding)
        {
            if (script == null)
                return;

            // Only bindings we can actually toggle are accepted; anything else (empty, wheel,
            // modifier-only) can't reliably toggle a script, so treat it as a clear.
            if (binding?.IsTriggerable != true)
            {
                ClearBinding(script);
                return;
            }

            string rel = script.RelativePath;
            Profile profile = ProfileManager.CurrentProfile;
            if (profile?.ScriptHotkeys != null && !profile.ScriptHotkeys.Contains(rel))
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
            ProfileManager.CurrentProfile?.ScriptHotkeys?.Remove(rel);
            HotKeys.Unregister(IdPrefix + rel);
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
