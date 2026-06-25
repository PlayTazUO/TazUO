using System;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// A hotkey registered with the central <see cref="HotKeys"/> registry. Code registers one of
    /// these with a stable <see cref="Id"/>, a display <see cref="Name"/>, and a default binding;
    /// the registry adopts the user's saved binding (from hotkeys.json) when one exists.
    /// </summary>
    public sealed class HotKeyEntry
    {
        public string Id { get; }
        public string Name { get; set; }
        public string Category { get; set; }

        /// <summary>The binding this hotkey falls back to when there is no saved override.</summary>
        public HotkeyBinding Default { get; private set; }

        /// <summary>The active binding (saved override or default).</summary>
        public HotkeyBinding Binding { get; set; }

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When false, this hotkey is excluded from conflict detection in the Hotkeys tab — it neither
        /// reports conflicts nor counts as a conflict for others. Use for context modifiers where the
        /// same binding in different situations does not actually clash (e.g. grid / gump modifiers).
        /// </summary>
        public bool CheckConflicts { get; set; } = true;

        /// <summary>
        /// Optional edge-triggered callback fired by <see cref="HotKeys.HandleKeyDown"/> when the
        /// bound key is pressed. Hold-style consumers (e.g. SelfHeal) ignore this and poll
        /// <see cref="IsPressed"/> instead.
        /// </summary>
        internal Action OnPressed { get; set; }

        /// <summary>True while a consumer has this id registered this session (drives prune-on-save).</summary>
        internal bool Registered { get; set; }

        /// <summary>
        /// When true, this hotkey keeps working while hotkeys are globally disabled. Used by the
        /// global shutoff hotkey itself so it can always toggle the rest back on.
        /// </summary>
        internal bool IgnoresGlobalDisable { get; set; }

        public HotKeyEntry(string id, string name, HotkeyBinding defaults, string category)
        {
            Id = id;
            Name = name;
            Category = category;
            Default = defaults?.Clone() ?? new HotkeyBinding();
            Binding = Default.Clone();
        }

        public bool IsPressed(bool allowAdditionalModifiers = true)
            => Enabled
               && (IgnoresGlobalDisable || !HotKeys.GloballyDisabled)
               && Binding != null && Binding.IsPressed(allowAdditionalModifiers);

        public void ResetToDefault() => Binding = Default.Clone();

        /// <summary>
        /// Refresh the fallback default (used when a consumer re-registers under a different profile
        /// whose legacy import differs).
        /// </summary>
        internal void SetDefault(HotkeyBinding defaults) => Default = defaults?.Clone() ?? new HotkeyBinding();
    }
}
