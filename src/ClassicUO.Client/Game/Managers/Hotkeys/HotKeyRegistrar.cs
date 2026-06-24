using ClassicUO.Configuration;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// Central place to register every system's hotkeys with <see cref="HotKeys"/>. Runs once after
    /// <see cref="HotKeys.Load"/> (see GameScene). New systems add their registrations here so there
    /// is a single, discoverable list of code-registered hotkeys. Ids are exposed as constants so
    /// consumers can cache the returned <see cref="HotKeyEntry"/> for fast IsPressed checks.
    /// </summary>
    public static class HotKeyRegistrar
    {
        #region Global
        public const string GlobalToggleId = "global.togglehotkeys";
        #endregion

        #region Grid container
        public const string GridMultiMoveId = "grid.multimove";
        public const string GridAutoLootId = "grid.autoloot";
        public const string GridLockSlotId = "grid.lockslot";
        public const string GridCompareId = "grid.compareequipped";
        #endregion

        /// <summary>Register all systems' hotkeys. Safe to call again on profile switch.</summary>
        public static void RegisterAll()
        {
            RegisterGlobal();
            RegisterGridContainer();
        }

        private static void RegisterGlobal()
        {
            // Unbound by default. Pressing it toggles the shared Profile.DisableHotkeys flag, turning
            // every other hotkey off/on. It is exempt from that flag so it can always toggle back on.
            HotKeyEntry entry = HotKeys.Register(GlobalToggleId, "Toggle all hotkeys", new HotkeyBinding(), "Global", ToggleAllHotkeys);
            entry.IgnoresGlobalDisable = true;
        }

        private static void ToggleAllHotkeys()
        {
            Profile p = ProfileManager.CurrentProfile;
            if (p == null)
                return;

            p.DisableHotkeys = !p.DisableHotkeys;
            GameActions.Print($"Hotkeys {(p.DisableHotkeys ? "disabled" : "enabled")}.");
        }

        private static void RegisterGridContainer()
        {
            const string category = "Grid Container";
            HotKeys.Register(GridMultiMoveId, "Grid: move multiple items", Modifier(alt: true), category);
            HotKeys.Register(GridAutoLootId, "Grid: add item to autoloot", Modifier(shift: true), category);
            HotKeys.Register(GridLockSlotId, "Grid: lock item in slot", Modifier(ctrl: true), category);
            HotKeys.Register(GridCompareId, "Grid: compare item to equipped", Modifier(ctrl: true), category);
        }

        private static HotkeyBinding Modifier(bool ctrl = false, bool shift = false, bool alt = false)
            => new() { Ctrl = ctrl, Shift = shift, Alt = alt };
    }
}
