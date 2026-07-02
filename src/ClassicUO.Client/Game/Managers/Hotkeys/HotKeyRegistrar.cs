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

        #region Chat
        public const string ChatHistoryModId = "chat.historymodifier";
        #endregion

        #region Gumps
        public const string GumpModifierId = "gump.modifier";
        public const string GumpLockId = "gump.lock";
        public const string GumpOpacityId = "gump.opacityscroll";
        #endregion

        #region World map
        public const string WorldMapMarkerId = "worldmap.addmarker";
        public const string WorldMapPathfindId = "worldmap.pathfind";
        public const string WorldMapPathfindAppendId = "worldmap.pathfind.append";
        #endregion

        #region World interaction
        public const string FollowMobileId = "world.followmobile";
        public const string PathfindId = "world.pathfind";
        public const string ItemDragLockId = "world.itemdraglock";
        public const string ZoomScrollId = "world.zoomscroll";
        public const string SplitStackId = "world.splitstack";
        public const string ShopBulkId = "world.shopbulk";
        public const string ShowNameplatesId = "world.shownameplates";
        #endregion

        /// <summary>Register all systems' hotkeys. Safe to call again on profile switch.</summary>
        public static void RegisterAll()
        {
            RegisterGlobal();
            RegisterGridContainer();
            RegisterWorld();
            RegisterWorldMap();
            RegisterGumps();
            RegisterChat();
        }

        private static void RegisterChat()
        {
            // The keys are fixed (Q = previous, W = next); this rebinds the modifier held with them.
            ContextModifier(ChatHistoryModId, "Message-history modifier (with Q/W)", Modifier(ctrl: true), "Chat");
        }

        private static void RegisterGumps()
        {
            const string category = "Gumps";
            ContextModifier(GumpModifierId, "Move / detach / show lock icon", Modifier(alt: true), category);
            ContextModifier(GumpLockId, "Lock or unlock", Modifier(ctrl: true, alt: true), category);
            ContextModifier(GumpOpacityId, "Adjust opacity with wheel", Modifier(alt: true), category);
        }

        private static void RegisterWorldMap()
        {
            const string category = "World Map";
            ContextModifier(WorldMapMarkerId, "Add marker", Modifier(ctrl: true), category);
            ContextModifier(WorldMapPathfindId, "Pathfind to point", Modifier(ctrl: true), category);
            // Held together with the pathfind modifier to append a new segment onto the current route.
            ContextModifier(WorldMapPathfindAppendId, "Append to current path (with pathfind)", Modifier(shift: true), category);
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
            ContextModifier(GridMultiMoveId, "Move multiple items", Modifier(alt: true), category);
            ContextModifier(GridAutoLootId, "Add item to autoloot", Modifier(shift: true), category);
            ContextModifier(GridLockSlotId, "Lock item in slot", Modifier(ctrl: true), category);
            ContextModifier(GridCompareId, "Compare item to equipped", Modifier(ctrl: true), category);
        }

        private static void RegisterWorld()
        {
            const string category = "World";
            ContextModifier(FollowMobileId, "Click to follow a mobile", Modifier(alt: true), category);
            ContextModifier(PathfindId, "Pathfind modifier", Modifier(shift: true), category);
            ContextModifier(ItemDragLockId, "Lock item drag position", Modifier(ctrl: true), category);
            ContextModifier(ZoomScrollId, "Zoom with mouse wheel", Modifier(ctrl: true), category);
            ContextModifier(SplitStackId, "Split stack modifier", Modifier(shift: true), category);
            ContextModifier(ShopBulkId, "Buy/sell entire stack", Modifier(shift: true), category);
            ContextModifier(ShowNameplatesId, "Show all nameplates", Modifier(ctrl: true, shift: true), category);
        }

        /// <summary>
        /// Register a context modifier (e.g. Alt-to-move-gumps, Shift-to-pathfind): excluded from
        /// conflict checks (the same modifier is fine in different situations) and exempt from the
        /// global hotkey shutoff, since these are UI/interaction conveniences rather than action
        /// hotkeys and shouldn't stop working when hotkeys are toggled off.
        /// </summary>
        private static void ContextModifier(string id, string name, HotkeyBinding binding, string category)
            => HotKeys.Register(id, name, binding, category, checkConflicts: false).IgnoresGlobalDisable = true;

        private static HotkeyBinding Modifier(bool ctrl = false, bool shift = false, bool alt = false)
            => new() { Ctrl = ctrl, Shift = shift, Alt = alt };
    }
}
