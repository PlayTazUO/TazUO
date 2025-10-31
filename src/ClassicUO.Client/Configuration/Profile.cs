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
using System.Xml;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Game.UI.ImGuiControls;

namespace ClassicUO.Configuration
{
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



    public sealed class Profile
    {
        [JsonIgnore] public string Username { get; set; }
        [JsonIgnore] public string ServerName { get; set; }
        [JsonIgnore] public string CharacterName { get; set; }

        // sounds
        public bool EnableSound { get; set; } = true;
        public int SoundVolume { get; set; } = 70;
        public bool EnableMusic { get; set; } = true;
        public int MusicVolume { get; set; } = 70;
        public bool EnableFootstepsSound { get; set; } = true;
        public bool EnableCombatMusic { get; set; } = true;
        public bool ReproduceSoundsInBackground { get; set; }

        // fonts and speech
        public byte ChatFont { get; set; } = 1;
        public int SpeechDelay { get; set; } = 100;
        public bool ScaleSpeechDelay { get; set; } = true;
        public bool SaveJournalToFile { get; set; } = false;
        public bool ForceUnicodeJournal { get; set; }
        public bool IgnoreAllianceMessages { get; set; }
        public bool IgnoreGuildMessages { get; set; }

        // hues
        public ushort SpeechHue { get; set; } = 0x02B2;
        public ushort WhisperHue { get; set; } = 0x0033;
        public ushort EmoteHue { get; set; } = 0x0021;
        public ushort YellHue { get; set; } = 0x0021;
        public ushort PartyMessageHue { get; set; } = 0x0044;
        public ushort GuildMessageHue { get; set; } = 0x0044;
        public ushort AllyMessageHue { get; set; } = 0x0057;
        public ushort ChatMessageHue { get; set; } = 0x0256;
        public ushort InnocentHue { get; set; } = 0x005A;
        public ushort PartyAuraHue { get; set; } = 0x0044;
        public ushort FriendHue { get; set; } = 0x0044;
        public ushort CriminalHue { get; set; } = 0x03B2;
        public ushort CanAttackHue { get; set; } = 0x03B2;
        public ushort EnemyHue { get; set; } = 0x0031;
        public ushort MurdererHue { get; set; } = 0x0023;
        public ushort BeneficHue { get; set; } = 0x0059;
        public ushort HarmfulHue { get; set; } = 0x0020;
        public ushort NeutralHue { get; set; } = 0x03B1;
        public bool EnabledSpellHue { get; set; }
        public bool EnabledSpellFormat { get; set; }
        public string SpellDisplayFormat { get; set; } = "{power} [{spell}]";
        public ushort PoisonHue { get; set; } = 0x0044;
        public ushort ParalyzedHue { get; set; } = 0x014C;
        public ushort InvulnerableHue { get; set; } = 0x0030;
        public ushort AltJournalBackgroundHue { get; set; } = 0x0000;
        public ushort AltGridContainerBackgroundHue { get; set; } = 0x0000;
        public bool OverridePartyAndGuildHue { get; set; } = false;

        // visual
        public bool EnabledCriminalActionQuery { get; set; } = true;
        public bool EnabledBeneficialCriminalActionQuery { get; set; } = false;
        public bool UseOldStatusGump { get; set; }
        public bool StatusGumpBarMutuallyExclusive { get; set; } = true;
        public int BackpackStyle { get; set; }
        public bool HighlightGameObjects { get; set; }
        public bool HighlightMobilesByParalize { get; set; } = true;
        public bool HighlightMobilesByPoisoned { get; set; } = true;
        public bool HighlightMobilesByInvul { get; set; } = true;
        public bool ShowMobilesHP { get; set; }
        public bool ShowTargetIndicator { get; set; }
        public bool AutoAvoidObstacules { get; set; } = true;
        public int MobileHPType { get; set; }     // 0 = %, 1 = line, 2 = both
        public int MobileHPShowWhen { get; set; } // 0 = Always, 1 - <100%
        public bool DrawRoofs { get; set; } = true;
        public bool TreeToStumps { get; set; }
        public bool EnableCaveBorder { get; set; }
        public bool HideVegetation { get; set; }
        public int FieldsType { get; set; } // 0 = normal, 1 = static, 2 = tile
        public bool NoColorObjectsOutOfRange { get; set; }
        public bool UseCircleOfTransparency { get; set; }
        public int CircleOfTransparencyRadius { get; set; } = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2;
        public int CircleOfTransparencyType { get; set; } // 0 = normal, 1 = like original client
        public int VendorGumpHeight { get; set; } = 350;   //original vendor gump size
        public float DefaultScale { get; set; } = 1.0f;
        public bool EnableMousewheelScaleZoom { get; set; }
        public bool RestoreScaleAfterUnpressCtrl { get; set; }
        public bool BandageSelfOld { get; set; } = true;

        // Bandage Agent Settings
        public bool EnableBandageAgent { get; set; } = false;
        public int BandageAgentDelay { get; set; } = 3000;
        public bool BandageAgentCheckForBuff { get; set; } = false;
        public ushort BandageAgentGraphic { get; set; } = 0x0E21;
        public bool BandageAgentUseNewPacket { get; set; } = true;
        public bool BandageAgentCheckHidden { get; set; } = false;
        public bool BandageAgentCheckPoisoned { get; set; } = false;
        public int BandageAgentHPPercentage { get; set; } = 80;
        public bool BandageAgentCheckInvul { get; set; } = true;
        public bool BandageAgentBandageFriends { get; set; } = false;
        public bool BandageAgentUseDexFormula { get; set; } = false;
        public bool BandageAgentDisableSelfHeal { get; set; } = false;

        public bool EnableDeathScreen { get; set; } = true;
        public bool EnableBlackWhiteEffect { get; set; } = true;
        public ushort HiddenBodyHue { get; set; } = 0x038E;
        public byte HiddenBodyAlpha { get; set; } = 40;
        public int PlayerConstantAlpha { get; set; } = 100;

        // tooltip
        public bool UseTooltip { get; set; } = true;
        public ushort TooltipTextHue { get; set; } = 0xFFFF;
        public int TooltipDelayBeforeDisplay { get; set; } = 250;
        public int TooltipDisplayZoom { get; set; } = 100;
        public int TooltipBackgroundOpacity { get; set; } = 70;
        public byte TooltipFont { get; set; } = 1;

        // movements
        public bool EnablePathfind { get; set; } = true;
        public bool UseShiftToPathfind { get; set; }
        public bool PathfindSingleClick { get; set; }
        public bool AlwaysRun { get; set; } = true;
        public bool AlwaysRunUnlessHidden { get; set; } = true;
        public bool HoldDownKeyTab { get; set; }
        public bool HoldShiftForContext { get; set; } = false;
        public bool HoldShiftToSplitStack { get; set; } = false;

        // general
        [JsonConverter(typeof(Point2Converter))] public Point WindowClientBounds { get; set; } = new Point(600, 480);
        [JsonConverter(typeof(Point2Converter))] public Point ContainerDefaultPosition { get; set; } = new Point(24, 24);
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowPosition { get; set; } = new Point(10, 10);
        public bool GameWindowLock { get; set; }
        public bool GameWindowFullSize { get; set; }
        public bool WindowBorderless { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point GameWindowSize { get; set; } = new Point(800, 680);
        [JsonConverter(typeof(Point2Converter))] public Point TopbarGumpPosition { get; set; } = new Point(0, 0);
        public bool TopbarGumpIsMinimized { get; set; }
        public bool TopbarGumpIsDisabled { get; set; }
        public bool UseAlternativeLights { get; set; }
        public bool UseCustomLightLevel { get; set; }
        public byte LightLevel { get; set; }
        public int LightLevelType { get; set; } // 0 = absolute, 1 = minimum
        public bool UseColoredLights { get; set; } = true;
        public bool UseDarkNights { get; set; }
        public int CloseHealthBarType { get; set; } = 2; // 0 = none, 1 == not exists, 2 == is dead
        public bool ActivateChatAfterEnter { get; set; }
        public bool ActivateChatAdditionalButtons { get; set; } = true;
        public bool ActivateChatShiftEnterSupport { get; set; } = true;
        public bool UseObjectsFading { get; set; } = true;
        public bool HoldDownKeyAltToCloseAnchored { get; set; } = true;
        public bool CloseAllAnchoredGumpsInGroupWithRightClick { get; set; } = false;
        public bool HoldAltToMoveGumps { get; set; }
        public byte JournalOpacity { get; set; } = 50;
        public int JournalStyle { get; set; } = 0;
        public bool HideScreenshotStoredInMessage { get; set; }
        public bool UseModernPaperdoll { get; set; } = false;
        public bool OpenModernPaperdollAtMinimizeLoc { get; set; } = false;

        // Experimental
        public bool CastSpellsByOneClick { get; set; }
        public bool BuffBarTime { get; set; }
        public bool FastSpellsAssign { get; set; }
        public bool AutoOpenDoors { get; set; } = true;
        public bool SmoothDoors { get; set; } = true;
        public bool AutoOpenCorpses { get; set; } = true;
        public int AutoOpenCorpseRange { get; set; } = 2;
        public int CorpseOpenOptions { get; set; } = 3;
        public bool SkipEmptyCorpse { get; set; }
        public bool AutoOpenOwnCorpse { get; set; } = true;
        public bool DisableDefaultHotkeys { get; set; }
        public bool DisableArrowBtn { get; set; }
        public bool DisableTabBtn { get; set; }
        public bool DisableCtrlQWBtn { get; set; }
        public bool DisableAutoMove { get; set; }
        public bool EnableDragSelect { get; set; }
        public int DragSelectModifierKey { get; set; } // 0 = none, 1 = control, 2 = shift, 3 = alt
        public int DragSelect_PlayersModifier { get; set; } = 0;
        public int DragSelect_MonstersModifier { get; set; } = 0;
        public int DragSelect_NameplateModifier { get; set; } = 0;
        public bool OverrideContainerLocation { get; set; }

        public int OverrideContainerLocationSetting { get; set; } // 0 = container position, 1 = top right of screen, 2 = last dragged position, 3 = remember every container

        [JsonConverter(typeof(Point2Converter))] public Point OverrideContainerLocationPosition { get; set; } = new Point(200, 200);
        public bool HueContainerGumps { get; set; } = true;
        public int DragSelectStartX { get; set; } = 100;
        public int DragSelectStartY { get; set; } = 100;
        public bool DragSelectAsAnchor { get; set; } = false;
        public string LastActiveNameOverheadOption { get; set; } = "All";
        public bool NameOverheadToggled { get; set; } = false;
        public bool ShowTargetRangeIndicator { get; set; }
        public bool PartyInviteGump { get; set; } = true;
        public bool CustomBarsToggled { get; set; }
        public bool CBBlackBGToggled { get; set; }

        public bool ShowInfoBar { get; set; }
        public int InfoBarHighlightType { get; set; } // 0 = text colour changes, 1 = underline

        public bool CounterBarEnabled { get; set; }
        public bool CounterBarHighlightOnUse { get; set; }
        public bool CounterBarHighlightOnAmount { get; set; }
        public bool CounterBarDisplayAbbreviatedAmount { get; set; }
        public int CounterBarAbbreviatedAmount { get; set; } = 1000;
        public int CounterBarHighlightAmount { get; set; } = 5;
        public int CounterBarCellSize { get; set; } = 40;

        // title bar stats
        public bool EnableTitleBarStats { get; set; } = false;
        public TitleBarStatsMode TitleBarStatsMode { get; set; } = TitleBarStatsMode.Text;
        public int CounterBarRows { get; set; } = 1;
        public int CounterBarColumns { get; set; } = 5;

        public bool ShowSkillsChangedMessage { get; set; } = true;
        public int ShowSkillsChangedDeltaValue { get; set; } = 1;
        public bool ShowStatsChangedMessage { get; set; } = true;


        public bool ShadowsEnabled { get; set; } = true;
        public bool ShadowsStatics { get; set; } = true;
        public int TerrainShadowsLevel { get; set; } = 15;
        public int AuraUnderFeetType { get; set; } // 0 = NO, 1 = in warmode, 2 = ctrl+shift, 3 = always
        public bool AuraOnMouse { get; set; } = true;
        public bool AnimatedWaterEffect { get; set; } = false;

        public bool PartyAura { get; set; }

        public bool HideChatGradient { get; set; } = false;

        public bool StandardSkillsGump { get; set; } = true;

        public bool ShowNewMobileNameIncoming { get; set; } = true;
        public bool ShowNewCorpseNameIncoming { get; set; } = true;

        public uint GrabBagSerial { get; set; }

        public int GridLootType { get; set; } // 0 = none, 1 = only grid, 2 = both

        public bool ReduceFPSWhenInactive { get; set; }

        public bool EnableVSync { get; set; } = true;

        public bool OverrideAllFonts { get; set; }
        public bool OverrideAllFontsIsUnicode { get; set; } = true;

        public bool SallosEasyGrab { get; set; }

        public bool JournalDarkMode { get; set; }

        public byte ContainersScale { get; set; } = 100;

        public byte ContainerOpacity { get; set; } = 50;

        public bool ScaleItemsInsideContainers { get; set; }

        public bool DoubleClickToLootInsideContainers { get; set; }

        public bool UseLargeContainerGumps { get; set; } = false;

        public bool RelativeDragAndDropItems { get; set; }

        public bool HighlightContainerWhenSelected { get; set; }

        public bool UseNewTargetSystem { get; set; } = true;
        public bool UseKrEquipUnequipPacket { get; set; }
        public bool ShowHouseContent { get; set; }
        public bool SaveHealthbars { get; set; }
        public bool TextFading { get; set; } = true;

        public bool UseSmoothBoatMovement { get; set; } = false;

        public bool IgnoreStaminaCheck { get; set; } = false;

        public bool ShowJournalClient { get; set; } = true;
        public bool ShowJournalObjects { get; set; } = true;
        public bool ShowJournalSystem { get; set; } = true;
        public bool ShowJournalGuildAlly { get; set; } = true;

        public int WorldMapWidth { get; set; } = 400;
        public int WorldMapHeight { get; set; } = 400;
        public int WorldMapFont { get; set; } = 3;
        public bool WorldMapFlipMap { get; set; } = true;
        public bool WorldMapTopMost { get; set; }
        public bool WorldMapFreeView { get; set; }
        public bool WorldMapShowParty { get; set; } = true;
        public int WorldMapZoomIndex { get; set; } = 4;
        public bool WorldMapShowCoordinates { get; set; } = true;
        public bool WorldMapShowMouseCoordinates { get; set; } = true;
        public bool WorldMapShowCorpse { get; set; } = true;
        public bool WorldMapShowSextantCoordinates { get; set; } = false;
        public bool WorldMapShowMobiles { get; set; } = true;
        public bool WorldMapShowPlayerName { get; set; } = true;
        public bool WorldMapShowPlayerBar { get; set; } = true;
        public bool WorldMapShowGroupName { get; set; } = true;
        public bool WorldMapShowGroupBar { get; set; } = true;
        public bool WorldMapShowMarkers { get; set; } = true;
        public bool WorldMapShowMarkersNames { get; set; } = true;
        public bool WorldMapShowMultis { get; set; } = true;
        public string WorldMapHiddenMarkerFiles { get; set; } = string.Empty;
        public string WorldMapHiddenZoneFiles { get; set; } = string.Empty;
        public bool WorldMapShowGridIfZoomed { get; set; } = true;
        public bool WorldMapAllowPositionalTarget { get; set; } = true;

        public int AutoFollowDistance { get; set; } = 2;
        public bool DisableAutoFollowAlt { get; set; } = false;
        [JsonConverter(typeof(Point2Converter))] public Point ResizeJournalSize { get; set; } = new Point(410, 350);
        public bool FollowingMode { get; set; } = false;
        public uint FollowingTarget { get; set; }
        public bool NamePlateHealthBar { get; set; } = true;
        public byte NamePlateOpacity { get; set; } = 75;
        public byte NamePlateHealthBarOpacity { get; set; } = 50;
        public bool NamePlateHideAtFullHealth { get; set; } = true;
        public bool NamePlateHideAtFullHealthInWarmode { get; set; } = true;
        public byte NamePlateBorderOpacity { get; set; } = 50;
        public bool NamePlateAvoidOverlap { get; set; }

        public bool LeftAlignToolTips { get; set; } = false;
        public bool ForceCenterAlignTooltipMobiles { get; set; } = true;

        public bool CorpseSingleClickLoot { get; set; } = false;

        public bool DisableSystemChat { get; set; } = false;

        public uint SetFavoriteMoveBagSerial { get; set; } = 0;

        #region GRID CONTAINER
        public bool UseGridLayoutContainerGumps { get; set; } = true;
        public bool GridContainersDefaultToOldStyleView { get; set; } = false;
        public int GridContainerSearchMode { get; set; } = 1;
        public bool EnableGridContainerAnchor { get; set; } = false;
        public byte GridBorderAlpha { get; set; } = 75;
        public ushort GridBorderHue { get; set; } = 0;
        public byte GridContainersScale { get; set; } = 100;
        public bool GridContainerScaleItems { get; set; } = true;
        public bool GridEnableContPreview { get; set; } = true;
        public int Grid_BorderStyle { get; set; } = 0;
        public int Grid_DefaultColumns { get; set; } = 5;
        public int Grid_DefaultRows { get; set; } = 5;
        public bool Grid_UseContainerHue { get; set; } = false;
        public bool Grid_HideBorder { get; set; } = false;
        #endregion

        #region COOLDOWNS
        public int CoolDownX { get; set; } = 50;
        public int CoolDownY { get; set; } = 50;

        public List<ushort> Condition_Hue { get; set; } = new List<ushort>();
        public List<string> Condition_Label { get; set; } = new List<string>();
        public List<int> Condition_Duration { get; set; } = new List<int>();
        public List<string> Condition_Trigger { get; set; } = new List<string>();
        public List<int> Condition_Type { get; set; } = new List<int>();
        public List<bool> Condition_ReplaceIfExists { get; set; } = new List<bool>();
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
        public bool UseImprovedBuffBar { get; set; } = true;
        public ushort ImprovedBuffBarHue { get; set; } = 905;
        #endregion

        #region DAMAGE NUMBER HUES
        public ushort DamageHueSelf { get; set; } = 0x0034;
        public ushort DamageHuePet { get; set; } = 0x0033;
        public ushort DamageHueAlly { get; set; } = 0x0030;
        public ushort DamageHueLastAttck { get; set; } = 0x1F;
        public ushort DamageHueOther { get; set; } = 0x0021;

        public bool ShowDPS { get; set; } = true;
        #endregion

        #region GridHighlightingProps
        public List<string> GridHighlight_Name { get; set; } = new List<string>();
        public List<ushort> GridHighlight_Hue { get; set; } = new List<ushort>();
        public List<List<string>> GridHighlight_PropNames { get; set; } = new List<List<string>>();
        public List<List<int>> GridHighlight_PropMinVal { get; set; } = new List<List<int>>();
        public bool GridHighlight_CorpseOnly { get; set; } = false;
        public int GridHighlightSize { get; set; } = 1;
        public bool GridHighlightProperties { get; set; } = true;
        public bool GridHighlightShowRuleName { get; set; } = true;
        public List<bool> GridHighlight_AcceptExtraProperties { get; set; } = new List<bool>();
        public List<List<bool>> GridHighlight_IsOptionalProperties { get; set; } = new List<List<bool>>();
        public List<List<string>> GridHighlight_ExcludeNegatives { get; set; } = new List<List<string>>();
        public List<List<string>> GridHighlight_RequiredRarities { get; set; } = new();
        public List<GridHighlightSetupEntry> GridHighlightSetup { get; set; } = new();
        public List<string> ConfigurableProperties { get; set; } = new();
        public List<string> ConfigurableResistances { get; set; } = new();
        public List<string> ConfigurableNegatives { get; set; } = new();
        public List<string> ConfigurableSuperSlayers { get; set; } = new();
        public List<string> ConfigurableSlayers { get; set; } = new();
        public List<string> ConfigurableRarities { get; set; } = new();

        #endregion

        #region Modern paperdoll
        public ushort ModernPaperDollHue { get; set; } = 0;
        public ushort ModernPaperDollDurabilityHue { get; set; } = 32;
        public int ModernPaperDoll_DurabilityPercent { get; set; } = 90;
        [JsonConverter(typeof(Point2Converter))] public Point ModernPaperdollPosition { get; set; } = new Point(100, 100);
        #endregion

        #region Health indicator
        public float ShowHealthIndicatorBelow { get; set; } = 0.9f;
        public bool EnableHealthIndicator { get; set; } = true;
        public int HealthIndicatorWidth { get; set; } = 10;
        #endregion

        public ushort MainWindowBackgroundHue { get; set; } = 1;

        public int MoveMultiObjectDelay { get; set; } = 1000;

        public bool SpellIcon_DisplayHotkey { get; set; } = true;
        public ushort SpellIcon_HotkeyHue { get; set; } = 1;

        public int SpellIconScale { get; set; } = 100;

        public bool EnableAlphaScrollingOnGumps { get; set; } = true;

        [JsonConverter(typeof(Point2Converter))] public Point WorldMapPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point PaperdollPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point JournalPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point StatusGumpPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridPosition { get; set; } = new Point(100, 100);
        [JsonConverter(typeof(Point2Converter))] public Point BackpackGridSize { get; set; } = new Point(300, 300);
        public bool WorldMapLocked { get; set; } = false;
        public bool PaperdollLocked { get; set; } = false;
        public bool JournalLocked { get; set; } = false;
        public bool StatusGumpLocked { get; set; } = false;
        public bool BackPackLocked { get; set; } = false;

        public bool DisplayPartyChatOverhead { get; set; } = true;

        public string SelectedTTFJournalFont { get; set; } = "avadonian";
        public int SelectedJournalFontSize { get; set; } = 20;

        public string SelectedToolTipFont { get; set; } = "Roboto-Regular";
        public int SelectedToolTipFontSize { get; set; } = 20;

        public string GameWindowSideChatFont { get; set; } = "avadonian";
        public int GameWindowSideChatFontSize { get; set; } = 20;

        public string OverheadChatFont { get; set; } = "avadonian";
        public int OverheadChatFontSize { get; set; } = 20;
        public int OverheadChatWidth { get; set; } = 200;

        public string NamePlateFont { get; set; } = "avadonian";
        public int NamePlateFontSize { get; set; } = 20;

        public string OptionsFont { get; set; } = "Roboto-Regular";
        public int OptionsFontSize { get; set; } = 18;

        public int TextBorderSize { get; set; } = 1;

        public uint SavedMountSerial { get; set; } = 0;

        public uint SavedMainHandSerial { get; set; } = 0;
        public uint SavedOffHandSerial { get; set; } = 0;

        public bool UseModernShopGump { get; set; } = false;

        public int MaxJournalEntries { get; set; } = 250;
        public bool HideJournalBorder { get; set; } = false;
        public bool HideJournalTimestamp { get; set; } = false;
        public bool HideJournalSystemPrefix { get; set; } = false;

        public int HealthLineSizeMultiplier { get; set; } = 1;

        public bool OpenHealthBarForLastAttack { get; set; } = true;
        [JsonConverter(typeof(Point2Converter))]
        public Point LastTargetHealthBarPos { get; set; } = Point.Zero;
        public ushort ToolTipBGHue { get; set; } = 0;

        public string LastVersionHistoryShown { get; set; }

        public int AdvancedSkillsGumpHeight { get; set; } = 510;

        #region ToolTip Overrides
        public List<string> ToolTipOverride_SearchText { get; set; } = new List<string>() { "Physical Res", "Fire Resist", "Cold Resist", "Poison Resist", "Energy Resist", "Weapon Damage" };
        public List<string> ToolTipOverride_NewFormat { get; set; } = new List<string>() { "/c[#8c733e]Physical Resist {1}%", "/c[red]Fire Resist {1}%", "/c[teal]Cold Resist {1}%", "/c[green]Poison Resist {1}%", "/c[purple]Energy Resist {1}%", "{0} /c[orange]{1}{4} /cd- /c[red]{2}{5}" };
        public List<int> ToolTipOverride_MinVal1 { get; set; } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MinVal2 { get; set; } = new List<int>() { -1, -1, -1, -1, -1, -1 };
        public List<int> ToolTipOverride_MaxVal1 { get; set; } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        public List<int> ToolTipOverride_MaxVal2 { get; set; } = new List<int>() { 100, 100, 100, 100, 100, 100 };
        public List<byte> ToolTipOverride_Layer { get; set; } = new List<byte>() { (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any, (byte)TooltipLayers.Any };
        #endregion

        public string TooltipHeaderFormat { get; set; } = "/c[yellow]{0}";

        public bool DisplaySkillBarOnChange { get; set; } = true;
        public string SkillBarFormat { get; set; } = "{0}: {1} / {2}";

        public bool DisplayRadius { get; set; } = false;
        public int DisplayRadiusDistance { get; set; } = 10;
        public ushort DisplayRadiusHue { get; set; } = 22;

        public bool EnableSpellIndicators { get; set; } = true;

        public bool EnableAutoLoot { get; set; } = false;
        public bool AutoLootHumanCorpses { get; set; } = false;

        public bool ItemDatabaseEnabled { get; set; } = true;

        public static uint GumpsVersion { get; private set; }

        [JsonConverter(typeof(Point2Converter))]
        public Point InfoBarSize { get; set; } = new Point(400, 20);
        public bool InfoBarLocked { get; set; } = false;
        public string InfoBarFont { get; set; } = "Roboto-Regular";
        public int InfoBarFontSize { get; set; } = 18;

        public int LastJournalTab { get; set; } = 0;
        public Dictionary<string, MessageType[]> JournalTabs { get; set; } = new Dictionary<string, MessageType[]>()
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

        public bool UseLastMovedCooldownPosition { get; set; } = true;
        public bool CloseHealthBarIfAnchored { get; set; } = false;

        [JsonConverter(typeof(Point2Converter))]
        public Point SkillProgressBarPosition { get; set; } = Point.Zero;

        public bool ForceResyncOnHang { get; set; } = false;

        public bool UseOneHPBarForLastAttack { get; set; } = true;

        public bool DisableMouseInteractionOverheadText { get; set; } = false;

        public bool HiddenLayersEnabled { get; set; } = false;
        public List<int> HiddenLayers { get; set; } = new List<int>();
        public bool HideLayersForSelf { get; set; } = true;

        public List<string> AutoOpenXmlGumps { get; set; } = new List<string>();

        public int ControllerMouseSensativity { get => Input.Mouse.ControllerSensativity; set => Input.Mouse.ControllerSensativity = value; }

        [JsonConverter(typeof(Point2Converter))]
        public Point PlayerOffset { get; set; } = new Point(0, 0);

        public double PaperdollScale { get; set; } = 1f;

        public uint SOSGumpID { get; set; } = 1915258020;

        public bool ModernPaperdollAnchorEnabled { get; set; }
        public bool JournalAnchorEnabled { get; set; } = false;
        public bool EnableAutoLootProgressBar { get; set; } = true;
        public bool UseWASDInsteadArrowKeys { get; set; }
        public int NearbyLootGumpHeight { get; set; } = 550;
        public bool ForceTooltipsOnOldClients { get; set; } = true;
        public bool NearbyLootOpensHumanCorpses { get; set; }
        public bool GlobalScaling { get; set; } = false;
        public float GlobalScale { get; set; } = 1.5f;
        public ushort TurnDelay { get; set; } = 100;
        public bool SellAgentEnabled { get; set; }
        public int SellAgentMaxUniques { get; set; } = 50;
        public int SellAgentMaxItems { get; set; } = 0;
        public bool BuyAgentEnabled { get; set; }
        public int BuyAgentMaxUniques { get; set; } = 50;
        public int BuyAgentMaxItems { get; set; } = 0;
        public bool DisableTargetingGridContainers { get; set; }
        public bool ControllerEnabled { get; set; } = true;
        public bool EnableScavenger { get; set; } = true;
        public bool CounterGumpLocked { get; set; }
        public bool NearbyLootConcealsContainerOnOpen { get; set; } = true;
        public bool SpellBar_ShowHotkeys { get; set; } = true;
        public byte ForcedHouseTransparency { get;  set; } = 40;
        public ushort ForcedTransparencyHouseTileHue { get; set; } = 0;
        public bool ForceHouseTransparency { get; set; }
        public ulong HideHudGumpFlags { get; set; }
        public bool DisableGrayEnemies { get; set; }
        public bool EnablePostProcessingEffects { get; set; }
        public ushort PostProcessingType { get; set; }
        public bool DisableHotkeys { get; set; }
        public bool DisableDismountInWarMode { get; set; }
        public bool EnableASyncMapLoading { get; set; }

        private long lastSave;
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

                foreach (Gump gump in UIManager.Gumps)
                {
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

                #region ImGui
                if (ImGuiManager.IsInitialized)
                {
                    try
                    {
                        ImGuiWindow[] windows = ImGuiManager.Windows;
                        if (windows != null && windows.Length > 0)
                        {
                            foreach (ImGuiWindow window in windows)
                            {
                                if(window == null || !window.IsOpen) continue;

                                try
                                {
                                    xml.WriteStartElement("window");
                                    window.Save(xml);
                                    xml.WriteEndElement();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Failed to save ImGui window '{window?.Title ?? "Unknown"}': {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to save ImGui windows: {ex.Message}");
                    }
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
                                case GumpType.ScriptManager:
                                    // ScriptManagerGump removed - use ScriptManagerWindow instead
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
                    SingletonImGuiWindow<ScriptManagerWindow> w = ScriptManagerWindow.GetInstance();
                    w.Load(xml);
                    ImGuiManager.AddWindow(w);
                    break;
                case "ClassicUO.Game.UI.ImGuiControls.AssistantWindow":
                    SingletonImGuiWindow<AssistantWindow> w2 = AssistantWindow.GetInstance();
                    w2.Load(xml);
                    ImGuiManager.AddWindow(w2);
                    break;
            }
        }
    }
}
