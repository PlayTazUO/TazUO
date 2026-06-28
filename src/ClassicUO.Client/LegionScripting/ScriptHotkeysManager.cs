using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using SDL3;

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
    /// Toggling: key bindings fire through the entry's OnPressed callback (dispatched by
    /// <see cref="HotKeys.HandleKeyDown"/>, which is already focus-gated and ignores key repeat). Mouse
    /// and controller bindings have no such dispatch, so we listen to the raw button-down events and
    /// toggle the matching script when <c>HotKeys.Get(id).IsPressed()</c> reports it active.
    /// </summary>
    internal static class ScriptHotkeysManager
    {
        private const string IdPrefix = "lscript:";
        private const string Category = "Legion Scripts";

        private static bool _subscribed;

        /// <summary>
        /// Re-register a hotkey entry for every tracked script, pruning any whose script no longer
        /// exists. Call after <see cref="HotKeys.Load"/> so saved bindings are re-applied.
        /// </summary>
        public static void RegisterAll()
        {
            EnsureSubscribed();

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

            // Only bindings we can actually toggle on are accepted: a key (OnPressed dispatch) or a
            // mouse button / controller button (button-down listeners). Empty, wheel and modifier-only
            // bindings can't reliably toggle a script, so treat them as a clear.
            if (!IsToggleable(binding))
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

        private static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            // Key bindings toggle via the entry's OnPressed dispatch; mouse/controller bindings are
            // toggled here on their button-down events. IsPressed re-checks the live input state, so
            // these fire only when the bound button (plus modifiers) is the one actually held.
            Mouse.ButtonDownEvent += OnMouseButtonDown;
            Controller.ButtonDownEvent += OnControllerButtonDown;
            _subscribed = true;
        }

        // Match the specific button that fired so an unrelated click/press can't retrigger a script
        // whose (different) bound button merely happens to still be held.
        private static void OnMouseButtonDown(MouseButtonType button)
            => TogglePressed(b => b.HasMouseButton && b.MouseButton == button);

        private static void OnControllerButtonDown(SDL.SDL_GamepadButton button)
            => TogglePressed(b => b.HasController && b.ControllerButtons != null && b.ControllerButtons.Contains(button));

        private static void TogglePressed(Func<HotkeyBinding, bool> isKind)
        {
            // Mirror the keyboard path's gate: only dispatch while the game world owns input. This
            // stops a button from toggling the script while it's being (re)bound in the capture box,
            // or while a window/textbox otherwise has focus.
            if (!WorldHasInputFocus())
                return;

            Profile profile = ProfileManager.CurrentProfile;
            if (profile?.ScriptHotkeys == null || profile.ScriptHotkeys.Count == 0)
                return;

            foreach (string rel in profile.ScriptHotkeys.ToArray())
            {
                HotKeyEntry entry = HotKeys.Get(IdPrefix + rel);
                // Exact modifier match (like HotKeys.HandleKeyDown) so a no-modifier button binding
                // doesn't also fire while modifiers are held, and a plain + modified binding on the
                // same button don't both toggle.
                if (entry?.Binding != null && isKind(entry.Binding) && entry.IsPressed(allowAdditionalModifiers: false))
                    Toggle(rel);
            }
        }

        // Equivalent to GameSceneInputHandler.CanExecuteMacro: input belongs to the world (the system
        // chat box holds focus and isn't mid-compose), not to a gump/window such as the capture box.
        private static bool WorldHasInputFocus()
        {
            SystemChatControl chat = UIManager.SystemChat;
            return chat != null
                   && UIManager.KeyboardFocusControl == chat.TextBoxControl
                   && chat.Mode >= ChatMode.Default;
        }

        private static HotKeyEntry Register(ScriptFile script)
        {
            string rel = script.RelativePath;
            return HotKeys.Register(IdPrefix + rel, script.FileName, new HotkeyBinding(), Category, () => Toggle(rel));
        }

        // A binding can toggle a script only when it has a key (OnPressed dispatch) or a mouse/
        // controller button (button-down listeners). Wheel and modifier-only bindings can't.
        private static bool IsToggleable(HotkeyBinding binding)
            => binding != null && (binding.HasKey || binding.HasMouseButton || binding.HasController);

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
