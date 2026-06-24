namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// The grid container's modifier actions, registered with the central hotkey system so they
    /// show up in the Hotkeys tab and are rebindable. These are modifier-only bindings (held while
    /// clicking/hovering a slot); GridContainer polls them via the <c>Is*</c> accessors instead of
    /// reading <c>Keyboard.Alt/Ctrl/Shift</c> directly.
    /// </summary>
    public static class GridContainerHotkeys
    {
        public const string MultiMoveId = "grid.multimove";
        public const string AutoLootId = "grid.autoloot";
        public const string LockSlotId = "grid.lockslot";
        public const string CompareId = "grid.compareequipped";

        private const string Category = "Grid Container";

        public static void Register()
        {
            HotKeys.Register(MultiMoveId, "Grid: move multiple items", Modifier(alt: true), Category);
            HotKeys.Register(AutoLootId, "Grid: add item to autoloot", Modifier(shift: true), Category);
            HotKeys.Register(LockSlotId, "Grid: lock item in slot", Modifier(ctrl: true), Category);
            HotKeys.Register(CompareId, "Grid: compare item to equipped", Modifier(ctrl: true), Category);
        }

        public static bool IsMultiMove => Pressed(MultiMoveId);
        public static bool IsAutoLoot => Pressed(AutoLootId);
        public static bool IsLockSlot => Pressed(LockSlotId);
        public static bool IsCompare => Pressed(CompareId);

        private static bool Pressed(string id) => HotKeys.Get(id)?.IsPressed() ?? false;

        private static HotkeyBinding Modifier(bool ctrl = false, bool shift = false, bool alt = false)
            => new() { Ctrl = ctrl, Shift = shift, Alt = alt };
    }
}
