using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// Central, per-profile hotkey registry. Code registers named hotkeys at startup with
    /// <see cref="Register"/> and queries them with <c>HotKeys.Get(id).IsPressed(...)</c>.
    /// Bindings persist to <c>hotkeys.json</c> in the current profile folder.
    ///
    /// Registered entries live for the lifetime of the process (they are code registrations);
    /// <see cref="Load"/> re-applies the active profile's saved bindings onto them, and
    /// <see cref="Save"/> writes out only the entries that are currently registered — so any
    /// saved entry whose owner no longer registers it is pruned on exit.
    /// </summary>
    public static class HotKeys
    {
        private static readonly Dictionary<string, HotKeyEntry> _entries = new();
        private static readonly Dictionary<string, HotkeyEntryDto> _saved = new();
        private static readonly HashSet<SDL.SDL_Keycode> _heldKeys = new();
        private static bool _subscribed;

        public static string FilePath => Path.Combine(ProfileManager.ProfilePath, "hotkeys.json");

        /// <summary>
        /// True when all hotkeys are globally disabled (the shared Profile.DisableHotkeys flag,
        /// toggled by the global shutoff hotkey). Entries flagged
        /// <see cref="HotKeyEntry.IgnoresGlobalDisable"/> ignore this.
        /// </summary>
        public static bool GloballyDisabled => ProfileManager.CurrentProfile?.DisableHotkeys ?? false;

        /// <summary>
        /// Load the active profile's saved bindings and re-apply them to any already-registered
        /// entries. Call this before consumers register (so first-time registration can adopt a
        /// saved binding) and again whenever the active profile changes.
        /// </summary>
        public static void Load()
        {
            if (!_subscribed)
            {
                Keyboard.KeyDownEvent += OnRawKeyDown;
                Keyboard.KeyUpEvent += OnRawKeyUp;
                _subscribed = true;
            }

            _heldKeys.Clear();
            _saved.Clear();

            HotkeysFile file = ConfigurationResolver.Load<HotkeysFile>(FilePath, HotkeysJsonContext.Default.HotkeysFile);
            if (file?.Entries != null)
            {
                foreach (HotkeyEntryDto dto in file.Entries)
                {
                    if (!string.IsNullOrEmpty(dto.Id))
                        _saved[dto.Id] = dto;
                }
            }

            // Re-apply the freshly loaded profile onto entries that are already registered.
            foreach (HotKeyEntry entry in _entries.Values)
                ApplySaved(entry);
        }

        /// <summary>
        /// Register a hotkey. If a saved binding exists for <paramref name="id"/> it is adopted,
        /// otherwise <paramref name="defaults"/> is used. Returns the (possibly pre-existing) entry.
        /// </summary>
        public static HotKeyEntry Register(string id, string name, HotkeyBinding defaults, string category = null, Action onPressed = null, bool checkConflicts = true)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Hotkey id cannot be null or empty", nameof(id));

            if (!_entries.TryGetValue(id, out HotKeyEntry entry))
            {
                entry = new HotKeyEntry(id, name, defaults, category);
                _entries[id] = entry;
            }
            else
            {
                // Re-registration (e.g. after a profile switch): refresh display info and the
                // fallback default so a different profile's legacy import is honoured.
                entry.Name = name;
                entry.Category = category;
                entry.SetDefault(defaults);
            }

            entry.OnPressed = onPressed;
            entry.CheckConflicts = checkConflicts;
            entry.Registered = true;
            ApplySaved(entry);
            return entry;
        }

        /// <summary>Returns the registered entry for <paramref name="id"/>, or null.</summary>
        public static HotKeyEntry Get(string id)
        {
            _entries.TryGetValue(id, out HotKeyEntry entry);
            return entry;
        }

        /// <summary>
        /// Remove a hotkey entry entirely. Used when a consumer permanently drops a binding (e.g. a
        /// script hotkey is cleared), so it disappears from the registry immediately and is not
        /// written back on the next <see cref="Save"/>.
        /// </summary>
        public static void Unregister(string id)
        {
            if (!string.IsNullOrEmpty(id))
                _entries.Remove(id);
        }

        /// <summary>
        /// Convenience poll: true when the hotkey <paramref name="id"/> is registered and currently
        /// pressed. Honors the entry's Enabled flag and the global disable. Unknown ids return false.
        /// </summary>
        public static bool IsPressed(string id, bool allowAdditionalModifiers = true)
            => _entries.TryGetValue(id, out HotKeyEntry entry) && entry.IsPressed(allowAdditionalModifiers);

        /// <summary>All entries currently registered this session.</summary>
        public static IEnumerable<HotKeyEntry> AllRegistered() => _entries.Values.Where(e => e.Registered);

        /// <summary>
        /// Other registered hotkeys whose binding would be triggered by the same input as
        /// <paramref name="binding"/>. Pass <paramref name="excludeId"/> to skip the entry being edited.
        /// </summary>
        public static List<HotKeyEntry> FindConflicts(HotkeyBinding binding, string excludeId = null)
        {
            var result = new List<HotKeyEntry>();
            if (binding == null || binding.IsEmpty)
                return result;

            foreach (HotKeyEntry entry in AllRegistered())
            {
                if (entry.Id == excludeId || !entry.CheckConflicts)
                    continue;
                if (entry.Binding != null && entry.Binding.Matches(binding))
                    result.Add(entry);
            }
            return result;
        }

        public static List<HotKeyEntry> FindConflicts(HotKeyEntry entry)
            => entry == null ? new List<HotKeyEntry>() : FindConflicts(entry.Binding, entry.Id);

        /// <summary>Write the currently-registered entries to disk, pruning any stale saved ids.</summary>
        public static void Save()
        {
            var file = new HotkeysFile();

            foreach (HotKeyEntry entry in _entries.Values)
            {
                if (!entry.Registered)
                    continue;
                file.Entries.Add(ToDto(entry));
            }

            ConfigurationResolver.Save(file, FilePath, HotkeysJsonContext.Default.HotkeysFile);
        }

        /// <summary>
        /// Edge-triggered dispatch for action hotkeys. Called from the game scene input handler
        /// (already focus-gated and DisableHotkeys-gated). Hold-style consumers do not use this.
        /// </summary>
        public static void HandleKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod, bool repeat)
        {
            if (repeat || key == SDL.SDL_Keycode.SDLK_UNKNOWN)
                return;

            SDL.SDL_Keymod n = HotkeyUtil.NormalizeMods(mod);

            // Snapshot so an OnPressed callback that (re)registers a hotkey can't invalidate the enumerator.
            foreach (HotKeyEntry entry in _entries.Values.ToArray())
            {
                if (!entry.Registered || !entry.Enabled || entry.OnPressed == null)
                    continue;

                if (GloballyDisabled && !entry.IgnoresGlobalDisable)
                    continue;

                HotkeyBinding b = entry.Binding;
                if (b == null || !b.HasKey || b.HasMouseButton || b.HasController || b.WheelScroll)
                    continue;

                if (b.Key == key && b.Mod == n)
                    entry.OnPressed.Invoke();
            }
        }

        internal static bool IsKeyHeld(SDL.SDL_Keycode key) => _heldKeys.Contains(key);

        /// <summary>Clear tracked key state (e.g. on focus loss) to avoid keys appearing stuck.</summary>
        public static void ClearHeldKeys() => _heldKeys.Clear();

        private static void OnRawKeyDown(string hotkey)
        {
            if (HotkeyUtil.TryParseKeycode(hotkey, out SDL.SDL_Keycode key))
                _heldKeys.Add(key);
        }

        private static void OnRawKeyUp(string hotkey)
        {
            if (HotkeyUtil.TryParseKeycode(hotkey, out SDL.SDL_Keycode key))
                _heldKeys.Remove(key);
        }

        private static void ApplySaved(HotKeyEntry entry)
        {
            if (_saved.TryGetValue(entry.Id, out HotkeyEntryDto dto))
            {
                entry.Binding = FromDto(dto);
                entry.Enabled = dto.Enabled;
            }
            else
            {
                // No saved override for this profile: fall back to the registered default.
                entry.ResetToDefault();
                entry.Enabled = true;
            }
        }

        private static HotkeyEntryDto ToDto(HotKeyEntry entry)
        {
            HotkeyBinding b = entry.Binding ?? new HotkeyBinding();
            return new HotkeyEntryDto
            {
                Id = entry.Id,
                Key = (int)b.Key,
                Ctrl = b.Ctrl,
                Shift = b.Shift,
                Alt = b.Alt,
                MouseButton = (int)b.MouseButton,
                WheelScroll = b.WheelScroll,
                WheelUp = b.WheelUp,
                ControllerButtons = b.ControllerButtons == null ? null : b.ControllerButtons.Select(x => (int)x).ToArray(),
                Enabled = entry.Enabled
            };
        }

        private static HotkeyBinding FromDto(HotkeyEntryDto dto) => new()
        {
            Key = (SDL.SDL_Keycode)dto.Key,
            Ctrl = dto.Ctrl,
            Shift = dto.Shift,
            Alt = dto.Alt,
            MouseButton = (MouseButtonType)dto.MouseButton,
            WheelScroll = dto.WheelScroll,
            WheelUp = dto.WheelUp,
            ControllerButtons = dto.ControllerButtons == null ? null : dto.ControllerButtons.Select(x => (SDL.SDL_GamepadButton)x).ToArray()
        };
    }
}
