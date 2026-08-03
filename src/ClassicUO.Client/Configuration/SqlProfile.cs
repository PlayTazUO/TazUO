using System;
using System.Text.Json.Serialization;
using ClassicUO.Game;
using Microsoft.Xna.Framework;

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
        [SqlSetting(SettingsScope.Global, "counter_bar__show_hotkeys", false)]
        public partial bool CounterBarShowHotkeys { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__disable_item_scaling", false)]
        public partial bool CounterBarDisableItemScaling { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "counter_bar__disable_icon_scaling", false)]
        public partial bool CounterBarDisableIconScaling { get; set; }


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

        // When enabled (alongside TreeToStumps), trees are only rendered as stumps while they
        // are within the circle of transparency radius from the player.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.TREE_TO_STUMPS_WITHIN_RADIUS, false)]
        public partial bool TreeToStumpsWithinRadius { get; set; }


        // Clamp used by the gump-scale SQL settings above (see their OnSet).
        private static double ClampGumpScale(double value) => System.Math.Clamp(value, 0.5d, 3.0d);

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_loaded", "")]
        public partial string LastLoaded { get; set; }


        // When true, in-game lights gently ebb and flow like a mild candle flame.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, "candle_flicker_lights", true)]
        public partial bool CandleFlickerLights { get; set; }

        // Persisted size/position of the Legion Script Manager window. A null value means
        // "not set": no stored size auto-sizes to content, no stored position centers on open.
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "script_manager_window_size")]
        public partial Point? ScriptManagerWindowSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "script_manager_window_position")]
        public partial Point? ScriptManagerWindowPosition { get; set; }
}
