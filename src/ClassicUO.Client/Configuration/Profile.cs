// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration.Json;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Game.UI.MyraWindows;

namespace ClassicUO.Configuration
{
    public enum NamePlateBackgroundMode
    {
        FixedColor,
        NotorietyColor
    }

    public enum NamePlateHealthBarMode
    {
        StatusColor,
        Green,
        Blue,
        Red,
        Cyan,
        Yellow,
        Orange,
        Purple,
        White,
        Gray,
        Black
    }

    public enum NamePlatePreset
    {
        Custom,
        Orion,
        WorldOfWarcraftBlockyBars,
        WorldOfWarcraftCleanHealth,
        WorldOfWarcraftBlockyCast,
        WorldOfWarcraftRedName
    }

    //[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(Profile), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class ProfileJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name) =>
                // Conversion to other naming convention goes here. Like SnakeCase, KebabCase etc.
                string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance;
            return options;
        });

        public static ProfileJsonContext DefaultToUse { get; } = new ProfileJsonContext(_jsonOptions.Value);
    }



    public sealed partial class Profile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static Profile _defaultPreview;

        /// <summary>
        /// A cached default profile with safe default settings, used as a fallback when no profile
        /// is loaded (e.g. rendering character previews on the login screen). Never touches disk
        /// and never fires <see cref="PropertyChanged"/>.
        /// </summary>
        public static Profile DefaultPreviewProfile => _defaultPreview ??= new Profile();

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event with the specified property name
        /// </summary>
        /// <param name="propertyName">The property that was updated. Passed by the compiler.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Updates the given property with the given value if it is different from the current one.
        /// Raises the <see cref="PropertyChanged" /> event, if a change has occurred
        /// </summary>
        /// <param name="storage">The field to update</param>
        /// <param name="value">The value to set</param>
        /// <param name="propertyName">The name of the property being updated</param>
        /// <typeparam name="T">The type of property being updated</typeparam>
        /// <returns><c>true</c> if a change has occurred, <c>false</c> otherwise</returns>
        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        [JsonIgnore] public string Username { get; set => SetProperty(ref field, value); }
        [JsonIgnore] public string ServerName { get; set => SetProperty(ref field, value); }
        [JsonIgnore] public string CharacterName { get; set => SetProperty(ref field, value); }

        // voice recognition
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "voice_recognition_enabled", false)]
        public partial bool VoiceRecognitionEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "voice_model_path", "")]
        public partial string VoiceModelPath { get; set; }

        // sounds
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_sound", true)]
        public partial bool EnableSound { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sound_volume", 50)]
        public partial int SoundVolume { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_music", true)]
        public partial bool EnableMusic { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "music_volume", 50)]
        public partial int MusicVolume { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_footsteps_sound", true)]
        public partial bool EnableFootstepsSound { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_rain_sound", true)]
        public partial bool EnableRainSound { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_combat_music", true)]
        public partial bool EnableCombatMusic { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "reproduce_sounds_in_background", false)]
        public partial bool ReproduceSoundsInBackground { get; set; }

        // fonts and speech
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "chat_font", 1)]
        public partial byte ChatFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "speech_delay", 100)]
        public partial int SpeechDelay { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "scale_speech_delay", true)]
        public partial bool ScaleSpeechDelay { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "save_journal_to_file", false)]
        public partial bool SaveJournalToFile { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "force_unicode_journal", false)]
        public partial bool ForceUnicodeJournal { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "ignore_alliance_messages", false)]
        public partial bool IgnoreAllianceMessages { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "ignore_guild_messages", false)]
        public partial bool IgnoreGuildMessages { get; set; }

        // hues
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "speech_hue", 0x02B2)]
        public partial ushort SpeechHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "whisper_hue", 0x0033)]
        public partial ushort WhisperHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "emote_hue", 0x0021)]
        public partial ushort EmoteHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "yell_hue", 0x0021)]
        public partial ushort YellHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "party_message_hue", 0x0044)]
        public partial ushort PartyMessageHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "guild_message_hue", 0x0044)]
        public partial ushort GuildMessageHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "ally_message_hue", 0x0057)]
        public partial ushort AllyMessageHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "chat_message_hue", 0x0256)]
        public partial ushort ChatMessageHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "innocent_hue", 0x005A)]
        public partial ushort InnocentHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "party_aura_hue", 0x0044)]
        public partial ushort PartyAuraHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "friend_hue", 0x0044)]
        public partial ushort FriendHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "criminal_hue", 0x03B2)]
        public partial ushort CriminalHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "can_attack_hue", 0x03B2)]
        public partial ushort CanAttackHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enemy_hue", 0x0031)]
        public partial ushort EnemyHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "murderer_hue", 0x0023)]
        public partial ushort MurdererHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "benefic_hue", 0x0059)]
        public partial ushort BeneficHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "harmful_hue", 0x0020)]
        public partial ushort HarmfulHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "neutral_hue", 0x03B1)]
        public partial ushort NeutralHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enabled_spell_hue", false)]
        public partial bool EnabledSpellHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enabled_spell_format", true)]
        public partial bool EnabledSpellFormat { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "spell_display_format", "{power} [{spell}]")]
        public partial string SpellDisplayFormat { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "poison_hue", 0x0044)]
        public partial ushort PoisonHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "paralyzed_hue", 0x014C)]
        public partial ushort ParalyzedHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "invulnerable_hue", 0x0030)]
        public partial ushort InvulnerableHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "alt_journal_background_hue", 0x0000)]
        public partial ushort AltJournalBackgroundHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "alt_grid_container_background_hue", 0x0000)]
        public partial ushort AltGridContainerBackgroundHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "override_party_and_guild_hue", false)]
        public partial bool OverridePartyAndGuildHue { get; set; }

        // visual
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enabled_criminal_action_query", true)]
        public partial bool EnabledCriminalActionQuery { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enabled_beneficial_criminal_action_query", false)]
        public partial bool EnabledBeneficialCriminalActionQuery { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_old_status_gump", false)]
        public partial bool UseOldStatusGump { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "status_gump_bar_mutually_exclusive", true)]
        public partial bool StatusGumpBarMutuallyExclusive { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "backpack_style", 0)]
        public partial int BackpackStyle { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "highlight_game_objects", false)]
        public partial bool HighlightGameObjects { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "highlight_mobiles_by_paralize", true)]
        public partial bool HighlightMobilesByParalize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "highlight_mobiles_by_poisoned", true)]
        public partial bool HighlightMobilesByPoisoned { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "highlight_mobiles_by_invul", true)]
        public partial bool HighlightMobilesByInvul { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_mobiles_h_p", false)]
        public partial bool ShowMobilesHP { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_target_indicator", false)]
        public partial bool ShowTargetIndicator { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_avoid_obstacules", true)]
        public partial bool AutoAvoidObstacules { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "mobile_h_p_type", 0)]
        public partial int MobileHPType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "mobile_h_p_show_when", 0)]
        public partial int MobileHPShowWhen { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "draw_roofs", true)]
        public partial bool DrawRoofs { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "mobile_depth_slice_step", 0)]
        public partial int MobileDepthSliceStep { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tree_to_stumps", false)]
        public partial bool TreeToStumps { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_cave_border", false)]
        public partial bool EnableCaveBorder { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_vegetation", false)]
        public partial bool HideVegetation { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_gargoyle_flying_animation", false)]
        public partial bool DisableGargoyleFlyingAnimation { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "fields_type", 0)]
        public partial int FieldsType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "no_color_objects_out_of_range", false)]
        public partial bool NoColorObjectsOutOfRange { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_circle_of_transparency", false)]
        public partial bool UseCircleOfTransparency { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "circle_of_transparency_radius", Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2)]
        public partial int CircleOfTransparencyRadius { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "circle_of_transparency_type", 0)]
        public partial int CircleOfTransparencyType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "vendor_gump_height", 350)]
        public partial int VendorGumpHeight { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "default_scale", 1.0f)]
        public partial float DefaultScale { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_mousewheel_scale_zoom", true)]
        public partial bool EnableMousewheelScaleZoom { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "restore_scale_after_unpress_ctrl", false)]
        public partial bool RestoreScaleAfterUnpressCtrl { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_self_old", true)]
        public partial bool BandageSelfOld { get; set; }

        // Bandage Agent Settings
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_bandage_agent", false)]
        public partial bool EnableBandageAgent { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_delay", 3000)]
        public partial int BandageAgentDelay { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_check_for_buff", false)]
        public partial bool BandageAgentCheckForBuff { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_graphic", 0x0E21)]
        public partial ushort BandageAgentGraphic { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_use_new_packet", true)]
        public partial bool BandageAgentUseNewPacket { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_check_hidden", true)]
        public partial bool BandageAgentCheckHidden { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_check_poisoned", true)]
        public partial bool BandageAgentCheckPoisoned { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_h_p_percentage", 80)]
        public partial int BandageAgentHPPercentage { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_check_invul", true)]
        public partial bool BandageAgentCheckInvul { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_bandage_friends", false)]
        public partial bool BandageAgentBandageFriends { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_bandage_allies", false)]
        public partial bool BandageAgentBandageAllies { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_bandage_pets", false)]
        public partial bool BandageAgentBandagePets { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_use_dex_formula", false)]
        public partial bool BandageAgentUseDexFormula { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "bandage_agent_disable_self_heal", false)]
        public partial bool BandageAgentDisableSelfHeal { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__enabled", false)]
        public partial bool SelfHeal_Enabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__use_chivalry", false)]
        public partial bool SelfHeal_UseChivalry { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__f_c", 2)]
        public partial int SelfHeal_FC { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__f_c_r", 6)]
        public partial int SelfHeal_FCR { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__key", 0)]
        public partial int SelfHeal_Key { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__mod", 0)]
        public partial int SelfHeal_Mod { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__recast_delay_ms", 50)]
        public partial int SelfHeal_RecastDelayMs { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__cast_start_grace_ms", 800)]
        public partial int SelfHeal_CastStartGraceMs { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__cure_verify_ms", 600)]
        public partial int SelfHeal_CureVerifyMs { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "self_heal__interrupt_retry_ms", 100)]
        public partial int SelfHeal_InterruptRetryMs { get; set; }

        // RelativePaths of Legion scripts that have a hotkey assigned. The key binding itself lives in
        // the central hotkey system (hotkeys.json); this per-profile list records which scripts to
        // re-register on load. Entries whose script no longer exists are pruned on load.
        public List<string> ScriptHotkeys { get; set => SetProperty(ref field, value); } = new List<string>();

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_TRIGGER, false)]
        public partial bool BandageAgentUseJournalTrigger { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, Constants.SqlSettings.BANDAGE_JOURNAL_MESSAGES, "")]
        public partial string BandageAgentJournalMessages { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_death_screen", true)]
        public partial bool EnableDeathScreen { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_black_white_effect", true)]
        public partial bool EnableBlackWhiteEffect { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hidden_body_hue", 0x038E)]
        public partial ushort HiddenBodyHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hidden_body_alpha", 40)]
        public partial byte HiddenBodyAlpha { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "player_constant_alpha", 100)]
        public partial int PlayerConstantAlpha { get; set; }

        // tooltip
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_tooltip", true)]
        public partial bool UseTooltip { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_text_hue", 0xFFFF)]
        public partial ushort TooltipTextHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_delay_before_display", 250)]
        public partial int TooltipDelayBeforeDisplay { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_display_zoom", 100)]
        public partial int TooltipDisplayZoom { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_background_opacity", 70)]
        public partial int TooltipBackgroundOpacity { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_font", 1)]
        public partial byte TooltipFont { get; set; }

        // movements
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_pathfind", true)]
        public partial bool EnablePathfind { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_shift_to_pathfind", false)]
        public partial bool UseShiftToPathfind { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "pathfind_single_click", false)]
        public partial bool PathfindSingleClick { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "always_run", true)]
        public partial bool AlwaysRun { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "always_run_unless_hidden", true)]
        public partial bool AlwaysRunUnlessHidden { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hold_down_key_tab", false)]
        public partial bool HoldDownKeyTab { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hold_shift_for_context", false)]
        public partial bool HoldShiftForContext { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hold_shift_to_split_stack", false)]
        public partial bool HoldShiftToSplitStack { get; set; }

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set => SetProperty(ref field, value); } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set => SetProperty(ref field, value); } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set => SetProperty(ref field, value); } = new Point(10, 10);
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "game_window_lock", false)]
        public partial bool GameWindowLock { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "game_window_full_size", false)]
        public partial bool GameWindowFullSize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "window_borderless", false)]
        public partial bool WindowBorderless { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "borderless_window", false)]
        public partial bool BorderlessWindow { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set => SetProperty(ref field, value); } = new Point(800, 680);
        [JsonConverter(typeof(Point2Converter))] public Point TopbarGumpPosition { get; set => SetProperty(ref field, value); } = new Point(0, 0);
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "topbar_gump_is_minimized", false)]
        public partial bool TopbarGumpIsMinimized { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "topbar_gump_is_disabled", false)]
        public partial bool TopbarGumpIsDisabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_alternative_lights", false)]
        public partial bool UseAlternativeLights { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_custom_light_level", false)]
        public partial bool UseCustomLightLevel { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "light_level", 0)]
        public partial byte LightLevel { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "light_level_type", 0)]
        public partial int LightLevelType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_colored_lights", true)]
        public partial bool UseColoredLights { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_dark_nights", false)]
        public partial bool UseDarkNights { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "close_health_bar_type", 2)]
        public partial int CloseHealthBarType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "activate_chat_after_enter", false)]
        public partial bool ActivateChatAfterEnter { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "activate_chat_additional_buttons", true)]
        public partial bool ActivateChatAdditionalButtons { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "activate_chat_shift_enter_support", true)]
        public partial bool ActivateChatShiftEnterSupport { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_objects_fading", true)]
        public partial bool UseObjectsFading { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hold_down_key_alt_to_close_anchored", true)]
        public partial bool HoldDownKeyAltToCloseAnchored { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "close_all_anchored_gumps_in_group_with_right_click", false)]
        public partial bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hold_alt_to_move_gumps", false)]
        public partial bool HoldAltToMoveGumps { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_opacity", 50)]
        public partial byte JournalOpacity { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_style", 0)]
        public partial int JournalStyle { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_screenshot_stored_in_message", false)]
        public partial bool HideScreenshotStoredInMessage { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_modern_paperdoll", false)]
        public partial bool UseModernPaperdoll { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "open_modern_paperdoll_at_minimize_loc", false)]
        public partial bool OpenModernPaperdollAtMinimizeLoc { get; set; }

        // Experimental
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "cast_spells_by_one_click", false)]
        public partial bool CastSpellsByOneClick { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buff_bar_time", false)]
        public partial bool BuffBarTime { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "fast_spells_assign", false)]
        public partial bool FastSpellsAssign { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_open_doors", true)]
        public partial bool AutoOpenDoors { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "smooth_doors", true)]
        public partial bool SmoothDoors { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_open_corpses", true)]
        public partial bool AutoOpenCorpses { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_open_corpse_range", 2)]
        public partial int AutoOpenCorpseRange { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "corpse_open_options", 3)]
        public partial int CorpseOpenOptions { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "skip_empty_corpse", false)]
        public partial bool SkipEmptyCorpse { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_open_own_corpse", true)]
        public partial bool AutoOpenOwnCorpse { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_default_hotkeys", false)]
        public partial bool DisableDefaultHotkeys { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_arrow_btn", false)]
        public partial bool DisableArrowBtn { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_tab_btn", false)]
        public partial bool DisableTabBtn { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_ctrl_q_w_btn", false)]
        public partial bool DisableCtrlQWBtn { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_auto_move", false)]
        public partial bool DisableAutoMove { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_drag_select", false)]
        public partial bool EnableDragSelect { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select_modifier_key", 0)]
        public partial int DragSelectModifierKey { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select__players_modifier", 0)]
        public partial int DragSelect_PlayersModifier { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select__monsters_modifier", 0)]
        public partial int DragSelect_MonstersModifier { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select__nameplate_modifier", 0)]
        public partial int DragSelect_NameplateModifier { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "override_container_location", false)]
        public partial bool OverrideContainerLocation { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "override_container_location_setting", 0)]
        public partial int OverrideContainerLocationSetting { get; set; }

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set => SetProperty(ref field, value); } = new Point(200, 200);
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hue_container_gumps", true)]
        public partial bool HueContainerGumps { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select_start_x", 100)]
        public partial int DragSelectStartX { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select_start_y", 100)]
        public partial int DragSelectStartY { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "drag_select_as_anchor", false)]
        public partial bool DragSelectAsAnchor { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_active_name_overhead_option", "All")]
        public partial string LastActiveNameOverheadOption { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_overhead_toggled", false)]
        public partial bool NameOverheadToggled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_target_range_indicator", false)]
        public partial bool ShowTargetRangeIndicator { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "party_invite_gump", true)]
        public partial bool PartyInviteGump { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "custom_bars_toggled", false)]
        public partial bool CustomBarsToggled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "c_b_black_b_g_toggled", false)]
        public partial bool CBBlackBGToggled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_party_health_bars", true)]
        public partial bool UsePartyHealthBars { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_info_bar", false)]
        public partial bool ShowInfoBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "info_bar_highlight_type", 0)]
        public partial int InfoBarHighlightType { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_enabled", false)]
        public partial bool CounterBarEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_highlight_on_use", false)]
        public partial bool CounterBarHighlightOnUse { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_highlight_on_amount", false)]
        public partial bool CounterBarHighlightOnAmount { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_display_abbreviated_amount", false)]
        public partial bool CounterBarDisplayAbbreviatedAmount { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_abbreviated_amount", 1000)]
        public partial int CounterBarAbbreviatedAmount { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_highlight_amount", 5)]
        public partial int CounterBarHighlightAmount { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_cell_size", 40)]
        public partial int CounterBarCellSize { get; set; }

        // title bar stats
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_title_bar_stats", false)]
        public partial bool EnableTitleBarStats { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "title_bar_stats_mode", TitleBarStatsMode.Text)]
        public partial TitleBarStatsMode TitleBarStatsMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_rows", 1)]
        public partial int CounterBarRows { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_bar_columns", 5)]
        public partial int CounterBarColumns { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_skills_changed_message", true)]
        public partial bool ShowSkillsChangedMessage { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_skills_changed_delta_value", 1)]
        public partial int ShowSkillsChangedDeltaValue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_stats_changed_message", true)]
        public partial bool ShowStatsChangedMessage { get; set; }


        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "shadows_enabled", true)]
        public partial bool ShadowsEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "shadows_statics", true)]
        public partial bool ShadowsStatics { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "terrain_shadows_level", 15)]
        public partial int TerrainShadowsLevel { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "aura_under_feet_type", 0)]
        public partial int AuraUnderFeetType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "aura_on_mouse", true)]
        public partial bool AuraOnMouse { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "animated_water_effect", false)]
        public partial bool AnimatedWaterEffect { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_weather_effects", false)]
        public partial bool EnableWeatherEffects { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_enhanced_weather", false)]
        public partial bool EnableEnhancedWeather { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "party_aura", false)]
        public partial bool PartyAura { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_chat_gradient", false)]
        public partial bool HideChatGradient { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "standard_skills_gump", true)]
        public partial bool StandardSkillsGump { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_new_mobile_name_incoming", true)]
        public partial bool ShowNewMobileNameIncoming { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_new_corpse_name_incoming", true)]
        public partial bool ShowNewCorpseNameIncoming { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grab_bag_serial", 0)]
        public partial uint GrabBagSerial { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_loot_type", 0)]
        public partial int GridLootType { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "reduce_f_p_s_when_inactive", false)]
        public partial bool ReduceFPSWhenInactive { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_v_sync", true)]
        public partial bool EnableVSync { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "override_all_fonts", false)]
        public partial bool OverrideAllFonts { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "override_all_fonts_is_unicode", true)]
        public partial bool OverrideAllFontsIsUnicode { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sallos_easy_grab", false)]
        public partial bool SallosEasyGrab { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_dark_mode", false)]
        public partial bool JournalDarkMode { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "containers_scale", 100)]
        public partial byte ContainersScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "container_opacity", 50)]
        public partial byte ContainerOpacity { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "scale_items_inside_containers", false)]
        public partial bool ScaleItemsInsideContainers { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "double_click_to_loot_inside_containers", false)]
        public partial bool DoubleClickToLootInsideContainers { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_large_container_gumps", false)]
        public partial bool UseLargeContainerGumps { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "relative_drag_and_drop_items", false)]
        public partial bool RelativeDragAndDropItems { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "highlight_container_when_selected", false)]
        public partial bool HighlightContainerWhenSelected { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_new_target_system", true)]
        public partial bool UseNewTargetSystem { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_kr_equip_unequip_packet", false)]
        public partial bool UseKrEquipUnequipPacket { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_house_content", false)]
        public partial bool ShowHouseContent { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "save_healthbars", false)]
        public partial bool SaveHealthbars { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "text_fading", true)]
        public partial bool TextFading { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_smooth_boat_movement", false)]
        public partial bool UseSmoothBoatMovement { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "ignore_stamina_check", false)]
        public partial bool IgnoreStaminaCheck { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_journal_client", true)]
        public partial bool ShowJournalClient { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_journal_objects", true)]
        public partial bool ShowJournalObjects { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_journal_system", true)]
        public partial bool ShowJournalSystem { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_journal_guild_ally", true)]
        public partial bool ShowJournalGuildAlly { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_width", 400)]
        public partial int WorldMapWidth { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_height", 400)]
        public partial int WorldMapHeight { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_font", 3)]
        public partial int WorldMapFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_ttf_font", "")]
        public partial string WorldMapTtfFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_ttf_font_size", 20)]
        public partial int WorldMapTtfFontSize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_flip_map", true)]
        public partial bool WorldMapFlipMap { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_top_most", false)]
        public partial bool WorldMapTopMost { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_free_view", false)]
        public partial bool WorldMapFreeView { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_double_click_action", WorldMapDoubleClickAction.ToggleLock)]
        public partial WorldMapDoubleClickAction WorldMapDoubleClickAction { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_party", true)]
        public partial bool WorldMapShowParty { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_zoom_index", 4)]
        public partial int WorldMapZoomIndex { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_coordinates", true)]
        public partial bool WorldMapShowCoordinates { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_mouse_coordinates", true)]
        public partial bool WorldMapShowMouseCoordinates { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_corpse", true)]
        public partial bool WorldMapShowCorpse { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_sextant_coordinates", false)]
        public partial bool WorldMapShowSextantCoordinates { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_mobiles", true)]
        public partial bool WorldMapShowMobiles { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_player_name", true)]
        public partial bool WorldMapShowPlayerName { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_player_bar", true)]
        public partial bool WorldMapShowPlayerBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_group_name", true)]
        public partial bool WorldMapShowGroupName { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_group_bar", true)]
        public partial bool WorldMapShowGroupBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_markers", true)]
        public partial bool WorldMapShowMarkers { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_markers_names", true)]
        public partial bool WorldMapShowMarkersNames { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_multis", true)]
        public partial bool WorldMapShowMultis { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_hidden_marker_files", "")]
        public partial string WorldMapHiddenMarkerFiles { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_hidden_zone_files", "")]
        public partial string WorldMapHiddenZoneFiles { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_show_grid_if_zoomed", true)]
        public partial bool WorldMapShowGridIfZoomed { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_allow_positional_target", true)]
        public partial bool WorldMapAllowPositionalTarget { get; set; }

        [JsonIgnore]
        public int WebMapServerPort
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_PORT, value);
            }
        }

        [JsonIgnore]
        public bool WebMapAutoStart
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    Client.Settings?.SetAsync(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_AUTO_START, value);
            }
        }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_follow_distance", 1)]
        public partial int AutoFollowDistance { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_auto_follow_alt", false)]
        public partial bool DisableAutoFollowAlt { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point ResizeJournalSize { get; set => SetProperty(ref field, value); } = new(410, 350);
        [JsonConverter(typeof(NullablePoint2Converter))] public Point? OptionsWindowsSize { get; set => SetProperty(ref field, value); }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "following_mode", false)]
        public partial bool FollowingMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "following_target", 0)]
        public partial uint FollowingTarget { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_health_bar", true)]
        public partial bool NamePlateHealthBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_opacity", 75)]
        public partial byte NamePlateOpacity { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_health_bar_opacity", 50)]
        public partial byte NamePlateHealthBarOpacity { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_hide_at_full_health", false)]
        public partial bool NamePlateHideAtFullHealth { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_hide_at_full_health_in_warmode", false)]
        public partial bool NamePlateHideAtFullHealthInWarmode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_border_opacity", 50)]
        public partial byte NamePlateBorderOpacity { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_avoid_overlap", false)]
        public partial bool NamePlateAvoidOverlap { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_use_fixed_width", false)]
        public partial bool NamePlateUseFixedWidth { get; set; }
        public int NamePlateFixedWidth { get; set => SetProperty(ref field, Math.Clamp(value, 60, 300)); } = 120;
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_use_fixed_health_bar_width", false)]
        public partial bool NamePlateUseFixedHealthBarWidth { get; set; }
        public int NamePlateHealthBarFixedWidth { get; set => SetProperty(ref field, Math.Clamp(value, 60, 300)); } = 120;
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_show_word_of_death_icon", false)]
        public partial bool NamePlateShowWordOfDeathIcon { get; set; }
        public int NamePlateHeight { get; set => SetProperty(ref field, Math.Clamp(value, 0, 80)); }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_split_health_bar", false)]
        public partial bool NamePlateSplitHealthBar { get; set; }
        public int NamePlateCornerRadius { get; set => SetProperty(ref field, Math.Clamp(value, 0, 40)); } = 0;
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_health_bar_mode", NamePlateHealthBarMode.StatusColor)]
        public partial NamePlateHealthBarMode NamePlateHealthBarMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_background_mode", NamePlateBackgroundMode.FixedColor)]
        public partial NamePlateBackgroundMode NamePlateBackgroundMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_background_r", 0)]
        public partial byte NamePlateBackgroundR { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_background_g", 0)]
        public partial byte NamePlateBackgroundG { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_background_b", 0)]
        public partial byte NamePlateBackgroundB { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_preset", NamePlatePreset.Custom)]
        public partial NamePlatePreset NamePlatePreset { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "left_align_tool_tips", false)]
        public partial bool LeftAlignToolTips { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "force_center_align_tooltip_mobiles", true)]
        public partial bool ForceCenterAlignTooltipMobiles { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "corpse_single_click_loot", false)]
        public partial bool CorpseSingleClickLoot { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_system_chat", false)]
        public partial bool DisableSystemChat { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_system_chat_while_journal_open", false)]
        public partial bool DisableSystemChatWhileJournalOpen { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_prompt_popup", true)]
        public partial bool UsePromptPopup { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "set_favorite_move_bag_serial", 0)]
        public partial uint SetFavoriteMoveBagSerial { get; set; }

        #region GRID CONTAINER
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_grid_layout_container_gumps", true)]
        public partial bool UseGridLayoutContainerGumps { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_containers_default_to_old_style_view", false)]
        public partial bool GridContainersDefaultToOldStyleView { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_container_view_mode", 0)]
        public partial int GridContainerViewMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_container_search_mode", 0)]
        public partial int GridContainerSearchMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_grid_container_anchor", false)]
        public partial bool EnableGridContainerAnchor { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_border_alpha", 75)]
        public partial byte GridBorderAlpha { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_border_hue", 0)]
        public partial ushort GridBorderHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_containers_scale", 100)]
        public partial byte GridContainersScale { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_container_scale_items", true)]
        public partial bool GridContainerScaleItems { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight_low_contrast_items", false)]
        public partial bool GridHighlightLowContrastItems { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight_low_contrast_items_style", 0)]
        public partial int GridHighlightLowContrastItemsStyle { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_enable_cont_preview", true)]
        public partial bool GridEnableContPreview { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid__border_style", 0)]
        public partial int Grid_BorderStyle { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid__default_columns", 5)]
        public partial int Grid_DefaultColumns { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid__default_rows", 5)]
        public partial int Grid_DefaultRows { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid__use_container_hue", false)]
        public partial bool Grid_UseContainerHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid__hide_border", false)]
        public partial bool Grid_HideBorder { get; set; }
        #endregion

        #region COOLDOWNS
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "cool_down_x", 50)]
        public partial int CoolDownX { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "cool_down_y", 50)]
        public partial int CoolDownY { get; set; }

        public List<ushort> Condition_Hue { get; set => SetProperty(ref field, value); } = new List<ushort>();
        public List<string> Condition_Label { get; set => SetProperty(ref field, value); } = new List<string>();
        public List<int> Condition_Duration { get; set => SetProperty(ref field, value); } = new List<int>();
        public List<string> Condition_Trigger { get; set => SetProperty(ref field, value); } = new List<string>();
        public List<int> Condition_Type { get; set => SetProperty(ref field, value); } = new List<int>();
        public List<bool> Condition_ReplaceIfExists { get; set => SetProperty(ref field, value); } = new List<bool>();
        public int CoolDownConditionCount
        {
            get
            {
                return Condition_Hue.Count;
            }
            set { }
        }
        #endregion

        #region IMPROVED BUFF BAR
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_improved_buff_bar", true)]
        public partial bool UseImprovedBuffBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "improved_buff_bar_hue", 905)]
        public partial ushort ImprovedBuffBarHue { get; set; }
        #endregion

        #region DAMAGE NUMBER HUES
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "damage_hue_self", 0x0034)]
        public partial ushort DamageHueSelf { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "damage_hue_pet", 0x0033)]
        public partial ushort DamageHuePet { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "damage_hue_ally", 0x0030)]
        public partial ushort DamageHueAlly { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "damage_hue_last_attck", 0x1F)]
        public partial ushort DamageHueLastAttck { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "damage_hue_other", 0x0021)]
        public partial ushort DamageHueOther { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_d_p_s", true)]
        public partial bool ShowDPS { get; set; }
        #endregion

        #region GridHighlightingProps
        public List<string> GridHighlight_Name { get; set => SetProperty(ref field, value); } = new List<string>();
        public List<ushort> GridHighlight_Hue { get; set => SetProperty(ref field, value); } = new List<ushort>();
        public List<List<string>> GridHighlight_PropNames { get; set => SetProperty(ref field, value); } = new List<List<string>>();
        public List<List<int>> GridHighlight_PropMinVal { get; set => SetProperty(ref field, value); } = new List<List<int>>();
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight__corpse_only", false)]
        public partial bool GridHighlight_CorpseOnly { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight_size", 1)]
        public partial int GridHighlightSize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight_properties", true)]
        public partial bool GridHighlightProperties { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "grid_highlight_show_rule_name", true)]
        public partial bool GridHighlightShowRuleName { get; set; }
        public List<bool> GridHighlight_AcceptExtraProperties { get; set => SetProperty(ref field, value); } = new List<bool>();
        public List<List<bool>> GridHighlight_IsOptionalProperties { get; set => SetProperty(ref field, value); } = new List<List<bool>>();
        public List<List<string>> GridHighlight_ExcludeNegatives { get; set => SetProperty(ref field, value); } = new List<List<string>>();
        public List<List<string>> GridHighlight_RequiredRarities { get; set => SetProperty(ref field, value); } = new();
        public List<GridHighlightSetupEntry> GridHighlightSetup { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableProperties { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableResistances { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableNegatives { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableSuperSlayers { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableSlayers { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableRarities { get; set => SetProperty(ref field, value); } = new();

        #endregion

        #region Modern paperdoll
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "modern_paper_doll_hue", 0)]
        public partial ushort ModernPaperDollHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "modern_paper_doll_durability_hue", 32)]
        public partial ushort ModernPaperDollDurabilityHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "modern_paper_doll__durability_percent", 90)]
        public partial int ModernPaperDoll_DurabilityPercent { get; set; }
        [JsonConverter(typeof(Point2Converter))] public Point ModernPaperdollPosition { get; set => SetProperty(ref field, value); } = new Point(100, 100);
        #endregion

        #region Health indicator
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "show_health_indicator_below", 0.9f)]
        public partial float ShowHealthIndicatorBelow { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_health_indicator", true)]
        public partial bool EnableHealthIndicator { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "health_indicator_width", 10)]
        public partial int HealthIndicatorWidth { get; set; }
        #endregion

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "main_window_background_hue", 1)]
        public partial ushort MainWindowBackgroundHue { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "move_multi_object_delay", 1000)]
        public partial int MoveMultiObjectDelay { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "spell_icon__display_hotkey", true)]
        public partial bool SpellIcon_DisplayHotkey { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "spell_icon__hotkey_hue", 1)]
        public partial ushort SpellIcon_HotkeyHue { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "spell_icon_scale", 100)]
        public partial int SpellIconScale { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_alpha_scrolling_on_gumps", true)]
        public partial bool EnableAlphaScrollingOnGumps { get; set; }

        [JsonConverter(typeof(Point2Converter))] public Point WorldMapPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point PaperdollPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point JournalPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point StatusGumpPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridSize { get; set => SetProperty(ref field, value); } = new(300, 300);
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "world_map_locked", false)]
        public partial bool WorldMapLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "paperdoll_locked", false)]
        public partial bool PaperdollLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_locked", false)]
        public partial bool JournalLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "status_gump_locked", false)]
        public partial bool StatusGumpLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "back_pack_locked", false)]
        public partial bool BackPackLocked { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "display_party_chat_overhead", true)]
        public partial bool DisplayPartyChatOverhead { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_macro_target_message", false)]
        public partial bool HideMacroTargetMessage { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "selected_t_t_f_journal_font", "avadonian")]
        public partial string SelectedTTFJournalFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "selected_journal_font_size", 20)]
        public partial int SelectedJournalFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "selected_tool_tip_font", "Roboto-Regular")]
        public partial string SelectedToolTipFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "selected_tool_tip_font_size", 20)]
        public partial int SelectedToolTipFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "game_window_side_chat_font", "avadonian")]
        public partial string GameWindowSideChatFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "game_window_side_chat_font_size", 20)]
        public partial int GameWindowSideChatFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "overhead_chat_font", "avadonian")]
        public partial string OverheadChatFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "overhead_chat_font_size", 20)]
        public partial int OverheadChatFontSize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "overhead_chat_width", 400)]
        public partial int OverheadChatWidth { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_font", "avadonian")]
        public partial string NamePlateFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "name_plate_font_size", 20)]
        public partial int NamePlateFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_new_options_window", true)]
        public partial bool UseNewOptionsWindow { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "options_font", "Roboto-Regular")]
        public partial string OptionsFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "options_font_size", 18)]
        public partial int OptionsFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "text_border_size", 1)]
        public partial int TextBorderSize { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "saved_mount_serial", 0)]
        public partial uint SavedMountSerial { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "saved_main_hand_serial", 0)]
        public partial uint SavedMainHandSerial { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "saved_off_hand_serial", 0)]
        public partial uint SavedOffHandSerial { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_modern_shop_gump", false)]
        public partial bool UseModernShopGump { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "max_journal_entries", 250)]
        public partial int MaxJournalEntries { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "max_sound_entries", 250)]
        public partial int MaxSoundEntries { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_journal_border", false)]
        public partial bool HideJournalBorder { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_transparency_when_inactive", false)]
        public partial bool JournalTransparencyWhenInactive { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_journal_timestamp", false)]
        public partial bool HideJournalTimestamp { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_journal_system_prefix", false)]
        public partial bool HideJournalSystemPrefix { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "health_line_size_multiplier", 1)]
        public partial int HealthLineSizeMultiplier { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "open_health_bar_for_last_attack", true)]
        public partial bool OpenHealthBarForLastAttack { get; set; }
        [JsonConverter(typeof(Point2Converter))]
        public Point LastTargetHealthBarPos { get; set => SetProperty(ref field, value); } = Point.Zero;
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tool_tip_b_g_hue", 0)]
        public partial ushort ToolTipBGHue { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_version_history_shown", null)]
        public partial string LastVersionHistoryShown { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "advanced_skills_gump_height", 510)]
        public partial int AdvancedSkillsGumpHeight { get; set; }

        #region ToolTip Overrides
        public List<string> ToolTipOverride_SearchText { get; set => SetProperty(ref field, value); } = new List<string>() { "Physical Res", "Fire Resist", "Cold Resist", "Poison Resist", "Energy Resist", "Weapon Damage" };
        public List<string> ToolTipOverride_NewFormat { get; set => SetProperty(ref field, value); } = new List<string>() { "/c[#8c733e]Physical Resist {1}%", "/c[red]Fire Resist {1}%", "/c[teal]Cold Resist {1}%", "/c[green]Poison Resist {1}%", "/c[purple]Energy Resist {1}%", "{0} /c[orange]{1}{4} /cd- /c[red]{2}{5}" };
        public List<int> ToolTipOverride_MinVal1 { get; set => SetProperty(ref field, value); } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MinVal2 { get; set => SetProperty(ref field, value); } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MaxVal1 { get; set => SetProperty(ref field, value); } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        public List<int> ToolTipOverride_MaxVal2 { get; set => SetProperty(ref field, value); } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        public List<byte> ToolTipOverride_Layer { get; set => SetProperty(ref field, value); } = new List<byte>() { (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any };
        #endregion

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "tooltip_header_format", "/c[yellow]{0}")]
        public partial string TooltipHeaderFormat { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "display_skill_bar_on_change", true)]
        public partial bool DisplaySkillBarOnChange { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "skill_bar_format", "{0}: {1} / {2}")]
        public partial string SkillBarFormat { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "display_radius", false)]
        public partial bool DisplayRadius { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "display_radius_distance", 10)]
        public partial int DisplayRadiusDistance { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "display_radius_hue", 22)]
        public partial ushort DisplayRadiusHue { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_spell_indicators", true)]
        public partial bool EnableSpellIndicators { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_auto_loot", false)]
        public partial bool EnableAutoLoot { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "auto_loot_human_corpses", false)]
        public partial bool AutoLootHumanCorpses { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "item_database_enabled", true)]
        public partial bool ItemDatabaseEnabled { get; set; }

        public static uint GumpsVersion { get; private set; }

        [JsonConverter(typeof(Point2Converter))]
        public Point InfoBarSize { get; set => SetProperty(ref field, value); } = new Point(400, 20);
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "info_bar_locked", false)]
        public partial bool InfoBarLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "info_bar_font", "Roboto-Regular")]
        public partial string InfoBarFont { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "info_bar_font_size", 18)]
        public partial int InfoBarFontSize { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "last_journal_tab", 0)]
        public partial int LastJournalTab { get; set; }
        public Dictionary<string, MessageType[]> JournalTabs { get; set => SetProperty(ref field, value); } = new Dictionary<string, MessageType[]>()
        {
            { "All", new MessageType[] {
                MessageType.Alliance, MessageType.Command, MessageType.Emote,
                MessageType.Encoded, MessageType.Focus, MessageType.Guild,
                MessageType.Label, MessageType.Limit3Spell, MessageType.Party,
                MessageType.Regular, MessageType.Spell, MessageType.System,
                MessageType.Whisper, MessageType.Yell, MessageType.ChatSystem }
            },
            { "Chat", new MessageType[] {
                MessageType.Regular,
                MessageType.Guild,
                MessageType.Alliance,
                MessageType.Emote,
                MessageType.Party,
                MessageType.Whisper,
                MessageType.Yell,
                MessageType.ChatSystem }
            },
            {
                "Guild|Party", new MessageType[] {
                    MessageType.Guild,
                    MessageType.Alliance,
                    MessageType.Party }
            },
            {
                "System", new MessageType[] {
                    MessageType.System }
            }
        };

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_last_moved_cooldown_position", true)]
        public partial bool UseLastMovedCooldownPosition { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "close_health_bar_if_anchored", false)]
        public partial bool CloseHealthBarIfAnchored { get; set; }

        [JsonConverter(typeof(Point2Converter))]
        public Point SkillProgressBarPosition { get; set => SetProperty(ref field, value); } = Point.Zero;

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "force_resync_on_hang", false)]
        public partial bool ForceResyncOnHang { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_one_h_p_bar_for_last_attack", true)]
        public partial bool UseOneHPBarForLastAttack { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_mouse_interaction_overhead_text", false)]
        public partial bool DisableMouseInteractionOverheadText { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hidden_layers_enabled", false)]
        public partial bool HiddenLayersEnabled { get; set; }
        public List<int> HiddenLayers { get; set => SetProperty(ref field, value); } = new List<int>();
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_layers_for_self", true)]
        public partial bool HideLayersForSelf { get; set; }

        public List<string> AutoOpenXmlGumps { get; set => SetProperty(ref field, value); } = new List<string>();

        /// <summary>
        /// The sensitivity of the controller mouse input.
        /// </summary>
        /// <remarks>
        /// The typo here is a bit problematic as it's also serialized, meaning if we change it here, we essentially invalidate the user's configuration.
        /// </remarks>
        public int ControllerMouseSensativity
        {
            get => Input.Mouse.ControllerSensitivity;
            set
            {
                if (Input.Mouse.ControllerSensitivity != value)
                {
                    Input.Mouse.ControllerSensitivity = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonConverter(typeof(Point2Converter))]
        public Point PlayerOffset { get; set => SetProperty(ref field, value); } = new Point(0, 0);

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "camera_smoothing_factor", 0f)]
        public partial float CameraSmoothingFactor { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "paperdoll_scale", 1f)]
        public partial double PaperdollScale { get; set; }

        public double StatusGumpScale { get; set => SetProperty(ref field, Math.Clamp(value, 0.5d, 3.0d)); } = 1f;

        public double ContextMenuScale { get; set => SetProperty(ref field, Math.Clamp(value, 0.5d, 3.0d)); } = 1f;

        public double TradeGumpScale { get; set => SetProperty(ref field, Math.Clamp(value, 0.5d, 3.0d)); } = 1f;

        /// <summary>
        /// Scale applied to every server created gump (and all of its controls).
        /// </summary>
        public double ServerGumpScale { get; set => SetProperty(ref field, Math.Clamp(value, 0.5d, 3.0d)); } = 1f;

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "s_o_s_gump_i_d", 1915258020)]
        public partial uint SOSGumpID { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "modern_paperdoll_anchor_enabled", false)]
        public partial bool ModernPaperdollAnchorEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "journal_anchor_enabled", false)]
        public partial bool JournalAnchorEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_auto_loot_progress_bar", true)]
        public partial bool EnableAutoLootProgressBar { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "use_w_a_s_d_instead_arrow_keys", false)]
        public partial bool UseWASDInsteadArrowKeys { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "nearby_loot_gump_height", 550)]
        public partial int NearbyLootGumpHeight { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "force_tooltips_on_old_clients", true)]
        public partial bool ForceTooltipsOnOldClients { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "nearby_loot_opens_human_corpses", false)]
        public partial bool NearbyLootOpensHumanCorpses { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "turn_delay", 100)]
        public partial ushort TurnDelay { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sell_agent_enabled", false)]
        public partial bool SellAgentEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sell_agent_max_uniques", 50)]
        public partial int SellAgentMaxUniques { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "sell_agent_max_items", 0)]
        public partial int SellAgentMaxItems { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buy_agent_enabled", false)]
        public partial bool BuyAgentEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buy_agent_max_uniques", 50)]
        public partial int BuyAgentMaxUniques { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buy_agent_max_items", 0)]
        public partial int BuyAgentMaxItems { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "buy_agent_sub_containers", true)]
        public partial bool BuyAgentSubContainers { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_targeting_grid_containers", false)]
        public partial bool DisableTargetingGridContainers { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "controller_enabled", true)]
        public partial bool ControllerEnabled { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_scavenger", true)]
        public partial bool EnableScavenger { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "counter_gump_locked", false)]
        public partial bool CounterGumpLocked { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "nearby_loot_conceals_container_on_open", true)]
        public partial bool NearbyLootConcealsContainerOnOpen { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "spell_bar__show_hotkeys", true)]
        public partial bool SpellBar_ShowHotkeys { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "forced_house_transparency", 40)]
        public partial byte ForcedHouseTransparency { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "forced_transparency_house_tile_hue", 0)]
        public partial ushort ForcedTransparencyHouseTileHue { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "force_house_transparency", false)]
        public partial bool ForceHouseTransparency { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "hide_hud_gump_flags", 0)]
        public partial ulong HideHudGumpFlags { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_gray_enemies", false)]
        public partial bool DisableGrayEnemies { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_post_processing_effects", false)]
        public partial bool EnablePostProcessingEffects { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "post_processing_type", 0)]
        public partial ushort PostProcessingType { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_hotkeys", false)]
        public partial bool DisableHotkeys { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "disable_dismount_in_war_mode", true)]
        public partial bool DisableDismountInWarMode { get; set; }
        [JsonIgnore]
        [SqlSetting(SettingsScope.Char, "enable_a_sync_map_loading", true)]
        public partial bool EnableASyncMapLoading { get; set; }

        public string TazUOChatNick
        {
            get
            {
                if (field == null)
                    SetProperty(ref field, TazUOChatManager.GenerateFantasyName(2, 3));

                return field;
            }
            set => SetProperty(ref field, value);
        }

        // SQL-backed settings — property implementations are source-generated into Profile.SqlSettings.g.cs
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

        // Hand-written: has side-effect beyond SetAsync
        [JsonIgnore]
        public bool OutlineMobilesNotoriety
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.OUTLINE_NOTORIETIES, value);
            }
        }

        // Hand-written: has side-effect (TazUOChatManager.Init)
        [JsonIgnore]
        public bool DisableConnectToIrcOnLogin
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.IRC_AUTO_CONNECT, value);

                // if(value && !TazUOChatManager.Instance.IsConnected)
                //     TazUOChatManager.Instance.Init();
            }
        }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.OVERHEAD_MESSAGE_TYPES_HIDDEN, (uint)0)]
        public partial uint DisabledOverheadMessageTypes { get; set; }

        [JsonIgnore]
        [SqlSetting(SettingsScope.Global, Constants.SqlSettings.DISABLE_AUTOLOOT_RETRY_CORPSE, false)]
        public partial bool DisableAutolootCorpseRetry { get; set; } = false;

        private long lastSave;

        internal void AfterLoad()
        {
            if (Client.Settings == null)
            {
                Log.Error("Warning, SQL settings failed to load!");
                return;
            }

            //These are fine if we continue without loading them yet (non-Char scoped)
            Client.Settings.GetAllAsync(SettingsScope.Global).ContinueWith(t =>
            {
                Dictionary<string, string> kvp = t.Result;
                MainThreadQueue.EnqueueAction(() =>
                {
                    LoadGeneratedGlobalSqlSettings(kvp);

                    // Hand-written: IRC has a side-effect in its setter
                    if (kvp.TryGetValue(Constants.SqlSettings.IRC_AUTO_CONNECT, out string val) && bool.TryParse(val, out bool b))
                        DisableConnectToIrcOnLogin = b;
                });
            });

            //These must be waited before continue for various purposes elsewhere
            Task[] mustWait = [
                Client.Settings.GetAsync(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_AUTO_START, false, b => WebMapAutoStart = b),
                Client.Settings.GetAsync(SettingsScope.Global, Constants.SqlSettings.WEB_MAP_PORT, 8088, p => WebMapServerPort = p),
                Client.Settings.GetAsync(SettingsScope.Global, Constants.SqlSettings.OUTLINE_NOTORIETIES, false, p => OutlineMobilesNotoriety = p)
            ];

            Task.WaitAll(mustWait, 5000);
        }

        internal void LoadCharScopedSettings()
        {
            if (Client.Settings == null)
            {
                Log.Error("Warning, char scoped SQL settings failed to load!");
                return;
            }

            // Load current Char-scoped values synchronously (single query) so every field is populated before
            // the game scene builds its gumps from these values.
            Dictionary<string, string> kvp = Client.Settings.GetAll(SettingsScope.Char);
            LoadGeneratedCharSqlSettings(kvp);

            // One-time migration: import existing profile.json values for the newly-migrated settings. Runs
            // after the bulk load (so it overrides only the keys not yet present in SQLite) and here (after the
            // player is created) so the Char scope key resolves against a valid serial. MigrateJsonToSql assigns
            // through the setters, which both populate the fields for this session and persist to SQLite.
            if (!Client.Settings.Get<bool>(SettingsScope.Char, Constants.SqlSettings.PROFILE_JSON_MIGRATED))
            {
                TryMigrateProfileJsonToSql();
                Client.Settings.Set(SettingsScope.Char, Constants.SqlSettings.PROFILE_JSON_MIGRATED, true);
            }

            // Re-apply settings that are consumed during ProfileManager.Load (before Char-scoped values exist).
            Client.Game?.SetVSync(EnableVSync);
        }

        // Reads the legacy per-character profile.json and copies every migrated scalar/enum setting into the
        // SQLite store (via the generated MigrateJsonToSql). Best-effort: failures are logged and ignored.
        private void TryMigrateProfileJsonToSql()
        {
            try
            {
                if (string.IsNullOrEmpty(ProfileManager.ProfilePath))
                    return;

                string file = Path.Combine(ProfileManager.ProfilePath, "profile.json");

                using JsonDocument doc = ConfigurationResolver.LoadDocument(file);
                if (doc == null)
                    return;

                MigrateJsonToSql(doc);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to migrate profile.json settings to SQL: {ex.Message}");
            }
        }

        internal void Save(World world, string path, bool saveGumps = true)
        {
            if (Time.Ticks - lastSave < 10) //Don't save if saved in the last 10 ms, prevent duplcate saving when exiting game with options menu open
                return;

            Log.Trace($"Saving path:\t\t{path}");
            string filePath = Path.Combine(path, "profile.json");

            // Create backup rotation before saving
            CreateBackupRotation(filePath);

            // Save profile settings
            ConfigurationResolver.Save(this, filePath, ProfileJsonContext.DefaultToUse.Profile);

            // Save opened gumps
            if (saveGumps)
                SaveGumps(world, path);

            Log.Trace("Saving done!");
            lastSave = Time.Ticks;
        }

        public void SaveAsFile(string path, string filename) => ConfigurationResolver.Save(this, Path.Combine(path, filename), ProfileJsonContext.DefaultToUse.Profile);

        private void CreateBackupRotation(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string backup3 = filePath + ".bak3";
            string backup2 = filePath + ".bak2";
            string backup1 = filePath + ".bak1";

            try
            {
                // Remove oldest backup if it exists
                if (File.Exists(backup3))
                {
                    File.Delete(backup3);
                }

                // Rotate backups: .bak2 -> .bak3, .bak1 -> .bak2
                if (File.Exists(backup2))
                {
                    File.Move(backup2, backup3);
                }

                if (File.Exists(backup1))
                {
                    File.Move(backup1, backup2);
                }

                // Copy current file to .bak1
                File.Copy(filePath, backup1);
            }
            catch (IOException e)
            {
                // Log backup rotation failure but don't prevent the save
                Log.Error($"Failed to create backup rotation: {e}");
            }
        }

        public void SaveAs(string path, string filename = "default.json") => ConfigurationResolver.Save(this, Path.Combine(path, filename), ProfileJsonContext.DefaultToUse.Profile);

        private void SaveGumps(World world, string path)
        {
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            using (var xml = new XmlTextWriter(gumpsXmlPath, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            })
            {
                xml.WriteStartDocument(true);
                xml.WriteStartElement("gumps");

                UIManager.AnchorManager.Save(xml);

                var gumps = new LinkedList<Gump>();
                var myraWindows = new List<MyraControl>();

                foreach (IGui igui in UIManager.Gumps)
                {
                    if (igui is MyraControl mc)
                    {
                        myraWindows.Add(mc);
                        continue;
                    }

                    if (igui is not Gump gump) continue;

                    if (!gump.IsDisposed && gump.CanBeSaved && !(gump is AnchorableGump anchored && UIManager.AnchorManager[anchored] != null))
                    {
                        gumps.AddLast(gump);
                    }
                }

                LinkedListNode<Gump> first = gumps.First;

                while (first != null)
                {
                    Gump gump = first.Value;

                    if (gump.LocalSerial != 0)
                    {
                        Item item = world.Items.Get(gump.LocalSerial);

                        if (item != null && !item.IsDestroyed && item.Opened)
                        {
                            while (SerialHelper.IsItem(item.Container))
                            {
                                item = world.Items.Get(item.Container);
                            }

                            SaveItemsGumpRecursive(item, xml, gumps);

                            if (first.List != null)
                            {
                                gumps.Remove(first);
                            }

                            first = gumps.First;

                            continue;
                        }
                    }

                    xml.WriteStartElement("gump");
                    gump.Save(xml);
                    xml.WriteEndElement();

                    if (first.List != null)
                    {
                        gumps.Remove(first);
                    }

                    first = gumps.First;
                }

                #region Myra

                foreach (MyraControl mc in myraWindows)
                {
                    if (!mc.CanBeSaved || mc.IsDisposed) continue;

                    xml.WriteStartElement("myra");
                    mc.Save(xml);
                    xml.WriteEndElement();
                }
                #endregion

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }


            world.SkillsGroupManager.Save();
        }

        private static void SaveItemsGumpRecursive(Item parent, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (parent != null && !parent.IsDestroyed && parent.Opened)
            {
                SaveItemsGump(parent, xml, list);

                var first = (Item)parent.Items;

                while (first != null)
                {
                    var next = (Item)first.Next;

                    SaveItemsGumpRecursive(first, xml, list);

                    first = next;
                }
            }
        }

        private static void SaveItemsGump(Item item, XmlTextWriter xml, LinkedList<Gump> list)
        {
            if (item != null && !item.IsDestroyed && item.Opened)
            {
                LinkedListNode<Gump> first = list.First;

                while (first != null)
                {
                    LinkedListNode<Gump> next = first.Next;

                    if (first.Value.LocalSerial == item.Serial && !first.Value.IsDisposed)
                    {
                        xml.WriteStartElement("gump");
                        first.Value.Save(xml);
                        xml.WriteEndElement();

                        list.Remove(first);

                        break;
                    }

                    first = next;
                }
            }
        }


        public List<Gump> ReadGumps(World world, string path)
        {
            var gumps = new List<Gump>();
            List<(Gump gump, GumpType type, int x, int y, uint serial, uint parent, XmlElement xml)> nestedGumps = new();

            // load skillsgroup
            world.SkillsGroupManager.Load();

            // load gumps
            string gumpsXmlPath = Path.Combine(path, "gumps.xml");

            if (File.Exists(gumpsXmlPath))
            {
                var doc = new XmlDocument();

                try
                {
                    doc.Load(gumpsXmlPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());

                    return gumps;
                }

                XmlElement root = doc["gumps"];

                if (root != null)
                {
                    int pdolc = 0;

                    foreach (XmlElement xml in root.ChildNodes /*.GetElementsByTagName("gump")*/)
                    {
                        if (xml.Name == "window")
                        {
                            LoadWindow(xml);
                            continue;
                        }

                        if (xml.Name == "myra")
                        {
                            LoadMyraControl(xml);
                            continue;
                        }

                        if (xml.Name != "gump")
                        {
                            continue;
                        }

                        try
                        {
                            GumpType type = (GumpType)int.Parse(xml.GetAttribute(nameof(type)));
                            int x = int.Parse(xml.GetAttribute(nameof(x)));
                            int y = int.Parse(xml.GetAttribute(nameof(y)));
                            uint serial = uint.Parse(xml.GetAttribute(nameof(serial)));
                            uint? parent = uint.TryParse(xml.GetAttribute(nameof(parent)), out uint result) ? result : null;

                            if (uint.TryParse(xml.GetAttribute("serverSerial"), out uint serverSerial))
                            {
                                UIManager.SavePosition(serverSerial, new Point(x, y));
                            }

                            Gump gump = null;

                            switch (type)
                            {
                                case GumpType.SpellBar: gump = new SpellBar(world); break;
                                case GumpType.NearbyCorpseLoot: gump = new NearbyLootGump(world); break;
                                case GumpType.Buff:
                                    if (ProfileManager.CurrentProfile.UseImprovedBuffBar)
                                        gump = new ImprovedBuffGump(world);
                                    else
                                        gump = new BuffGump(world);

                                    break;

                                case GumpType.Container:
                                    gump = new ContainerGump(world);

                                    break;

                                case GumpType.CounterBar:
                                    gump = new CounterBarGump(world);

                                    break;

                                case GumpType.HealthBar:
                                    if (CustomBarsToggled)
                                    {
                                        gump = new HealthBarGumpCustom(world);
                                    }
                                    else
                                    {
                                        gump = new HealthBarGump(world);
                                    }

                                    break;

                                case GumpType.InfoBar:
                                    gump = new InfoBarGump(world);

                                    break;

                                case GumpType.Journal:
                                    gump = new ResizableJournal(world);

                                    break;

                                case GumpType.MacroButton:
                                    gump = new MacroButtonGump(world);

                                    break;
                                case GumpType.MacroButtonEditor:
                                    gump = new MacroButtonEditorGump(world);

                                    break;

                                case GumpType.MiniMap:
                                    gump = new MiniMapGump(world);

                                    break;

                                case GumpType.PaperDoll:
                                    if (pdolc > 0)
                                    {
                                        break;
                                    }

                                    if (ProfileManager.CurrentProfile.UseModernPaperdoll && serial == world.Player.Serial)
                                    {
                                        gump = new ModernPaperdoll(world, serial);
                                        x = ProfileManager.CurrentProfile.ModernPaperdollPosition.X;
                                        y = ProfileManager.CurrentProfile.ModernPaperdollPosition.Y;
                                    }
                                    else
                                    {
                                        gump = new PaperDollGump(world, serial, serial == world.Player.Serial);
                                        x = ProfileManager.CurrentProfile.PaperdollPosition.X;
                                        y = ProfileManager.CurrentProfile.PaperdollPosition.Y;
                                    }
                                    pdolc++;

                                    break;

                                case GumpType.SkillMenu:
                                    if (StandardSkillsGump)
                                    {
                                        gump = new StandardSkillsGump(world);
                                    }
                                    else
                                    {
                                        gump = new SkillGumpAdvanced(world);
                                    }

                                    break;

                                case GumpType.SpellBook:
                                    gump = new SpellbookGump(world);

                                    break;

                                case GumpType.StatusGump:
                                    gump = StatusGumpBase.AddStatusGump(world, 0, 0);
                                    x = ProfileManager.CurrentProfile.StatusGumpPosition.X;
                                    y = ProfileManager.CurrentProfile.StatusGumpPosition.Y;
                                    break;

                                //case GumpType.TipNotice:
                                //    gump = new TipNoticeGump();
                                //    break;
                                case GumpType.AbilityButton:
                                    gump = new UseAbilityButtonGump(world);

                                    break;

                                case GumpType.SpellButton:
                                    gump = new UseSpellButtonGump(world);

                                    break;

                                case GumpType.SkillButton:
                                    gump = new SkillButtonGump(world);

                                    break;

                                case GumpType.RacialButton:
                                    gump = new RacialAbilityButton(world);

                                    break;

                                case GumpType.WorldMap:
                                    gump = new WorldMapGump(world);

                                    break;

                                case GumpType.Debug:
                                    gump = new DebugGump(world, 100, 100);

                                    break;

                                case GumpType.NetStats:
                                    gump = new NetworkStatsGump(world, 100, 100);

                                    break;

                                case GumpType.NameOverHeadHandler:
                                    NameOverHeadHandlerGump.LastPosition = new Point(x, y);
                                    // Gump gets opened by NameOverHeadManager, we just want to save the last position from profile
                                    break;

                                case GumpType.GridContainer:
                                    ushort ogContainer = ushort.Parse(xml.GetAttribute("ogContainer"));
                                    gump = new GridContainer(world, serial, ogContainer);
                                    if (((GridContainer)gump).IsPlayerBackpack)
                                    {
                                        x = ProfileManager.CurrentProfile.BackpackGridPosition.X;
                                        y = ProfileManager.CurrentProfile.BackpackGridPosition.Y;
                                    }
                                    break;

                                case GumpType.DurabilityGump:
                                    gump = new DurabilitysGump(world);
                                    break;

                                case GumpType.HealthBarCollector:
                                    gump = new HealthbarCollectorGump(world);
                                    break;
                            }

                            if (gump == null)
                            {
                                continue;
                            }

                            if (parent.HasValue)
                            {
                                nestedGumps.Add((gump, type, x, y, serial, parent.Value, xml));
                                continue;
                            }

                            gump.LocalSerial = serial;
                            gump.Restore(xml);
                            gump.X = x;
                            gump.Y = y;
                            //gump.SetInScreen();

                            if (gump.LocalSerial != 0)
                            {
                                UIManager.SavePosition(gump.LocalSerial, new Point(x, y));
                            }

                            if (!gump.IsDisposed)
                            {
                                gumps.Add(gump);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }

                    HashSet<uint> processedSerials = new();
                    while (nestedGumps.Count != 0)
                    {
                        int initialCount = nestedGumps.Count;
                        foreach ((Gump gump, GumpType type, int x, int y, uint serial, uint parent, XmlElement xml) entry in nestedGumps.ToList())
                        {
                            (Gump gump, GumpType type, int x, int y, uint serial, uint parent, XmlElement xml) = entry;
                            bool parentIsInList = nestedGumps.Any(g => parent == g.serial);
                            if (parentIsInList)
                            {
                                continue;
                            }

                            if (!processedSerials.Contains(parent) && world.Get(parent) is null)
                            {
                                continue;
                            }

                            processedSerials.Add(serial);
                            nestedGumps.Remove(entry);

                            gump.LocalSerial = serial;
                            gump.Restore(xml);
                            gump.X = x;
                            gump.Y = y;
                            //gump.SetInScreen();

                            if (gump.LocalSerial != 0)
                            {
                                UIManager.SavePosition(gump.LocalSerial, new Point(x, y));
                            }

                            if (!gump.IsDisposed)
                            {
                                gumps.Add(gump);
                            }
                        }

                        if (initialCount == nestedGumps.Count)
                        {
                            Log.Warn($"[Profile.ReadGumps] Skipping nested gumps: {string.Join(", ", nestedGumps)}");
                            break;
                        }
                    }

                    foreach (XmlElement group in root.GetElementsByTagName("anchored_group_gump"))
                    {
                        int matrix_width = int.Parse(group.GetAttribute("matrix_w"));
                        int matrix_height = int.Parse(group.GetAttribute("matrix_h"));

                        var ancoGroup = new AnchorManager.AnchorGroup();
                        ancoGroup.ResizeMatrix(matrix_width, matrix_height, 0, 0);

                        foreach (XmlElement xml in group.GetElementsByTagName("gump"))
                        {
                            try
                            {
                                var type = (GumpType)int.Parse(xml.GetAttribute("type"));
                                int x = int.Parse(xml.GetAttribute("x"));
                                int y = int.Parse(xml.GetAttribute("y"));
                                uint serial = uint.Parse(xml.GetAttribute("serial"));

                                int matrix_x = int.Parse(xml.GetAttribute("matrix_x"));
                                int matrix_y = int.Parse(xml.GetAttribute("matrix_y"));

                                AnchorableGump gump = null;

                                switch (type)
                                {
                                    case GumpType.SpellButton:
                                        gump = new UseSpellButtonGump(world);

                                        break;

                                    case GumpType.SkillButton:
                                        gump = new SkillButtonGump(world);

                                        break;

                                    case GumpType.HealthBar:
                                        if (CustomBarsToggled)
                                        {
                                            gump = new HealthBarGumpCustom(world);
                                        }
                                        else
                                        {
                                            gump = new HealthBarGump(world);
                                        }

                                        break;

                                    case GumpType.AbilityButton:
                                        gump = new UseAbilityButtonGump(world);

                                        break;

                                    case GumpType.MacroButton:
                                        gump = new MacroButtonGump(world);

                                        break;
                                    case GumpType.GridContainer:
                                        ushort ogContainer = ushort.Parse(xml.GetAttribute("ogContainer"));
                                        gump = new GridContainer(world, serial, ogContainer);
                                        break;
                                    case GumpType.Journal:
                                        gump = new ResizableJournal(world);
                                        break;
                                    case GumpType.WorldMap:
                                        gump = new WorldMapGump(world);
                                        break;
                                    case GumpType.InfoBar:
                                        gump = new InfoBarGump(world);
                                        break;
                                    case GumpType.PaperDoll:
                                        gump = new ModernPaperdoll(world, world.Player.Serial);
                                        break;
                                }

                                if (gump != null)
                                {
                                    gump.LocalSerial = serial;
                                    gump.Restore(xml);
                                    gump.X = x;
                                    gump.Y = y;
                                    //gump.SetInScreen();

                                    if (!gump.IsDisposed)
                                    {
                                        if (UIManager.AnchorManager[gump] == null && ancoGroup.IsEmptyDirection(matrix_x, matrix_y))
                                        {
                                            gumps.Add(gump);
                                            UIManager.AnchorManager[gump] = ancoGroup;
                                            ancoGroup.AddControlToMatrix(matrix_x, matrix_y, gump);
                                        }
                                        else
                                        {
                                            gump.Dispose();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }
                    }
                }
            }

            return gumps;
        }

        private void LoadMyraControl(XmlElement xml)
        {
            string type = xml.GetAttribute("type");

            if (string.IsNullOrEmpty(type)) return;

            switch (type)
            {
                default:
                    Log.Error($"No type setup in [Profile.cs] for {type}");
                    break;
                case "ClassicUO.Game.UI.MyraWindows.AssistantWindow":
                    var assistant = new AssistantWindow();
                    assistant.Load(xml);
                    UIManager.Add(assistant);
                    break;
                case "ClassicUO.Game.UI.MyraWindows.RunningScriptsWindow":
                    var rsw = new RunningScriptsWindow();
                    rsw.Load(xml);
                    UIManager.Add(rsw);
                    break;
                case "ClassicUO.Game.UI.MyraWindows.ScriptManagerWindow":
                    var smw = new ScriptManagerWindow();
                    smw.Load(xml);
                    UIManager.Add(smw);
                    break;
            }
        }

        private void LoadWindow(XmlElement xml)
        {
            string type = xml.GetAttribute("type");

            if (string.IsNullOrEmpty(type)) return;

            switch (type)
            {
                default:
                    Log.Error($"No type setup in [Profile.cs] for {type}");
                    break;
                case "ClassicUO.Game.UI.ImGuiControls.ScriptManagerWindow":
                    var smwCompat = new ScriptManagerWindow();
                    UIManager.Add(smwCompat);
                    break;
                case "ClassicUO.Game.UI.ImGuiControls.AssistantWindow":
                    AssistantWindow.Show();
                    break;
                case "ClassicUO.Game.UI.ImGuiControls.RunningScriptsWindow":
                    var rsw = new RunningScriptsWindow();
                    UIManager.Add(rsw);
                    break;
            }
        }
    }
}
