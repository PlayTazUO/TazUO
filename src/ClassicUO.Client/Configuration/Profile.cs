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
using System.Text.Json.Serialization.Metadata;
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



    public sealed partial class Profile : JsonSave<Profile>, INotifyPropertyChanged
    {
        private static Profile _defaultPreview;

        /// <summary>Lives in the profile folder as <c>profile.json</c>.</summary>
        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => "profile.json";

        protected override JsonTypeInfo<Profile> TypeInfo => ProfileJsonContext.DefaultToUse.Profile;

        /// <summary>
        /// A cached default profile with safe default settings, used as a fallback when no profile
        /// is loaded (e.g. rendering character previews on the login screen). Never touches disk
        /// and never fires <see cref="PropertyChanged"/>.
        /// </summary>
        public static Profile DefaultPreviewProfile => _defaultPreview ??= new Profile();

        [JsonIgnore] public string Username { get; set => SetProperty(ref field, value); }
        [JsonIgnore] public string ServerName { get; set => SetProperty(ref field, value); }
        [JsonIgnore] public string CharacterName { get; set => SetProperty(ref field, value); }
        [JsonIgnore] public uint Serial { get; set => SetProperty(ref field, value); }

        // voice recognition
        public bool VoiceRecognitionEnabled { get; set => SetProperty(ref field, value); } = false;
        public string VoiceModelPath { get; set => SetProperty(ref field, value); } = string.Empty;

        // sounds
        [Obsolete("Remove after 10/8/26")]
        public bool EnableSound { get; set => SetProperty(ref field, value); } = true;
        [Obsolete("Remove after 10/8/26")]
        public int SoundVolume { get; set => SetProperty(ref field, value); } = 50;
        [Obsolete("Remove after 10/8/26")]
        public bool EnableMusic { get; set => SetProperty(ref field, value); } = true;
        [Obsolete("Remove after 10/8/26")]
        public int MusicVolume { get; set => SetProperty(ref field, value); } = 50;
        [Obsolete("Remove after 10/8/26")]
        public bool EnableFootstepsSound { get; set => SetProperty(ref field, value); } = true;
        [Obsolete("Remove after 10/8/26")]
        public bool EnableRainSound { get; set => SetProperty(ref field, value); } = true;
        [Obsolete("Remove after 10/8/26")]
        public bool EnableCombatMusic { get; set => SetProperty(ref field, value); } = true;
        [Obsolete("Remove after 10/8/26")]
        public bool ReproduceSoundsInBackground { get; set => SetProperty(ref field, value); }

        // fonts and speech
        public byte ChatFont { get; set => SetProperty(ref field, value); } = 1;
        public int SpeechDelay { get; set => SetProperty(ref field, value); } = 100;
        public bool ScaleSpeechDelay { get; set => SetProperty(ref field, value); } = true;
        public bool SaveJournalToFile { get; set => SetProperty(ref field, value); } = false;
        public bool ForceUnicodeJournal { get; set => SetProperty(ref field, value); }
        public bool IgnoreAllianceMessages { get; set => SetProperty(ref field, value); }
        public bool IgnoreGuildMessages { get; set => SetProperty(ref field, value); }

        // hues
        public ushort SpeechHue { get; set => SetProperty(ref field, value); } = 0x02B2;
        public ushort WhisperHue { get; set => SetProperty(ref field, value); } = 0x0033;
        public ushort EmoteHue { get; set => SetProperty(ref field, value); } = 0x0021;
        public ushort YellHue { get; set => SetProperty(ref field, value); } = 0x0021;
        public ushort PartyMessageHue { get; set => SetProperty(ref field, value); } = 0x0044;
        public ushort GuildMessageHue { get; set => SetProperty(ref field, value); } = 0x0044;
        public ushort AllyMessageHue { get; set => SetProperty(ref field, value); } = 0x0057;
        public ushort ChatMessageHue { get; set => SetProperty(ref field, value); } = 0x0256;
        public ushort InnocentHue { get; set => SetProperty(ref field, value); } = 0x005A;
        public ushort PartyAuraHue { get; set => SetProperty(ref field, value); } = 0x0044;
        public ushort FriendHue { get; set => SetProperty(ref field, value); } = 0x0044;
        public ushort CriminalHue { get; set => SetProperty(ref field, value); } = 0x03B2;
        public ushort CanAttackHue { get; set => SetProperty(ref field, value); } = 0x03B2;
        public ushort EnemyHue { get; set => SetProperty(ref field, value); } = 0x0031;
        public ushort MurdererHue { get; set => SetProperty(ref field, value); } = 0x0023;
        public ushort BeneficHue { get; set => SetProperty(ref field, value); } = 0x0059;
        public ushort HarmfulHue { get; set => SetProperty(ref field, value); } = 0x0020;
        public ushort NeutralHue { get; set => SetProperty(ref field, value); } = 0x03B1;
        public bool EnabledSpellHue { get; set => SetProperty(ref field, value); }
        public bool EnabledSpellFormat { get; set => SetProperty(ref field, value); } = true;
        public string SpellDisplayFormat { get; set => SetProperty(ref field, value); } = "{power} [{spell}]";
        public ushort PoisonHue { get; set => SetProperty(ref field, value); } = 0x0044;
        public ushort ParalyzedHue { get; set => SetProperty(ref field, value); } = 0x014C;
        public ushort InvulnerableHue { get; set => SetProperty(ref field, value); } = 0x0030;
        public ushort AltJournalBackgroundHue { get; set => SetProperty(ref field, value); } = 0x0000;
        public ushort AltGridContainerBackgroundHue { get; set => SetProperty(ref field, value); } = 0x0000;
        public bool OverridePartyAndGuildHue { get; set => SetProperty(ref field, value); } = false;

        // visual
        public bool EnabledCriminalActionQuery { get; set => SetProperty(ref field, value); } = true;
        public bool EnabledBeneficialCriminalActionQuery { get; set => SetProperty(ref field, value); }
        public bool UseOldStatusGump { get; set => SetProperty(ref field, value); }
        public bool StatusGumpBarMutuallyExclusive { get; set => SetProperty(ref field, value); } = true;
        public int BackpackStyle { get; set => SetProperty(ref field, value); }
        public bool HighlightGameObjects { get; set => SetProperty(ref field, value); }
        public bool HighlightMobilesByParalize { get; set => SetProperty(ref field, value); } = true;
        public bool HighlightMobilesByPoisoned { get; set => SetProperty(ref field, value); } = true;
        public bool HighlightMobilesByInvul { get; set => SetProperty(ref field, value); } = true;
        public bool ShowMobilesHP { get; set => SetProperty(ref field, value); }
        public bool ShowTargetIndicator { get; set => SetProperty(ref field, value); }
        public bool AutoAvoidObstacules { get; set => SetProperty(ref field, value); } = true;
        public int MobileHPType { get; set => SetProperty(ref field, value); }     // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set => SetProperty(ref field, value); } // 0 = Always, 1 - <100%
        public bool DrawRoofs { get; set => SetProperty(ref field, value); } = true;
        public int MobileDepthSliceStep { get; set => SetProperty(ref field, value); } = 1;
        public bool ShowMobileHealthbar { get; set => SetProperty(ref field, value); } // Draws a small healthbar directly under each mobile
        public bool ShowMobileNameOverhead { get; set => SetProperty(ref field, value); } // Always draws the mobile's name/title above their head
        public bool TreeToStumps { get; set => SetProperty(ref field, value); }
        public bool EnableCaveBorder { get; set => SetProperty(ref field, value); }
        public bool HideVegetation { get; set => SetProperty(ref field, value); }
        public bool DisableGargoyleFlyingAnimation { get; set => SetProperty(ref field, value); }
        public int FieldsType { get; set => SetProperty(ref field, value); } // 0 = normal, 1 = static, 2 = tile
        public bool NoColorObjectsOutOfRange { get; set => SetProperty(ref field, value); }

        [Obsolete("Remove after 10/8/26")]
        public bool UseCircleOfTransparency { get; set => SetProperty(ref field, value); }
        [Obsolete("Remove after 10/8/26")]
        public int CircleOfTransparencyRadius { get; set => SetProperty(ref field, value); } = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2;
        [Obsolete("Remove after 10/8/26")]
        public int CircleOfTransparencyType { get; set => SetProperty(ref field, value); } // 0 = normal, 1 = like original client

        public int VendorGumpHeight { get; set => SetProperty(ref field, value); } = 350;   //original vendor gump size
        public float DefaultScale { get; set => SetProperty(ref field, value); } = 1.0f;
        public bool EnableMousewheelScaleZoom { get; set => SetProperty(ref field, value); } = true;
        public bool RestoreScaleAfterUnpressCtrl { get; set => SetProperty(ref field, value); }
        public bool BandageSelfOld { get; set => SetProperty(ref field, value); } = true;

        // Bandage Agent Settings
        public bool EnableBandageAgent { get; set => SetProperty(ref field, value); } = false;
        public int BandageAgentDelay { get; set => SetProperty(ref field, value); } = 3000;
        public bool BandageAgentCheckForBuff { get; set => SetProperty(ref field, value); } = false;
        public ushort BandageAgentGraphic { get; set => SetProperty(ref field, value); } = 0x0E21;
        public bool BandageAgentUseNewPacket { get; set => SetProperty(ref field, value); } = true;
        public bool BandageAgentCheckHidden { get; set => SetProperty(ref field, value); } = true;
        public bool BandageAgentCheckPoisoned { get; set => SetProperty(ref field, value); } = true;
        public int BandageAgentHPPercentage { get; set => SetProperty(ref field, value); } = 80;
        public bool BandageAgentCheckInvul { get; set => SetProperty(ref field, value); } = true;
        public bool BandageAgentBandageFriends { get; set => SetProperty(ref field, value); } = false;
        public bool BandageAgentBandageAllies { get; set => SetProperty(ref field, value); } = false;
        public bool BandageAgentBandagePets { get; set => SetProperty(ref field, value); } = false;
        public bool BandageAgentUseDexFormula { get; set => SetProperty(ref field, value); } = false;
        public bool BandageAgentDisableSelfHeal { get; set => SetProperty(ref field, value); } = false;
        public bool SelfHeal_Enabled { get; set => SetProperty(ref field, value); } = false;
        public bool SelfHeal_UseChivalry { get; set => SetProperty(ref field, value); } = false; // false = Magery (Heal/Cure), true = Chivalry (Close Wounds/Cleanse by Fire)
        public int SelfHeal_FC { get; set => SetProperty(ref field, value); } = 2;   // Faster Casting (used to auto-compute timings)
        public int SelfHeal_FCR { get; set => SetProperty(ref field, value); } = 6;  // Faster Cast Recovery (used to auto-compute timings)
        public int SelfHeal_Key { get; set => SetProperty(ref field, value); } = 0;   // (int)SDL.SDL_Keycode, 0 = unbound
        public int SelfHeal_Mod { get; set => SetProperty(ref field, value); } = 0;   // (int)SDL.SDL_Keymod
        public int SelfHeal_RecastDelayMs { get; set => SetProperty(ref field, value); } = 50;  // pad after a successful heal before the next cast
        public int SelfHeal_CastStartGraceMs { get; set => SetProperty(ref field, value); } = 800; // max wait for a cast to register / produce a cursor
        public int SelfHeal_CureVerifyMs { get; set => SetProperty(ref field, value); } = 600; // wait for poison to clear before recasting Cure
        public int SelfHeal_InterruptRetryMs { get; set => SetProperty(ref field, value); } = 100; // delay before recasting after an interrupted cast

        // RelativePaths of Legion scripts that have a hotkey assigned. The key binding itself lives in
        // the central hotkey system (hotkeys.json); this per-profile list records which scripts to
        // re-register on load. Entries whose script no longer exists are pruned on load.
        public List<string> ScriptHotkeys { get; set => SetProperty(ref field, value); } = new List<string>();

        public bool EnableDeathScreen { get; set => SetProperty(ref field, value); } = true;
        public bool ScreenshotOnDeath { get; set => SetProperty(ref field, value); } = true;
        public bool EnableBlackWhiteEffect { get; set => SetProperty(ref field, value); } = true;
        public ushort HiddenBodyHue { get; set => SetProperty(ref field, value); } = 0x038E;
        public byte HiddenBodyAlpha { get; set => SetProperty(ref field, value); } = 40;
        public int PlayerConstantAlpha { get; set => SetProperty(ref field, value); } = 100;

        // tooltip
        public bool UseTooltip { get; set => SetProperty(ref field, value); } = true;
        public ushort TooltipTextHue { get; set => SetProperty(ref field, value); } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set => SetProperty(ref field, value); } = 250;
        public int TooltipDisplayZoom { get; set => SetProperty(ref field, value); } = 100;
        public int TooltipBackgroundOpacity { get; set => SetProperty(ref field, value); } = 70;
        public byte TooltipFont { get; set => SetProperty(ref field, value); } = 1;

        // movements
        public bool EnablePathfind { get; set => SetProperty(ref field, value); } = true;
        public bool UseShiftToPathfind { get; set => SetProperty(ref field, value); }
        public bool PathfindSingleClick { get; set => SetProperty(ref field, value); }
        public bool AlwaysRun { get; set => SetProperty(ref field, value); } = true;
        public bool AlwaysRunUnlessHidden { get; set => SetProperty(ref field, value); } = true;
        public bool HoldDownKeyTab { get; set => SetProperty(ref field, value); }
        public bool HoldShiftForContext { get; set => SetProperty(ref field, value); } = false;
        public bool HoldShiftToSplitStack { get; set => SetProperty(ref field, value); } = false;

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set => SetProperty(ref field, value); } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set => SetProperty(ref field, value); } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set => SetProperty(ref field, value); } = new Point(10, 10);
        public bool GameWindowLock { get; set => SetProperty(ref field, value); }
        public bool GameWindowFullSize { get; set => SetProperty(ref field, value); }
        public bool WindowBorderless { get; set => SetProperty(ref field, value); } = false;
        public bool BorderlessWindow { get; set => SetProperty(ref field, value); } = false;
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set => SetProperty(ref field, value); } = new Point(800, 680);
        [JsonConverter(typeof(Point2Converter))] public Point TopbarGumpPosition { get; set => SetProperty(ref field, value); } = new Point(0, 0);
        public bool TopbarGumpIsMinimized { get; set => SetProperty(ref field, value); }
        public bool TopbarGumpIsDisabled { get; set => SetProperty(ref field, value); }
        public bool UseAlternativeLights { get; set => SetProperty(ref field, value); }
        public bool UseCustomLightLevel { get; set => SetProperty(ref field, value); }
        public byte LightLevel { get; set => SetProperty(ref field, value); }
        public int LightLevelType { get; set => SetProperty(ref field, value); } // 0 = absolute, 1 = minimum
        public bool UseColoredLights { get; set => SetProperty(ref field, value); } = true;
        public bool UseDarkNights { get; set => SetProperty(ref field, value); }
        public int CloseHealthBarType { get; set => SetProperty(ref field, value); } = 2; // 0 = none, 1 == not exists, 2 == is dead
        public bool ActivateChatAfterEnter { get; set => SetProperty(ref field, value); }
        public bool ActivateChatAdditionalButtons { get; set => SetProperty(ref field, value); } = true;
        public bool ActivateChatShiftEnterSupport { get; set => SetProperty(ref field, value); } = true;
        public bool UseObjectsFading { get; set => SetProperty(ref field, value); } = true;
        public bool HoldDownKeyAltToCloseAnchored { get; set => SetProperty(ref field, value); } = true;
        public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set => SetProperty(ref field, value); } = false;
        public bool HoldAltToMoveGumps { get; set => SetProperty(ref field, value); }
        public byte JournalOpacity { get; set => SetProperty(ref field, value); } = 50;
        public int JournalStyle { get; set => SetProperty(ref field, value); } = 0;
        public bool HideScreenshotStoredInMessage { get; set => SetProperty(ref field, value); }
        public bool UseModernPaperdoll { get; set => SetProperty(ref field, value); } = false;
        public bool OpenModernPaperdollAtMinimizeLoc { get; set => SetProperty(ref field, value); } = false;

        // Experimental
        public bool CastSpellsByOneClick { get; set => SetProperty(ref field, value); }
        public bool BuffBarTime { get; set => SetProperty(ref field, value); }
        public bool FastSpellsAssign { get; set => SetProperty(ref field, value); }
        public bool AutoOpenDoors { get; set => SetProperty(ref field, value); } = true;
        public bool SmoothDoors { get; set => SetProperty(ref field, value); } = true;
        public bool AutoOpenCorpses { get; set => SetProperty(ref field, value); } = true;
        public int AutoOpenCorpseRange { get; set => SetProperty(ref field, value); } = 2;
        public int CorpseOpenOptions { get; set => SetProperty(ref field, value); } = 3;
        public bool SkipEmptyCorpse { get; set => SetProperty(ref field, value); }
        public bool AutoOpenOwnCorpse { get; set => SetProperty(ref field, value); } = true;
        public bool DisableDefaultHotkeys { get; set => SetProperty(ref field, value); }
        public bool DisableArrowBtn { get; set => SetProperty(ref field, value); }
        public bool DisableTabBtn { get; set => SetProperty(ref field, value); }
        public bool DisableCtrlQWBtn { get; set => SetProperty(ref field, value); }
        public bool DisableAutoMove { get; set => SetProperty(ref field, value); }
        public bool EnableDragSelect { get; set => SetProperty(ref field, value); }
        public int DragSelectModifierKey { get; set => SetProperty(ref field, value); } // 0 = none, 1 = control, 2 = shift, 3 = alt
        public int DragSelect_PlayersModifier { get; set => SetProperty(ref field, value); } = 0;
        public int DragSelect_MonstersModifier { get; set => SetProperty(ref field, value); } = 0;
        public int DragSelect_NameplateModifier { get; set => SetProperty(ref field, value); } = 0;
        public bool OverrideContainerLocation { get; set => SetProperty(ref field, value); }

        public int OverrideContainerLocationSetting { get; set => SetProperty(ref field, value); } // 0 = container position, 1 = top right of screen, 2 = last dragged position, 3 = remember every container

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set => SetProperty(ref field, value); } = new Point(200, 200);
        public bool HueContainerGumps { get; set => SetProperty(ref field, value); } = true;
        public int DragSelectStartX { get; set => SetProperty(ref field, value); } = 100;
        public int DragSelectStartY { get; set => SetProperty(ref field, value); } = 100;
        public bool DragSelectAsAnchor { get; set => SetProperty(ref field, value); } = false;
        public string LastActiveNameOverheadOption { get; set => SetProperty(ref field, value); } = "All";
        public bool NameOverheadToggled { get; set => SetProperty(ref field, value); } = false;
        public bool ShowTargetRangeIndicator { get; set => SetProperty(ref field, value); }
        public bool PartyInviteGump { get; set => SetProperty(ref field, value); } = true;
        public bool CustomBarsToggled { get; set => SetProperty(ref field, value); }
        public bool CBBlackBGToggled { get; set => SetProperty(ref field, value); }
        public bool UsePartyHealthBars { get; set => SetProperty(ref field, value); } = true;
        public bool ShowHealCureButtonsAllHealthbars { get; set => SetProperty(ref field, value); }
        public bool ShowHealCureButtonsFriends { get; set => SetProperty(ref field, value); }

        public bool ShowInfoBar { get; set => SetProperty(ref field, value); }
        public int InfoBarHighlightType { get; set => SetProperty(ref field, value); } // 0 = text colour changes, 1 = underline

        public bool CounterBarEnabled { get; set => SetProperty(ref field, value); }
        public bool CounterBarHighlightOnUse { get; set => SetProperty(ref field, value); }
        public bool CounterBarHighlightOnAmount { get; set => SetProperty(ref field, value); }
        public bool CounterBarDisplayAbbreviatedAmount { get; set => SetProperty(ref field, value); }
        public int CounterBarAbbreviatedAmount { get; set => SetProperty(ref field, value); } = 1000;
        public int CounterBarHighlightAmount { get; set => SetProperty(ref field, value); } = 5;
        public int CounterBarCellSize { get; set => SetProperty(ref field, value); } = 40;

        // title bar stats
        public bool EnableTitleBarStats { get; set => SetProperty(ref field, value); } = false;
        public TitleBarStatsMode TitleBarStatsMode { get; set => SetProperty(ref field, value); } = TitleBarStatsMode.Text;
        public int CounterBarRows { get; set => SetProperty(ref field, value); } = 1;
        public int CounterBarColumns { get; set => SetProperty(ref field, value); } = 5;

        public bool ShowSkillsChangedMessage { get; set => SetProperty(ref field, value); } = true;
        public int ShowSkillsChangedDeltaValue { get; set => SetProperty(ref field, value); } = 1;
        public bool ShowStatsChangedMessage { get; set => SetProperty(ref field, value); } = true;


        public bool ShadowsEnabled { get; set => SetProperty(ref field, value); } = true;
        public bool ShadowsStatics { get; set => SetProperty(ref field, value); } = true;
        public int TerrainShadowsLevel { get; set => SetProperty(ref field, value); } = 15;
        public int AuraUnderFeetType { get; set => SetProperty(ref field, value); } // 0 = NO, 1 = in warmode, 2 = ctrl+shift, 3 = always
        public bool AuraOnMouse { get; set => SetProperty(ref field, value); } = true;
        public bool AnimatedWaterEffect { get; set => SetProperty(ref field, value); } = false;
        public bool EnableWeatherEffects { get; set => SetProperty(ref field, value); } = false;
        public bool EnableEnhancedWeather { get; set => SetProperty(ref field, value); } = false;

        public bool PartyAura { get; set => SetProperty(ref field, value); }

        public bool HideChatGradient { get; set => SetProperty(ref field, value); } = false;

        public bool StandardSkillsGump { get; set => SetProperty(ref field, value); } = true;

        public bool ShowNewMobileNameIncoming { get; set => SetProperty(ref field, value); } = true;
        public bool ShowNewCorpseNameIncoming { get; set => SetProperty(ref field, value); } = true;

        public uint GrabBagSerial { get; set => SetProperty(ref field, value); }

        public bool ReduceFPSWhenInactive { get; set => SetProperty(ref field, value); }

        public bool EnableVSync { get; set => SetProperty(ref field, value); } = true;

        public bool OverrideAllFonts { get; set => SetProperty(ref field, value); }
        public bool OverrideAllFontsIsUnicode { get; set => SetProperty(ref field, value); } = true;

        public bool SallosEasyGrab { get; set => SetProperty(ref field, value); }

        public bool JournalDarkMode { get; set => SetProperty(ref field, value); }

        public byte ContainersScale { get; set => SetProperty(ref field, value); } = 100;

        public byte ContainerOpacity { get; set => SetProperty(ref field, value); } = 50;

        public bool ScaleItemsInsideContainers { get; set => SetProperty(ref field, value); }

        public bool DoubleClickToLootInsideContainers { get; set => SetProperty(ref field, value); }

        public bool UseLargeContainerGumps { get; set => SetProperty(ref field, value); }

        public bool RelativeDragAndDropItems { get; set => SetProperty(ref field, value); }

        public bool HighlightContainerWhenSelected { get; set => SetProperty(ref field, value); }

        public bool UseNewTargetSystem { get; set => SetProperty(ref field, value); } = true;
        public bool UseKrEquipUnequipPacket { get; set => SetProperty(ref field, value); }
        public bool ShowHouseContent { get; set => SetProperty(ref field, value); }
        public bool SaveHealthbars { get; set => SetProperty(ref field, value); }
        public bool TextFading { get; set => SetProperty(ref field, value); } = true;

        public bool UseSmoothBoatMovement { get; set => SetProperty(ref field, value); }

        public bool IgnoreStaminaCheck { get; set => SetProperty(ref field, value); }

        public bool ShowJournalClient { get; set => SetProperty(ref field, value); } = true;
        public bool ShowJournalObjects { get; set => SetProperty(ref field, value); } = true;
        public bool ShowJournalSystem { get; set => SetProperty(ref field, value); } = true;
        public bool ShowJournalGuildAlly { get; set => SetProperty(ref field, value); } = true;

        public int WorldMapWidth { get; set => SetProperty(ref field, value); } = 400;
        public int WorldMapHeight { get; set => SetProperty(ref field, value); } = 400;
        public int WorldMapFont { get; set => SetProperty(ref field, value); } = 3;
        public string WorldMapTtfFont { get; set => SetProperty(ref field, value); } = string.Empty;
        public int WorldMapTtfFontSize { get; set => SetProperty(ref field, value); } = 20;
        public bool WorldMapFlipMap { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapTopMost { get; set => SetProperty(ref field, value); }
        public bool WorldMapFreeView { get; set => SetProperty(ref field, value); }
        public WorldMapDoubleClickAction WorldMapDoubleClickAction { get; set => SetProperty(ref field, value); } = WorldMapDoubleClickAction.ToggleLock;
        public bool WorldMapShowParty { get; set => SetProperty(ref field, value); } = true;
        public int WorldMapZoomIndex { get; set => SetProperty(ref field, value); } = 4;
        public bool WorldMapShowCoordinates { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowMouseCoordinates { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowCorpse { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowSextantCoordinates { get; set => SetProperty(ref field, value); } = false;
        public int WorldMapSextantBaseX { get; set => SetProperty(ref field, value); } = 1323;
        public int WorldMapSextantBaseY { get; set => SetProperty(ref field, value); } = 1624;
        public bool WorldMapShowMobiles { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowPlayerName { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowPlayerBar { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowGroupName { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowGroupBar { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowMarkers { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowMarkersNames { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapShowMultis { get; set => SetProperty(ref field, value); } = true;
        public string WorldMapHiddenMarkerFiles { get; set => SetProperty(ref field, value); } = string.Empty;
        public string WorldMapHiddenZoneFiles { get; set => SetProperty(ref field, value); } = string.Empty;
        public bool WorldMapShowGridIfZoomed { get; set => SetProperty(ref field, value); } = true;
        public bool WorldMapAllowPositionalTarget { get; set => SetProperty(ref field, value); } = true;

        public int WebMapServerPort { get; set; } = 8088;
        public bool WebMapAutoStart { get; set; }

        public int AutoFollowDistance { get; set => SetProperty(ref field, value); } = 1;
        public bool DisableAutoFollowAlt { get; set => SetProperty(ref field, value); } = false;
        [JsonConverter(typeof(Point2Converter))] public Point ResizeJournalSize { get; set => SetProperty(ref field, value); } = new(410, 350);
        [JsonConverter(typeof(NullablePoint2Converter))] public Point? OptionsWindowsSize { get; set => SetProperty(ref field, value); }
        public bool FollowingMode { get; set => SetProperty(ref field, value); } = false;
        public uint FollowingTarget { get; set => SetProperty(ref field, value); }
        public bool NamePlateHealthBar { get; set => SetProperty(ref field, value); } = true;
        public byte NamePlateOpacity { get; set => SetProperty(ref field, value); } = 75;
        public byte NamePlateHealthBarOpacity { get; set => SetProperty(ref field, value); } = 50;
        public bool NamePlateHideAtFullHealth { get; set => SetProperty(ref field, value); }
        public bool NamePlateHideAtFullHealthInWarmode { get; set => SetProperty(ref field, value); }
        public byte NamePlateBorderOpacity { get; set => SetProperty(ref field, value); } = 50;
        public bool NamePlateAvoidOverlap { get; set => SetProperty(ref field, value); }
        public bool NamePlateUseFixedWidth { get; set => SetProperty(ref field, value); }
        public int NamePlateFixedWidth { get; set => SetProperty(ref field, Math.Clamp(value, 60, 300)); } = 120;
        public bool NamePlateUseFixedHealthBarWidth { get; set => SetProperty(ref field, value); }
        public int NamePlateHealthBarFixedWidth { get; set => SetProperty(ref field, Math.Clamp(value, 60, 300)); } = 120;
        public bool NamePlateShowWordOfDeathIcon { get; set => SetProperty(ref field, value); }
        public int NamePlateHeight { get; set => SetProperty(ref field, Math.Clamp(value, 0, 80)); }
        public bool NamePlateSplitHealthBar { get; set => SetProperty(ref field, value); }
        public int NamePlateCornerRadius { get; set => SetProperty(ref field, Math.Clamp(value, 0, 40)); } = 0;
        public NamePlateHealthBarMode NamePlateHealthBarMode { get; set => SetProperty(ref field, value); } = NamePlateHealthBarMode.StatusColor;
        public NamePlateBackgroundMode NamePlateBackgroundMode { get; set => SetProperty(ref field, value); } = NamePlateBackgroundMode.FixedColor;
        public byte NamePlateBackgroundR { get; set => SetProperty(ref field, value); }
        public byte NamePlateBackgroundG { get; set => SetProperty(ref field, value); }
        public byte NamePlateBackgroundB { get; set => SetProperty(ref field, value); }
        public NamePlatePreset NamePlatePreset { get; set => SetProperty(ref field, value); } = NamePlatePreset.Custom;

        public bool LeftAlignToolTips { get; set => SetProperty(ref field, value); }
        public bool ForceCenterAlignTooltipMobiles { get; set => SetProperty(ref field, value); } = true;

        public bool CorpseSingleClickLoot { get; set => SetProperty(ref field, value); }

        public bool DisableSystemChat { get; set => SetProperty(ref field, value); }

        public bool DisableSystemChatWhileJournalOpen { get; set => SetProperty(ref field, value); }

        public bool UsePromptPopup { get; set => SetProperty(ref field, value); } = true;

        public uint SetFavoriteMoveBagSerial { get; set => SetProperty(ref field, value); }

        #region GRID CONTAINER

        public ContainerStyle ContainerStyle { get; set => SetProperty(ref field, value); }
        public CorpseContainerStyle CorpseContainerStyle { get; set => SetProperty(ref field, value); }

        public bool GridContainersDefaultToOldStyleView { get; set => SetProperty(ref field, value); } = false;
        public int GridContainerViewMode { get; set => SetProperty(ref field, value); } = 0; // 0 = Grid, 1 = List
        public int GridContainerSearchMode { get; set => SetProperty(ref field, value); } = 0;
        public bool EnableGridContainerAnchor { get; set => SetProperty(ref field, value); } = false;
        public byte GridBorderAlpha { get; set => SetProperty(ref field, value); } = 75;
        public ushort GridBorderHue { get; set => SetProperty(ref field, value); } = 0;
        public byte GridContainersScale { get; set => SetProperty(ref field, value); } = 100;
        public bool GridContainerScaleItems { get; set => SetProperty(ref field, value); } = true;
        public bool GridHighlightLowContrastItems { get; set => SetProperty(ref field, value); } = false;
        public int GridHighlightLowContrastItemsStyle { get; set => SetProperty(ref field, value); } = 0;
        /// <summary>
        /// Minimum contrast level used for low-contrast grid item highlighting.
        /// </summary>
        public byte GridHighlightLowContrastMinimum
        {
            get;
            set => SetProperty(ref field, Math.Clamp(value, (byte)1, (byte)10));
        } = 5;
        public bool GridEnableContPreview { get; set => SetProperty(ref field, value); } = true;
        public int Grid_BorderStyle { get; set => SetProperty(ref field, value); } = 0;
        public int Grid_DefaultColumns { get; set => SetProperty(ref field, value); } = 5;
        public int Grid_DefaultRows { get; set => SetProperty(ref field, value); } = 5;
        public bool Grid_UseContainerHue { get; set => SetProperty(ref field, value); } = false;
        public bool Grid_HideBorder { get; set => SetProperty(ref field, value); } = false;
        #endregion

        #region COOLDOWNS
        public int CoolDownX { get; set => SetProperty(ref field, value); } = 50;
        public int CoolDownY { get; set => SetProperty(ref field, value); } = 50;

        // The Condition_* lists and CoolDownConditionCount below are the legacy cooldown-bar storage.
        // They have been superseded by cooldownbars.json (see CooldownBarsConfig) and are retained only so
        // existing profiles can be migrated on load. Do not use them in new code.
        private const string CooldownMigratedMessage = "Migrated to cooldownbars.json (CooldownBarsConfig); retained only for one-time migration of existing profiles.";

        [Obsolete(CooldownMigratedMessage)]
        public List<ushort> Condition_Hue { get; set => SetProperty(ref field, value); } = new List<ushort>();
        [Obsolete(CooldownMigratedMessage)]
        public List<string> Condition_Label { get; set => SetProperty(ref field, value); } = new List<string>();
        [Obsolete(CooldownMigratedMessage)]
        public List<int> Condition_Duration { get; set => SetProperty(ref field, value); } = new List<int>();
        [Obsolete(CooldownMigratedMessage)]
        public List<string> Condition_Trigger { get; set => SetProperty(ref field, value); } = new List<string>();
        [Obsolete(CooldownMigratedMessage)]
        public List<int> Condition_Type { get; set => SetProperty(ref field, value); } = new List<int>();
        [Obsolete(CooldownMigratedMessage)]
        public List<bool> Condition_ReplaceIfExists { get; set => SetProperty(ref field, value); } = new List<bool>();
        [Obsolete(CooldownMigratedMessage)]
        public List<bool> Condition_SkipIfExists { get; set => SetProperty(ref field, value); } = new List<bool>();
        [Obsolete(CooldownMigratedMessage)]
        public int CoolDownConditionCount
        {
#pragma warning disable CS0618
            get
            {
                return Condition_Hue.Count;
            }
#pragma warning restore CS0618
            set { }
        }
        #endregion

        #region IMPROVED BUFF BAR
        public bool UseImprovedBuffBar { get; set => SetProperty(ref field, value); } = true;
        public ushort ImprovedBuffBarHue { get; set => SetProperty(ref field, value); } = 905;
        #endregion

        #region DAMAGE NUMBER HUES
        public ushort DamageHueSelf { get; set => SetProperty(ref field, value); } = 0x0034;
        public ushort DamageHuePet { get; set => SetProperty(ref field, value); } = 0x0033;
        public ushort DamageHueAlly { get; set => SetProperty(ref field, value); } = 0x0030;
        public ushort DamageHueLastAttck { get; set => SetProperty(ref field, value); } = 0x1F;
        public ushort DamageHueOther { get; set => SetProperty(ref field, value); } = 0x0021;

        public bool ShowDPS { get; set => SetProperty(ref field, value); } = true;
        #endregion

        #region GridHighlightingProps
        public List<string> GridHighlight_Name { get; set => SetProperty(ref field, value); } = new List<string>();
        public List<ushort> GridHighlight_Hue { get; set => SetProperty(ref field, value); } = new List<ushort>();
        public List<List<string>> GridHighlight_PropNames { get; set => SetProperty(ref field, value); } = new List<List<string>>();
        public List<List<int>> GridHighlight_PropMinVal { get; set => SetProperty(ref field, value); } = new List<List<int>>();
        public bool GridHighlight_CorpseOnly { get; set => SetProperty(ref field, value); } = false;
        public int GridHighlightSize { get; set => SetProperty(ref field, value); } = 1;
        public bool GridHighlightProperties { get; set => SetProperty(ref field, value); } = true;
        public bool GridHighlightShowRuleName { get; set => SetProperty(ref field, value); } = true;
        public List<bool> GridHighlight_AcceptExtraProperties { get; set => SetProperty(ref field, value); } = new List<bool>();
        public List<List<bool>> GridHighlight_IsOptionalProperties { get; set => SetProperty(ref field, value); } = new List<List<bool>>();
        public List<List<string>> GridHighlight_ExcludeNegatives { get; set => SetProperty(ref field, value); } = new List<List<string>>();
        public List<List<string>> GridHighlight_RequiredRarities { get; set => SetProperty(ref field, value); } = new();

        // GridHighlightSetup has been superseded by grid_highlights.json (see GridHighlightsConfig) and is
        // retained only so existing profiles can be migrated on load. Do not use it in new code.
        [Obsolete("Migrated to grid_highlights.json (GridHighlightsConfig); retained only for one-time migration of existing profiles.")]
        public List<GridHighlightSetupEntry> GridHighlightSetup { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableProperties { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableResistances { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableNegatives { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableSuperSlayers { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableSlayers { get; set => SetProperty(ref field, value); } = new();
        public List<string> ConfigurableRarities { get; set => SetProperty(ref field, value); } = new();

        #endregion

        #region Modern paperdoll
        public ushort ModernPaperDollHue { get; set => SetProperty(ref field, value); } = 0;
        public ushort ModernPaperDollDurabilityHue { get; set => SetProperty(ref field, value); } = 32;
        public int ModernPaperDoll_DurabilityPercent { get; set => SetProperty(ref field, value); } = 90;
        [JsonConverter(typeof(Point2Converter))] public Point ModernPaperdollPosition { get; set => SetProperty(ref field, value); } = new Point(100, 100);
        #endregion

        #region Health indicator
        public float ShowHealthIndicatorBelow { get; set => SetProperty(ref field, value); } = 0.9f;
        public bool EnableHealthIndicator { get; set => SetProperty(ref field, value); } = true;
        public int HealthIndicatorWidth { get; set => SetProperty(ref field, value); } = 10;
        #endregion

        public ushort MainWindowBackgroundHue { get; set => SetProperty(ref field, value); } = 1;

        public int MoveMultiObjectDelay { get; set => SetProperty(ref field, value); } = 1000;

        public bool SpellIcon_DisplayHotkey { get; set => SetProperty(ref field, value); } = true;
        public ushort SpellIcon_HotkeyHue { get; set => SetProperty(ref field, value); } = 1;

        public int SpellIconScale { get; set => SetProperty(ref field, value); } = 100;

        public bool EnableAlphaScrollingOnGumps { get; set => SetProperty(ref field, value); } = true;

        [JsonConverter(typeof(Point2Converter))] public Point WorldMapPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point PaperdollPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point JournalPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point StatusGumpPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridPosition { get; set => SetProperty(ref field, value); } = new(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridSize { get; set => SetProperty(ref field, value); } = new(300, 300);
        public bool WorldMapLocked { get; set => SetProperty(ref field, value); } = false;
        public bool PaperdollLocked { get; set => SetProperty(ref field, value); } = false;
        public bool JournalLocked { get; set => SetProperty(ref field, value); } = false;
        public bool StatusGumpLocked { get; set => SetProperty(ref field, value); } = false;
        public bool BackPackLocked { get; set => SetProperty(ref field, value); } = false;

        public bool DisplayPartyChatOverhead { get; set => SetProperty(ref field, value); } = true;

        public bool HideMacroTargetMessage { get; set => SetProperty(ref field, value); } = false;

        public string SelectedTTFJournalFont { get; set => SetProperty(ref field, value); } = "avadonian";
        public int SelectedJournalFontSize { get; set => SetProperty(ref field, value); } = 20;

        public string SelectedToolTipFont { get; set => SetProperty(ref field, value); } = "Roboto-Regular";
        public int SelectedToolTipFontSize { get; set => SetProperty(ref field, value); } = 20;

        public string GameWindowSideChatFont { get; set => SetProperty(ref field, value); } = "avadonian";
        public int GameWindowSideChatFontSize { get; set => SetProperty(ref field, value); } = 20;

        public string OverheadChatFont { get; set => SetProperty(ref field, value); } = "avadonian";
        public int OverheadChatFontSize { get; set => SetProperty(ref field, value); } = 20;
        public int OverheadChatWidth { get; set => SetProperty(ref field, value); } = 400;

        public string NamePlateFont { get; set => SetProperty(ref field, value); } = "avadonian";
        public int NamePlateFontSize { get; set => SetProperty(ref field, value); } = 20;

        public bool UseNewOptionsWindow { get; set => SetProperty(ref field, value); } = true;
        public string OptionsFont
        {
            get; set
            {
                SetProperty(ref field, value);
                MyraStyle.SetDefault();
            }
        } = "Roboto-Regular";
        public int OptionsFontSize { get; set => SetProperty(ref field, value); } = 18;

        public int TextBorderSize { get; set => SetProperty(ref field, value); } = 1;
        public uint SavedMountSerial { get; set => SetProperty(ref field, value); } = 0;

        public uint SavedMainHandSerial { get; set => SetProperty(ref field, value); } = 0;
        public uint SavedOffHandSerial { get; set => SetProperty(ref field, value); } = 0;

        public bool UseModernShopGump { get; set => SetProperty(ref field, value); } = false;

        public int MaxJournalEntries { get; set => SetProperty(ref field, value); } = 250;
        public int MaxSoundEntries { get; set => SetProperty(ref field, value); } = 250;
        public bool HideJournalBorder { get; set => SetProperty(ref field, value); } = false;
        public bool JournalTransparencyWhenInactive { get; set => SetProperty(ref field, value); } = false;
        public bool HideJournalTimestamp { get; set => SetProperty(ref field, value); } = false;
        public bool HideJournalSystemPrefix { get; set => SetProperty(ref field, value); } = false;

        public int HealthLineSizeMultiplier { get; set => SetProperty(ref field, value); } = 1;

        public bool OpenHealthBarForLastAttack { get; set => SetProperty(ref field, value); } = true;
        [JsonConverter(typeof(Point2Converter))]
        public Point LastTargetHealthBarPos { get; set => SetProperty(ref field, value); } = Point.Zero;
        public ushort ToolTipBGHue { get; set => SetProperty(ref field, value); } = 0;

        public string LastVersionHistoryShown { get; set => SetProperty(ref field, value); }

        public int AdvancedSkillsGumpHeight { get; set => SetProperty(ref field, value); } = 510;

        #region ToolTip Overrides
        // The ToolTipOverride_* parallel lists below are the legacy tooltip-override storage. They have been
        // superseded by tooltip_overrides.json (see TooltipOverridesConfig) and are retained only so existing
        // profiles can be migrated on load. Do not use them in new code. The defaults are kept so a fresh
        // profile still migrates the standard resist/damage overrides into the new file.
        private const string TooltipOverrideMigratedMessage = "Migrated to tooltip_overrides.json (TooltipOverridesConfig); retained only for one-time migration of existing profiles.";

        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<string> ToolTipOverride_SearchText { get; set => SetProperty(ref field, value); } = new List<string>() { "Physical Res", "Fire Resist", "Cold Resist", "Poison Resist", "Energy Resist", "Weapon Damage" };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<string> ToolTipOverride_NewFormat { get; set => SetProperty(ref field, value); } = new List<string>() { "/c[#8c733e]Physical Resist {1}%", "/c[red]Fire Resist {1}%", "/c[teal]Cold Resist {1}%", "/c[green]Poison Resist {1}%", "/c[purple]Energy Resist {1}%", "{0} /c[orange]{1}{4} /cd- /c[red]{2}{5}" };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<int> ToolTipOverride_MinVal1 { get; set => SetProperty(ref field, value); } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<int> ToolTipOverride_MinVal2 { get; set => SetProperty(ref field, value); } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<int> ToolTipOverride_MaxVal1 { get; set => SetProperty(ref field, value); } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<int> ToolTipOverride_MaxVal2 { get; set => SetProperty(ref field, value); } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<byte> ToolTipOverride_Layer { get; set => SetProperty(ref field, value); } = new List<byte>() { (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any };
        /// <summary>Optional per-override border hue drawn around the tooltip when the rule matches; -1 means no override.</summary>
        [Obsolete(TooltipOverrideMigratedMessage)]
        public List<int> ToolTipOverride_BorderHue { get; set => SetProperty(ref field, value); } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        /// <summary>When enabled, tooltip overrides are not applied to mobile tooltips.</summary>
        public bool ToolTipOverride_IgnoreMobiles { get; set => SetProperty(ref field, value); } = true;
        #endregion

        public string TooltipHeaderFormat { get; set => SetProperty(ref field, value); } = "/c[yellow]{0}";
        public bool DisplaySkillBarOnChange { get; set => SetProperty(ref field, value); } = true;
        public string SkillBarFormat { get; set => SetProperty(ref field, value); } = "{0}: {1} / {2}";
        public bool DisplayRadius { get; set => SetProperty(ref field, value); } = false;
        public int DisplayRadiusDistance { get; set => SetProperty(ref field, value); } = 10;
        public ushort DisplayRadiusHue { get; set => SetProperty(ref field, value); } = 22;
        public bool EnableSpellIndicators { get; set => SetProperty(ref field, value); } = true;
        public bool EnableAutoLoot { get; set => SetProperty(ref field, value); } = false;
        public bool AutoLootHumanCorpses { get; set => SetProperty(ref field, value); } = false;
        public bool EnableAutoSkinning { get; set => SetProperty(ref field, value); } = false;
        public bool AutoSkinningHumanCorpses { get; set => SetProperty(ref field, value); } = false;
        public string AutoSkinningKnifeGraphics { get; set => SetProperty(ref field, value); } = "0x2D2C;0x0F52;0x0EC4;0x0EC3;0x13F6;0x13B6";
        public bool ItemDatabaseEnabled { get; set => SetProperty(ref field, value); } = true;
        public static uint GumpsVersion { get; private set; }

        [JsonConverter(typeof(Point2Converter))]
        public Point InfoBarSize { get; set => SetProperty(ref field, value); } = new Point(400, 20);
        public bool InfoBarLocked { get; set => SetProperty(ref field, value); } = false;
        public string InfoBarFont { get; set => SetProperty(ref field, value); } = "Roboto-Regular";
        public int InfoBarFontSize { get; set => SetProperty(ref field, value); } = 18;
        public int LastJournalTab { get; set => SetProperty(ref field, value); } = 0;
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

        public bool UseLastMovedCooldownPosition { get; set => SetProperty(ref field, value); } = true;
        public bool CloseHealthBarIfAnchored { get; set => SetProperty(ref field, value); } = false;
        [JsonConverter(typeof(Point2Converter))]
        public Point SkillProgressBarPosition { get; set => SetProperty(ref field, value); } = Point.Zero;
        public bool ForceResyncOnHang { get; set => SetProperty(ref field, value); } = false;
        public bool UseOneHPBarForLastAttack { get; set => SetProperty(ref field, value); } = true;
        public bool DisableMouseInteractionOverheadText { get; set => SetProperty(ref field, value); } = false;
        public bool HiddenLayersEnabled { get; set => SetProperty(ref field, value); } = false;
        public List<int> HiddenLayers { get; set => SetProperty(ref field, value); } = new List<int>();
        public bool HideLayersForSelf { get; set => SetProperty(ref field, value); } = true;

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
        public double PaperdollScale { get; set => SetProperty(ref field, value); } = 1f;
        public bool BuyAgentSubContainers { get; set => SetProperty(ref field, value); } = true;
        public bool DisableTargetingGridContainers { get; set => SetProperty(ref field, value); }
        public bool ControllerEnabled { get; set => SetProperty(ref field, value); } = true;
        public bool EnableScavenger { get; set => SetProperty(ref field, value); } = true;
        public bool CounterGumpLocked { get; set => SetProperty(ref field, value); }
        public bool NearbyLootConcealsContainerOnOpen { get; set => SetProperty(ref field, value); } = true;
        public bool SpellBar_ShowHotkeys { get; set => SetProperty(ref field, value); } = true;
        public byte ForcedHouseTransparency { get; set => SetProperty(ref field, value); } = 40;
        public ushort ForcedTransparencyHouseTileHue { get; set => SetProperty(ref field, value); } = 0;
        public bool ForceHouseTransparency { get; set => SetProperty(ref field, value); }
        public ulong HideHudGumpFlags { get; set => SetProperty(ref field, value); }
        public bool DisableGrayEnemies { get; set => SetProperty(ref field, value); }
        public bool EnablePostProcessingEffects { get; set => SetProperty(ref field, value); }
        public ushort PostProcessingType { get; set => SetProperty(ref field, value); }
        /// <summary>
        ///     Persistently disable hotkey usage.
        ///     To merely temporarily disable hotkeys, use <see cref="Game.Managers.Hotkeys.HotKeys.RequestDisableHotkeys" />.
        /// </summary>
        public bool PersistentDisableHotkeys { get; set => SetProperty(ref field, value); }
        public bool DisableDismountInWarMode { get; set => SetProperty(ref field, value); } = true;
        public bool EnableASyncMapLoading { get; set => SetProperty(ref field, value); } = true;
        public bool UseGridLayoutContainerGumps { get; set => SetProperty(ref field, value); } = true;
        public int GridLootType { get; set => SetProperty(ref field, value); } // 0 = none, 1 = only grid, 2 = both
        public bool ModernPaperdollAnchorEnabled { get; set => SetProperty(ref field, value); }
        public bool JournalAnchorEnabled { get; set => SetProperty(ref field, value); } = false;
        public bool EnableAutoLootProgressBar { get; set => SetProperty(ref field, value); } = true;
        public bool UseWASDInsteadArrowKeys { get; set => SetProperty(ref field, value); }
        public int NearbyLootGumpHeight { get; set => SetProperty(ref field, value); } = 550;
        public bool ForceTooltipsOnOldClients { get; set => SetProperty(ref field, value); } = true;
        public bool NearbyLootOpensHumanCorpses { get; set => SetProperty(ref field, value); }
        public ushort TurnDelay { get; set => SetProperty(ref field, value); } = 100;
        public bool SellAgentEnabled { get; set => SetProperty(ref field, value); }
        public int SellAgentMaxUniques { get; set => SetProperty(ref field, value); } = 50;
        public int SellAgentMaxItems { get; set => SetProperty(ref field, value); } = 0;
        public bool BuyAgentEnabled { get; set => SetProperty(ref field, value); }
        public int BuyAgentMaxUniques { get; set => SetProperty(ref field, value); } = 50;
        public int BuyAgentMaxItems { get; set => SetProperty(ref field, value); } = 0;
        public uint SOSGumpID { get; set => SetProperty(ref field, value); } = 1915258020;
        public double StatusGumpScale { get; set => SetProperty(ref field, value); } = 1f;
        public double SkillsGumpScale { get; set => SetProperty(ref field, value); } = 1f;
        public double ContextMenuScale { get; set => SetProperty(ref field, value); } = 1f;
        public double TradeGumpScale { get; set => SetProperty(ref field, value); } = 1f;
        public double ServerGumpScale { get; set => SetProperty(ref field, value); } = 1f;
        [JsonConverter(typeof(Point2Converter))] public Point ScriptManagerWindowPosition { get; set; } = Point.Zero;
        [JsonConverter(typeof(Point2Converter))] public Point ScriptManagerWindowSize { get; set; } = Point.Zero;
        public bool CandleFlickerLights { get; set; } = true;
        public string LastLoaded { get; set; }
        public bool TreeToStumpsWithinRadius { get; set; }
        public bool AutoSaveGumpPositions { get; set; }
        public bool StripChatUsernameId { get; set; }
        public bool OverheadsScaleWithZoom { get; set; } = true;
        public string VotedPolls { get; set; }
        public string BandageAgentJournalMessages { get; set; } = "You apply the bandages;You finish applying;You heal what little;You have been cured;You failed to cure;Your fingers slip";
        public bool BandageAgentUseJournalTrigger { get; set; }
        public bool CounterBarDisableIconScaling { get; set; }
        public bool CounterBarDisableItemScaling { get; set; }
        public bool CounterBarShowHotkeys { get; set; }
        public bool QueueManualItemMoves { get; set; }
        public bool AutoOpenDoorsIfHidden { get; set; } = true;
        public bool QueueManualItemUses { get; set; }
        public bool HueCorpseAfterAutoloot { get; set; }
        public int AutoLootRetryDelay { get; set; } = 5000;
        public int PathfindingZLevelDiff { get; set; } = 10;
        public int PathfindingMaxNodes { get; set; } = 150000;
        public int PathfindingMultiBuffer { get; set; } = 4;
        public int WorldMapPathfindingMaxNodes { get; set; } = 1000000;
        public int WorldMapPathfindingMaxRetries { get; set; } = 3;
        public int WorldMapPathfindingTimeout { get; set; } = 5000;
        public bool SingleClickMobileSetsLastTarget { get; set; } = true;
        public bool OutlineMobilesNotoriety { get; set; }
        public uint DisabledOverheadMessageTypes { get; set; }
        public bool DisableAutolootCorpseRetry { get; set; } = false;
        public bool DisableWeather { get; set; }
        public bool EnablePetScaling { get; set; }
        public bool AutoUnequipForActions { get; set; }
        public int MinGumpMoveDistance { get; set; } = 5;
        public int QuickHealSpell { get; set; } = 29;
        public int QuickCureSpell { get; set; } = 11;


        private long lastSave;

        internal void AfterLoad()
        {
            if (Client.Settings == null)
            {
                Log.Error("Warning, SQL settings failed to load!");
                return;
            }

            Task<Dictionary<string, string>> globalTask = Client.Settings.GetAllAsync(SettingsScope.Global);
            Task<Dictionary<string, string>> accountTask = Client.Settings.GetAllAsync(SettingsScope.Account);
            Task<Dictionary<string, string>> serverTask = Client.Settings.GetAllAsync(SettingsScope.Server);
            Task<Dictionary<string, string>> charTask = Client.Settings.GetAllAsync(SettingsScope.Char);

            if (Task.WhenAll(globalTask, accountTask, serverTask, charTask).Wait(10000))
            {
                LoadGeneratedGlobalSqlSettings(globalTask.Result);
                LoadGeneratedAccountSqlSettings(accountTask.Result);
                LoadGeneratedServerSqlSettings(serverTask.Result);
                LoadGeneratedCharSqlSettings(charTask.Result);

                HandleMigration();
            }
            else
            {
                Log.Error("SQL settings failed to load within the timeout; using defaults.");
            }

            MyraStyle.SetDefault(); //Also loaded here in case profile settings affect styling

            LastLoaded = DateTime.Now.ToUniversalTime().ToString();
        }

        private void HandleMigration()
        {
            if (ProfileMigrationVersion < 4) //3
            {
                MigrateToolTipOverrides();

                ProfileMigrationVersion = 4;
            }

            if (ProfileMigrationVersion < 5) //4
            {
                ProfileMigrationVersion++;
            }

            if (ProfileMigrationVersion < 6)
            {
                CounterBarShowHotkeys = OldCounterBarShowHotkeys;
                CounterBarDisableItemScaling = OldCounterBarDisableItemScaling;
                CounterBarDisableIconScaling = OldCounterBarDisableIconScaling;
                BandageAgentUseJournalTrigger = OldBandageAgentUseJournalTrigger;
                BandageAgentJournalMessages = OldBandageAgentJournalMessages;
                VotedPolls = OldVotedPolls;
                OverheadsScaleWithZoom = OldOverheadsScaleWithZoom;
                StripChatUsernameId = OldStripChatUsernameId;
                AutoSaveGumpPositions = OldAutoSaveGumpPositions;
                TreeToStumpsWithinRadius = OldTreeToStumpsWithinRadius;
                CandleFlickerLights = OldCandleFlickerLights;

                if (OldScriptManagerWindowSize.HasValue)
                    ScriptManagerWindowSize = OldScriptManagerWindowSize.Value;

                if (OldScriptManagerWindowPosition.HasValue)
                    ScriptManagerWindowPosition = OldScriptManagerWindowPosition.Value;

                QueueManualItemMoves = OldQueueManualItemMoves;
                AutoOpenDoorsIfHidden = OldAutoOpenDoorsIfHidden;
                QueueManualItemUses = OldQueueManualItemUses;
                HueCorpseAfterAutoloot = OldHueCorpseAfterAutoloot;
                AutoLootRetryDelay = OldAutoLootRetryDelay;
                PathfindingZLevelDiff = OldPathfindingZLevelDiff;
                PathfindingMaxNodes = OldPathfindingMaxNodes;
                PathfindingMultiBuffer = OldPathfindingMultiBuffer;
                WorldMapPathfindingMaxNodes = OldWorldMapPathfindingMaxNodes;
                WorldMapPathfindingMaxRetries = OldWorldMapPathfindingMaxRetries;
                WorldMapPathfindingTimeout = OldWorldMapPathfindingTimeout;
                SingleClickMobileSetsLastTarget = OldSingleClickMobileSetsLastTarget;
                OutlineMobilesNotoriety = OldOutlineMobilesNotoriety;
                DisabledOverheadMessageTypes = OldDisabledOverheadMessageTypes;
                DisableAutolootCorpseRetry = OldDisableAutolootCorpseRetry;
                DisableWeather = OldDisableWeather;
                EnablePetScaling = OldEnablePetScaling;
                AutoUnequipForActions = OldAutoUnequipForActions;
                MinGumpMoveDistance = OldMinGumpMoveDistance;
                QuickHealSpell = OldQuickHealSpell;
                QuickCureSpell = OldQuickCureSpell;
                WebMapServerPort = OldWebMapServerPort;
                WebMapAutoStart = OldWebMapAutoStart;

                ProfileMigrationVersion++;
            }

            if (ProfileMigrationVersion < 7)
            {
                ProfileManager.GlobalSettings.UseCircleOfTransparency = UseCircleOfTransparency;
                ProfileManager.GlobalSettings.CircleOfTransparencyRadius = CircleOfTransparencyRadius;
                ProfileManager.GlobalSettings.CircleOfTransparencyType = CircleOfTransparencyType;
                
                ProfileMigrationVersion++;
            }

            if (ProfileMigrationVersion < 8)
            {
                ProfileManager.GlobalSettings.EnableSound = EnableSound;
                ProfileManager.GlobalSettings.SoundVolume = SoundVolume;
                ProfileManager.GlobalSettings.EnableMusic = EnableMusic;
                ProfileManager.GlobalSettings.MusicVolume = MusicVolume;
                ProfileManager.GlobalSettings.EnableFootstepsSound = EnableFootstepsSound;
                ProfileManager.GlobalSettings.EnableRainSound = EnableRainSound;
                ProfileManager.GlobalSettings.EnableCombatMusic = EnableCombatMusic;
                ProfileManager.GlobalSettings.ReproduceSoundsInBackground = ReproduceSoundsInBackground;

                ProfileMigrationVersion++;
            }

            try //Cleanup old backups from previous save system
            {
                string dir = JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Char);
                foreach (string f in Directory.EnumerateFiles(dir, "*.backup*"))
                {
                    if (!f.Contains("grid_container"))
                        File.Delete(f);
                }

                dir = JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Char);
                foreach (string f in Directory.EnumerateFiles(dir, "*.bak*"))
                {
                    if (!f.Contains("grid_container"))
                        File.Delete(f);
                }

                dir = JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Server);
                foreach (string f in Directory.EnumerateFiles(dir, "*.backup*"))
                {
                    if (!f.Contains("grid_container"))
                        File.Delete(f);
                }
            }
            catch
            {}
        }

        /// <summary>
        /// Moves the legacy parallel <c>ToolTipOverride_*</c> lists into the dedicated
        /// tooltip_overrides.json (see <see cref="TooltipOverridesConfig"/>) and clears them. The config
        /// list is rebuilt (not appended) so re-running the migration - e.g. if the profile isn't saved
        /// before the next launch - stays idempotent.
        /// </summary>
#pragma warning disable CS0618 // Reading the obsolete legacy lists is the whole point of migration.
        private void MigrateToolTipOverrides()
        {
            int count = ToolTipOverride_SearchText.Count;
            if (count == 0)
                return;

            var overrides = new List<ToolTipOverrideData>(count);

            for (int i = 0; i < count; i++)
            {
                overrides.Add(new ToolTipOverrideData(
                    i,
                    ToolTipOverride_SearchText[i],
                    ToolTipOverride_NewFormat.ElementAtOrDefault(i) ?? string.Empty,
                    i < ToolTipOverride_MinVal1.Count ? ToolTipOverride_MinVal1[i] : -1,
                    i < ToolTipOverride_MaxVal1.Count ? ToolTipOverride_MaxVal1[i] : 100,
                    i < ToolTipOverride_MinVal2.Count ? ToolTipOverride_MinVal2[i] : -1,
                    i < ToolTipOverride_MaxVal2.Count ? ToolTipOverride_MaxVal2[i] : 100,
                    i < ToolTipOverride_Layer.Count ? ToolTipOverride_Layer[i] : (byte)TooltipLayers.Any,
                    i < ToolTipOverride_BorderHue.Count ? ToolTipOverride_BorderHue[i] : -1));
            }

            TooltipOverridesConfig config = TooltipOverridesConfig.Current;
            config.Overrides = overrides;
            config.Save();

            // Clear the legacy lists so tooltip overrides live only in tooltip_overrides.json going forward.
            ToolTipOverride_SearchText.Clear();
            ToolTipOverride_NewFormat.Clear();
            ToolTipOverride_MinVal1.Clear();
            ToolTipOverride_MinVal2.Clear();
            ToolTipOverride_MaxVal1.Clear();
            ToolTipOverride_MaxVal2.Clear();
            ToolTipOverride_Layer.Clear();
            ToolTipOverride_BorderHue.Clear();
        }
#pragma warning restore CS0618

        internal void Save(World world, string path, bool saveGumps = true)
        {
            if (Time.Ticks - lastSave < 10) //Don't save if saved in the last 10 ms, prevent duplcate saving when exiting game with options menu open
                return;

            Log.Trace($"Saving path:\t\t{path}");

            // Atomic write with rotating backups, handled by JsonSave.
            SaveTo(Path.Combine(path, "profile.json"));

            // Grid highlights live in a separate grid_highlights.json (see GridHighlightsConfig); persist
            // them alongside the profile so in-place rule edits are saved on the same cadence as before.
            if (ReferenceEquals(this, ProfileManager.CurrentProfile))
                GridHighlightsConfig.Current.Save();

            // Save opened gumps
            if (saveGumps)
                SaveGumps(world, path);

            Log.Trace("Saving done!");
            lastSave = Time.Ticks;
        }

        public void SaveAsFile(string path, string filename) => SaveTo(Path.Combine(path, filename));

        public void SaveAs(string path, string filename = "default.json") => SaveTo(Path.Combine(path, filename));

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

            // Seed the in-memory gump position cache from the permanent (database-backed) positions so
            // pinned gumps reopen at their saved location. Seeded before the XML gumps are restored below.
            UIManager.LoadPersistentPositions();

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
                case "ClassicUO.Game.UI.MyraWindows.PollsWindow":
                    var polls = new PollsWindow();
                    polls.Load(xml);
                    UIManager.Add(polls);
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
