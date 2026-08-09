using System;
using System.Text.Json.Serialization;
using ClassicUO.Game;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration;

public sealed partial class Profile
{
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.QUEUE_MANUAL_ITEM_MOVES, false)]
        public partial bool OldQueueManualItemMoves { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.AUTO_OPEN_DOORS_HIDDEN, true)]
        public partial bool OldAutoOpenDoorsIfHidden { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.QUEUE_MANUAL_ITEM_USES, false)]
        public partial bool OldQueueManualItemUses { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.HUE_CORPSE_AFTER_AUTOLOOT, false)]
        public partial bool OldHueCorpseAfterAutoloot { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.AUTOLOOT_RETRY_DELAY, 5000)]
        public partial int OldAutoLootRetryDelay { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATH_Z_LEVEL, 10)]
        public partial int OldPathfindingZLevelDiff { get; set; }

        // Maximum number of A* nodes the local (in-game) pathfinder will expand before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATHFINDING_MAX_NODES, 150000)]
        public partial int OldPathfindingMaxNodes { get; set; }

        // Extra A* cost applied to tiles bordering a house/multi wall, giving paths a soft 1-tile
        // standoff from houses. 0 disables the buffer.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.PATHFINDING_MULTI_BUFFER, 4)]
        public partial int OldPathfindingMultiBuffer { get; set; }

        // Maximum number of A* nodes the world map (long-distance) pathfinder will expand before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_MAX_NODES, 1000000)]
        public partial int OldWorldMapPathfindingMaxNodes { get; set; }

        // How many times world map navigation will replan around a blocked tile before giving up.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_MAX_RETRIES, 3)]
        public partial int OldWorldMapPathfindingMaxRetries { get; set; }

        // Wall-clock cap (milliseconds) on a single world map pathfinding search.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WORLDMAP_PATH_TIMEOUT, 5000)]
        public partial int OldWorldMapPathfindingTimeout { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.SINGLE_CLICK_SET_LAST_TARG, true)]
        public partial bool OldSingleClickMobileSetsLastTarget { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OUTLINE_NOTORIETIES, false)]
        public partial bool OldOutlineMobilesNotoriety { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OVERHEAD_MESSAGE_TYPES_HIDDEN, (uint)0)]
        public partial uint OldDisabledOverheadMessageTypes { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.DISABLE_AUTOLOOT_RETRY_CORPSE, false)]
        public partial bool OldDisableAutolootCorpseRetry { get; set; } = false;

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "profile_migration_version", 0)]
        public partial int ProfileMigrationVersion { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.DISABLE_WEATHER, false)]
        public partial bool OldDisableWeather { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.SCALE_PETS_ENABLED, false)]
        public partial bool OldEnablePetScaling { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.AUTO_UNEQUIP_FOR_ACTIONS, false)]
        public partial bool OldAutoUnequipForActions { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.MIN_GUMP_MOVE_DIST, 5)]
        public partial int OldMinGumpMoveDistance { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.QUICK_HEAL_SPELL, 29)]
        public partial int OldQuickHealSpell { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.QUICK_CURE_SPELL, 11)]
        public partial int OldQuickCureSpell { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_PORT, 8088)]
        public partial int OldWebMapServerPort { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_AUTO_START, false)]
        public partial bool OldWebMapAutoStart { get; set; }


        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__show_hotkeys", false)]
        public partial bool OldCounterBarShowHotkeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__disable_item_scaling", false)]
        public partial bool OldCounterBarDisableItemScaling { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__disable_icon_scaling", false)]
        public partial bool OldCounterBarDisableIconScaling { get; set; }


        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_TRIGGER, false)]
        public partial bool OldBandageAgentUseJournalTrigger { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_MESSAGES, "")]
        public partial string OldBandageAgentJournalMessages { get; set; }

        // Semicolon-separated list of poll ids the user has already voted on (see PollsWindow / FirebasePollsManager).
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.VOTED_POLLS, "")]
        public partial string OldVotedPolls { get; set; }

        // When false, overheads (names, health bars, overhead text) keep a constant on-screen size
        // regardless of the camera zoom. Their positions still follow the zoomed world.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OVERHEADS_SCALE_WITH_ZOOM, true)]
        public partial bool OldOverheadsScaleWithZoom { get; set; }

        // When true, strips the leading "<id>" prefix from chat usernames (e.g. "<36475858>username" -> "username").
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "strip_chat_username_id", false)]
        public partial bool OldStripChatUsernameId { get; set; }

        // When true, every server gump's position is permanently saved automatically (see the Gump
        // Position Manager). The backing database is global, so this setting is global too.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "auto_save_gump_positions", false)]
        public partial bool OldAutoSaveGumpPositions { get; set; }

        // When enabled (alongside TreeToStumps), trees are only rendered as stumps while they
        // are within the circle of transparency radius from the player.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.TREE_TO_STUMPS_WITHIN_RADIUS, false)]
        public partial bool OldTreeToStumpsWithinRadius { get; set; }


        // Clamp used by the gump-scale SQL settings above (see their OnSet).
        private static double ClampGumpScale(double value) => System.Math.Clamp(value, 0.5d, 3.0d);


        // When true, in-game lights gently ebb and flow like a mild candle flame.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "candle_flicker_lights", true)]
        public partial bool OldCandleFlickerLights { get; set; }

        // Persisted size/position of the Legion Script Manager window. A null value means
        // "not set": no stored size auto-sizes to content, no stored position centers on open.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "script_manager_window_size")]
        public partial Point? OldScriptManagerWindowSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "script_manager_window_position")]
        public partial Point? OldScriptManagerWindowPosition { get; set; }
}
