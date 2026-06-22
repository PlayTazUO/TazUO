using System.IO;
using System.Text.Json;

namespace ClassicUO.Configuration
{
    /// <summary>
    ///     Compatibility shell for the legacy JSON-backed language system. All string
    ///     properties now resolve through <see cref="TazLang"/> so they participate in
    ///     the UI language dropdown. The object graph is preserved so the ~400 call
    ///     sites (<c>lang.XXX</c>) keep compiling without changes.
    /// </summary>
    public class Language
    {
        public ModernOptionsGumpLanguage GetModernOptionsGumpLanguage { get; set; } = new();
        public AssistantLanguage Assistant { get; set; } = new();

        public string TazuoVersionHistory => TazLang.Get("tazuoversionhistory");
        public string CurrentVersion => TazLang.Get("currentversion");
        public string TazUOWiki => TazLang.Get("tazuowiki");
        public string TazUODiscord => TazLang.Get("tazuodiscord");
        public string CommandGump => TazLang.Get("commandgump");

        public static Language Instance { get; } = new();
    }

    public class ModernOptionsGumpLanguage
    {
        public string OptionsTitle => TazLang.Get("options_optionstitle");
        public string Search => TazLang.Get("options_search");

        public string ButtonGeneral => TazLang.Get("options_buttongeneral");
        public string ButtonSound => TazLang.Get("options_buttonsound");
        public string ButtonVideo => TazLang.Get("options_buttonvideo");
        public string ButtonMacros => TazLang.Get("options_buttonmacros");
        public string ButtonTooltips => TazLang.Get("options_buttontooltips");
        public string ButtonSpeech => TazLang.Get("options_buttonspeech");
        public string ButtonCombatSpells => TazLang.Get("options_buttoncombatspells");
        public string ButtonCounters => TazLang.Get("options_buttoncounters");
        public string ButtonInfobar => TazLang.Get("options_buttoninfobar");
        public string ButtonContainers => TazLang.Get("options_buttoncontainers");
        public string ButtonExperimental => TazLang.Get("options_buttonexperimental");
        public string ButtonIgnoreList => TazLang.Get("options_buttonignorelist");
        public string ButtonNameplates => TazLang.Get("options_buttonnameplates");
        public string ButtonCooldowns => TazLang.Get("options_buttoncooldowns");
        public string ButtonTazUO => TazLang.Get("options_buttontazuo");
        public string ButtonMobiles => TazLang.Get("options_buttonmobiles");
        public string ButtonGumpContext => TazLang.Get("options_buttongumpcontext");
        public string ButtonMisc => TazLang.Get("options_buttonmisc");
        public string ButtonTerrainStatics => TazLang.Get("options_buttonterrainstatics");
        public string ButtonGameWindow => TazLang.Get("options_buttongamewindow");
        public string ButtonZoom => TazLang.Get("options_buttonzoom");
        public string ButtonLighting => TazLang.Get("options_buttonlighting");
        public string ButtonShadows => TazLang.Get("options_buttonshadows");

        public General GetGeneral { get; set; } = new();
        public Video GetVideo { get; set; } = new();
        public Sound GetSound { get; set; } = new();
        public Macros GetMacros { get; set; } = new();
        public ToolTips GetToolTips { get; set; } = new();
        public Speech GetSpeech { get; set; } = new();
        public CombatSpells GetCombatSpells { get; set; } = new();
        public Counters GetCounters { get; set; } = new();
        public InfoBars GetInfoBars { get; set; } = new();
        public Containers GetContainers { get; set; } = new();
        public Experimental GetExperimental { get; set; } = new();
        public NamePlates GetNamePlates { get; set; } = new();
        public Cooldowns GetCooldowns { get; set; } = new();
        public TazUO GetTazUO { get; set; } = new();

        public class General
        {
            public string SharedNone => TazLang.Get("options_general_sharednone");
            public string SharedShift => TazLang.Get("options_general_sharedshift");
            public string SharedCtrl => TazLang.Get("options_general_sharedctrl");
            public string SharedAlt => TazLang.Get("options_general_sharedalt");

            #region General->General
            public string HighlightObjects => TazLang.Get("options_general_highlightobjects");
            public string Pathfinding => TazLang.Get("options_general_pathfinding");
            public string ShiftPathfinding => TazLang.Get("options_general_shiftpathfinding");
            public string SingleClickPathfind => TazLang.Get("options_general_singleclickpathfind");
            public string AlwaysRun => TazLang.Get("options_general_alwaysrun");
            public string RunUnlessHidden => TazLang.Get("options_general_rununlesshidden");
            public string AutoOpenDoors => TazLang.Get("options_general_autoopendoors");
            public string AutoOpenPathfinding => TazLang.Get("options_general_autoopenpathfinding");
            public string AutoOpenCorpse => TazLang.Get("options_general_autoopencorpse");
            public string CorpseOpenDistance => TazLang.Get("options_general_corpseopendistance");
            public string CorpseSkipEmpty => TazLang.Get("options_general_corpseskipempty");
            public string CorpseOpenOptions => TazLang.Get("options_general_corpseopenoptions");
            public string CorpseOptNone => TazLang.Get("options_general_corpseoptnone");
            public string CorpseOptNotTarg => TazLang.Get("options_general_corpseoptnottarg");
            public string CorpseOptNotHiding => TazLang.Get("options_general_corpseoptnothiding");
            public string CorpseOptBoth => TazLang.Get("options_general_corpseoptboth");
            public string OutRangeColor => TazLang.Get("options_general_outrangecolor");
            public string SallosEasyGrab => TazLang.Get("options_general_salloseasygrab");
            public string SallosTooltip => TazLang.Get("options_general_sallostooltip");
            public string ShowHouseContent => TazLang.Get("options_general_showhousecontent");
            public string SmoothBoat => TazLang.Get("options_general_smoothboat");
            #endregion

            #region General->Mobiles
            public string ShowMobileHP => TazLang.Get("options_general_showmobilehp");
            public string ShowTargetIndicator => TazLang.Get("options_general_showtargetindicator");
            public string MobileHPType => TazLang.Get("options_general_mobilehptype");
            public string HPTypePerc => TazLang.Get("options_general_hptypeperc");
            public string HPTypeBar => TazLang.Get("options_general_hptypebar");
            public string HPTypeNBoth => TazLang.Get("options_general_hptypenboth");
            public string HPShowWhen => TazLang.Get("options_general_hpshowwhen");
            public string HPShowWhen_Always => TazLang.Get("options_general_hpshowwhen_always");
            public string HPShowWhen_Less100 => TazLang.Get("options_general_hpshowwhen_less100");
            public string HPShowWhen_Smart => TazLang.Get("options_general_hpshowwhen_smart");
            public string HighlightPoisoned => TazLang.Get("options_general_highlightpoisoned");
            public string PoisonHighlightColor => TazLang.Get("options_general_poisonhighlightcolor");
            public string HighlightPara => TazLang.Get("options_general_highlightpara");
            public string ParaHighlightColor => TazLang.Get("options_general_parahighlightcolor");
            public string HighlightInvul => TazLang.Get("options_general_highlightinvul");
            public string InvulHighlightColor => TazLang.Get("options_general_invulhighlightcolor");
            public string IncomingMobiles => TazLang.Get("options_general_incomingmobiles");
            public string IncomingCorpses => TazLang.Get("options_general_incomingcorpses");
            public string AuraUnderFeet => TazLang.Get("options_general_auraunderfeet");
            public string AuraOptDisabled => TazLang.Get("options_general_auraoptdisabled");
            public string AuroOptWarmode => TazLang.Get("options_general_aurooptwarmode");
            public string AuraOptCtrlShift => TazLang.Get("options_general_auraoptctrlshift");
            public string AuraOptAlways => TazLang.Get("options_general_auraoptalways");
            public string AuraForParty => TazLang.Get("options_general_auraforparty");
            public string AuraPartyColor => TazLang.Get("options_general_aurapartycolor");
            public string IgnoreStaminaCheck => TazLang.Get("options_general_ignorestaminacheck");
            public string DisableGrayEnemies => TazLang.Get("options_general_disablegrayenemies");
            public string DisableDismountWarmode => TazLang.Get("options_general_disabledismountwarmode");
            #endregion

            #region General->Gumps
            public string DisableTopMenu => TazLang.Get("options_general_disabletopmenu");
            public string AltForAnchorsGumps => TazLang.Get("options_general_altforanchorsgumps");
            public string AltToMoveGumps => TazLang.Get("options_general_alttomovegumps");
            public string CloseEntireAnchorWithRClick => TazLang.Get("options_general_closeentireanchorwithrclick");
            public string OriginalSkillsGump => TazLang.Get("options_general_originalskillsgump");
            public string OldStatusGump => TazLang.Get("options_general_oldstatusgump");
            public string PartyInviteGump => TazLang.Get("options_general_partyinvitegump");
            public string ModernHealthBars => TazLang.Get("options_general_modernhealthbars");
            public string ModernHPBlackBG => TazLang.Get("options_general_modernhpblackbg");
            public string SaveHPBars => TazLang.Get("options_general_savehpbars");
            public string CloseHPGumpsWhen => TazLang.Get("options_general_closehpgumpswhen");
            public string CloseHPOptDisable => TazLang.Get("options_general_closehpoptdisable");
            public string CloseHPOptOOR => TazLang.Get("options_general_closehpoptoor");
            public string CloseHPOptDead => TazLang.Get("options_general_closehpoptdead");
            public string CloseHPOptBoth => TazLang.Get("options_general_closehpoptboth");
            public string GridLoot => TazLang.Get("options_general_gridloot");
            public string GridLootOptDisable => TazLang.Get("options_general_gridlootoptdisable");
            public string GridLootOptOnly => TazLang.Get("options_general_gridlootoptonly");
            public string GridLootOptBoth => TazLang.Get("options_general_gridlootoptboth");
            public string GridLootTooltip => TazLang.Get("options_general_gridloottooltip");
            public string ShiftContext => TazLang.Get("options_general_shiftcontext");
            public string ShiftSplit => TazLang.Get("options_general_shiftsplit");

            #endregion

            #region General->Misc
            public string EnableCOT => TazLang.Get("options_general_enablecot");
            public string COTDistance => TazLang.Get("options_general_cotdistance");
            public string COTType => TazLang.Get("options_general_cottype");
            public string COTTypeOptFull => TazLang.Get("options_general_cottypeoptfull");
            public string COTTypeOptGrad => TazLang.Get("options_general_cottypeoptgrad");
            public string COTTypeOptModern => TazLang.Get("options_general_cottypeoptmodern");
            public string HideScreenshotMessage => TazLang.Get("options_general_hidescreenshotmessage");
            public string ObjFade => TazLang.Get("options_general_objfade");
            public string TextFade => TazLang.Get("options_general_textfade");
            public string CursorRange => TazLang.Get("options_general_cursorrange");

            public string AutoAvoidObstacules => TazLang.Get("options_general_autoavoidobstacules");
            public string DragSelectHP => TazLang.Get("options_general_dragselecthp");
            public string DragKeyMod => TazLang.Get("options_general_dragkeymod");
            public string DragPlayersOnly => TazLang.Get("options_general_dragplayersonly");
            public string DragMobsOnly => TazLang.Get("options_general_dragmobsonly");
            public string DragNameplatesOnly => TazLang.Get("options_general_dragnameplatesonly");
            public string DragX => TazLang.Get("options_general_dragx");
            public string DragY => TazLang.Get("options_general_dragy");
            public string DragAnchored => TazLang.Get("options_general_draganchored");
            public string ShowStatsChangedMsg => TazLang.Get("options_general_showstatschangedmsg");
            public string ShowSkillsChangedMsg => TazLang.Get("options_general_showskillschangedmsg");
            public string ChangeVolume => TazLang.Get("options_general_changevolume");
            #endregion

            #region General->TerrainStatics
            public string HideRoof => TazLang.Get("options_general_hideroof");
            public string TreesToStump => TazLang.Get("options_general_treestostump");
            public string HideVegetation => TazLang.Get("options_general_hidevegetation");
            public string MagicFieldType => TazLang.Get("options_general_magicfieldtype");
            public string MagicFieldOpt_Normal => TazLang.Get("options_general_magicfieldopt_normal");
            public string MagicFieldOpt_Static => TazLang.Get("options_general_magicfieldopt_static");
            public string MagicFieldOpt_Tile => TazLang.Get("options_general_magicfieldopt_tile");
            #endregion
        }

        public class Sound
        {
            public string SharedVolume => TazLang.Get("options_sound_sharedvolume");

            public string EnableSound => TazLang.Get("options_sound_enablesound");
            public string EnableMusic => TazLang.Get("options_sound_enablemusic");
            public string LoginMusic => TazLang.Get("options_sound_loginmusic");
            public string PlayFootsteps => TazLang.Get("options_sound_playfootsteps");
            public string CombatMusic => TazLang.Get("options_sound_combatmusic");
            public string BackgroundMusic => TazLang.Get("options_sound_backgroundmusic");
        }

        public class Video
        {
            #region GameWindow
            public string FPSCap => TazLang.Get("options_video_fpscap");
            public string BackgroundFPS => TazLang.Get("options_video_backgroundfps");
            public string EnableVSync => TazLang.Get("options_video_enablevsync");
            public string FullsizeViewport => TazLang.Get("options_video_fullsizeviewport");
            public string FullScreen => TazLang.Get("options_video_fullscreen");
            public string LockViewport => TazLang.Get("options_video_lockviewport");
            public string ViewportX => TazLang.Get("options_video_viewportx");
            public string ViewportY => TazLang.Get("options_video_viewporty");
            public string ViewportW => TazLang.Get("options_video_viewportw");
            public string ViewportH => TazLang.Get("options_video_viewporth");
            #endregion

            #region Zoom
            public string DefaultZoom => TazLang.Get("options_video_defaultzoom");
            public string ZoomWheel => TazLang.Get("options_video_zoomwheel");
            public string ReturnDefaultZoom => TazLang.Get("options_video_returndefaultzoom");
            #endregion

            #region Lighting
            public string AltLights => TazLang.Get("options_video_altlights");
            public string CustomLLevel => TazLang.Get("options_video_customllevel");
            public string Level => TazLang.Get("options_video_level");
            public string LightType => TazLang.Get("options_video_lighttype");
            public string LightType_Absolute => TazLang.Get("options_video_lighttype_absolute");
            public string LightType_Minimum => TazLang.Get("options_video_lighttype_minimum");
            public string DarkNight => TazLang.Get("options_video_darknight");
            public string ColoredLight => TazLang.Get("options_video_coloredlight");
            #endregion

            #region Misc
            public string EnableDeathScreen => TazLang.Get("options_video_enabledeathscreen");
            public string BWDead => TazLang.Get("options_video_bwdead");
            public string MouseThread => TazLang.Get("options_video_mousethread");
            public string TargetAura => TazLang.Get("options_video_targetaura");
            public string AnimWater => TazLang.Get("options_video_animwater");
            #endregion

            #region Shadows
            public string EnableShadows => TazLang.Get("options_video_enableshadows");
            public string RockTreeShadows => TazLang.Get("options_video_rocktreeshadows");
            public string TerrainShadowLevel => TazLang.Get("options_video_terrainshadowlevel");
            #endregion
        }

        public class Macros
        {
            public string NewMacro => TazLang.Get("options_macros_newmacro");
            public string DelMacro => TazLang.Get("options_macros_delmacro");
        }

        public class ToolTips
        {
            public string EnableToolTips => TazLang.Get("options_tooltips_enabletooltips");
            public string ToolTipDelay => TazLang.Get("options_tooltips_tooltipdelay");
            public string ToolTipBG => TazLang.Get("options_tooltips_tooltipbg");
            public string ToolTipFont => TazLang.Get("options_tooltips_tooltipfont");
        }

        public class Speech
        {
            public string ScaleSpeechDelay => TazLang.Get("options_speech_scalespeechdelay");
            public string SpeechDelay => TazLang.Get("options_speech_speechdelay");
            public string SaveJournalE => TazLang.Get("options_speech_savejournale");
            public string ChatEnterActivation => TazLang.Get("options_speech_chatenteractivation");
            public string ChatEnterSpecial => TazLang.Get("options_speech_chatenterspecial");
            public string ShiftEnterChat => TazLang.Get("options_speech_shiftenterchat");
            public string ChatGradient => TazLang.Get("options_speech_chatgradient");
            public string HideGuildChat => TazLang.Get("options_speech_hideguildchat");
            public string HideAllianceChat => TazLang.Get("options_speech_hidealliancechat");
            public string SpeechColor => TazLang.Get("options_speech_speechcolor");
            public string YellColor => TazLang.Get("options_speech_yellcolor");
            public string PartyColor => TazLang.Get("options_speech_partycolor");
            public string AllianceColor => TazLang.Get("options_speech_alliancecolor");
            public string EmoteColor => TazLang.Get("options_speech_emotecolor");
            public string WhisperColor => TazLang.Get("options_speech_whispercolor");
            public string GuildColor => TazLang.Get("options_speech_guildcolor");
            public string CharColor => TazLang.Get("options_speech_charcolor");
        }

        public class CombatSpells
        {
            public string HoldTabForCombat => TazLang.Get("options_combatspells_holdtabforcombat");
            public string QueryBeforeAttack => TazLang.Get("options_combatspells_querybeforeattack");
            public string QueryBeforeBeneficial => TazLang.Get("options_combatspells_querybeforebeneficial");
            public string EnableOverheadSpellFormat => TazLang.Get("options_combatspells_enableoverheadspellformat");
            public string EnableOverheadSpellHue => TazLang.Get("options_combatspells_enableoverheadspellhue");
            public string SingleClickForSpellIcons => TazLang.Get("options_combatspells_singleclickforspellicons");
            public string ShowBuffDurationOnOldStyleBuffBar => TazLang.Get("options_combatspells_showbuffdurationonoldstylebuffbar");
            public string EnableFastSpellHotkeyAssigning => TazLang.Get("options_combatspells_enablefastspellhotkeyassigning");
            public string EnableDPSCounter => TazLang.Get("options_combatspells_enabledpscounter");
            public string TooltipFastSpellAssign => TazLang.Get("options_combatspells_tooltipfastspellassign");
            public string InnocentColor => TazLang.Get("options_combatspells_innocentcolor");
            public string BeneficialSpell => TazLang.Get("options_combatspells_beneficialspell");
            public string FriendColor => TazLang.Get("options_combatspells_friendcolor");
            public string HarmfulSpell => TazLang.Get("options_combatspells_harmfulspell");
            public string Criminal => TazLang.Get("options_combatspells_criminal");
            public string NeutralSpell => TazLang.Get("options_combatspells_neutralspell");
            public string CanBeAttackedHue => TazLang.Get("options_combatspells_canbeattackedhue");
            public string Murderer => TazLang.Get("options_combatspells_murderer");
            public string Enemy => TazLang.Get("options_combatspells_enemy");
            public string SpellOverheadFormat => TazLang.Get("options_combatspells_spelloverheadformat");
            public string TooltipSpellFormat => TazLang.Get("options_combatspells_tooltipspellformat");
        }

        public class Counters
        {
            public string EnableCounters => TazLang.Get("options_counters_enablecounters");
            public string HighlightItemsOnUse => TazLang.Get("options_counters_highlightitemsonuse");
            public string AbbreviatedValues => TazLang.Get("options_counters_abbreviatedvalues");
            public string AbbreviateIfAmountExceeds => TazLang.Get("options_counters_abbreviateifamountexceeds");
            public string HighlightRedWhenAmountIsLow => TazLang.Get("options_counters_highlightredwhenamountislow");
            public string HighlightRedIfAmountIsBelow => TazLang.Get("options_counters_highlightredifamountisbelow");
            public string CounterLayout => TazLang.Get("options_counters_counterlayout");
            public string GridSize => TazLang.Get("options_counters_gridsize");
            public string Rows => TazLang.Get("options_counters_rows");
            public string Columns => TazLang.Get("options_counters_columns");
        }

        public class InfoBars
        {
            public string ShowInfoBar => TazLang.Get("options_infobars_showinfobar");
            public string HighlightType => TazLang.Get("options_infobars_highlighttype");
            public string HighLightOpt_TextColor => TazLang.Get("options_infobars_highlightopt_textcolor");
            public string HighLightOpt_ColoredBars => TazLang.Get("options_infobars_highlightopt_coloredbars");
            public string AddItem => TazLang.Get("options_infobars_additem");
            public string Hp => TazLang.Get("options_infobars_hp");
            public string Label => TazLang.Get("options_infobars_label");
            public string Color => TazLang.Get("options_infobars_color");
            public string Data => TazLang.Get("options_infobars_data");
        }

        public class Containers
        {
            public string Description => TazLang.Get("options_containers_description");
            public string CharacterBackpackStyle => TazLang.Get("options_containers_characterbackpackstyle");
            public string BackpackOpt_Default => TazLang.Get("options_containers_backpackopt_default");
            public string BackpackOpt_Suede => TazLang.Get("options_containers_backpackopt_suede");
            public string BackpackOpt_PolarBear => TazLang.Get("options_containers_backpackopt_polarbear");
            public string BackpackOpt_GhoulSkin => TazLang.Get("options_containers_backpackopt_ghoulskin");
            public string ContainerScale => TazLang.Get("options_containers_containerscale");
            public string AlsoScaleItems => TazLang.Get("options_containers_alsoscaleitems");
            public string UseLargeContainerGumps => TazLang.Get("options_containers_uselargecontainergumps");
            public string DoubleClickToLootItemsInsideContainers => TazLang.Get("options_containers_doubleclicktolootitemsinsidecontainers");
            public string RelativeDragAndDropItemsInContainers => TazLang.Get("options_containers_relativedraganddropitemsincontainers");
            public string HighlightContainerOnGroundWhenMouseIsOverAContainerGump => TazLang.Get("options_containers_highlightcontainerongroundwhenmouseisoveracontainergump");
            public string RecolorContainerGumpByWithContainerHue => TazLang.Get("options_containers_recolorcontainergumpbywithcontainerhue");
            public string OverrideContainerGumpLocations => TazLang.Get("options_containers_overridecontainergumplocations");
            public string OverridePosition => TazLang.Get("options_containers_overrideposition");
            public string PositionOpt_NearContainer => TazLang.Get("options_containers_positionopt_nearcontainer");
            public string PositionOpt_TopRight => TazLang.Get("options_containers_positionopt_topright");
            public string PositionOpt_LastDraggedPosition => TazLang.Get("options_containers_positionopt_lastdraggedposition");
            public string RememberEachContainer => TazLang.Get("options_containers_remembereachcontainer");
            public string RebuildContainersTxt => TazLang.Get("options_containers_rebuildcontainerstxt");
        }

        public class Experimental
        {
            public string DisableDefaultUoHotkeys => TazLang.Get("options_experimental_disabledefaultuohotkeys");
            public string DisableArrowsNumlockArrowsPlayerMovement => TazLang.Get("options_experimental_disablearrowsnumlockarrowsplayermovement");
            public string DisableTabToggleWarmode => TazLang.Get("options_experimental_disabletabtogglewarmode");
            public string DisableCtrlQWMessageHistory => TazLang.Get("options_experimental_disablectrlqwmessagehistory");
            public string DisableRightLeftClickAutoMove => TazLang.Get("options_experimental_disablerightleftclickautomove");
        }

        public class NamePlates
        {
            public string NewEntry => TazLang.Get("options_nameplates_newentry");
            public string NameOverheadEntryName => TazLang.Get("options_nameplates_nameoverheadentryname");
            public string DeleteEntry => TazLang.Get("options_nameplates_deleteentry");
        }

        public class Cooldowns
        {
            public string CustomCooldownBars => TazLang.Get("options_cooldowns_customcooldownbars");
            public string PositionX => TazLang.Get("options_cooldowns_positionx");
            public string PositionY => TazLang.Get("options_cooldowns_positiony");
            public string UseLastMovedBarPosition => TazLang.Get("options_cooldowns_uselastmovedbarposition");
            public string Conditions => TazLang.Get("options_cooldowns_conditions");
            public string AddCondition => TazLang.Get("options_cooldowns_addcondition");
        }

        public class TazUO
        {
            #region General
            public string GridContainers => TazLang.Get("options_tazuo_gridcontainers");
            public string EnableGridContainers => TazLang.Get("options_tazuo_enablegridcontainers");
            public string GridContainersDefaultToOldStyleView => TazLang.Get("options_tazuo_gridcontainersdefaulttooldstyleview");
            public string GridContainerScale => TazLang.Get("options_tazuo_gridcontainerscale");
            public string AlsoScaleItems => TazLang.Get("options_tazuo_alsoscaleitems");
            public string HighlightLowContrastItems => TazLang.Get("options_tazuo_highlightlowcontrastitems");
            public string LowContrastHighlightStyle => TazLang.Get("options_tazuo_lowcontrasthighlightstyle");
            public string GridItemBorderOpacity => TazLang.Get("options_tazuo_griditemborderopacity");
            public string BorderColor => TazLang.Get("options_tazuo_bordercolor");
            public string ContainerOpacity => TazLang.Get("options_tazuo_containeropacity");
            public string BackgroundColor => TazLang.Get("options_tazuo_backgroundcolor");
            public string UseContainersHue => TazLang.Get("options_tazuo_usecontainershue");
            public string SearchStyle => TazLang.Get("options_tazuo_searchstyle");
            public string OnlyShow => TazLang.Get("options_tazuo_onlyshow");
            public string Highlight => TazLang.Get("options_tazuo_highlight");
            public string EnableContainerPreview => TazLang.Get("options_tazuo_enablecontainerpreview");
            public string TooltipPreview => TazLang.Get("options_tazuo_tooltippreview");
            public string MakeAnchorable => TazLang.Get("options_tazuo_makeanchorable");
            public string TooltipGridAnchor => TazLang.Get("options_tazuo_tooltipgridanchor");
            public string ContainerStyle => TazLang.Get("options_tazuo_containerstyle");
            public string HideBorders => TazLang.Get("options_tazuo_hideborders");
            public string DefaultGridRows => TazLang.Get("options_tazuo_defaultgridrows");
            public string DefaultGridColumns => TazLang.Get("options_tazuo_defaultgridcolumns");
            public string GridHighlightSettings => TazLang.Get("options_tazuo_gridhighlightsettings");
            public string GridHighlightSize => TazLang.Get("options_tazuo_gridhighlightsize");
            public string GridHighlightProperties => TazLang.Get("options_tazuo_gridhighlightproperties");
            public string GridHighlightShowRuleName => TazLang.Get("options_tazuo_gridhighlightshowrulename");
            public string GridDisableTargeting => TazLang.Get("options_tazuo_griddisabletargeting");
            #endregion

            #region Journal
            public string Journal => TazLang.Get("options_tazuo_journal");
            public string MaxJournalEntries => TazLang.Get("options_tazuo_maxjournalentries");
            public string JournalOpacity => TazLang.Get("options_tazuo_journalopacity");
            public string JournalBackgroundColor => TazLang.Get("options_tazuo_journalbackgroundcolor");
            public string JournalStyle => TazLang.Get("options_tazuo_journalstyle");
            public string JournalHideBorders => TazLang.Get("options_tazuo_journalhideborders");
            public string JournalHideSystemPrefix => TazLang.Get("options_tazuo_journalhidesystemprefix");
            public string HideTimestamp => TazLang.Get("options_tazuo_hidetimestamp");
            public string JournalAnchor => TazLang.Get("options_tazuo_journalanchor");
            #endregion

            #region ModernPaperdoll
            public string ModernPaperdoll => TazLang.Get("options_tazuo_modernpaperdoll");
            public string EnableModernPaperdoll => TazLang.Get("options_tazuo_enablemodernpaperdoll");
            public string PaperdollHue => TazLang.Get("options_tazuo_paperdollhue");
            public string DurabilityBarHue => TazLang.Get("options_tazuo_durabilitybarhue");
            public string ShowDurabilityBarBelow => TazLang.Get("options_tazuo_showdurabilitybarbelow");
            public string PaperdollAnchor => TazLang.Get("options_tazuo_paperdollanchor");
            #endregion

            #region Nameplates
            public string Nameplates => TazLang.Get("options_tazuo_nameplates");
            public string NameplatesAlsoActAsHealthBars => TazLang.Get("options_tazuo_nameplatesalsoactashealthbars");
            public string HpOpacity => TazLang.Get("options_tazuo_hpopacity");
            public string HideNameplatesIfFullHealth => TazLang.Get("options_tazuo_hidenameplatesiffullhealth");
            public string OnlyInWarmode => TazLang.Get("options_tazuo_onlyinwarmode");
            public string BorderOpacity => TazLang.Get("options_tazuo_borderopacity");
            public string BackgroundOpacity => TazLang.Get("options_tazuo_backgroundopacity");
            #endregion

            #region Mobile
            public string Mobiles => TazLang.Get("options_tazuo_mobiles");
            public string DamageToSelf => TazLang.Get("options_tazuo_damagetoself");
            public string DamageToOthers => TazLang.Get("options_tazuo_damagetoothers");
            public string DamageToPets => TazLang.Get("options_tazuo_damagetopets");
            public string DamageToAllies => TazLang.Get("options_tazuo_damagetoallies");
            public string DamageToLastAttack => TazLang.Get("options_tazuo_damagetolastattack");
            public string DisplayPartyChatOverPlayerHeads => TazLang.Get("options_tazuo_displaypartychatoverplayerheads");
            public string TooltipPartyChat => TazLang.Get("options_tazuo_tooltippartychat");
            public string OverheadTextWidth => TazLang.Get("options_tazuo_overheadtextwidth");
            public string TooltipOverheadText => TazLang.Get("options_tazuo_tooltipoverheadtext");
            public string BelowMobileHealthBarScale => TazLang.Get("options_tazuo_belowmobilehealthbarscale");
            public string AutomaticallyOpenHealthBarsForLastAttack => TazLang.Get("options_tazuo_automaticallyopenhealthbarsforlastattack");
            public string UpdateOneBarAsLastAttack => TazLang.Get("options_tazuo_updateonebaraslastattack");
            public string HiddenPlayerOpacity => TazLang.Get("options_tazuo_hiddenplayeropacity");
            public string HiddenPlayerHue => TazLang.Get("options_tazuo_hiddenplayerhue");
            public string RegularPlayerOpacity => TazLang.Get("options_tazuo_regularplayeropacity");
            public string AutoFollowDistance => TazLang.Get("options_tazuo_autofollowdistance");
            public string DisableAutoFollow => TazLang.Get("options_tazuo_disableautofollow");
            public string DisableMouseInteractionsForOverheadText => TazLang.Get("options_tazuo_disablemouseinteractionsforoverheadtext");
            public string OverridePartyMemberHues => TazLang.Get("options_tazuo_overridepartymemberhues");
            public string TurnDelay => TazLang.Get("options_tazuo_turndelay");
            #endregion

            #region Misc
            public string Misc => TazLang.Get("options_tazuo_misc");
            public string DisableSystemChat => TazLang.Get("options_tazuo_disablesystemchat");
            public string EnableImprovedBuffGump => TazLang.Get("options_tazuo_enableimprovedbuffgump");
            public string BuffGumpHue => TazLang.Get("options_tazuo_buffgumphue");
            public string MainGameWindowBackground => TazLang.Get("options_tazuo_maingamewindowbackground");
            public string EnableHealthIndicatorBorder => TazLang.Get("options_tazuo_enablehealthindicatorborder");
            public string OnlyShowBelowHp => TazLang.Get("options_tazuo_onlyshowbelowhp");
            public string Size => TazLang.Get("options_tazuo_size");
            public string SpellIconScale => TazLang.Get("options_tazuo_spelliconscale");
            public string DisplayMatchingHotkeysOnSpellIcons => TazLang.Get("options_tazuo_displaymatchinghotkeysonspellicons");
            public string HotkeyTextHue => TazLang.Get("options_tazuo_hotkeytexthue");
            public string EnableGumpOpacityAdjustViaAltScroll => TazLang.Get("options_tazuo_enablegumpopacityadjustviaaltscroll");
            public string EnableAdvancedShopGump => TazLang.Get("options_tazuo_enableadvancedshopgump");
            public string DisplaySkillProgressBarOnSkillChanges => TazLang.Get("options_tazuo_displayskillprogressbaronskillchanges");
            public string TextFormat => TazLang.Get("options_tazuo_textformat");
            public string EnableSpellIndicatorSystem => TazLang.Get("options_tazuo_enablespellindicatorsystem");
            public string ImportFromUrl => TazLang.Get("options_tazuo_importfromurl");
            public string InputRequestUrl => TazLang.Get("options_tazuo_inputrequesturl");
            public string Download => TazLang.Get("options_tazuo_download");
            public string Cancel => TazLang.Get("options_tazuo_cancel");
            public string AttemptingToDownloadSpellConfig => TazLang.Get("options_tazuo_attemptingtodownloadspellconfig");
            public string SuccesfullyDownloadedNewSpellConfig => TazLang.Get("options_tazuo_succesfullydownloadednewspellconfig");
            public string FailedToDownloadTheSpellConfigExMessage => TazLang.Get("options_tazuo_failedtodownloadthespellconfigexmessage");
            public string AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars => TazLang.Get("options_tazuo_alsocloseanchoredhealthbarswhenautoclosinghealthbars");
            public string EnableAutoResyncOnHangDetection => TazLang.Get("options_tazuo_enableautoresynconhangdetection");
            public string PlayerOffsetX => TazLang.Get("options_tazuo_playeroffsetx");
            public string PlayerOffsetY => TazLang.Get("options_tazuo_playeroffsety");
            public string UseLandTexturesWhereAvailable => TazLang.Get("options_tazuo_uselandtextureswhereavailable");
            public string SOSGumpID => TazLang.Get("options_tazuo_sosgumpid");
            public string UseWASDMovement => TazLang.Get("options_tazuo_usewasdmovement");
            public string ApplyBorderCaveTiles => TazLang.Get("options_tazuo_applybordercavetiles");
            public string ForcedHouseTransparencyLevel => TazLang.Get("options_tazuo_forcedhousetransparencylevel");
            public string EnableHouseTransparency => TazLang.Get("options_tazuo_enablehousetransparency");
            public string HouseTransparencyTileHue => TazLang.Get("options_tazuo_housetransparencytilehue");
            public string EnableASyncMapLoading => TazLang.Get("options_tazuo_enableasyncmaploading");
            public string ForceManagedZlib => TazLang.Get("options_tazuo_forcemanagedzlib");
            #endregion

            #region Tooltips
            public string Tooltips => TazLang.Get("options_tazuo_tooltips");
            public string AlignTooltipsToTheLeftSide => TazLang.Get("options_tazuo_aligntooltipstotheleftside");
            public string AlignMobileTooltipsToCenter => TazLang.Get("options_tazuo_alignmobiletooltipstocenter");
            public string BackgroundHue => TazLang.Get("options_tazuo_backgroundhue");
            public string HeaderFormatItemName => TazLang.Get("options_tazuo_headerformatitemname");
            public string TooltipOverrideSettings => TazLang.Get("options_tazuo_tooltipoverridesettings");
            public string ForcedTooltips => TazLang.Get("options_tazuo_forcedtooltips");
            #endregion

            #region Fontsettings
            public string FontSettings => TazLang.Get("options_tazuo_fontsettings");
            public string TtfFontBorder => TazLang.Get("options_tazuo_ttffontborder");
            public string InfobarFont => TazLang.Get("options_tazuo_infobarfont");
            public string SharedSize => TazLang.Get("options_tazuo_sharedsize");
            public string SystemChatFont => TazLang.Get("options_tazuo_systemchatfont");
            public string TooltipFont => TazLang.Get("options_tazuo_tooltipfont");
            public string OverheadFont => TazLang.Get("options_tazuo_overheadfont");
            public string JournalFont => TazLang.Get("options_tazuo_journalfont");
            public string NameplateFont => TazLang.Get("options_tazuo_nameplatefont");
            public string Optionsfont => TazLang.Get("options_tazuo_optionsfont");
            #endregion

            #region Controller
            public string Controller => TazLang.Get("options_tazuo_controller");
            public string MouseSesitivity => TazLang.Get("options_tazuo_mousesesitivity");
            public string EnableController => TazLang.Get("options_tazuo_enablecontroller");
            #endregion

            #region SettingsTransfer
            public string SettingsTransfers => TazLang.Get("options_tazuo_settingstransfers");
            public string SettingsWarning => TazLang.Get("options_tazuo_settingswarning");
            public string OverrideAll => TazLang.Get("options_tazuo_overrideall");
            public string OverrideAllMacros => TazLang.Get("options_tazuo_overrideallmacros");
            public string OverrideSuccess => TazLang.Get("options_tazuo_overridesuccess");
            public string OverrideSame => TazLang.Get("options_tazuo_overridesame");
            public string SetAsDefault => TazLang.Get("options_tazuo_setasdefault");
            public string SetMacrosAsDefault => TazLang.Get("options_tazuo_setmacrosasdefault");
            public string SetAsDefaultSuccess => TazLang.Get("options_tazuo_setasdefaultsuccess");
            public string SetMacrosAsDefaultSuccess => TazLang.Get("options_tazuo_setmacrosasdefaultsuccess");

            #endregion

            #region GumpScaling
            public string GumpScaling => TazLang.Get("options_tazuo_gumpscaling");
            public string ScalingInfo => TazLang.Get("options_tazuo_scalinginfo");
            public string PaperdollGump => TazLang.Get("options_tazuo_paperdollgump");
            public string GlobalScaling => TazLang.Get("options_tazuo_globalscaling");
            public string GlobalScale => TazLang.Get("options_tazuo_globalscale");
            #endregion

            public string AutoLoot => TazLang.Get("options_tazuo_autoloot");
            public string AutoLootEnable => TazLang.Get("options_tazuo_autolootenable");
            public string ScavengerEnable => TazLang.Get("options_tazuo_scavengerenable");
            public string AutoLootProgessBarEnable => TazLang.Get("options_tazuo_autolootprogessbarenable");
            public string AutoLootHumanCorpses => TazLang.Get("options_tazuo_autoloothumancorpses");

            public string GraphicChangeFilter => TazLang.Get("options_tazuo_graphicchangefilter");
            public string Hotkeys => TazLang.Get("options_tazuo_hotkeys");


            #region VoiceRecognition
            public string VoiceRecognition => TazLang.Get("options_tazuo_voicerecognition");
            public string VoiceRecognitionEnable => TazLang.Get("options_tazuo_voicerecognitionenable");
            public string VoiceModelPath => TazLang.Get("options_tazuo_voicemodelpath");
            public string VoiceModelPathTooltip => TazLang.Get("options_tazuo_voicemodelpathtooltip");
            public string VoiceRecognitionStatus => TazLang.Get("options_tazuo_voicerecognitionstatus");
            public string VoiceStatusReady => TazLang.Get("options_tazuo_voicestatusready");
            public string VoiceStatusNotInitialized => TazLang.Get("options_tazuo_voicestatusnotinitialized");
            public string VoiceStatusListening => TazLang.Get("options_tazuo_voicestatuslistening");
            public string VoiceApplyModel => TazLang.Get("options_tazuo_voiceapplymodel");
            public string VoiceCreateMacro => TazLang.Get("options_tazuo_voicecreatemacro");
            #endregion

            #region VisibileLayers
            public string VisibleLayers => TazLang.Get("options_tazuo_visiblelayers");
            public string VisLayersInfo => TazLang.Get("options_tazuo_vislayersinfo");
            public string OnlyForYourself => TazLang.Get("options_tazuo_onlyforyourself");
            public string HiddenLayersEnabled => TazLang.Get("options_tazuo_hiddenlayersenabled");
            #endregion
        }
    }

    public class AssistantLanguage
    {
        public string VisualConfig => TazLang.Get("assistant_visualconfig");
        public string DelayConfig => TazLang.Get("assistant_delayconfig");
        public string CameraSmoothing => TazLang.Get("assistant_camerasmoothing");
        public string CameraSmoothingTooltip => TazLang.Get("assistant_camerasmoothingtooltip");
        public string HighlightGameObjects => TazLang.Get("assistant_highlightgameobjects");
        public string ShowNameplates => TazLang.Get("assistant_shownameplates");
        public string PetScaling => TazLang.Get("assistant_petscaling");
        public string PetScalingTooltip => TazLang.Get("assistant_petscalingtooltip");
        public string OutlineMobiles => TazLang.Get("assistant_outlinemobiles");
        public string MinGumpDragDist => TazLang.Get("assistant_mingumpdragdist");
        public string MinGumpDragDistTooltip => TazLang.Get("assistant_mingumpdragdisttooltip");
        public string GameScale => TazLang.Get("assistant_gamescale");
        public string GameScaleTooltip => TazLang.Get("assistant_gamescaletooltip");
        public string TurnDelay => TazLang.Get("assistant_turndelay");
        public string ObjectDelay => TazLang.Get("assistant_objectdelay");
        public string AutoDelayChecker => TazLang.Get("assistant_autodelaychecker");
        public string AutoDelayCheckerTooltip => TazLang.Get("assistant_autodelaycheckertooltip");
        public string Misc => TazLang.Get("assistant_misc");
        public string QueueItemMoves => TazLang.Get("assistant_queueitemmoves");
        public string QueueItemMovesTooltip => TazLang.Get("assistant_queueitemmovestooltip");
        public string QueueObjectUses => TazLang.Get("assistant_queueobjectuses");
        public string QueueObjectUsesTooltip => TazLang.Get("assistant_queueobjectusestooltip");
        public string AutoOpenOwnCorpse => TazLang.Get("assistant_autoopenowncorpse");
        public string AutoOpenOwnCorpseTooltip => TazLang.Get("assistant_autoopenowncorpsetooltip");
        public string AutoUnequipForActions => TazLang.Get("assistant_autounequipforactions");
        public string AutoUnequipForActionsTooltip => TazLang.Get("assistant_autounequipforactionstooltip");
        public string DisableWeather => TazLang.Get("assistant_disableweather");
        public string DisableWeatherTooltip => TazLang.Get("assistant_disableweathertooltip");
        public string SetQuickHealSpell => TazLang.Get("assistant_setquickhealspell");
        public string SetQuickCureSpell => TazLang.Get("assistant_setquickcurespell");
        public string QuickSpellTooltip => TazLang.Get("assistant_quickspelltooltip");
        public string SingleClickLastTarg => TazLang.Get("assistant_singleclicklasttarg");
    }
}
