using System;
using System.Text.Json.Serialization;
using ClassicUO.Game;

namespace ClassicUO.Configuration;

public sealed partial class Profile
{
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.QUEUE_MANUAL_ITEM_MOVES, false)]
        public partial bool QueueManualItemMoves { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.AUTO_OPEN_DOORS_HIDDEN, true)]
        public partial bool AutoOpenDoorsIfHidden { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.QUEUE_MANUAL_ITEM_USES, false)]
        public partial bool QueueManualItemUses { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.HUE_CORPSE_AFTER_AUTOLOOT, false)]
        public partial bool HueCorpseAfterAutoloot { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.AUTOLOOT_RETRY_DELAY, 5000)]
        public partial int AutoLootRetryDelay { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATH_Z_LEVEL, 10)]
        public partial int PathfindingZLevelDiff { get; set; }

        // Maximum number of A* nodes the local (in-game) pathfinder will expand before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATHFINDING_MAX_NODES, 150000)]
        public partial int PathfindingMaxNodes { get; set; }

        // Extra A* cost applied to tiles bordering a house/multi wall, giving paths a soft 1-tile
        // standoff from houses. 0 disables the buffer.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATHFINDING_MULTI_BUFFER, 4)]
        public partial int PathfindingMultiBuffer { get; set; }

        // Maximum number of A* nodes the world map (long-distance) pathfinder will expand before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_MAX_NODES, 1000000)]
        public partial int WorldMapPathfindingMaxNodes { get; set; }

        // How many times world map navigation will replan around a blocked tile before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_MAX_RETRIES, 3)]
        public partial int WorldMapPathfindingMaxRetries { get; set; }

        // Wall-clock cap (milliseconds) on a single world map pathfinding search.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_TIMEOUT, 5000)]
        public partial int WorldMapPathfindingTimeout { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.SINGLE_CLICK_SET_LAST_TARG, true)]
        public partial bool SingleClickMobileSetsLastTarget { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OUTLINE_NOTORIETIES, false)]
        public partial bool OutlineMobilesNotoriety { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OVERHEAD_MESSAGE_TYPES_HIDDEN, (uint)0)]
        public partial uint DisabledOverheadMessageTypes { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.DISABLE_AUTOLOOT_RETRY_CORPSE, false)]
        public partial bool DisableAutolootCorpseRetry { get; set; } = false;

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "profile_migration_version", 0)]
        public partial int ProfileMigrationVersion { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "enable_a_sync_map_loading", true)]
        public partial bool EnableASyncMapLoading { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.DISABLE_WEATHER, false)]
        public partial bool DisableWeather { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.SCALE_PETS_ENABLED, false)]
        public partial bool EnablePetScaling { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.AUTO_UNEQUIP_FOR_ACTIONS, false)]
        public partial bool AutoUnequipForActions { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.MIN_GUMP_MOVE_DIST, 5)]
        public partial int MinGumpMoveDistance { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.QUICK_HEAL_SPELL, 29)]
        public partial int QuickHealSpell { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.QUICK_CURE_SPELL, 11)]
        public partial int QuickCureSpell { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "disable_dismount_in_war_mode", true)]
        public partial bool DisableDismountInWarMode { get; set; }

        /// <summary>
        ///     Persistently disable hotkey usage.
        ///     <p>
        ///         To merely temporarily disable hotkeys, use <see cref="Game.Managers.Hotkeys.HotKeys.RequestDisableHotkeys" />.
        ///     </p>
        /// </summary>
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "disable_hotkeys", false)]
        public partial bool PersistentDisableHotkeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "post_processing_type", (ushort)0)]
        public partial ushort PostProcessingType { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "enable_post_processing_effects", false)]
        public partial bool EnablePostProcessingEffects { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "disable_gray_enemies", false)]
        public partial bool DisableGrayEnemies { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "hide_hud_gump_flags", (ulong)0)]
        public partial ulong HideHudGumpFlags { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "force_house_transparency", false)]
        public partial bool ForceHouseTransparency { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "forced_transparency_house_tile_hue", (ushort)0)]
        public partial ushort ForcedTransparencyHouseTileHue { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "forced_house_transparency", (byte)40)]
        public partial byte ForcedHouseTransparency { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "spell_bar__show_hotkeys", true)]
        public partial bool SpellBar_ShowHotkeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__show_hotkeys", false)]
        public partial bool CounterBarShowHotkeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "nearby_loot_conceals_container_on_open", true)]
        public partial bool NearbyLootConcealsContainerOnOpen { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_gump_locked", false)]
        public partial bool CounterGumpLocked { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_scavenger", true)]
        public partial bool EnableScavenger { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "controller_enabled", true)]
        public partial bool ControllerEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_TRIGGER, false)]
        public partial bool BandageAgentUseJournalTrigger { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_MESSAGES, "")]
        public partial string BandageAgentJournalMessages { get; set; }

        // Semicolon-separated list of poll ids the user has already voted on (see PollsWindow / FirebasePollsManager).
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.VOTED_POLLS, "")]
        public partial string VotedPolls { get; set; }

        // When false, overheads (names, health bars, overhead text) keep a constant on-screen size
        // regardless of the camera zoom. Their positions still follow the zoomed world.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OVERHEADS_SCALE_WITH_ZOOM, true)]
        public partial bool OverheadsScaleWithZoom { get; set; }

        // When true, strips the leading "<id>" prefix from chat usernames (e.g. "<36475858>username" -> "username").
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "strip_chat_username_id", false)]
        public partial bool StripChatUsernameId { get; set; }

        // When true, every server gump's position is permanently saved automatically (see the Gump
        // Position Manager). The backing database is global, so this setting is global too.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "auto_save_gump_positions", false)]
        public partial bool AutoSaveGumpPositions { get; set; }

        // Which container style newly opened (non-corpse) containers use. Backing store for the
        // <see cref="ContainerStyle"/> enum: 0 = Grid (default), 1 = Original.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "container_style", 0)]
        public partial int ContainerStyleValue { get; set; }

        // Which container style corpses open in. Backing store for the
        // <see cref="CorpseContainerStyle"/> enum: 0 = Grid (default), 1 = Original, 2 = Old Grid Loot,
        // 3 = Old Grid Loot + Container.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "corpse_container_style", 0)]
        public partial int CorpseContainerStyleValue { get; set; }

        // When enabled (alongside TreeToStumps), trees are only rendered as stumps while they
        // are within the circle of transparency radius from the player.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.TREE_TO_STUMPS_WITHIN_RADIUS, false)]
        public partial bool TreeToStumpsWithinRadius { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "modern_paperdoll_anchor_enabled", false)]
        public partial bool ModernPaperdollAnchorEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "journal_anchor_enabled", false)]
        public partial bool JournalAnchorEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "enable_auto_loot_progress_bar", true)]
        public partial bool EnableAutoLootProgressBar { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "use_w_a_s_d_instead_arrow_keys", false)]
        public partial bool UseWASDInsteadArrowKeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "nearby_loot_gump_height", 550)]
        public partial int NearbyLootGumpHeight { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "force_tooltips_on_old_clients", true)]
        public partial bool ForceTooltipsOnOldClients { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "nearby_loot_opens_human_corpses", false)]
        public partial bool NearbyLootOpensHumanCorpses { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "turn_delay", (ushort)80)]
        public partial ushort TurnDelay { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sell_agent_enabled", false)]
        public partial bool SellAgentEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "sell_agent_max_uniques", 50)]
        public partial int SellAgentMaxUniques { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "sell_agent_max_items", 0)]
        public partial int SellAgentMaxItems { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buy_agent_enabled", false)]
        public partial bool BuyAgentEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "buy_agent_max_uniques", 50)]
        public partial int BuyAgentMaxUniques { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "buy_agent_max_items", 0)]
        public partial int BuyAgentMaxItems { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "s_o_s_gump_i_d", (uint)1915258020)]
        public partial uint SOSGumpID { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "status_gump_scale", 1d, OnSet = nameof(ClampGumpScale))]
        public partial double StatusGumpScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "skills_gump_scale", 1d, OnSet = nameof(ClampGumpScale))]
        public partial double SkillsGumpScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "context_menu_scale", 1d, OnSet = nameof(ClampGumpScale))]
        public partial double ContextMenuScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "trade_gump_scale", 1d, OnSet = nameof(ClampGumpScale))]
        public partial double TradeGumpScale { get; set; }

        /// <summary>
        /// Scale applied to every server created gump (and all of its controls).
        /// </summary>
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "server_gump_scale", 1d, OnSet = nameof(ClampGumpScale))]
        public partial double ServerGumpScale { get; set; }

        // Clamp used by the gump-scale SQL settings above (see their OnSet).
        private static double ClampGumpScale(double value) => System.Math.Clamp(value, 0.5d, 3.0d);

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_loaded", "")]
        public partial string LastLoaded { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "disable_targeting_grid_containers", false)]
        public partial bool DisableTargetingGridContainers { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "buy_agent_sub_containers", true)]
        public partial bool BuyAgentSubContainers { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "paperdoll_scale", 1d)]
        public partial double PaperdollScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "camera_smoothing_factor", 0f)]
        public partial float CameraSmoothingFactor { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "hide_layers_for_self", true)]
        public partial bool HideLayersForSelf { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "hidden_layers_enabled", false)]
        public partial bool HiddenLayersEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "disable_mouse_interaction_overhead_text", false)]
        public partial bool DisableMouseInteractionOverheadText { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "use_one_h_p_bar_for_last_attack", true)]
        public partial bool UseOneHPBarForLastAttack { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Server, "force_resync_on_hang", false)]
        public partial bool ForceResyncOnHang { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "close_health_bar_if_anchored", false)]
        public partial bool CloseHealthBarIfAnchored { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "use_last_moved_cooldown_position", true)]
        public partial bool UseLastMovedCooldownPosition { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_journal_tab", 0)]
        public partial int LastJournalTab { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "info_bar_locked", false)]
        public partial bool InfoBarLocked { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "info_bar_font", "Roboto-Regular")]
        public partial string InfoBarFont { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "info_bar_font_size", 18)]
        public partial int InfoBarFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "item_database_enabled", true)]
        public partial bool ItemDatabaseEnabled { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "auto_skinning_knife_graphics", "0x2D2C;0x0F52;0x0EC4;0x0EC3;0x13F6;0x13B6")]
        public partial string AutoSkinningKnifeGraphics { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "auto_skinning_human_corpses", false)]
        public partial bool AutoSkinningHumanCorpses { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "enable_auto_skinning", false)]
        public partial bool EnableAutoSkinning { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "auto_loot_human_corpses", false)]
        public partial bool AutoLootHumanCorpses { get; set; }
}
