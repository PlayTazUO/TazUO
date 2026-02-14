// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    internal static class Constants
    {
        public const int MIN_FPS = 12;
        public const int MAX_FPS = 1000;

        public const int CHARACTER_ANIMATION_DELAY = 80;
        public const int ITEM_EFFECT_ANIMATION_DELAY = 50;

        public const int ALLOWED_Z_DIFFERENCE = 10;

        public const int MAX_STEP_COUNT = 5;
        public const int WALKING_DELAY = 150; // 750
        public const int PLAYER_WALKING_DELAY = 150;
        public const int DEFAULT_CHARACTER_HEIGHT = 16;
        public const int DEFAULT_BLOCK_HEIGHT = 16;

        public const uint TIME_DISPLAY_SYSTEM_MESSAGE_TEXT = 10000;

        public const int MIN_TERRAIN_SHADOWS_LEVEL = 5;
        public const int MAX_TERRAIN_SHADOWS_LEVEL = 25;

        public const int USED_LAYER_COUNT = 23;

        public const int CLEAR_TEXTURES_DELAY = 3000;
        public const int MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 50;

        public const int MAX_FAST_WALK_STACK_SIZE = 5;

        public const byte FOLIAGE_ALPHA = 76;
        public const byte ALPHA_TIME = 20;

        public const int OBJECT_HANDLES_GUMP_HEIGHT = 18;

        public const int SPELLBOOK_1_SPELLS_COUNT = 64;
        public const int SPELLBOOK_2_SPELLS_COUNT = 17;
        public const int SPELLBOOK_3_SPELLS_COUNT = 10;
        public const int SPELLBOOK_4_SPELLS_COUNT = 6;
        public const int SPELLBOOK_5_SPELLS_COUNT = 8;
        public const int SPELLBOOK_6_SPELLS_COUNT = 16;
        public const int SPELLBOOK_7_SPELLS_COUNT = 30;

        public const int WAIT_FOR_TARGET_DELAY = 5000;

        public const int CONTAINER_RECT_STEP = 20;
        public const int CONTAINER_RECT_DEFAULT_POSITION = 40;
        public const int CONTAINER_RECT_LINESTEP = 800;
        public const int ITEM_GUMP_TEXTURE_OFFSET = 11369;

        public const int MAX_MUSIC_DATA_INDEX_COUNT = 150;


        public const ushort FIELD_REPLACE_GRAPHIC = 0x1826;
        public const ushort TREE_REPLACE_GRAPHIC = 0x0E59;

        public const int MIN_CIRCLE_OF_TRANSPARENCY_RADIUS = 50;
        public const int MAX_CIRCLE_OF_TRANSPARENCY_RADIUS = 1000;

        public const int MAX_ABILITIES_COUNT = 32;

        public const int DRAG_ITEMS_DISTANCE = 3;
        public const int MIN_GUMP_DRAG_DISTANCE = 5;
        public const int MIN_PICKUP_DRAG_DISTANCE_PIXELS = 5;

        public const int MIN_VIEW_RANGE = 5;
        public const int MAX_VIEW_RANGE = 24;
        public const int MAX_CONTAINER_OPENED_ON_GROUND_RANGE = 3;

        public const int OUT_RANGE_COLOR = 0x038B;
        public const int DEAD_RANGE_COLOR = 0x038E;
        public const int DEATH_SCREEN_TIMER = 1500;

        public const ushort HIGHLIGHT_CURRENT_OBJECT_HUE = 0x014;

        public const int MAX_JOURNAL_HISTORY_COUNT = 1000;

        public const byte MIN_CONTAINER_SIZE_PERC = 50;
        public const byte MAX_CONTAINER_SIZE_PERC = 200;

        public const int MALE_GUMP_OFFSET = 50000;
        public const int FEMALE_GUMP_OFFSET = 60000;

        public const int WEATHER_TIMER = 6 * 60 * 1000;

        public const int PREDICTABLE_CHUNKS = 300;
        public const float MAX_GAME_SCALE = 1.5f;
        public const float MIN_GAME_SCALE = -0.6f;
        public static Color SELECTED_COLOR = Color.DarkRed;

        public static readonly bool[] BAD_CONTAINER_LAYERS =
        {
            false, // invalid [body]
            true, true, true, true, true, true, true, true,
            true, true, false, true, true, true, false, false,
            true, true, true, true,
            false, // backpack
            true, true, true, false, false, false, false, false
        };

        public const uint RECHECK_HITS_STATUS = 20000;

        public const ushort HUE_ERROR = 32;
        public const ushort HUE_WARN = 53;
        public const ushort HUE_SUCCESS = 62;

        public static class SqlSettings
        {
            public const string MANAGED_ZLIB = "USE_MANAGED_ZLIB";
            public const string IMGUI_ALPHA = "imgui_window_alpha";
            public const string IMGUI_THEME = "imgui_theme";
            public const string IMGUI_CUSTOM_THEME_JSON = "imgui_custom_theme_json";
            public const string USE_LONG_DISTANCE_PATHING = "use_long_distance_pathing";
            public const string LONG_DISTANCE_PATHING_SPEED = "long_distance_pathing_speed";
            public const string SCALE_PETS_ENABLED = "scale_pets_enabled";
            public const string WEB_MAP_PORT = "web_map_port";
            public const string WEB_MAP_AUTO_START = "web_map_auto_start";
            public const string MIN_GUMP_MOVE_DIST = "min_gump_move_dist";
            public const string GAME_SCALE = "game_scale";
            public const string AUTO_UNEQUIP_FOR_ACTIONS = "auto_unequip_for_actions";
            public const string SOUND_FILTER_IDS = "sound_filter_ids";
            public const string DISABLE_WEATHER = "disable_weather";
            public const string SEASON_FILTER = "season_filter";
            public const string ENABLE_ENHANCED_PACKETS = "enhanced_packets_enabled";
            public const string QUICK_HEAL_SPELL = "quick_heal_spell";
            public const string QUICK_CURE_SPELL = "quick_cure_spell";
            public const string QUEUE_MANUAL_ITEM_MOVES = "queue_manual_item_moves";
            public const string QUEUE_MANUAL_ITEM_USES = "queue_manual_item_uses";
            public const string HUE_CORPSE_AFTER_AUTOLOOT = "hue_corpse_after_autoloot";
            public const string OUTLINE_NOTORIETIES = "outline_notorieties";
            public const string IRC_AUTO_CONNECT = "irc_auto_connect";
        }
    }
}
