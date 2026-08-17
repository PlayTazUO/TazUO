using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Common.Enums;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using static ClassicUO.Configuration.ProfileManager;

namespace ClassicUO.Game.UI.Gumps
{
    [Obsolete("Use OptionsWindow instead. This will be removed in the future.")]
    public class ModernOptionsGump : BaseOptionsGump
    {
        private List<SettingsOption> _options = new List<SettingsOption>();
        private Profile profile;

        private string[] GetNamePlatePresetOptions() => new[]
        {
            TazLang.Get("nameplate_preset_custom", "Custom"),
            TazLang.Get("nameplate_preset_orion", "Orion"),
            TazLang.Get("nameplate_preset_wow_blockybars", "WoW - Blocky Bars"),
            TazLang.Get("nameplate_preset_wow_cleanhealth", "WoW - Clean Health"),
            TazLang.Get("nameplate_preset_wow_blockycast", "WoW - Blocky Cast"),
            TazLang.Get("nameplate_preset_wow_redname", "WoW - Red Name")
        };

        private string[] GetNamePlateBackgroundModeOptions() => new[]
        {
            TazLang.Get("nameplate_background_fixedcolor", "Fixed color"),
            TazLang.Get("nameplate_background_notorietycolor", "Notoriety color")
        };

        private string[] GetNamePlateHealthBarModeOptions() => new[]
        {
            TazLang.Get("nameplate_health_statuscolor", "Status color"),
            TazLang.Get("nameplate_health_green", "Green"),
            TazLang.Get("nameplate_health_blue", "Blue"),
            TazLang.Get("nameplate_health_red", "Red"),
            TazLang.Get("nameplate_health_cyan", "Cyan"),
            TazLang.Get("nameplate_health_yellow", "Yellow"),
            TazLang.Get("nameplate_health_orange", "Orange"),
            TazLang.Get("nameplate_health_purple", "Purple"),
            TazLang.Get("nameplate_health_white", "White"),
            TazLang.Get("nameplate_health_gray", "Gray"),
            TazLang.Get("nameplate_health_black", "Black")
        };

        public ModernOptionsGump(World world) : base(world, 900, 700,
            TazLang.Get("mog_optionstitle"))
        {
            profile = CurrentProfile;

            CenterXInScreen();
            CenterYInScreen();

            Build();
        }

        private void Build()
        {
            ModernButton b;
            MainContent.AddToLeft(b = CategoryButton(TazLang.Get("mog_buttongeneral"), (int)PAGE.General, MainContent.LeftWidth));
            b.IsSelected = true;
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttonsound"), (int)PAGE.Sound, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttonvideo"), (int)PAGE.Video, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttonmacros"), (int)PAGE.Macros, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttontooltips"), (int)PAGE.Tooltip, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttonspeech"), (int)PAGE.Speech, MainContent.LeftWidth));
            MainContent.AddToLeft(
                CategoryButton(TazLang.Get("mog_buttoncombatspells"), (int)PAGE.CombatSpells, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttoncounters"), (int)PAGE.Counters, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttoninfobar"), (int)PAGE.InfoBar, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttoncontainers"), (int)PAGE.Containers, MainContent.LeftWidth));
            MainContent.AddToLeft(
                CategoryButton(TazLang.Get("mog_buttonexperimental"), (int)PAGE.Experimental, MainContent.LeftWidth));

            MainContent.AddToLeft
            (
                b = new ModernButton(0, 0, MainContent.LeftWidth, 40, ButtonAction.Activate, TazLang.Get("mog_buttonignorelist"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = 999 }
            );

            b.MouseUp += (s, e) =>
            {
                UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                UIManager.Add(new IgnoreManagerGump(World));
            };

            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttonnameplates"), (int)PAGE.NameplateOptions,
                MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttoncooldowns"), (int)PAGE.TUOCooldowns, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(TazLang.Get("mog_buttontazuo"), (int)PAGE.TUOOptions, MainContent.LeftWidth));

            BuildGeneral();
            BuildSound();
            BuildVideo();
            BuildMacros();
            BuildTooltips();
            BuildSpeech();
            BuildCombatSpells();
            BuildCounters();
            BuildInfoBar();
            BuildContainers();
            BuildExperimental();
            BuildNameplates();
            BuildCooldowns();
            BuildTazUO();

            foreach (SettingsOption option in _options)
            {
                MainContent.AddToRight(option.FullControl, false, (int)option.OptionsPage);
            }

            ChangePage((int)PAGE.General);
        }

        private void BuildGeneral()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General

            page = ((int)PAGE.General + 1000);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttongeneral"), page, content.LeftWidth));

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_highlightobjects"), isChecked: profile.HighlightGameObjects,
                    valueChanged: (b) => { profile.HighlightGameObjects = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_pathfinding"), isChecked: profile.EnablePathfind, valueChanged: (b) => { profile.EnablePathfind = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_shiftpathfinding"), isChecked: profile.UseShiftToPathfind,
                    valueChanged: (b) => { profile.UseShiftToPathfind = b; }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_singleclickpathfind"), isChecked: profile.PathfindSingleClick,
                    valueChanged: (b) => { profile.PathfindSingleClick = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_alwaysrun"), isChecked: profile.AlwaysRun,
                    valueChanged: (b) => { profile.AlwaysRun = b; }), true, page);
            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_rununlesshidden"), isChecked: profile.AlwaysRunUnlessHidden,
                    valueChanged: (b) => { profile.AlwaysRunUnlessHidden = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_autoopendoors"), isChecked: profile.AutoOpenDoors, valueChanged: (b) => { profile.AutoOpenDoors = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_autoopenpathfinding"), isChecked: profile.SmoothDoors, valueChanged: (b) => { profile.SmoothDoors = b; }),
                true, page);

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("auto_open_doors_hidden"), isChecked: profile.AutoOpenDoorsIfHidden, valueChanged: (b) => { profile.AutoOpenDoorsIfHidden = b; }),
                true, page);

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_autoclosedoors"), isChecked: ProfileManager.GlobalSettings.AutoCloseDoors, valueChanged: (b) => { ProfileManager.GlobalSettings.AutoCloseDoors = b; }),
                true, page);

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_movementtab_doors_blockdoormovement"), isChecked: profile.BlockDoorMovement, valueChanged: (b) => { profile.BlockDoorMovement = b; }),
                true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_autoopencorpse"), isChecked: profile.AutoOpenCorpses, valueChanged: (b) => { profile.AutoOpenCorpses = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_general_corpseopendistance"), 0, ThemeSettings.SLIDER_WIDTH, 0, 5,
                    profile.AutoOpenCorpseRange, (r) => { profile.AutoOpenCorpseRange = r; }), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World, TazLang.Get("mog_general_corpseopenoptions"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_corpseoptnone"), TazLang.Get("mog_general_corpseoptnottarg"),
                        TazLang.Get("mog_general_corpseoptnothiding"), TazLang.Get("mog_general_corpseoptboth")
                    },
                    profile.CorpseOpenOptions, (s, n) => { profile.CorpseOpenOptions = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_outrangecolor"), isChecked: profile.NoColorObjectsOutOfRange,
                    valueChanged: (b) => { profile.NoColorObjectsOutOfRange = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("disable_gargoyle_flying_animation", "Disable gargoyle flying animation"), isChecked: profile.DisableGargoyleFlyingAnimation,
                    valueChanged: (b) => { profile.DisableGargoyleFlyingAnimation = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (c = new CheckboxWithLabel(TazLang.Get("mog_general_salloseasygrab"), isChecked: profile.SallosEasyGrab, valueChanged: (b) => { profile.SallosEasyGrab = b; }),
                true, page);

            c.SetTooltip(TazLang.Get("mog_general_sallostooltip"));

            if (Client.Game.UO.Version > ClientVersion.CV_70796)
            {
                content.BlankLine();

                content.AddToRight
                (
                    new CheckboxWithLabel(TazLang.Get("mog_general_showhousecontent"), isChecked: profile.ShowHouseContent,
                        valueChanged: (b) => { profile.ShowHouseContent = b; }), true, page
                );
            }

            if (Client.Game.UO.Version >= ClientVersion.CV_7090)
            {
                content.BlankLine();

                content.AddToRight
                (
                    new CheckboxWithLabel(TazLang.Get("mog_general_smoothboat"), isChecked: profile.UseSmoothBoatMovement,
                        valueChanged: (b) => { profile.UseSmoothBoatMovement = b; }), true,
                    page
                );
            }

            content.BlankLine();

            #endregion

            #region Mobiles

            page = ((int)PAGE.General + 1001);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonmobiles"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_showmobilehp"), isChecked: profile.ShowMobilesHP, valueChanged: (b) => { profile.ShowMobilesHP = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_mobilehptype"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { TazLang.Get("mog_general_hptypeperc"), TazLang.Get("mog_general_hptypebar"), TazLang.Get("mog_general_hptypenboth") },
                    profile.MobileHPType,
                    (s, n) => { profile.MobileHPType = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_hpshowwhen"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_hpshowwhen_always"), TazLang.Get("mog_general_hpshowwhen_less100"),
                        TazLang.Get("mog_general_hpshowwhen_smart")
                    }, profile.MobileHPShowWhen,
                    (s, n) => { profile.MobileHPShowWhen = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_highlightpoisoned"), isChecked: profile.HighlightMobilesByPoisoned,
                    valueChanged: (b) => { profile.HighlightMobilesByPoisoned = b; }), true, page
            );

            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_general_poisonhighlightcolor"), profile.PoisonHue,
                    (h) => { profile.PoisonHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_highlightpara"), isChecked: profile.HighlightMobilesByParalize,
                    valueChanged: (b) => { profile.HighlightMobilesByParalize = b; }), true, page
            );

            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_general_parahighlightcolor"), profile.ParalyzedHue,
                    (h) => { profile.ParalyzedHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_highlightinvul"), isChecked: profile.HighlightMobilesByInvul,
                    valueChanged: (b) => { profile.HighlightMobilesByInvul = b; }),
                true, page
            );

            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_general_invulhighlightcolor"), profile.InvulnerableHue,
                    (h) => { profile.InvulnerableHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_incomingmobiles"), isChecked: profile.ShowNewMobileNameIncoming,
                    valueChanged: (b) => { profile.ShowNewMobileNameIncoming = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_incomingcorpses"), isChecked: profile.ShowNewCorpseNameIncoming,
                    valueChanged: (b) => { profile.ShowNewCorpseNameIncoming = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_auraunderfeet"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_auraoptdisabled"), TazLang.Get("mog_general_aurooptwarmode"),
                        TazLang.Get("mog_general_auraoptctrlshift"), TazLang.Get("mog_general_auraoptalways")
                    },
                    profile.AuraUnderFeetType, (s, n) => { profile.AuraUnderFeetType = s; }
                ), true, page
            );

            content.Indent();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_auraforparty"), isChecked: profile.PartyAura,
                    valueChanged: (b) => { profile.PartyAura = b; }), true, page);
            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_general_aurapartycolor"), profile.PartyAuraHue,
                    (h) => { profile.PartyAuraHue = h; }), true, page);
            content.RemoveIndent();
            content.RemoveIndent();

            #endregion

            #region Gumps & Context

            page = ((int)PAGE.General + 1002);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttongumpcontext"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_disabletopmenu"), isChecked: profile.TopbarGumpIsDisabled,
                    valueChanged: (b) => { profile.TopbarGumpIsDisabled = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_altforanchorsgumps"), isChecked: profile.HoldDownKeyAltToCloseAnchored,
                    valueChanged: (b) => { profile.HoldDownKeyAltToCloseAnchored = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_alttomovegumps"), isChecked: profile.HoldAltToMoveGumps,
                    valueChanged: (b) => { profile.HoldAltToMoveGumps = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_general_closeentireanchorwithrclick"),
                    isChecked: profile.CloseAllAnchoredGumpsInGroupWithRightClick,
                    valueChanged: (b) => { profile.CloseAllAnchoredGumpsInGroupWithRightClick = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_originalskillsgump"), isChecked: profile.StandardSkillsGump,
                    valueChanged: (b) => { profile.StandardSkillsGump = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_oldstatusgump"), isChecked: profile.UseOldStatusGump, valueChanged: (b) => { profile.UseOldStatusGump = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_partyinvitegump"), isChecked: profile.PartyInviteGump, valueChanged: (b) => { profile.PartyInviteGump = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_modernhealthbars"), isChecked: profile.CustomBarsToggled,
                    valueChanged: (b) => { profile.CustomBarsToggled = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_modernhpblackbg"), isChecked: profile.CBBlackBGToggled, valueChanged: (b) => { profile.CBBlackBGToggled = b; }),
                true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_savehpbars"), isChecked: profile.SaveHealthbars, valueChanged: (b) => { profile.SaveHealthbars = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_closehpgumpswhen"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_closehpoptdisable"), TazLang.Get("mog_general_closehpoptoor"),
                        TazLang.Get("mog_general_closehpoptdead"), TazLang.Get("mog_general_closehpoptboth")
                    },
                    profile.CloseHealthBarType, (s, n) => { profile.CloseHealthBarType = s; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_tazuo_corpsecontainerstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_tazuo_corpsestyleopt_grid"), TazLang.Get("mog_tazuo_corpsestyleopt_original"),
                        TazLang.Get("mog_tazuo_corpsestyleopt_oldgridloot"), TazLang.Get("mog_tazuo_corpsestyleopt_oldgridlootandcontainer")
                    }, (int)profile.CorpseContainerStyle,
                    (s, n) => { profile.CorpseContainerStyle = (CorpseContainerStyle)s; }
                ), true, page
            );

            c.SetTooltip(TazLang.Get("mog_tazuo_tooltipcorpsecontainerstyle"));

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_shiftcontext"), isChecked: profile.HoldShiftForContext,
                    valueChanged: (b) => { profile.HoldShiftForContext = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_shiftsplit"), isChecked: profile.HoldShiftToSplitStack,
                    valueChanged: (b) => { profile.HoldShiftToSplitStack = b; }), true, page
            );

            #endregion

            #region Misc

            page = ((int)PAGE.General + 1003);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonmisc"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_enablecot"), isChecked: ProfileManager.GlobalSettings.UseCircleOfTransparency,
                    valueChanged: (b) => { ProfileManager.GlobalSettings.UseCircleOfTransparency = b; }), true,
                page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_general_cotdistance"), 0, ThemeSettings.SLIDER_WIDTH,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    ProfileManager.GlobalSettings.CircleOfTransparencyRadius, (r) => { ProfileManager.GlobalSettings.CircleOfTransparencyRadius = r; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_cottype"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_cottypeoptfull"), TazLang.Get("mog_general_cottypeoptgrad"),
                        TazLang.Get("mog_general_cottypeoptmodern")
                    }, ProfileManager.GlobalSettings.CircleOfTransparencyType,
                    (s, n) => { ProfileManager.GlobalSettings.CircleOfTransparencyType = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_hidescreenshotmessage"), isChecked: profile.HideScreenshotStoredInMessage,
                    valueChanged: (b) => { profile.HideScreenshotStoredInMessage = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_objfade"), isChecked: profile.UseObjectsFading, valueChanged: (b) => { profile.UseObjectsFading = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_textfade"), isChecked: profile.TextFading,
                    valueChanged: (b) => { profile.TextFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_cursorrange"), isChecked: profile.ShowTargetRangeIndicator,
                    valueChanged: (b) => { profile.ShowTargetRangeIndicator = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_dragselecthp"), isChecked: profile.EnableDragSelect, valueChanged: (b) => { profile.EnableDragSelect = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_dragkeymod"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_sharednone"), TazLang.Get("mog_general_sharedctrl"), TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    }, profile.DragSelectModifierKey,
                    (s, n) => { profile.DragSelectModifierKey = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_dragplayersonly"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_sharednone"), TazLang.Get("mog_general_sharedctrl"), TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    },
                    profile.DragSelect_PlayersModifier, (s, n) => { profile.DragSelect_PlayersModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_dragmobsonly"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_sharednone"), TazLang.Get("mog_general_sharedctrl"), TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    },
                    profile.DragSelect_MonstersModifier, (s, n) => { profile.DragSelect_MonstersModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_dragnameplatesonly"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_sharednone"), TazLang.Get("mog_general_sharedctrl"), TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    },
                    profile.DragSelect_NameplateModifier, (s, n) => { profile.DragSelect_NameplateModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_general_dragx"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width,
                    profile.DragSelectStartX,
                    (r) => { profile.DragSelectStartX = r; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_general_dragy"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width,
                    profile.DragSelectStartY,
                    (r) => { profile.DragSelectStartY = r; }
                ), true, page
            );

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_draganchored"), isChecked: profile.DragSelectAsAnchor, valueChanged: (b) => { profile.DragSelectAsAnchor = b; }),
                true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_showstatschangedmsg"), isChecked: profile.ShowStatsChangedMessage,
                    valueChanged: (b) => { profile.ShowStatsChangedMessage = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_general_showskillschangedmsg"), isChecked: profile.ShowSkillsChangedMessage,
                    valueChanged: (b) => { profile.ShowSkillsChangedMessage = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_general_changevolume"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                    profile.ShowSkillsChangedDeltaValue, (r) => { profile.ShowSkillsChangedDeltaValue = r; }),
                true, page
            );

            content.RemoveIndent();

            #endregion

            #region Terrain and statics

            page = ((int)PAGE.General + 1004);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonterrainstatics"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_hideroof"), isChecked: !profile.DrawRoofs,
                    valueChanged: (b) => { profile.DrawRoofs = !b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_treestostump"), isChecked: profile.TreeToStumps, valueChanged: (b) => { profile.TreeToStumps = b; }),
                true, page);

            content.Indent();
            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_treestostumpradius"), isChecked: profile.TreeToStumpsWithinRadius, valueChanged: (b) => { profile.TreeToStumpsWithinRadius = b; }),
                true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_general_hidevegetation"), isChecked: profile.HideVegetation, valueChanged: (b) => { profile.HideVegetation = b; }),
                true, page);

            //content.BlankLine();

            //content.AddToRight(new CheckboxWithLabel("Mark cave tiles", isChecked: profile.EnableCaveBorder, valueChanged: (b) => { profile.EnableCaveBorder = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_magicfieldtype"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_general_magicfieldopt_normal"), TazLang.Get("mog_general_magicfieldopt_static"),
                        TazLang.Get("mog_general_magicfieldopt_tile")
                    }, profile.FieldsType,
                    (s, n) => { profile.FieldsType = s; }
                ), true, page
            );

            #endregion

            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.General));
        }

        private void BuildSound()
        {
            PositionHelper.Reset();

            var scroll = new ScrollArea(0, 0, MainContent.RightWidth, MainContent.Height)
            {
                CanMove = true,
                AcceptMouseInput = true
            };
            _options.Add(new SettingsOption("", scroll, MainContent.RightWidth, (int)PAGE.Sound));

            Control c;

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_enablesound"), 0, ProfileManager.GlobalSettings.EnableSound,
                (b) => { ProfileManager.GlobalSettings.EnableSound = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.Indent();

            scroll.Add(c = new SliderWithLabel(TazLang.Get("mog_sound_sharedvolume"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                ProfileManager.GlobalSettings.SoundVolume, (i) => { ProfileManager.GlobalSettings.SoundVolume = i; }));
            PositionHelper.PositionControl(c);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_enablemusic"), 0, ProfileManager.GlobalSettings.EnableMusic,
                (b) => { ProfileManager.GlobalSettings.EnableMusic = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.Indent();

            scroll.Add(c = new SliderWithLabel(TazLang.Get("mog_sound_sharedvolume"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                ProfileManager.GlobalSettings.MusicVolume, (i) => { ProfileManager.GlobalSettings.MusicVolume = i; }));
            PositionHelper.PositionControl(c);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_loginmusic"), 0, Settings.GlobalSettings.LoginMusic,
                (b) => { Settings.GlobalSettings.LoginMusic = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.Indent();

            scroll.Add(c = new SliderWithLabel(
                TazLang.Get("mog_sound_sharedvolume"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                Settings.GlobalSettings.LoginMusicVolume,
                (i) => { Settings.GlobalSettings.LoginMusicVolume = i; }));
            PositionHelper.PositionControl(c);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_playfootsteps"), 0, ProfileManager.GlobalSettings.EnableFootstepsSound,
                (b) => { ProfileManager.GlobalSettings.EnableFootstepsSound = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("sound_play_rain", "Play rain sound"), 0, ProfileManager.GlobalSettings.EnableRainSound,
                (b) => { ProfileManager.GlobalSettings.EnableRainSound = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_combatmusic"), 0, ProfileManager.GlobalSettings.EnableCombatMusic,
                (b) => { ProfileManager.GlobalSettings.EnableCombatMusic = b; }));
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_sound_backgroundmusic"), 0, ProfileManager.GlobalSettings.ReproduceSoundsInBackground,
                (b) => { ProfileManager.GlobalSettings.ReproduceSoundsInBackground = b; }));
            PositionHelper.PositionControl(c);

            BuildVoiceRecognition(scroll);
        }

        private void BuildVoiceRecognition(ScrollArea scroll)
        {
            VoiceRecognitionManager voiceManager = VoiceRecognitionManager.Instance;

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            Control c;

            scroll.Add(c = TextBox.GetOne(TazLang.Get("mog_tazuo_voicerecognition"), ThemeSettings.FONT,
                ThemeSettings.STANDARD_TEXT_SIZE + 2, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            var createMacroBtn = new ModernButton(0, 0, 160, 30, ButtonAction.Activate, TazLang.Get("mog_tazuo_voicecreatemacro"),
                ThemeSettings.BUTTON_FONT_COLOR);
            createMacroBtn.MouseUp += (sender, e) =>
            {
                var macroManager = MacroManager.TryGetMacroManager(World);
                if (macroManager == null) return;
                var macro = Macro.CreateFastMacro("Toggle Voice", MacroType.ToggleVoiceRecognition,
                    MacroSubType.MSC_NONE);
                macroManager.PushToBack(macro);
                UIManager.Add(new MacroButtonGump(World, macro, Mouse.Position.X, Mouse.Position.Y));
            };
            scroll.Add(c = createMacroBtn);
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            var modelPathInput = new InputFieldWithLabel(TazLang.Get("mog_tazuo_voicemodelpath"), 300, profile.VoiceModelPath,
                onTextChange: (sender, e) =>
                {
                    profile.VoiceModelPath = ((InputField.StbTextBox)sender).Text;
                });
            modelPathInput.SetTooltip(TazLang.Get("mog_tazuo_voicemodelpathtooltip"));
            scroll.Add(c = modelPathInput);
            PositionHelper.PositionControl(c);
            PositionHelper.BlankLine();

            var applyBtn = new ModernButton(0, 0, 160, 30, ButtonAction.Activate, TazLang.Get("mog_tazuo_voiceapplymodel"),
                ThemeSettings.BUTTON_FONT_COLOR);
            applyBtn.MouseUp += (sender, e) =>
            {
                voiceManager.Reinitialize();
            };
            scroll.Add(c = applyBtn);
            PositionHelper.PositionControl(c);
        }

        private void BuildVideo()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 0.3));

            #region Game window

            int page = ((int)PAGE.Video + 1000);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttongamewindow"), page, content.LeftWidth));

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_fpscap"), 0, ThemeSettings.SLIDER_WIDTH, Constants.MIN_FPS, Constants.MAX_FPS,
                    Settings.GlobalSettings.FPS, (r) =>
                    {
                        Settings.GlobalSettings.FPS = r;
                        Client.Game.SetRefreshRate(r);
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_video_backgroundfps"), isChecked: profile.ReduceFPSWhenInactive,
                    valueChanged: (b) => { profile.ReduceFPSWhenInactive = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_video_enablevsync"), isChecked: profile.EnableVSync, valueChanged: (b) =>
                {
                    profile.EnableVSync = b;
                    Client.Game?.SetVSync(b);
                }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_video_fullsizeviewport"), isChecked: profile.GameWindowFullSize, valueChanged: (b) =>
                    {
                        profile.GameWindowFullSize = b;

                        WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();
                        if (viewport != null)
                        {
                            if (b)
                            {
                                viewport.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width,
                                    Client.Game.Window.ClientBounds.Height));
                                viewport.SetGameWindowPosition(new Point(0, 0));
                                profile.GameWindowPosition = new Point(0, 0);
                            }
                            else
                            {
                                viewport.ResizeGameWindow(new Point(600, 480));
                                viewport.SetGameWindowPosition(new Point(25, 25));
                                profile.GameWindowPosition = new Point(25, 25);
                            }

                            // Trigger a full update to ensure borders and positioning are correct
                            viewport.OnWindowResized();
                        }
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_video_fullscreen"), isChecked: profile.WindowBorderless, valueChanged: (b) =>
                    {
                        profile.WindowBorderless = b;
                        Client.Game.SetWindowBorderless(b);
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("video_borderless_window", "Borderless window (no title bar)"), isChecked: profile.BorderlessWindow, valueChanged: (b) =>
                    {
                        profile.BorderlessWindow = b;
                        if (!profile.WindowBorderless)
                            Client.Game.SetWindowBordered(!b);
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_lockviewport"), isChecked: profile.GameWindowLock, valueChanged: (b) => { profile.GameWindowLock = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_viewportx"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width,
                    profile.GameWindowPosition.X, (r) =>
                    {
                        profile.GameWindowPosition = new Point(r, profile.GameWindowPosition.Y);
                        UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(profile.GameWindowPosition);
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_viewporty"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height,
                    profile.GameWindowPosition.Y, (r) =>
                    {
                        profile.GameWindowPosition = new Point(profile.GameWindowPosition.X, r);
                        UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(profile.GameWindowPosition);
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_viewportw"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width,
                    profile.GameWindowSize.X, (r) =>
                    {
                        profile.GameWindowSize = new Point(r, profile.GameWindowSize.Y);
                        UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(profile.GameWindowSize);
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_viewporth"), 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height,
                    profile.GameWindowSize.Y, (r) =>
                    {
                        profile.GameWindowSize = new Point(profile.GameWindowSize.X, r);
                        UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(profile.GameWindowSize);
                    }
                ), true, page
            );

            #endregion

            #region Zoom

            page = ((int)PAGE.Video + 1001);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonzoom"), page, content.LeftWidth));
            content.ResetRightSide();

            int cameraZoomCount = (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin) /
                                        Client.Game.Scene.Camera.ZoomStep);
            int cameraZoomIndex = cameraZoomCount -
                                  (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom) /
                                        Client.Game.Scene.Camera.ZoomStep);

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_defaultzoom"), 0, ThemeSettings.SLIDER_WIDTH, 0, cameraZoomCount, cameraZoomIndex,
                    (r) =>
                    {
                        profile.DefaultScale = Client.Game.Scene.Camera.Zoom =
                            (r * Client.Game.Scene.Camera.ZoomStep) + Client.Game.Scene.Camera.ZoomMin;
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_video_zoomwheel"), isChecked: profile.EnableMousewheelScaleZoom,
                    valueChanged: (b) => { profile.EnableMousewheelScaleZoom = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_video_returndefaultzoom"), isChecked: profile.RestoreScaleAfterUnpressCtrl,
                    valueChanged: (b) => { profile.RestoreScaleAfterUnpressCtrl = b; }), true,
                page
            );

            #endregion

            #region Lighting

            page = ((int)PAGE.Video + 1002);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonlighting"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_altlights"), isChecked: profile.UseAlternativeLights, valueChanged: (b) => { profile.UseAlternativeLights = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_video_customllevel"), isChecked: profile.UseCustomLightLevel, valueChanged: (b) =>
                    {
                        profile.UseCustomLightLevel = b;

                        if (b)
                        {
                            World.Light.Overall = profile.LightLevelType == 1
                                ? Math.Min(World.Light.RealOverall, profile.LightLevel)
                                : profile.LightLevel;
                            World.Light.Personal = 0;
                        }
                        else
                        {
                            World.Light.Overall = World.Light.RealOverall;
                            World.Light.Personal = World.Light.RealPersonal;
                        }
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_level"), 0, ThemeSettings.SLIDER_WIDTH, 0, 0x1E, 0x1E - profile.LightLevel, (r) =>
                    {
                        profile.LightLevel = (byte)(0x1E - r);

                        if (profile.UseCustomLightLevel)
                        {
                            World.Light.Overall = profile.LightLevelType == 1
                                ? Math.Min(World.Light.RealOverall, profile.LightLevel)
                                : profile.LightLevel;
                            World.Light.Personal = 0;
                        }
                        else
                        {
                            World.Light.Overall = World.Light.RealOverall;
                            World.Light.Personal = World.Light.RealPersonal;
                        }
                    }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_video_lighttype"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { TazLang.Get("mog_video_lighttype_absolute"), TazLang.Get("mog_video_lighttype_minimum") },
                    profile.LightLevelType, (s, n) => { profile.LightLevelType = s; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_video_darknight"), isChecked: profile.UseDarkNights,
                    valueChanged: (b) => { profile.UseDarkNights = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_coloredlight"), isChecked: profile.UseColoredLights, valueChanged: (b) => { profile.UseColoredLights = b; }),
                true, page);

            #endregion

            #region Misc

            page = ((int)PAGE.Video + 1003);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonmisc"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_enabledeathscreen"), isChecked: profile.EnableDeathScreen, valueChanged: (b) => { profile.EnableDeathScreen = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_bwdead"), isChecked: profile.EnableBlackWhiteEffect, valueChanged: (b) => { profile.EnableBlackWhiteEffect = b; }),
                true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_video_mousethread"), isChecked: Settings.GlobalSettings.RunMouseInASeparateThread,
                    valueChanged: (b) => { Settings.GlobalSettings.RunMouseInASeparateThread = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_video_targetaura"), isChecked: profile.AuraOnMouse,
                    valueChanged: (b) => { profile.AuraOnMouse = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_animwater"), isChecked: profile.AnimatedWaterEffect, valueChanged: (b) => { profile.AnimatedWaterEffect = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(
                    TazLang.Get("enhanced_weather"),
                    isChecked: profile.EnableEnhancedWeather,
                    valueChanged: (b) =>
                    {
                        profile.EnableEnhancedWeather = b;
                        World.Instance?.SwitchWeather(b);
                    }), true, page);

            content.Indent();

            content.AddToRight(
                new CheckboxWithLabel(
                    TazLang.Get("enhanced_weather_particle_effects"),
                    isChecked: profile.EnableWeatherEffects,
                    valueChanged: (b) => { profile.EnableWeatherEffects = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable post processing effects", 0,
                profile.EnablePostProcessingEffects, (b) =>
                {
                    profile.EnablePostProcessingEffects = b;
                    Client.Game.GetScene<GameScene>()?.SetPostProcessingSettings();
                }), true, page);

            content.BlankLine();

            content.AddToRight(
                new ComboBoxWithLabel(
                    World,
                    "Processing type",
                    150,
                    ThemeSettings.COMBO_BOX_WIDTH,
                    ["point", "linear", "anisotropic", "xbr", "fsr"],
                    profile.PostProcessingType,
                    (s, n) =>
                    {
                        profile.PostProcessingType = (ushort)s;
                        Client.Game.GetScene<GameScene>()?.SetPostProcessingSettings();
                    }), true, page);
            ;

            #endregion

            #region Shadows

            page = ((int)PAGE.Video + 1004);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_buttonshadows"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_enableshadows"), isChecked: profile.ShadowsEnabled, valueChanged: (b) => { profile.ShadowsEnabled = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_video_rocktreeshadows"), isChecked: profile.ShadowsStatics, valueChanged: (b) => { profile.ShadowsStatics = b; }),
                true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_video_terrainshadowlevel"), 0, ThemeSettings.SLIDER_WIDTH,
                    Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL,
                    profile.TerrainShadowsLevel, (r) => { profile.TerrainShadowsLevel = r; }
                ), true, page
            );

            #endregion

            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.Video));
        }

        private void BuildMacros()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 0.3));
            int page = ((int)PAGE.Macros + 1000);
            int bParam = page + 1;

            #region New Macro

            ModernButton b;

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, TazLang.Get("mog_macros_newmacro"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (sender, e) =>
            {
                var dialog = new EntryDialog
                (
                    World, 250, 150, TazLang.Get("macro_name"), name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }

                        MacroManager manager = World.Macros;

                        if (manager.FindMacro(name) != null)
                        {
                            return;
                        }

                        ModernButton nb;

                        var macroControl = new MacroControl(World, name);

                        content.AddToLeft
                        (
                            nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, name,
                                ThemeSettings.BUTTON_FONT_COLOR)
                            {
                                ButtonParameter = bParam++, Tag = macroControl.Macro
                            }
                        );

                        content.ResetRightSide();
                        content.AddToRight(macroControl, true, nb.ButtonParameter);

                        nb.IsSelected = true;
                        content.ActivePage = nb.ButtonParameter;

                        manager.PushToBack(macroControl.Macro);

                        nb.DragBegin += (sss, eee) =>
                        {
                            var mupNiceButton = (ModernButton)sss;

                            var m = mupNiceButton.Tag as Macro;

                            if (m == null)
                            {
                                return;
                            }

                            if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == m)?.Dispose();

                            var macroButtonGump = new MacroButtonGump(World, m, Mouse.Position.X, Mouse.Position.Y);

                            macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                            macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                            UIManager.Add(macroButtonGump);

                            UIManager.AttemptDragControl(macroButtonGump, true);
                        };
                    }
                ) { CanCloseWithRightClick = true };

                UIManager.Add(dialog);
            };

            #endregion

            #region Delete Macro

            page = ((int)PAGE.Macros + 1001);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, TazLang.Get("mog_macros_delmacro"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    var dialog = new QuestionGump
                    (
                        World, TazLang.Get("macro_delete_confirmation"), b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (nb.Tag is Macro macro)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == macro)
                                    ?.Dispose();
                                World.Macros.Remove(macro);

                                foreach (Control c in content.RightArea.Children)
                                {
                                    if (c.Page == nb.ButtonParameter)
                                    {
                                        c.Dispose();
                                    }
                                }

                                nb.Dispose();
                                content.RepositionLeftMenuChildren();
                            }
                        }
                    );

                    UIManager.Add(dialog);
                }
            };

            #endregion

            #region Move Macro Up

            page = ((int)PAGE.Macros + 1003);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, "Move Up",
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null && nb.Tag is Macro macro)
                {
                    if (World.Macros.MoveMacroUp(macro))
                    {
                        RebuildMacroButtons(content, ref bParam);
                        World.Macros.Save();
                    }
                }
            };

            #endregion

            #region Move Macro Down

            page = ((int)PAGE.Macros + 1004);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, "Move Down",
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null && nb.Tag is Macro macro)
                {
                    if (World.Macros.MoveMacroDown(macro))
                    {
                        RebuildMacroButtons(content, ref bParam);
                        World.Macros.Save();
                    }
                }
            };

            #endregion

            content.AddToLeft(new Line(0, 0, content.LeftWidth, 1, Color.Gray.PackedValue));

            #region Macros

            page = ((int)PAGE.Macros + 1002);
            MacroManager macroManager = World.Macros;

            for (var macro = (Macro)macroManager.Items; macro != null; macro = (Macro)macro.Next)
            {
                content.AddToLeft
                (
                    b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, macro.Name,
                        ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = bParam++, Tag = macro }
                );

                content.ResetRightSide();
                content.AddToRight(new MacroControl(World, macro.Name), true, b.ButtonParameter);
            }

            b.IsSelected = true;
            content.ActivePage = b.ButtonParameter;

            #endregion

            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.Macros));
        }

        private void RebuildMacroButtons(LeftSideMenuRightSideContent content, ref int bParam)
        {
            Macro selectedMacro = null;
            ModernButton selectedButton =
                content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);
            if (selectedButton != null && selectedButton.Tag is Macro m)
            {
                selectedMacro = m;
            }

            var macroButtons = content.LeftArea.FindControls<ModernButton>().Where(btn => btn.Tag is Macro).ToList();
            foreach (ModernButton btn in macroButtons)
            {
                btn.Dispose();
            }

            bParam = ((int)PAGE.Macros + 1002);
            MacroManager macroManager = World.Macros;
            ModernButton lastButton = null;

            for (var macro = (Macro)macroManager.Items; macro != null; macro = (Macro)macro.Next)
            {
                var b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, macro.Name,
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = bParam++, Tag = macro };

                content.AddToLeft(b);

                MacroControl macroControl = content.RightArea.FindControls<MacroControl>()
                    .FirstOrDefault(mc => mc.Macro == macro);
                if (macroControl == null)
                {
                    content.ResetRightSide();
                    content.AddToRight(new MacroControl(World, macro.Name), true, b.ButtonParameter);
                }
                else
                {
                    macroControl.Page = b.ButtonParameter;
                }

                b.DragBegin += (sss, eee) =>
                {
                    var mupNiceButton = (ModernButton)sss;
                    var dragMacro = mupNiceButton.Tag as Macro;

                    if (dragMacro == null || UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == dragMacro)?.Dispose();
                    var macroButtonGump = new MacroButtonGump(World, dragMacro, Mouse.Position.X, Mouse.Position.Y);
                    macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                    macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);
                    UIManager.Add(macroButtonGump);
                    UIManager.AttemptDragControl(macroButtonGump, true);
                };

                if (macro == selectedMacro)
                {
                    b.IsSelected = true;
                    content.ActivePage = b.ButtonParameter;
                }

                lastButton = b;
            }

            if (selectedMacro == null && lastButton != null)
            {
                lastButton.IsSelected = true;
                content.ActivePage = lastButton.ButtonParameter;
            }

            content.RepositionLeftMenuChildren();
        }

        private void BuildInfoBar()
        {
            var content = new mainScrollArea(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 1.0));
            //int page = ((int)PAGE.InfoBar + 1000);

            #region Active Info Bar

            CheckboxWithLabel b;

            content.AddToLeft
            (
                b = new CheckboxWithLabel
                (
                    TazLang.Get("mog_infobars_showinfobar"), 0, profile.ShowInfoBar, (b) =>
                    {
                        profile.ShowInfoBar = b;
                        InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

                        if (b)
                        {
                            if (infoBarGump == null)
                            {
                                UIManager.Add
                                (
                                    new InfoBarGump(World) { X = 300, Y = 300 }
                                );
                            }
                            else
                            {
                                infoBarGump.ResetItems();
                                infoBarGump.SetInScreen();
                            }
                        }
                        else
                        {
                            infoBarGump?.Dispose();
                        }
                    }
                )
            );

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            #endregion

            #region Select type infobar

            ComboBoxWithLabel c;

            content.AddToLeft
            (
                c = new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_infobars_highlighttype"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { TazLang.Get("mog_infobars_highlightopt_textcolor"), TazLang.Get("mog_infobars_highlightopt_coloredbars") },
                    profile.InfoBarHighlightType,
                    (i, s) => { profile.InfoBarHighlightType = i; }
                )
            );

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            #endregion

            #region Select type infobar

            var infoBarItems = new DataBox(0, 0, 0, 0) { AcceptMouseInput = true };

            ModernButton addItem;

            content.AddToLeft
            (
                addItem = new ModernButton(0, 0, 150, 40, ButtonAction.Activate, TazLang.Get("mog_infobars_additem"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = -1, IsSelectable = true, IsSelected = true }
            );

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            addItem.MouseUp += (s, e) =>
            {
                InfoBarItem ibi;
                var ibbc = new InfoBarBuilderControl(World, ibi = new InfoBarItem("HP", InfoBarVars.HP, 0x3B9),
                    content);
                infoBarItems.Add(ibbc);
                infoBarItems.ReArrangeChildren();
                infoBarItems.ForceSizeUpdate();
                infoBarItems.Parent?.ForceSizeUpdate();
                World.InfoBars?.AddItem(ibi);
                UIManager.GetGump<InfoBarGump>()?.ResetItems();
                content.AddToLeft(ibbc);
                content.ForceSizeUpdate();
                int yOffset = 0;

                foreach (Control child in content.Children)
                {
                    if (child is ScrollArea scrollArea)
                    {
                        foreach (Control scrollChild in scrollArea.Children)
                        {
                            if (scrollChild is InfoBarBuilderControl control)
                            {
                                control.Y = yOffset + 170;
                                yOffset += control.Height;
                                content.ForceSizeUpdate();
                            }
                        }
                    }
                }

                content.ForceSizeUpdate();
            };

            content.BlankLine();

            content.AddToLeftText
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_infobars_label"), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(100).MouseInput()
                ), 0, 135
            );

            content.AddToLeftText
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_infobars_color"), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(100).MouseInput()
                ), 120, 135
            );

            content.AddToLeftText
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_infobars_data"), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.DefaultCentered(100).MouseInput()
                ), 180, 135
            );

            content.AddToLine(new Line(0, 10, content.LeftWidth, 1, Color.Gray.PackedValue), 0, 160);
            content.BlankLine();
            InfoBarManager ibmanager = World.InfoBars;
            List<InfoBarItem> _infoBarItems = ibmanager.GetInfoBars();

            for (int i = 0; i < _infoBarItems.Count; i++)
            {
                var ibbc = new InfoBarBuilderControl(World, _infoBarItems[i], content);
                infoBarItems.ReArrangeChildren();
                infoBarItems.ForceSizeUpdate();
                infoBarItems.Parent?.ForceSizeUpdate();
                int yOffset = 0;

                content.AddToLeft(ibbc);
                content.ForceSizeUpdate();

                foreach (Control child in content.Children)
                {
                    if (child is ScrollArea scrollArea)
                    {
                        // Iterar pelos filhos dentro de cada ScrollArea
                        foreach (Control scrollChild in scrollArea.Children)
                        {
                            if (scrollChild is InfoBarBuilderControl control)
                            {
                                control.Y = yOffset + 170;
                                yOffset += control.Height; // Ajuste o espaçamento conforme necessário
                                content.ForceSizeUpdate();
                            }
                        }

                        content.ForceSizeUpdate();
                    }
                }

                content.ForceSizeUpdate();
            }

            #endregion


            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.InfoBar));
        }

        private void BuildTooltips()
        {
            SettingsOption s;
            PositionHelper.Reset();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_tooltips_enabletooltips"), 0, profile.UseTooltip,
                        (b) => { profile.UseTooltip = b; }), MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel
                    (
                        TazLang.Get("mog_tooltips_tooltipdelay"), 0, ThemeSettings.SLIDER_WIDTH, 0, 1000,
                        profile.TooltipDelayBeforeDisplay, (i) => { profile.TooltipDelayBeforeDisplay = i; }
                    ), MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel
                    (TazLang.Get("mog_tooltips_tooltipbg"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                        profile.TooltipBackgroundOpacity, (i) => { profile.TooltipBackgroundOpacity = i; }),
                    MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_tooltips_tooltipfont"), profile.TooltipTextHue,
                        (h) => { profile.TooltipTextHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildSpeech()
        {
            SettingsOption s, ss;
            PositionHelper.Reset();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_scalespeechdelay"), 0, profile.ScaleSpeechDelay,
                        (b) => { profile.ScaleSpeechDelay = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel(TazLang.Get("mog_speech_speechdelay"), 0, ThemeSettings.SLIDER_WIDTH, 0, 1000,
                        profile.SpeechDelay, (i) => { profile.SpeechDelay = i; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_savejournale"), 0, profile.SaveJournalToFile,
                        (b) => { profile.SaveJournalToFile = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_chatenteractivation"), 0, profile.ActivateChatAfterEnter,
                        (b) => { profile.ActivateChatAfterEnter = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_chatenterspecial"), 0, profile.ActivateChatAdditionalButtons,
                        (b) => { profile.ActivateChatAdditionalButtons = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_shiftenterchat"), 0, profile.ActivateChatShiftEnterSupport,
                        (b) => { profile.ActivateChatShiftEnterSupport = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_chatgradient"), 0, profile.HideChatGradient,
                        (b) => { profile.HideChatGradient = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_hideguildchat"), 0, profile.IgnoreGuildMessages,
                        (b) => { profile.IgnoreGuildMessages = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_speech_hidealliancechat"), 0, profile.IgnoreAllianceMessages,
                        (b) => { profile.IgnoreAllianceMessages = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                ("",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_speechcolor"), profile.SpeechHue,
                        (h) => { profile.SpeechHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            _options.Add
            (
                s = new SettingsOption
                ("",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_yellcolor"), profile.YellHue,
                        (h) => { profile.YellHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_partycolor"), profile.PartyMessageHue,
                        (h) => { profile.PartyMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_alliancecolor"), profile.AllyMessageHue,
                        (h) => { profile.AllyMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            _options.Add
            (
                s = new SettingsOption
                ("",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_emotecolor"), profile.EmoteHue,
                        (h) => { profile.EmoteHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_whispercolor"), profile.WhisperHue,
                        (h) => { profile.WhisperHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_guildcolor"), profile.GuildMessageHue,
                        (h) => { profile.GuildMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ModernColorPickerWithLabel(World, TazLang.Get("mog_speech_charcolor"), profile.ChatMessageHue,
                        (h) => { profile.ChatMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);
        }

        private void BuildCombatSpells()
        {
            //SettingsOption s;
            PositionHelper.Reset();

            var scroll = new ScrollArea(0, 0, MainContent.RightWidth, MainContent.Height);
            _options.Add(new SettingsOption("", scroll, MainContent.RightWidth, (int)PAGE.CombatSpells));

            Control c;
            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_holdtabforcombat"), 0, profile.HoldDownKeyTab,
                (b) => { profile.HoldDownKeyTab = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add
            (c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_querybeforeattack"), 0,
                profile.EnabledCriminalActionQuery, (b) => { profile.EnabledCriminalActionQuery = b; }));

            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add
            (
                c = new CheckboxWithLabel
                (TazLang.Get("mog_combatspells_querybeforebeneficial"), 0, profile.EnabledBeneficialCriminalActionQuery,
                    (b) => { profile.EnabledBeneficialCriminalActionQuery = b; })
            );

            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_enableoverheadspellformat"), 0,
                profile.EnabledSpellFormat, (b) => { profile.EnabledSpellFormat = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_enableoverheadspellhue"), 0,
                profile.EnabledSpellHue, (b) => { profile.EnabledSpellHue = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_singleclickforspellicons"), 0,
                ProfileManager.GlobalSettings.SingleClickIconUse, (b) => { ProfileManager.GlobalSettings.SingleClickIconUse = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_showbuffdurationonoldstylebuffbar"), 0,
                profile.BuffBarTime, (b) => { profile.BuffBarTime = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_enablefastspellhotkeyassigning"), 0,
                profile.FastSpellsAssign, (b) => { profile.FastSpellsAssign = b; }));

            PositionHelper.PositionControl(c);
            c.SetTooltip(TazLang.Get("mog_combatspells_tooltipfastspellassign"));

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(TazLang.Get("mog_combatspells_enabledpscounter"), 0, profile.ShowDPS,
                (b) => { profile.ShowDPS = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_innocentcolor"),
                profile.InnocentHue, (h) => { profile.InnocentHue = h; }));

            PositionHelper.PositionControl(c);

            Control clast = c;
            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_beneficialspell"),
                profile.BeneficHue, (h) => { profile.BeneficHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_friendcolor"), profile.FriendHue,
                (h) => { profile.FriendHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_harmfulspell"), profile.HarmfulHue,
                (h) => { profile.HarmfulHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_criminal"), profile.CriminalHue,
                (h) => { profile.CriminalHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_neutralspell"), profile.NeutralHue,
                (h) => { profile.NeutralHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_canbeattackedhue"),
                profile.CanAttackHue, (h) => { profile.CanAttackHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_murderer"), profile.MurdererHue,
                (h) => { profile.MurdererHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_combatspells_enemy"), profile.EnemyHue,
                (h) => { profile.EnemyHue = h; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            InputFieldWithLabel spellFormat = spellFormat = new InputFieldWithLabel
            (
                TazLang.Get("mog_combatspells_spelloverheadformat"), 200, profile.SpellDisplayFormat,
                onTextChange: (s, e) => { profile.SpellDisplayFormat = ((InputField.StbTextBox)s).Text; }
            );

            scroll.Add(spellFormat);
            PositionHelper.PositionControl(spellFormat);
            spellFormat.SetTooltip(TazLang.Get("mog_combatspells_tooltipspellformat"));
        }

        private void BuildCounters()
        {
            SettingsOption s;
            PositionHelper.Reset();

            _options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel
                    (
                        TazLang.Get("mog_counters_enablecounters"), 0, profile.CounterBarEnabled, (b) =>
                        {
                            profile.CounterBarEnabled = b;
                            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

                            if (b)
                            {
                                if (counterGump != null)
                                {
                                    counterGump.IsEnabled = counterGump.IsVisible = b;
                                }
                                else
                                {
                                    UIManager.Add(counterGump = new CounterBarGump(World, 200, 200));
                                }
                            }
                            else
                            {
                                if (counterGump != null)
                                {
                                    counterGump.IsEnabled = counterGump.IsVisible = b;
                                }
                            }

                            counterGump?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows,
                                profile.CounterBarColumns);
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_counters_highlightitemsonuse"), 0, profile.CounterBarHighlightOnUse,
                        (b) => { profile.CounterBarHighlightOnUse = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (TazLang.Get("mog_counters_abbreviatedvalues"), 0, profile.CounterBarDisplayAbbreviatedAmount,
                        (b) => { profile.CounterBarDisplayAbbreviatedAmount = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    TazLang.Get("mog_counters_abbreviateifamountexceeds"), new InputField
                    (
                        100, 40, text: profile.CounterBarAbbreviatedAmount.ToString(), numbersOnly: true,
                        onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarAbbreviatedAmount = v;
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            PositionHelper.RemoveIndent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (TazLang.Get("mog_counters_highlightredwhenamountislow"), 0, profile.CounterBarHighlightOnAmount,
                        (b) => { profile.CounterBarHighlightOnAmount = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    TazLang.Get("mog_counters_highlightredifamountisbelow"), new InputField
                    (
                        100, 40, text: profile.CounterBarHighlightAmount.ToString(), numbersOnly: true,
                        onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarHighlightAmount = v;
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.RemoveIndent();

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            _options.Add(s = new SettingsOption(TazLang.Get("mog_counters_counterlayout"), new Area(false), MainContent.RightWidth,
                (int)PAGE.Counters));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel
                    (
                        TazLang.Get("mog_counters_gridsize"), 0, ThemeSettings.SLIDER_WIDTH, 30, 100, profile.CounterBarCellSize,
                        (v) =>
                        {
                            profile.CounterBarCellSize = v;
                            UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize,
                                profile.CounterBarRows, profile.CounterBarColumns);
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            _options.Add
            (
                s = new SettingsOption
                (
                    TazLang.Get("mog_counters_rows"), new InputField
                    (
                        100, 40, text: profile.CounterBarRows.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarRows = v;
                                UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize,
                                    profile.CounterBarRows, profile.CounterBarColumns);
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            SettingsOption ss = s;

            _options.Add
            (
                s = new SettingsOption
                (
                    TazLang.Get("mog_counters_columns"), new InputField
                    (
                        100, 40, text: profile.CounterBarColumns.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarColumns = v;
                                UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize,
                                    profile.CounterBarRows, profile.CounterBarColumns);
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }


        private void BuildContainers()
        {
            SettingsOption s;
            PositionHelper.Reset();

            _options.Add(s = new SettingsOption(TazLang.Get("mog_containers_description"), new Area(false), MainContent.RightWidth,
                (int)PAGE.Containers));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            if (Client.Game.UO.Version >= ClientVersion.CV_705301)
            {
                _options.Add
                (
                    s = new SettingsOption
                    (
                        "",
                        new ComboBoxWithLabel
                        (
                            World,
                            TazLang.Get("mog_containers_characterbackpackstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                            new string[]
                            {
                                TazLang.Get("mog_containers_backpackopt_default"), TazLang.Get("mog_containers_backpackopt_suede"),
                                TazLang.Get("mog_containers_backpackopt_polarbear"), TazLang.Get("mog_containers_backpackopt_ghoulskin")
                            }, profile.BackpackStyle, (i, s) => { profile.BackpackStyle = i; }
                        ), MainContent.RightWidth, (int)PAGE.Containers
                    )
                );

                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            _options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel
                    (
                        TazLang.Get("mog_containers_containerscale"), 0, ThemeSettings.SLIDER_WIDTH,
                        Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC,
                        profile.ContainersScale, (i) =>
                        {
                            profile.ContainersScale = (byte)i;
                            UIManager.ContainerScale = (byte)i / 100f;

                            UIManager.ForEach<ContainerGump>(c => c.RequestUpdateContents());
                        }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_containers_alsoscaleitems"), 0, profile.ScaleItemsInsideContainers,
                        (b) => { profile.ScaleItemsInsideContainers = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            if (Client.Game.UO.Version >= ClientVersion.CV_706000)
            {
                _options.Add
                (
                    s = new SettingsOption
                    (
                        "",
                        new CheckboxWithLabel(TazLang.Get("mog_containers_uselargecontainergumps"), 0,
                            profile.UseLargeContainerGumps, (b) => { profile.UseLargeContainerGumps = b; }),
                        MainContent.RightWidth, (int)PAGE.Containers
                    )
                );

                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (
                        TazLang.Get("mog_containers_doubleclicktolootitemsinsidecontainers"), 0,
                        profile.DoubleClickToLootInsideContainers,
                        (b) => { profile.DoubleClickToLootInsideContainers = b; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (TazLang.Get("mog_containers_relativedraganddropitemsincontainers"), 0, profile.RelativeDragAndDropItems,
                        (b) => { profile.RelativeDragAndDropItems = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (
                        TazLang.Get("mog_containers_highlightcontainerongroundwhenmouseisoveracontainergump"), 0,
                        profile.HighlightContainerWhenSelected,
                        (b) => { profile.HighlightContainerWhenSelected = b; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_containers_recolorcontainergumpbywithcontainerhue"), 0,
                        profile.HueContainerGumps, (b) => { profile.HueContainerGumps = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (TazLang.Get("mog_containers_overridecontainergumplocations"), 0, profile.OverrideContainerLocation,
                        (b) => { profile.OverrideContainerLocation = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ComboBoxWithLabel
                    (World,
                        TazLang.Get("mog_containers_overrideposition"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                        new string[]
                        {
                            TazLang.Get("mog_containers_positionopt_nearcontainer"), TazLang.Get("mog_containers_positionopt_topright"),
                            TazLang.Get("mog_containers_positionopt_lastdraggedposition"),
                            TazLang.Get("mog_containers_remembereachcontainer")
                        }, profile.OverrideContainerLocationSetting,
                        (i, s) => { profile.OverrideContainerLocationSetting = i; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            ModernButton rebuildContainers;

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    rebuildContainers =
                        new ModernButton(0, 0, 130, 40, ButtonAction.Activate, TazLang.Get("mog_containers_rebuildcontainerstxt"),
                            ThemeSettings.BUTTON_FONT_COLOR, 999) { IsSelected = true, IsSelectable = true },
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            rebuildContainers.MouseUp += (s, e) => { World.ContainerManager.BuildContainerFile(true); };
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildExperimental()
        {
            SettingsOption s;
            PositionHelper.Reset();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_experimental_disabledefaultuohotkeys"), 0,
                        profile.DisableDefaultHotkeys, (b) => { profile.DisableDefaultHotkeys = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_experimental_disablearrowsnumlockarrowsplayermovement"), 0,
                        profile.DisableArrowBtn, (b) => { profile.DisableArrowBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_experimental_disabletabtogglewarmode"), 0, profile.DisableTabBtn,
                        (b) => { profile.DisableTabBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_experimental_disablectrlqwmessagehistory"), 0, profile.DisableCtrlQWBtn,
                        (b) => { profile.DisableCtrlQWBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel(TazLang.Get("mog_experimental_disablerightleftclickautomove"), 0,
                        profile.DisableAutoMove, (b) => { profile.DisableAutoMove = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildNameplates()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 0.3));
            int page = ((int)PAGE.NameplateOptions + 1000);

            #region New entry

            ModernButton b;

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, TazLang.Get("mog_nameplates_newentry"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new
                (
                    World, 250, 150, TazLang.Get("mog_nameplates_nameoverheadentryname"), name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }

                        if (NameOverHeadManager.FindOption(name) != null)
                        {
                            return;
                        }

                        var option = new NameOverheadOption(name);

                        ModernButton nb;

                        content.AddToLeft
                        (
                            nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, name,
                                ThemeSettings.BUTTON_FONT_COLOR)
                            {
                                ButtonParameter = page + 1 + content.LeftArea.Children.Count, Tag = option
                            }
                        );

                        nb.IsSelected = true;
                        content.ActivePage = nb.ButtonParameter;
                        World.NameOverHeadManager.AddOption(option);

                        content.AddToRight(new NameOverheadAssignControl(World, option), false, nb.ButtonParameter);
                    }
                ) { CanCloseWithRightClick = true };

                UIManager.Add(dialog);
            };

            #endregion

            #region Delete entry

            page = ((int)PAGE.Macros + 1001);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, TazLang.Get("mog_nameplates_deleteentry"),
                    ThemeSettings.BUTTON_FONT_COLOR) { ButtonParameter = page, IsSelectable = false }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    var dialog = new QuestionGump
                    (
                        World, TazLang.Get("macro_delete_confirmation"), b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (nb.Tag is NameOverheadOption option)
                            {
                                World.NameOverHeadManager.RemoveOption(option);
                                nb.Dispose();
                            }
                        }
                    );

                    UIManager.Add(dialog);
                }
            };

            #endregion

            content.AddToLeft(new Line(0, 0, content.LeftWidth, 1, Color.Gray.PackedValue));

            List<NameOverheadOption> opts = NameOverHeadManager.GetAllOptions();
            ModernButton nb = null;

            for (int i = 0; i < opts.Count; i++)
            {
                NameOverheadOption option = opts[i];

                if (option == null)
                {
                    continue;
                }

                content.AddToLeft
                (
                    nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, option.Name,
                        ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = page + 1 + content.LeftArea.Children.Count, Tag = option
                    }
                );

                content.AddToRight(new NameOverheadAssignControl(World, option), false, nb.ButtonParameter);
            }

            if (nb != null)
            {
                nb.IsSelected = true;
                content.ActivePage = nb.ButtonParameter;
            }

            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.NameplateOptions));
        }

        private void BuildCooldowns()
        {
            SettingsOption s;
            PositionHelper.Reset();

            _options.Add(s = new SettingsOption(TazLang.Get("mog_cooldowns_customcooldownbars"), new Area(false),
                MainContent.RightWidth, (int)PAGE.TUOCooldowns));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            // Cooldown bar configuration now lives in the new (Myra) options menu, backed by cooldownbars.json.
            _options.Add
            (
                s = new SettingsOption
                (
                    "",
                    TextBox.GetOne(
                        TazLang.Get("mog_cooldowns_movednotice"),
                        ThemeSettings.FONT,
                        ThemeSettings.STANDARD_TEXT_SIZE,
                        ThemeSettings.TEXT_FONT_COLOR,
                        TextBox.RTLOptions.Default(MainContent.RightWidth)
                    ),
                    MainContent.RightWidth,
                    (int)PAGE.TUOCooldowns
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
        }

        private void SetNamePlatePresetCustom()
        {
            if (profile.NamePlatePreset != NamePlatePreset.Custom)
            {
                profile.NamePlatePreset = NamePlatePreset.Custom;
            }

            NameOverheadGump.InvalidateAllLayouts();
        }

        private void ApplyNamePlatePreset(NamePlatePreset preset)
        {
            profile.NamePlatePreset = preset;

            switch (preset)
            {
                case NamePlatePreset.Orion:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 160;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 160;
                    profile.NamePlateHeight = 0;
                    profile.NamePlateSplitHealthBar = false;
                    profile.NamePlateCornerRadius = 18;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.StatusColor;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.NotorietyColor;
                    profile.NamePlateBackgroundR = 0;
                    profile.NamePlateBackgroundG = 0;
                    profile.NamePlateBackgroundB = 0;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 75;
                    profile.NamePlateOpacity = 70;
                    profile.NamePlateBorderOpacity = 80;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 16;
                    break;

                case NamePlatePreset.WorldOfWarcraftBlockyBars:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 44;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 2;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 18;
                    profile.NamePlateBackgroundG = 14;
                    profile.NamePlateBackgroundB = 14;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 85;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 17;
                    break;

                case NamePlatePreset.WorldOfWarcraftCleanHealth:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 54;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 3;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 20;
                    profile.NamePlateBackgroundG = 18;
                    profile.NamePlateBackgroundB = 18;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 18;
                    break;

                case NamePlatePreset.WorldOfWarcraftBlockyCast:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 36;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 1;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 24;
                    profile.NamePlateBackgroundG = 18;
                    profile.NamePlateBackgroundB = 18;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 82;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 17;
                    break;

                case NamePlatePreset.WorldOfWarcraftRedName:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 38;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 1;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 28;
                    profile.NamePlateBackgroundG = 12;
                    profile.NamePlateBackgroundB = 12;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 18;
                    break;

                default:
                    break;
            }

            NameOverheadGump.InvalidateAllLayouts();
        }

        private void BuildTazUO()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height,
                (int)(MainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General

            page = ((int)PAGE.TUOOptions + 1000);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_gridcontainers"), page, content.LeftWidth));

            content.AddToRight
            (new HttpClickableLink("Grid Containers Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-Containers", ThemeSettings.TEXT_FONT_COLOR),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_general_containerstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[]
                    {
                        TazLang.Get("mog_containerstyleopt_grid"), TazLang.Get("mog_containerstyleopt_original")
                    }, (int)profile.ContainerStyle,
                    (s, n) => { profile.ContainerStyle = (ContainerStyle)s; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_gridcontainersdefaulttooldstyleview"), 0,
                    profile.GridContainersDefaultToOldStyleView,
                    (b) => { profile.GridContainersDefaultToOldStyleView = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_gridcontainerscale"), 0, ThemeSettings.SLIDER_WIDTH, 50, 200,
                    profile.GridContainersScale, (i) => { profile.GridContainersScale = (byte)i; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_alsoscaleitems"), 0, profile.GridContainerScaleItems, (b) => { profile.GridContainerScaleItems = b; }),
                true, page);

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_highlightlowcontrastitems"), 0, profile.GridHighlightLowContrastItems, (b) => { profile.GridHighlightLowContrastItems = b; }),
                true, page);

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World,
                    TazLang.Get("mog_tazuo_lowcontrasthighlightstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    Enum.GetNames(typeof(LowContrastHighlightStyle)), profile.GridHighlightLowContrastItemsStyle,
                    (i, s) => { profile.GridHighlightLowContrastItemsStyle = i; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_minimumitemcontrast"), 0, ThemeSettings.SLIDER_WIDTH, 1, 10,
                    profile.GridHighlightLowContrastMinimum,
                    (i) => { profile.GridHighlightLowContrastMinimum = (byte)i; }), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_griditemborderopacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.GridBorderAlpha,
                    (i) =>
                    {
                        profile.GridBorderAlpha = (byte)i;
                        GridItem.StaticGridContainerSettingUpdated();
                    }), true, page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_bordercolor"), profile.GridBorderHue,
                (h) =>
                {
                    profile.GridBorderHue = h;
                    GridItem.StaticGridContainerSettingUpdated();
                }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_containeropacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.ContainerOpacity,
                    (i) =>
                    {
                        profile.ContainerOpacity = (byte)i;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, TazLang.Get("mog_tazuo_backgroundcolor"), profile.AltGridContainerBackgroundHue, (h) =>
                    {
                        profile.AltGridContainerBackgroundHue = h;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_usecontainershue"), 0, profile.Grid_UseContainerHue, (b) =>
                    {
                        profile.Grid_UseContainerHue = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("gridcontainer_defaultview", "Default container view"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { TazLang.Get("gridcontainer_view_grid_short", "Grid"), TazLang.Get("gridcontainer_view_list_short", "List") }, profile.GridContainerViewMode,
                    (i, s) =>
                    {
                        profile.GridContainerViewMode = i;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_tazuo_searchstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { TazLang.Get("mog_tazuo_onlyshow"), TazLang.Get("mog_tazuo_highlight") }, profile.GridContainerSearchMode,
                    (i, s) => { profile.GridContainerSearchMode = i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_enablecontainerpreview"), 0, profile.GridEnableContPreview, (b) => { profile.GridEnableContPreview = b; }),
                true, page);

            c.SetTooltip(TazLang.Get("mog_tazuo_tooltippreview"));

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_makeanchorable"), 0, profile.EnableGridContainerAnchor, (b) =>
                    {
                        profile.EnableGridContainerAnchor = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            c.SetTooltip(TazLang.Get("mog_tazuo_tooltipgridanchor"));

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_tazuo_containerstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    Enum.GetNames(typeof(BorderStyle)), profile.Grid_BorderStyle, (i, s) =>
                    {
                        profile.Grid_BorderStyle = i;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_hideborders"), 0, profile.Grid_HideBorder, (b) =>
                    {
                        profile.Grid_HideBorder = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_defaultgridrows"), 0, ThemeSettings.SLIDER_WIDTH, 1, 20,
                    profile.Grid_DefaultRows, (i) => { profile.Grid_DefaultRows = i; }), true,
                page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_defaultgridcolumns"), 0, ThemeSettings.SLIDER_WIDTH, 1, 20,
                    profile.Grid_DefaultColumns, (i) => { profile.Grid_DefaultColumns = i; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new HttpClickableLink
                ("Grid Highlighting Wiki",
                    "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-highlighting-based-on-item-properties",
                    ThemeSettings.TEXT_FONT_COLOR), true, page
            );

            content.AddToRight
            (
                c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, TazLang.Get("mog_tazuo_gridhighlightsettings"),
                    ThemeSettings.BUTTON_FONT_COLOR) { IsSelected = true }, true, page
            );

            c.MouseUp += (s, e) => { GridHighlightMenu.Open(World); };

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_gridhighlightsize"), 0, ThemeSettings.SLIDER_WIDTH, 1, 5,
                    profile.GridHighlightSize, (i) => { profile.GridHighlightSize = i; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_gridhighlightproperties"), 0, profile.GridHighlightProperties,
                    (b) => { profile.GridHighlightProperties = b; }),
                true, page
            );

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_gridhighlightshowrulename"), 0, profile.GridHighlightShowRuleName,
                    (b) => { profile.GridHighlightShowRuleName = b; }),
                true, page
            );

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_griddisabletargeting"), 0, profile.DisableTargetingGridContainers,
                    (b) => { profile.DisableTargetingGridContainers = b; }),
                true, page
            );

            #endregion

            #region Journal

            page = ((int)PAGE.TUOOptions + 1001);
            content.ResetRightSide();

            content.AddToRight(
                new HttpClickableLink("Journal Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Journal",
                    ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_journal"), page, content.LeftWidth));

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_maxjournalentries"), 0, ThemeSettings.SLIDER_WIDTH, 100, 2000,
                    profile.MaxJournalEntries, (i) => { profile.MaxJournalEntries = i; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_journalopacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.JournalOpacity, (i) =>
                    {
                        profile.JournalOpacity = (byte)i;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, TazLang.Get("mog_tazuo_journalbackgroundcolor"), profile.AltJournalBackgroundHue, (h) =>
                    {
                        profile.AltJournalBackgroundHue = h;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    TazLang.Get("mog_tazuo_journalstyle"), 0, ThemeSettings.COMBO_BOX_WIDTH,
                    Enum.GetNames(typeof(ResizableJournal.BorderStyle)), profile.JournalStyle, (i, s) =>
                    {
                        profile.JournalStyle = i;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_journalhideborders"), 0, profile.HideJournalBorder, (b) =>
                    {
                        profile.HideJournalBorder = b;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            content.BlankLine();
            content.AddToRight(
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_hidetimestamp"), 0, profile.HideJournalTimestamp,
                    (b) => { profile.HideJournalTimestamp = b; }), true, page);
            content.BlankLine();
            content.AddToRight(
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_journalhidesystemprefix"), 0, profile.HideJournalSystemPrefix,
                    (b) => { profile.HideJournalSystemPrefix = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_makeanchorable"), 0, profile.JournalAnchorEnabled, (b) =>
                    {
                        profile.JournalAnchorEnabled = b;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            #endregion

            #region Modern paperdoll

            page = ((int)PAGE.TUOOptions + 1002);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_modernpaperdoll"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new HttpClickableLink("Modern Paperdoll Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Alternate-Paperdoll", ThemeSettings.TEXT_FONT_COLOR),
                true, page);

            content.BlankLine();

            content.AddToRight
            (c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_enablemodernpaperdoll"), 0, profile.UseModernPaperdoll, (b) => { profile.UseModernPaperdoll = b; }),
                true, page);

            content.Indent();
            content.BlankLine();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, TazLang.Get("mog_tazuo_paperdollhue"), profile.ModernPaperDollHue, (h) =>
                    {
                        profile.ModernPaperDollHue = h;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, TazLang.Get("mog_tazuo_durabilitybarhue"), profile.ModernPaperDollDurabilityHue, (h) =>
                    {
                        profile.ModernPaperDollDurabilityHue = h;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_showdurabilitybarbelow"), 0, ThemeSettings.SLIDER_WIDTH, 1, 100,
                    profile.ModernPaperDoll_DurabilityPercent,
                    (i) => { profile.ModernPaperDoll_DurabilityPercent = i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_paperdollanchor"), 0, profile.ModernPaperdollAnchorEnabled, (b) =>
                    {
                        profile.ModernPaperdollAnchorEnabled = b;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ), true, page
            );

            #endregion

            #region Nameplates

            page = ((int)PAGE.TUOOptions + 1003);
            content.AddToLeft(SubCategoryButton(TazLang.Get("nameplate_title", "Nameplates"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new HttpClickableLink("Nameplates Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Nameplate-options", ThemeSettings.TEXT_FONT_COLOR),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World,
                    TazLang.Get("nameplate_preset", "Preset"),
                    0,
                    ThemeSettings.COMBO_BOX_WIDTH,
                    GetNamePlatePresetOptions(),
                    (int)profile.NamePlatePreset,
                    (i, s) => { ApplyNamePlatePreset((NamePlatePreset)i); },
                    false
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("nameplate_healthbars", "Nameplates also act as health bars"), 0, profile.NamePlateHealthBar, (b) => { profile.NamePlateHealthBar = b; SetNamePlatePresetCustom(); }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("nameplate_hpopacity", "HP opacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateHealthBarOpacity,
                    (i) => { profile.NamePlateHealthBarOpacity = (byte)i; SetNamePlatePresetCustom(); }),
                true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_showwordofdeathicon", "Show Word of Death icon at 30% health"), 0, profile.NamePlateShowWordOfDeathIcon,
                    (b) => { profile.NamePlateShowWordOfDeathIcon = b; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_hidefullhealth", "Hide nameplates if full health"), 0, profile.NamePlateHideAtFullHealth,
                    (b) => { profile.NamePlateHideAtFullHealth = b; SetNamePlatePresetCustom(); }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_onlywarmode", "Only in warmode"), 0, profile.NamePlateHideAtFullHealthInWarmode,
                    (b) => { profile.NamePlateHideAtFullHealthInWarmode = b; SetNamePlatePresetCustom(); }), true,
                page
            );

            content.RemoveIndent();
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_fixedwidth", "Fixed width"), 0, profile.NamePlateUseFixedWidth,
                    (b) => { profile.NamePlateUseFixedWidth = b; SetNamePlatePresetCustom(); }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_width", "Name width"), 0, ThemeSettings.SLIDER_WIDTH, 60, 300, profile.NamePlateFixedWidth,
                    (i) => { profile.NamePlateFixedWidth = i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_separatehealthbarwidth", "Separate health bar width"), 0, profile.NamePlateUseFixedHealthBarWidth,
                    (b) => { profile.NamePlateUseFixedHealthBarWidth = b; SetNamePlatePresetCustom(); }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_healthbarwidth", "Health bar width"), 0, ThemeSettings.SLIDER_WIDTH, 60, 300, profile.NamePlateHealthBarFixedWidth,
                    (i) => { profile.NamePlateHealthBarFixedWidth = i; SetNamePlatePresetCustom(); }), true, page
            );

            content.RemoveIndent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_height", "Height"), 0, ThemeSettings.SLIDER_WIDTH, 0, 80, profile.NamePlateHeight,
                    (i) => { profile.NamePlateHeight = i; SetNamePlatePresetCustom(); }), true, page
            );

            content.RemoveIndent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_splithealthbar", "Split health bar"), 0, profile.NamePlateSplitHealthBar,
                    (b) => { profile.NamePlateSplitHealthBar = b; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_cornerradius", "Corner radius"), 0, ThemeSettings.SLIDER_WIDTH, 0, 40, profile.NamePlateCornerRadius,
                    (i) => { profile.NamePlateCornerRadius = i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World,
                    TazLang.Get("nameplate_healthfill", "Health fill"),
                    0,
                    ThemeSettings.COMBO_BOX_WIDTH,
                    GetNamePlateHealthBarModeOptions(),
                    (int)profile.NamePlateHealthBarMode,
                    (i, s) => { profile.NamePlateHealthBarMode = (NamePlateHealthBarMode)i; SetNamePlatePresetCustom(); },
                    false
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World,
                    TazLang.Get("nameplate_backgroundmode", "Background mode"),
                    0,
                    ThemeSettings.COMBO_BOX_WIDTH,
                    GetNamePlateBackgroundModeOptions(),
                    (int)profile.NamePlateBackgroundMode,
                    (i, s) => { profile.NamePlateBackgroundMode = (NamePlateBackgroundMode)i; SetNamePlatePresetCustom(); },
                    false
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("nameplate_borderopacity", "Border opacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateBorderOpacity,
                    (i) => { profile.NamePlateBorderOpacity = (byte)i; SetNamePlatePresetCustom(); }),
                true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("nameplate_backgroundopacity", "Background opacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateOpacity,
                    (i) => { profile.NamePlateOpacity = (byte)i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_backgroundred", "Background red"), 0, ThemeSettings.SLIDER_WIDTH, 0, 255, profile.NamePlateBackgroundR,
                    (i) => { profile.NamePlateBackgroundR = (byte)i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_backgroundgreen", "Background green"), 0, ThemeSettings.SLIDER_WIDTH, 0, 255, profile.NamePlateBackgroundG,
                    (i) => { profile.NamePlateBackgroundG = (byte)i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("nameplate_backgroundblue", "Background blue"), 0, ThemeSettings.SLIDER_WIDTH, 0, 255, profile.NamePlateBackgroundB,
                    (i) => { profile.NamePlateBackgroundB = (byte)i; SetNamePlatePresetCustom(); }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("nameplate_avoidoverlap", "Avoid overlap"), 0, profile.NamePlateAvoidOverlap,
                    (b) => { profile.NamePlateAvoidOverlap = b; SetNamePlatePresetCustom(); }), true, page
            );

            #endregion

            #region Mobiles

            page = ((int)PAGE.TUOOptions + 1004);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_mobiles"), page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(
                c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_damagetoself"), profile.DamageHueSelf,
                    (h) => { profile.DamageHueSelf = h; }), true, page);

            content.AddToRight
            (
                c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_damagetoothers"), profile.DamageHueOther,
                    (h) => { profile.DamageHueOther = h; }) { X = 250, Y = c.Y }, false, page
            );

            content.AddToRight(
                c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_damagetopets"), profile.DamageHuePet,
                    (h) => { profile.DamageHuePet = h; }), true, page);

            content.AddToRight
            (
                c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_damagetoallies"), profile.DamageHueAlly,
                    (h) => { profile.DamageHueAlly = h; }) { X = 250, Y = c.Y }, false, page
            );

            content.AddToRight
            (c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_damagetolastattack"), profile.DamageHueLastAttck, (h) => { profile.DamageHueLastAttck = h; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_displaypartychatoverplayerheads"), 0,
                    profile.DisplayPartyChatOverhead, (b) => { profile.DisplayPartyChatOverhead = b; }),
                true, page
            );

            c.SetTooltip(TazLang.Get("mog_tazuo_tooltippartychat"));
            content.BlankLine();

            content.AddToRight
            (
                c = new SliderWithLabel
                (TazLang.Get("mog_tazuo_overheadtextwidth"), 0, ThemeSettings.SLIDER_WIDTH, 0, 600, profile.OverheadChatWidth,
                    (i) => { profile.OverheadChatWidth = i; }), true, page
            );

            c.SetTooltip(TazLang.Get("mog_tazuo_tooltipoverheadtext"));
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_belowmobilehealthbarscale"), 0, ThemeSettings.SLIDER_WIDTH, 1, 5,
                    profile.HealthLineSizeMultiplier,
                    (i) => { profile.HealthLineSizeMultiplier = (byte)i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (TazLang.Get("mog_tazuo_automaticallyopenhealthbarsforlastattack"), 0, profile.OpenHealthBarForLastAttack,
                    (b) => { profile.OpenHealthBarForLastAttack = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_updateonebaraslastattack"), 0, profile.UseOneHPBarForLastAttack,
                    (b) => { profile.UseOneHPBarForLastAttack = b; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_hiddenplayeropacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.HiddenBodyAlpha,
                    (i) => { profile.HiddenBodyAlpha = (byte)i; }), true, page
            );

            content.Indent();
            content.AddToRight(
                c = new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_hiddenplayerhue"), profile.HiddenBodyHue,
                    (h) => { profile.HiddenBodyHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_regularplayeropacity"), 0, ThemeSettings.SLIDER_WIDTH, 0, 100,
                    profile.PlayerConstantAlpha, (i) => { profile.PlayerConstantAlpha = i; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_autofollowdistance"), 0, ThemeSettings.SLIDER_WIDTH, 1, 10,
                    profile.AutoFollowDistance, (i) => { profile.AutoFollowDistance = i; }),
                true, page
            );

            content.Indent();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_disableautofollow"), 0, profile.DisableAutoFollowAlt,
                    (i) => { profile.DisableAutoFollowAlt = i; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_disablemouseinteractionsforoverheadtext"), 0,
                    profile.DisableMouseInteractionOverheadText,
                    (b) => { profile.DisableMouseInteractionOverheadText = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_overridepartymemberhues"), 0, profile.OverridePartyAndGuildHue,
                    (b) => { profile.OverridePartyAndGuildHue = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_showtargetindicator"), isChecked: profile.ShowTargetIndicator,
                    valueChanged: (b) => { profile.ShowTargetIndicator = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (c = new SliderWithLabel(TazLang.Get("mog_tazuo_turndelay"), 0, ThemeSettings.SLIDER_WIDTH, 45, 120, ProfileManager.ServerSettings.TurnDelay, i => ProfileManager.ServerSettings.TurnDelay = (ushort)i),
                true, page);

            c.SetTooltip("This settting may cause throttling, Use with caution.");

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_ignorestaminacheck"), 0, profile.IgnoreStaminaCheck,
                    (b) => profile.IgnoreStaminaCheck = b), true, page);

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_disablegrayenemies"), 0, profile.DisableGrayEnemies,
                    (b) => profile.DisableGrayEnemies = b), true, page);

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_general_disabledismountwarmode"), 0, profile.DisableDismountInWarMode,
                    (b) => profile.DisableDismountInWarMode = b), true, page);

            #endregion

            #region Misc

            page = ((int)PAGE.TUOOptions + 1005);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_misc"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(
                new HttpClickableLink("Misc Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Miscellaneous",
                    ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_disablesystemchat"), 0, profile.DisableSystemChat,
                    (b) => { profile.DisableSystemChat = b; }), true, page);
            content.BlankLine();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("disablesystemchat_journalopen", "Disable system chat while Resizable Journal is open"), 0, profile.DisableSystemChatWhileJournalOpen,
                    (b) => { profile.DisableSystemChatWhileJournalOpen = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_general_autoavoidobstacules"), isChecked: profile.AutoAvoidObstacules,
                    valueChanged: (b) => { profile.AutoAvoidObstacules = b; }), true,
                page
            );

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_enableimprovedbuffgump"), 0, profile.UseImprovedBuffBar,
                    (b) => { profile.UseImprovedBuffBar = b; }), true, page);
            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_buffgumphue"), profile.ImprovedBuffBarHue,
                    (h) => { profile.ImprovedBuffBarHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, TazLang.Get("mog_tazuo_maingamewindowbackground"), profile.MainWindowBackgroundHue, (h) =>
                    {
                        profile.MainWindowBackgroundHue = h;
                        GameController.UpdateBackgroundHueShader();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_healthbarindicator"), 0, profile.EnableHealthIndicator, (b) => { profile.EnableHealthIndicator = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_onlyshowbelowhp"), 0, ThemeSettings.SLIDER_WIDTH, 1, 100,
                    (int)profile.ShowHealthIndicatorBelow * 100,
                    (i) => { profile.ShowHealthIndicatorBelow = i / 100f; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_size"), 0, ThemeSettings.SLIDER_WIDTH, 1, 25,
                    profile.HealthIndicatorWidth, (i) => { profile.HealthIndicatorWidth = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_spelliconscale"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    profile.SpellIconScale, (i) => { profile.SpellIconScale = i; }), true,
                page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_displaymatchinghotkeysonspellicons"), 0,
                    profile.SpellIcon_DisplayHotkey, (b) => { profile.SpellIcon_DisplayHotkey = b; }), true,
                page
            );

            content.Indent();
            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_hotkeytexthue"), profile.SpellIcon_HotkeyHue,
                    (h) => { profile.SpellIcon_HotkeyHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (TazLang.Get("mog_tazuo_enablegumpopacityadjustviaaltscroll"), 0, profile.EnableAlphaScrollingOnGumps,
                    (b) => { profile.EnableAlphaScrollingOnGumps = b; }), true, page
            );

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_enableadvancedshopgump"), 0, profile.UseModernShopGump,
                    (b) => { profile.UseModernShopGump = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_displayskillprogressbaronskillchanges"), 0,
                    profile.DisplaySkillBarOnChange, (b) => { profile.DisplaySkillBarOnChange = b; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new InputFieldWithLabel
                (TazLang.Get("mog_tazuo_textformat"), ThemeSettings.INPUT_WIDTH, profile.SkillBarFormat, false,
                    (s, e) => { profile.SkillBarFormat = ((InputField.StbTextBox)s).Text; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_enablespellindicatorsystem"), 0, profile.EnableSpellIndicators, (b) => { profile.EnableSpellIndicators = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, TazLang.Get("mog_tazuo_importfromurl"),
                    ThemeSettings.BUTTON_FONT_COLOR) { IsSelectable = true, IsSelected = true }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    new PromptPopupWindow
                    (
                        TazLang.Get("mog_tazuo_importfromurl"), TazLang.Get("mog_tazuo_inputrequesturl"),
                        url =>
                        {
                            if (!string.IsNullOrEmpty(url))
                            {
                                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                                {
                                    GameActions.Print(World, TazLang.Get("mog_tazuo_attemptingtodownloadspellconfig"));

                                    Task.Factory.StartNew
                                    (() =>
                                        {
                                            try
                                            {
                                                using var httpClient = new HttpClient();
                                                string result = httpClient.GetStringAsync(uri).Result;

                                                if (SpellVisualRangeManager.Instance.LoadFromString(result))
                                                {
                                                    GameActions.Print(World,
                                                        TazLang.Get("mog_tazuo_succesfullydownloadednewspellconfig"));
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                GameActions.Print(World,
                                                    string.Format(
                                                        TazLang.Get("mog_tazuo_failedtodownloadthespellconfigexmessage"),
                                                        ex.Message));
                                            }
                                        }
                                    );
                                }
                            }
                        },
                        TazLang.Get("mog_tazuo_download"), TazLang.Get("mog_tazuo_cancel"), null,
                        "https://github.com/PlayTazUO/TazUO/raw/refs/heads/dev/src/ClassicUO.Client/Game/Managers/DefaultSpellIndicatorConfig.json"
                    );
                }
            };

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_alsocloseanchoredhealthbarswhenautoclosinghealthbars"), content.RightWidth - 30,
                    profile.CloseHealthBarIfAnchored,
                    (b) => { profile.CloseHealthBarIfAnchored = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_enableautoresynconhangdetection"), 0, profile.ForceResyncOnHang, (b) => { profile.ForceResyncOnHang = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_playeroffsetx"), 0, ThemeSettings.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.X,
                    (i) => { profile.PlayerOffset = new Point(i, profile.PlayerOffset.Y); }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_playeroffsety"), 0, ThemeSettings.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.Y,
                    (i) => { profile.PlayerOffset = new Point(profile.PlayerOffset.X, i); }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new InputFieldWithLabel
                (
                    TazLang.Get("mog_tazuo_sosgumpid"), ThemeSettings.INPUT_WIDTH, profile.SOSGumpID.ToString(), true, (s, e) =>
                    {
                        if (StringHelper.TryParseUint(((InputField.StbTextBox)s).Text, out uint id))
                        {
                            profile.SOSGumpID = id;
                        }
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_usewasdmovement"), isChecked: ProfileManager.GlobalSettings.UseWASDInsteadArrowKeys,
                    valueChanged: (e) => { ProfileManager.GlobalSettings.UseWASDInsteadArrowKeys = e; }),
                true, page
            );
            c.SetTooltip(
                "This only works if you have enable chat by pressing enter, and chat disabled. Otherwise you will still be typing into your chatbar.");

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_applybordercavetiles"), isChecked: profile.EnableCaveBorder,
                    valueChanged: (e) =>
                    {
                        profile.EnableCaveBorder = e;
                        if (e)
                            StaticFilters.ApplyCaveTileBorder();
                    }),
                true, page
            );
            c.SetTooltip("After disabling, you need to restart the client to revert to no borders.");

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_enableasyncmaploading"), isChecked: profile.EnableASyncMapLoading,
                    valueChanged: (e) =>
                    {
                        profile.EnableASyncMapLoading = e;
                        if (GameScene.Instance != null)
                            GameScene.Instance.ASyncMapLoading = e;
                    }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_forcemanagedzlib"), isChecked: ZLib.ManagedZlibForced,
                    valueChanged: (e) =>
                    {
                        _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.MANAGED_ZLIB, e);
                        ZLib.SetForceManagedZlib(e);
                    }),
                true, page
            );
            c.SetTooltip(
                "This may impact performance negatively, but some unix systems have issues using unmanaged zlibs.");

            #region HideHouses

            content.BlankLine();

            content.AddToRight(
                new HttpClickableLink("Houses Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.HideHouses",
                    ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();
            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    TazLang.Get("mog_tazuo_enablehousetransparency"), 0, profile.ForceHouseTransparency, (b) =>
                    {
                        profile.ForceHouseTransparency = b;
                    }
                ), true, page
            );

            content.BlankLine();
            content.Indent();
            content.AddToRight
            (
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_housetransparencytilehue"),
                    profile.ForcedTransparencyHouseTileHue, (h) => { profile.ForcedTransparencyHouseTileHue = h; }),
                true, page
            );
            content.RemoveIndent();

            content.BlankLine();
            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_forcedhousetransparencylevel"), 0, ThemeSettings.SLIDER_WIDTH, 0, 255,
                    profile.ForcedHouseTransparency, (i) =>
                    {
                        profile.ForcedHouseTransparency = (byte)i;
                    }
                ), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight(TextBox.GetOne(
                    "Disable overhead messages of these types:",
                    ThemeSettings.FONT,
                    ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.Default()),
                true, page);

            content.Indent();
            foreach (MessageType mtype in Enum.GetValues<MessageType>())
            {
                if(mtype == MessageType.Discord || mtype == MessageType.ChatSystem || mtype == MessageType.Encoded)
                    continue;

                content.AddToRight
                (
                    c = new CheckboxWithLabel
                    (
                        Enum.GetName(mtype), 0, MessageTypeFilter.IsEnabled(profile.DisabledOverheadMessageTypes, mtype), (b) =>
                        {
                            profile.DisabledOverheadMessageTypes = MessageTypeFilter.SetEnabled(profile.DisabledOverheadMessageTypes, mtype, b);
                        }
                    ), true, page
                );
            }

            content.BlankLine();

            #endregion

            #endregion

            #region Tooltips

            page = ((int)PAGE.TUOOptions + 1006);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_tooltips"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_aligntooltipstotheleftside"), 0, profile.LeftAlignToolTips, (b) => { profile.LeftAlignToolTips = b; }),
                true, page);

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_alignmobiletooltipstocenter"), 0,
                    profile.ForceCenterAlignTooltipMobiles, (b) => { profile.ForceCenterAlignTooltipMobiles = b; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight(
                new ModernColorPickerWithLabel(World, TazLang.Get("mog_tazuo_backgroundhue"), profile.ToolTipBGHue,
                    (h) => { profile.ToolTipBGHue = h; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new InputFieldWithLabel
                (
                    TazLang.Get("mog_tazuo_headerformatitemname"), ThemeSettings.INPUT_WIDTH, profile.TooltipHeaderFormat, false,
                    (s, e) => { profile.TooltipHeaderFormat = ((InputField.StbTextBox)s).Text; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (c = new CheckboxWithLabel(TazLang.Get("mog_tazuo_forcedtooltips"), 0, profile.ForceTooltipsOnOldClients, b => { profile.ForceTooltipsOnOldClients = b; }),
                true, page);

            c.SetTooltip("This feature relies on simulating single clicking items and is not a perfect solution.");

            content.BlankLine();

            content.AddToRight
            (new CheckboxWithLabel(TazLang.Get("mog_tazuo_ignoretooltipoverridesformobiles"), 0, profile.ToolTipOverride_IgnoreMobiles, b => { profile.ToolTipOverride_IgnoreMobiles = b; }),
                true, page);

            content.BlankLine();

            content.AddToRight
            (new HttpClickableLink("Tooltip Overrides Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Tooltip-Override", ThemeSettings.TEXT_FONT_COLOR),
                true, page);

            NiceButton tooltipConfigButton;
            content.AddToRight(
                tooltipConfigButton =
                    new NiceButton(0, 0, 150, 25, ButtonAction.Activate, "Open Tooltip Config")
                    {
                        IsSelectable = false, DisplayBorder = true
                    }, true, page);

            tooltipConfigButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    MyraWindows.TooltipOverrideConfigWindow.Show(World.Instance);
                }
            };

            #endregion

            #region Font settings

            const int minFontSize = 5;
            const int maxFontSize = 50;
            page = ((int)PAGE.TUOOptions + 1007);

            // Enumerate once to save a bit of compute
            (string[] availableFonts, int maxFontNameLength) = TrueTypeLoader.Instance.GetSortedFontNames(true);

            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_fontsettings"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(
                new HttpClickableLink("TTF Fonts Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.TTF-Fonts",
                    ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight
            (new SliderWithLabel(TazLang.Get("mog_tazuo_ttffontborder"), 0, ThemeSettings.SLIDER_WIDTH, 0, 2, profile.TextBorderSize, (i) => { profile.TextBorderSize = i; }),
                true, page);

            content.BlankLine();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                (
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_infobarfont"),
                    CurrentProfile.InfoBarFont,
                    (i, s) =>
                    {
                        CurrentProfile.InfoBarFont = s;
                        InfoBarGump.UpdateAllOptions();
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.InfoBarFontSize, (i) =>
                    {
                        profile.InfoBarFontSize = i;
                        InfoBarGump.UpdateAllOptions();
                    }
                ), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                (
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_systemchatfont"),
                    CurrentProfile.GameWindowSideChatFont,
                    (i, s) => { CurrentProfile.GameWindowSideChatFont = s; }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.GameWindowSideChatFontSize, (i) => { profile.GameWindowSideChatFontSize = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                (
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_tooltipfont"),
                    CurrentProfile.SelectedToolTipFont,
                    (i, s) => { CurrentProfile.SelectedToolTipFont = s; }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.SelectedToolTipFontSize, (i) => { profile.SelectedToolTipFontSize = i; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_overheadfont"),
                    CurrentProfile.OverheadChatFont,
                    (i, s) => { CurrentProfile.OverheadChatFont = s; }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.OverheadChatFontSize, (i) => { profile.OverheadChatFontSize = i; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                (
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_journalfont"),
                    CurrentProfile.SelectedTTFJournalFont,
                    (i, s) => { CurrentProfile.SelectedTTFJournalFont = s; }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.SelectedJournalFontSize, (i) => { profile.SelectedJournalFontSize = i; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_nameplatefont"),
                    CurrentProfile.NamePlateFont,
                    (i, s) => { CurrentProfile.NamePlateFont = s; SetNamePlatePresetCustom(); }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.NamePlateFontSize, (i) => { profile.NamePlateFontSize = i; SetNamePlatePresetCustom(); }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(
                    availableFonts,
                    maxFontNameLength,
                    TazLang.Get("mog_tazuo_optionsfont"),
                    CurrentProfile.OptionsFont,
                    (i, s) => { CurrentProfile.OptionsFont = s; }
                ),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(TazLang.Get("mog_tazuo_sharedsize"), 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize,
                    profile.OptionsFontSize, (i) => { profile.OptionsFontSize = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            #endregion

            #region Controller settings

            page = ((int)PAGE.TUOOptions + 1008);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_controller"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_enablecontroller"), 0, profile.ControllerEnabled,
                    (b) => profile.ControllerEnabled = b), true, page);
            content.BlankLine();

            content.AddToRight
            (new HttpClickableLink("Controller Support Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Controller-Support", ThemeSettings.TEXT_FONT_COLOR),
                true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (TazLang.Get("mog_tazuo_mousesesitivity"), 0, ThemeSettings.SLIDER_WIDTH, 1, 20,
                    profile.ControllerMouseSensativity, (i) => { profile.ControllerMouseSensativity = i; }),
                true, page
            );

            #endregion

            #region Settings transfers

            page = ((int)PAGE.TUOOptions + 1009);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_settingstransfers"), page, content.LeftWidth));
            content.ResetRightSide();

            string rootpath;

            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
            {
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            }
            else
            {
                rootpath = Settings.GlobalSettings.ProfilesPath;
            }

            var locations = new List<ProfileLocationData>();
            var sameServerLocations = new List<ProfileLocationData>();
            string[] allAccounts = Directory.GetDirectories(rootpath);

            foreach (string account in allAccounts)
            {
                string[] allServers = Directory.GetDirectories(account);

                foreach (string server in allServers)
                {
                    string[] allCharacters = Directory.GetDirectories(server);

                    foreach (string character in allCharacters)
                    {
                        locations.Add(new ProfileLocationData(server, account, character));

                        if (FileSystemHelper.RemoveInvalidChars(profile.ServerName) ==
                            FileSystemHelper.RemoveInvalidChars(Path.GetFileName(server)))
                        {
                            sameServerLocations.Add(new ProfileLocationData(server, account, character));
                        }
                    }
                }
            }

            content.AddToRight
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_tazuo_settingswarning", [locations.Count.ToString()]), ThemeSettings.FONT,
                    ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.AddToRight
            (
                c = new ModernButton
                (0, 0, content.RightWidth - 20, 40, ButtonAction.Activate,
                    TazLang.Get("mog_tazuo_overrideall", [(locations.Count - 1).ToString()]), ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true, IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(locations);
                    GameActions.Print(World, TazLang.Get("mog_tazuo_overridesuccess", [(locations.Count - 1).ToString()]), Constants.HUE_SUCCESS,
                        Data.MessageType.System);
                }
            };

            content.AddToRight
            (
                c = new ModernButton
                (
                    0, 0, content.RightWidth - 20, 40, ButtonAction.Activate,
                    TazLang.Get("mog_tazuo_overridesame", [(sameServerLocations.Count - 1).ToString()]),
                    ThemeSettings.BUTTON_FONT_COLOR
                ) { IsSelectable = true, IsSelected = true }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(sameServerLocations);
                    GameActions.Print(World,
                        TazLang.Get("mog_tazuo_overridesuccess", [(sameServerLocations.Count - 1).ToString()]), Constants.HUE_SUCCESS,
                        Data.MessageType.System);
                }
            };

            content.AddToRight
            (
                c = new ModernButton
                (0, 0, content.RightWidth - 20, 40, ButtonAction.Activate,
                    TazLang.Get("mog_tazuo_overrideallmacros", [(locations.Count - 1).ToString()]), ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true, IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllMacros(locations);
                    GameActions.Print(World, TazLang.Get("mog_tazuo_overridesuccess", [(locations.Count - 1).ToString()]), Constants.HUE_SUCCESS,
                        Data.MessageType.System);
                }
            };

            /// Defaults:
            content.AddToRight
            (
                c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate,
                    TazLang.Get("mog_tazuo_setasdefault"), ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true, IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    SetProfileAsDefault(CurrentProfile);
                    GameActions.Print(World, TazLang.Get("mog_tazuo_setasdefaultsuccess"), Constants.HUE_SUCCESS, Data.MessageType.System);
                }
            };

            content.AddToRight
            (
                c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate,
                    TazLang.Get("mog_tazuo_setmacrosasdefault"), ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true, IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    World.Macros.Save(Path.Combine(RootPath, "macros.xml"));
                    GameActions.Print(World, TazLang.Get("mog_tazuo_setmacrosasdefaultsuccess"), Constants.HUE_SUCCESS, Data.MessageType.System);
                }
            };

            #endregion

            #region Gump scaling

            page = ((int)PAGE.TUOOptions + 1010);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_gumpscaling"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(
                new HttpClickableLink("Scaling Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Global-Scaling",
                    ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_tazuo_scalinginfo"), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("mog_tazuo_paperdollgump"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.PaperdollScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.PaperdollScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("gumpscaling_statusgump", "Status Gump"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.StatusGumpScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.StatusGumpScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("gumpscaling_skillgump", "Skills Gump"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.SkillsGumpScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.SkillsGumpScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("gumpscaling_contextmenu", "Context Menus"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.ContextMenuScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.ContextMenuScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("gumpscaling_tradegump", "Trade Gump"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.TradeGumpScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.TradeGumpScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    TazLang.Get("gumpscaling_servergump", "Server Gumps"), 0, ThemeSettings.SLIDER_WIDTH, 50, 300,
                    (int)(profile.ServerGumpScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.ServerGumpScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            SliderWithLabel s;
            content.AddToRight(
                s = new SliderWithLabel(TazLang.Get("mog_tazuo_globalscale"), 0, ThemeSettings.SLIDER_WIDTH, 50, 175,
                    (int)(Client.Game.RenderScale * 100), null), true, page);

            ModernButton b;
            content.AddToRight(
                b = new ModernButton(s.X + s.Width + 75, s.Y - 20, 75, 40, ButtonAction.Activate, "Apply",
                    ThemeSettings.BUTTON_FONT_COLOR), false, page);

            b.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    float scale = ((float)s.GetValue() / (float)100);

                    Client.Game.SetScale(scale);
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, scale);
                }
            };

            #endregion

            #region Hidden layers

            page = ((int)PAGE.TUOOptions + 1011);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_visiblelayers"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    TazLang.Get("mog_tazuo_vislayersinfo"), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_hiddenlayersenabled"), 0, profile.HiddenLayersEnabled,
                    (b) => { profile.HiddenLayersEnabled = b; }), true, page);

            content.BlankLine();
            content.AddToRight(
                new CheckboxWithLabel(TazLang.Get("mog_tazuo_onlyforyourself"), 0, profile.HideLayersForSelf,
                    (b) => { profile.HideLayersForSelf = b; }), true, page);

            content.BlankLine();

            bool rightSide = false;

            foreach (Layer layer in (Layer[])Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.Invalid || layer == Layer.Hair || layer == Layer.Beard || layer == Layer.Backpack ||
                    layer == Layer.ShopBuyRestock || layer == Layer.ShopBuy ||
                    layer == Layer.ShopSell || layer == Layer.Bank || layer == Layer.Face || layer == Layer.Talisman ||
                    layer == Layer.Mount)
                {
                    continue;
                }

                if (!rightSide)
                {
                    content.AddToRight
                    (
                        c = new CheckboxWithLabel
                        (
                            layer.ToString(), 0, profile.HiddenLayers.Contains((int)layer), (b) =>
                            {
                                if (b)
                                {
                                    profile.HiddenLayers.Add((int)layer);
                                }
                                else
                                {
                                    profile.HiddenLayers.Remove((int)layer);
                                }
                            }
                        ), true, page
                    );

                    rightSide = true;
                }
                else
                {
                    content.AddToRight
                    (
                        new CheckboxWithLabel
                        (
                            layer.ToString(), 0, profile.HiddenLayers.Contains((int)layer), (b) =>
                            {
                                if (b)
                                {
                                    profile.HiddenLayers.Add((int)layer);
                                }
                                else
                                {
                                    profile.HiddenLayers.Remove((int)layer);
                                }
                            }
                        ) { X = 200, Y = c.Y }, false, page
                    );

                    rightSide = false;
                }
            }

            #endregion

            #region Hotkeys

            page = ((int)PAGE.TUOOptions + 1016);
            content.AddToLeft(SubCategoryButton(TazLang.Get("mog_tazuo_hotkeys"), page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    "These are not configurable here, this is a list of hotkeys built into the client.\nThere may be missing hotkeys, please report them on our Discord.",
                    ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.Default(content.RightWidth - 15)
                ), true, page
            );

            content.BlankLine();

            int ewidth = content.RightWidth - 15;

            //Gumps ish
            content.AddToRight(
                GenHotKeyDisplay("Move gumps", "ALT", ewidth, CurrentProfile.HoldAltToMoveGumps), true,
                page);

            content.AddToRight(
                GenHotKeyDisplay("Detatch anchored gumps", "ALT", ewidth,
                    CurrentProfile.HoldAltToMoveGumps), true, page);
            content.AddToRight(GenHotKeyDisplay("Show lock button on various gumps", "ALT", ewidth), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Hold to close anchored gumps", "ALT", ewidth,
                    CurrentProfile.HoldDownKeyAltToCloseAnchored), true, page);
            content.AddToRight(GenHotKeyDisplay("Lock gump if it's lockable", "ALT CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Show gump lock icon where applicable", "ALT HOVER", ewidth), true,
                page);
            content.AddToRight(
                GenHotKeyDisplay("Adjust gump opacity", "ALT SCROLL-WHEEL", ewidth,
                    CurrentProfile.EnableAlphaScrollingOnGumps), true, page);

            //Grid container
            content.AddToRight(GenHotKeyDisplay("Grid container - move multiple items", "ALT CLICK-ITEM", ewidth), true,
                page);

            content.AddToRight
            (
                GenHotKeyDisplay
                (
                    "Grid container - add item to autoloot", "SHIFT CLICK-ITEM", ewidth,
                    CurrentProfile.EnableAutoLoot &&
                    !CurrentProfile.HoldShiftForContext &&
                    !CurrentProfile.HoldShiftToSplitStack
                ), true, page
            );

            content.AddToRight(GenHotKeyDisplay("Grid container - lock item in slot", "CTRL CLICK-ITEM", ewidth), true,
                page);
            content.AddToRight(GenHotKeyDisplay("Grid container - compare item to equipped", "CTRL HOVER", ewidth),
                true, page);


            content.AddToRight(GenHotKeyDisplay("Remove item from counterbar", "ALT RIGHT-CLICK", ewidth), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Click a mobile to follow them", "ALT CLICK", ewidth,
                    !CurrentProfile.DisableAutoFollowAlt), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Activate chat", "ENTER", ewidth,
                    CurrentProfile.ActivateChatAfterEnter), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Split item stacks", "SHIFT", ewidth,
                    CurrentProfile.HoldShiftToSplitStack), true, page);
            content.AddToRight(GenHotKeyDisplay("Show name plates", "CTRL SHIFT", ewidth), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Pathfinding", "SHIFT CLICK/DOUBLE-CLICK", ewidth,
                    CurrentProfile.UseShiftToPathfind), true, page);
            content.AddToRight(GenHotKeyDisplay("Buy/Sell all of an item at a shop", "SHIFT DOUBLE-CLICK", ewidth),
                true, page);
            content.AddToRight(GenHotKeyDisplay("Item drag - Lock in position", "CTRL SCROL-WHEEL", ewidth), true,
                page);
            content.AddToRight(
                GenHotKeyDisplay("Zoom window", "CTRL SCROL-WHEEL", ewidth,
                    CurrentProfile.EnableMousewheelScaleZoom), true, page);
            content.AddToRight(
                GenHotKeyDisplay("Scroll through messages sent in chat", "CTRL q/w", ewidth,
                    !CurrentProfile.DisableCtrlQWBtn), true, page);
            content.AddToRight(GenHotKeyDisplay("Auto-start xml gump from menu", "CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("World Map - Pathfind", "CTRL RIGHT-CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("World Map - Add Marker", "CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Screen shot gump/tooltip only", "CTRL PRINTSCREEN", ewidth), true,
                page);

            #endregion


            _options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.TUOOptions));
        }

        public override void Dispose()
        {
            base.Dispose();
            CurrentProfile?.Save(World, ProfilePath);
        }

        private void OverrideAllProfiles(List<ProfileLocationData> allProfiles)
        {
            foreach (ProfileLocationData profile in allProfiles)
            {
                CurrentProfile.Save(World, profile.ToString(), false);
            }
        }

        private void OverrideAllMacros(List<ProfileLocationData> allProfiles)
        {
            foreach (ProfileLocationData profile in allProfiles) World.Macros.Save(Path.Combine(profile.ToString(), "macros.xml"));
        }

        private ComboBoxWithLabel GenerateFontSelector(
            string[] fontNames,
            int maxFontNameLength,
            string label,
            string selectedFont = "",
            Action<int, string> onSelect = null
        )
        {
            const int comboBoxMaxWidth = 300;

            // Fallback to embedded fonts if we've gotten nothing here, for some reason.
            string[] options;
            int comboBoxWidth;
            if (fontNames?.Length > 0)
            {
                options = fontNames;
                // Guesstimate the combo's width based on the longest font name, otherwise we get bad wrapping/truncations.
                // Definitely not a "pretty" solution but works well enough until we overhaul the settings pages.
                comboBoxWidth = Math.Min(maxFontNameLength * 8, comboBoxMaxWidth);
            }
            else
            {
                options = EmbeddedFontNames.Names.ToArray();
                comboBoxWidth = comboBoxMaxWidth;
            }

            // Make sure the index is never out-of-bounds;
            // This can technically happen if a profile is moved to a machine that lacks the currently selected font.
            // Ideally, we'd want some 'warning' marker in the UI, but that's for a later time.
            int selectedFontInd = Math.Clamp(Array.IndexOf(options, selectedFont), 0, options.Length - 1);

            return new ComboBoxWithLabel(
                World,
                label,
                0,
                comboBoxWidth,
                options,
                selectedFontInd,
                onSelect,
                false
            );
        }

        public Control GenHotKeyDisplay(string text, string hotkey, int width, bool enabled = true)
        {
            var d = new Area(false);
            d.Add(TextBox.GetOne(text, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));

            var hk = TextBox.GetOne(hotkey, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            hk.X = width - hk.MeasuredSize.X;

            d.Add
            (
                new AlphaBlendControl()
                {
                    Width = hk.MeasuredSize.X, Height = hk.MeasuredSize.Y, X = width - hk.MeasuredSize.X
                }
            );

            d.Add(hk);

            d.ForceSizeUpdate();

            if (!enabled)
                d.Add
                (
                    new AlphaBlendControl(0.65f) { Width = d.Width, Height = d.Height }
                );

            return d;
        }


        #region Custom Controls For Options

        private class InfoBarBuilderControl : Control
        {
            private readonly InputField infoLabel;
            private readonly ModernColorPickerWithLabel labelColor;
            private readonly ComboBoxWithLabel varStat;

            public InfoBarBuilderControl(World world, InfoBarItem item, mainScrollArea content)
            {
                AcceptMouseInput = true;

                infoLabel = new InputField
                (
                    130, 40, text: item.label, onTextChanges: (s, e) =>
                    {
                        item.label = ((InputField.StbTextBox)s).Text;
                        UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    }
                ) { X = 5 };

                string[] dataVars = InfoBarManager.GetVars();

                varStat = new ComboBoxWithLabel
                (world,
                    string.Empty, 0, 170, dataVars, (int)item.var, onOptionSelected: (i, s) =>
                    {
                        item.var = (InfoBarVars)i;
                        UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    }
                ) { X = 200, Y = 8 };

                labelColor = new ModernColorPickerWithLabel
                (
                    world,
                    string.Empty, item.hue, (h) =>
                    {
                        item.hue = h;
                        UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    }
                ) { X = 150, Y = 10 };


                var deleteButton =
                    new ModernButton(390, 8, 60, 25, ButtonAction.Activate, "Delete", ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = 999
                    };

                deleteButton.MouseUp += (sender, e) =>
                {
                    Dispose();

                    if (Parent != null && Parent is DataBox db)
                    {
                        db.Remove(this);
                        db.ReArrangeChildren();
                        db.ForceSizeUpdate();
                        content.ForceSizeUpdate();
                    }

                    world.InfoBars?.RemoveItem(item);
                    UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    content.Remove(this);
                    content.ForceSizeUpdate();

                    int yOffset = 0;

                    foreach (Control child in content.Children)
                    {
                        if (child is ScrollArea scrollArea)
                        {
                            foreach (Control scrollChild in scrollArea.Children)
                            {
                                if (scrollChild is InfoBarBuilderControl control)
                                {
                                    scrollChild.Remove(this);
                                    control.Y = yOffset + 170;
                                    yOffset += control.Height;
                                    control.ForceSizeUpdate();
                                    content.ForceSizeUpdate();
                                }
                            }

                            content.ForceSizeUpdate();
                        }
                    }

                    content.ForceSizeUpdate();
                };

                Add(infoLabel);
                Add(varStat);
                Add(labelColor);
                Add(deleteButton);
                ForceSizeUpdate();
                content.ForceSizeUpdate();
            }

            public override void Update()
            {
                if (IsDisposed)
                {
                    return;
                }

                if (Children.Count != 0)
                {
                    for (int i = 0; i < Children.Count; i++)
                    {
                        IGui c = Children[i];

                        if (c.IsDisposed)
                        {
                            OnChildRemoved();
                            Children.RemoveAt(i--);

                            continue;
                        }

                        c.Update();
                    }
                }
            }

            public string LabelText => infoLabel.Text;
            public InfoBarVars Var => (InfoBarVars)varStat.SelectedIndex;
            public ushort Hue => labelColor.Hue;
        }

        private class mainScrollArea : Control
        {
            private ScrollArea left;
            private int leftY, rightY = ThemeSettings.TOP_PADDING, leftX = 0, rightX;

            public ScrollArea LeftArea => left;

            public mainScrollArea(int width, int height, int leftWidth, int page = 0)
            {
                Width = width;
                Height = height;
                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                Add
                (
                    left = new ScrollArea(0, 0, leftWidth, height) { CanMove = true, AcceptMouseInput = true }, page
                );


                LeftWidth = leftWidth - ThemeSettings.SCROLL_BAR_WIDTH;
                RightWidth = Width - leftWidth;
            }

            public int LeftWidth { get; }
            public int RightWidth { get; }

            public void AddToLeft(Control c, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = leftY + 10;
                    c.X = leftX;
                    leftY += c.Height + 10;
                }

                left.Add(c, page);
            }

            public void AddToLine(Control c, int x, int y, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = y;
                    c.X = leftX + x;
                }

                left.Add(c, page);
            }

            public void AddToLeftText(Control c, int x, int y, bool autoPosition = true, int page = 0)
            {
                if (autoPosition)
                {
                    c.Y = y;
                    c.X = leftX + x;
                }

                left.Add(c, page);
            }


            public void BlankLine() => rightY += ThemeSettings.BLANK_LINE;

            public void Indent() => rightX += ThemeSettings.INDENT_SPACE;

            public void RemoveIndent()
            {
                rightX -= ThemeSettings.INDENT_SPACE;

                if (rightX < 0)
                {
                    rightX = 0;
                }
            }

            public void ResetRightSide()
            {
                rightY = ThemeSettings.TOP_PADDING;
                rightX = 0;
            }

            public void SetMatchingButton(int page)
            {
                foreach (Control c in left.Children)
                {
                    if (c is ModernButton button && button.ButtonParameter == page)
                    {
                        ((SearchableOption)button).OnSearchMatch();
                        int p = Parent == null ? Page : Parent.Page;
                        SetParentsForMatchingSearch(this, p);
                    }
                }
            }
        }

        private class MacroControl : Control
        {
            private static readonly string[] _allHotkeysNames = Enum.GetNames(typeof(MacroType));
            private static readonly string[] _allSubHotkeysNames = Enum.GetNames(typeof(MacroSubType));
            private readonly DataBox _databox;
            private readonly HotkeyBox _hotkeyBox;

            private enum buttonsOption
            {
                AddBtn,
                RemoveBtn,
                CreateNewMacro,
                OpenMacroOptions,
                OpenButtonEditor
            }

            private World world;

            public MacroControl(World world, string name)
            {
                this.world = world;
                CanMove = true;
                TextBox _keyBinding;
                Add(_keyBinding = TextBox.GetOne("Hotkey", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));

                _hotkeyBox = new HotkeyBox();
                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;
                _hotkeyBox.X = _keyBinding.X + _keyBinding.Width + 5;


                Add(_hotkeyBox);

                Control c;

                Add
                (
                    c = new ModernButton(0, _hotkeyBox.Height + 3, 200, 40, ButtonAction.Activate,
                        TazLang.Get("create_macro_button"), ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.CreateNewMacro, IsSelectable = true, IsSelected = true
                    }
                );

                Add
                (
                    c = new ModernButton(c.Width + c.X + 10, c.Y, 200, 40, ButtonAction.Activate,
                        TazLang.Get("macro_button_editor"), ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.OpenButtonEditor, IsSelectable = true, IsSelected = true
                    }
                );

                Add(c = new Line(0, c.Y + c.Height + 5, 325, 1, Color.Gray.PackedValue));

                Add
                (
                    c = new ModernButton(0, c.Y + 5, 75, 40, ButtonAction.Activate, TazLang.Get("add"),
                        ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.AddBtn, IsSelectable = false
                    }
                );

                Add(_databox = new DataBox(0, c.Y + c.Height + 5, 280, 280));

                Macro = world.Macros.FindMacro(name) ?? Macro.CreateEmptyMacro(name);

                SetupKeyByDefault();
                SetupMacroUI();
            }

            public Macro Macro { get; }

            private void AddEmptyMacro()
            {
                var ob = (MacroObject)Macro.Items;

                if (ob == null || ob.Code == MacroType.None)
                {
                    return;
                }

                while (ob.Next != null)
                {
                    var next = (MacroObject)ob.Next;

                    if (next.Code == MacroType.None)
                    {
                        return;
                    }

                    ob = next;
                }

                MacroObject obj = Macro.Create(MacroType.None);

                Macro.PushToBack(obj);

                _databox.Add(new MacroEntry(world, this, obj, _allHotkeysNames));
                _databox.ReArrangeChildren();
                _databox.ForceSizeUpdate();
                ForceSizeUpdate();
            }

            private void RemoveLastCommand()
            {
                if (_databox.Children.Count != 0)
                {
                    LinkedObject last = Macro.GetLast();

                    Macro.Remove(last);

                    _databox.Children[_databox.Children.Count - 1].Dispose();

                    SetupMacroUI();
                }

                if (_databox.Children.Count == 0)
                {
                    AddEmptyMacro();
                }
            }

            private void SetupMacroUI()
            {
                if (Macro == null)
                {
                    return;
                }

                _databox.Clear();
                _databox.Children.Clear();

                if (Macro.Items == null)
                {
                    Macro.Items = Macro.Create(MacroType.None);
                }

                var obj = (MacroObject)Macro.Items;

                while (obj != null)
                {
                    _databox.Add(new MacroEntry(world, this, obj, _allHotkeysNames));

                    if (obj.Next != null && obj.Code == MacroType.None)
                    {
                        break;
                    }

                    obj = (MacroObject)obj.Next;
                }

                _databox.ReArrangeChildren();
                _databox.ForceSizeUpdate();
            }

            private void SetupKeyByDefault()
            {
                if (Macro == null || _hotkeyBox == null)
                {
                    return;
                }

                if (Macro.ControllerButtons != null && Macro.ControllerButtons.Length > 0)
                {
                    _hotkeyBox.SetButtons(Macro.ControllerButtons);
                }

                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

                if (Macro.Alt)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                }

                if (Macro.Shift)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                }

                if (Macro.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                }

                if (Macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    _hotkeyBox.SetKey(Macro.Key, mod);
                }
                else if (Macro.MouseButton != MouseButtonType.None)
                {
                    _hotkeyBox.SetMouseButton(Macro.MouseButton, mod);
                }
                else if (Macro.WheelScroll == true)
                {
                    _hotkeyBox.SetMouseWheel(Macro.WheelUp, mod);
                }
            }

            private void BoxOnHotkeyChanged(object sender, EventArgs e)
            {
                bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

                if (_hotkeyBox.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    Macro macro = world.Macros.FindMacro(_hotkeyBox.Key, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(world, 250, 150,
                            string.Format(TazLang.Get("this_key_combination_already_exists"), macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.MouseButton != MouseButtonType.None)
                {
                    Macro macro = world.Macros.FindMacro(_hotkeyBox.MouseButton, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(world, 250, 150,
                            string.Format(TazLang.Get("this_key_combination_already_exists"), macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.WheelScroll == true)
                {
                    Macro macro = world.Macros.FindMacro(_hotkeyBox.WheelUp, alt, ctrl, shift);

                    if (macro != null)
                    {
                        if (Macro == macro)
                        {
                            return;
                        }

                        SetupKeyByDefault();
                        UIManager.Add(new MessageBoxGump(world, 250, 150,
                            string.Format(TazLang.Get("this_key_combination_already_exists"), macro.Name), null));

                        return;
                    }
                }
                else if (_hotkeyBox.Buttons != null && _hotkeyBox.Buttons.Length > 0)
                {
                }
                else
                {
                    return;
                }

                Macro m = Macro;

                if (_hotkeyBox.Buttons != null && _hotkeyBox.Buttons.Length > 0)
                {
                    m.ControllerButtons = _hotkeyBox.Buttons;
                }

                m.Key = _hotkeyBox.Key;
                m.MouseButton = _hotkeyBox.MouseButton;
                m.WheelScroll = _hotkeyBox.WheelScroll;
                m.WheelUp = _hotkeyBox.WheelUp;
                m.Shift = shift;
                m.Alt = alt;
                m.Ctrl = ctrl;
            }

            private void BoxOnHotkeyCancelled(object sender, EventArgs e)
            {
                Macro m = Macro;
                m.Alt = m.Ctrl = m.Shift = false;
                m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                m.MouseButton = MouseButtonType.None;
                m.WheelScroll = false;
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case (int)buttonsOption.AddBtn: AddEmptyMacro(); break;
                    case (int)buttonsOption.RemoveBtn: RemoveLastCommand(); break;

                    case (int)buttonsOption.CreateNewMacro:
                        UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == Macro)?.Dispose();

                        var macroButtonGump = new MacroButtonGump(world, Macro, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(macroButtonGump);

                        break;

                    case (int)buttonsOption.OpenMacroOptions:
                        UIManager.Gumps.OfType<MacroGump>().FirstOrDefault()?.Dispose();

                        GameActions.OpenSettings(world, 4);

                        break;

                    case (int)buttonsOption.OpenButtonEditor:
                        UIManager.Gumps.OfType<MacroButtonEditorGump>().FirstOrDefault()?.Dispose();
                        OpenMacroButtonEditor(Macro, null);

                        break;
                }
            }

            private void OpenMacroButtonEditor(Macro macro, Vector2? position = null)
            {
                MacroButtonEditorGump btnEditorGump = UIManager.GetGump<MacroButtonEditorGump>();

                if (btnEditorGump == null)
                {
                    int posX = (Client.Game.Window.ClientBounds.Width >> 1) - 300;
                    int posY = (Client.Game.Window.ClientBounds.Height >> 1) - 250;
                    Gump opt = UIManager.GetGump<ModernOptionsGump>();

                    if (opt != null)
                    {
                        posX = opt.X + opt.Width + 5;
                        posY = opt.Y;
                    }

                    if (position.HasValue)
                    {
                        posX = (int)position.Value.X;
                        posY = (int)position.Value.Y;
                    }

                    btnEditorGump = new MacroButtonEditorGump(world, macro, posX, posY);
                    UIManager.Add(btnEditorGump);
                }

                btnEditorGump.SetInScreen();
                btnEditorGump.BringOnTop();
            }

            private class MacroEntry : Control
            {
                private readonly MacroControl _control;
                private readonly MacroObject _obj;
                private readonly string[] _items;
                public event EventHandler<MacroObject> OnDelete;
                ComboBoxWithLabel mainBox;
                private World world;

                public MacroEntry(World world, MacroControl control, MacroObject obj, string[] items)
                {
                    this.world = world;
                    _control = control;
                    _items = items;
                    _obj = obj;

                    mainBox = new ComboBoxWithLabel(world, string.Empty, 0, 200, _items, (int)obj.Code,
                        BoxOnOnOptionSelected) { Tag = obj };

                    Add(mainBox);

                    Control c;

                    Add
                    (
                        c = new ModernButton(mainBox.Width + 10, 0, 75, 40, ButtonAction.Activate, TazLang.Get("remove"),
                            ThemeSettings.BUTTON_FONT_COLOR)
                        {
                            ButtonParameter = (int)buttonsOption.RemoveBtn, IsSelectable = false
                        }
                    );

                    mainBox.Y = (c.Height >> 1) - (mainBox.Height >> 1);

                    Height = c.Height;

                    AddSubMacro(obj);

                    ForceSizeUpdate();
                }


                private void AddSubMacro(MacroObject obj)
                {
                    if (obj == null || obj.Code == 0)
                    {
                        return;
                    }

                    switch (obj.SubMenuType)
                    {
                        case 1:
                            int count = 0;
                            int offset = 0;
                            Macro.GetBoundByCode(obj.Code, ref count, ref offset);

                            string[] names = new string[count];

                            for (int i = 0; i < count; i++)
                            {
                                names[i] = _allSubHotkeysNames[i + offset];
                            }

                            if (obj.Code == MacroType.CastSpell)
                            {
                                var namesList = new List<string>(names);

                                namesList.Remove("Hostile");
                                namesList.Remove("Party");
                                namesList.Remove("Follower");
                                namesList.Remove("Object");
                                namesList.Remove("Mobile");
                                namesList.Remove("MscTotalCount");
                                namesList.Remove("INVALID_0");
                                namesList.Remove("INVALID_1");
                                namesList.Remove("INVALID_2");
                                namesList.Remove("INVALID_3");
                                namesList.Remove("ConfusionBlastPotion");
                                namesList.Remove("CurePotion");
                                namesList.Remove("AgilityPotion");
                                namesList.Remove("StrengthPotion");
                                namesList.Remove("PoisonPotion");
                                namesList.Remove("RefreshPotion");
                                namesList.Remove("HealPotion");
                                namesList.Remove("ExplosionPotion");

                                namesList.Remove("DefaultZoom");
                                namesList.Remove("ZoomIn");
                                namesList.Remove("ZoomOut");

                                namesList.Remove("BestHealPotion");
                                namesList.Remove("BestCurePotion");
                                namesList.Remove("BestRefreshPotion");
                                namesList.Remove("BestStrengthPotion");
                                namesList.Remove("BestAgiPotion");
                                namesList.Remove("BestExplosionPotion");
                                namesList.Remove("BestConflagPotion");
                                namesList.Remove("EnchantedApple");
                                namesList.Remove("PetalsOfTrinsic");
                                namesList.Remove("OrangePetals");
                                namesList.Remove("TrappedBox");
                                namesList.Remove("SmokeBomb");
                                namesList.Remove("HealStone");
                                namesList.Remove("SpellStone");

                                namesList.Remove("LookForwards");
                                namesList.Remove("LookBackwards");
                                names = namesList.ToArray();
                            }

                            var sub = new ComboBoxWithLabel
                            (world,
                                string.Empty, 0, 200, names, (int)obj.SubCode - offset, (i, s) =>
                                {
                                    Macro.GetBoundByCode(obj.Code, ref count, ref offset);
                                    var subType = (MacroSubType)(offset + i);
                                    obj.SubCode = subType;
                                }
                            ) { Tag = obj, X = 20, Y = Height };

                            Add(sub);

                            //Height += sub.Height;
                            break;

                        case 2:
                            var textbox = new InputField
                            (
                                400, 40, 0, 80, obj.HasString() ? ((MacroObjectString)obj).Text : string.Empty, false,
                                (s, e) =>
                                {
                                    if (obj.HasString())
                                    {
                                        ((MacroObjectString)obj).Text = ((InputField.StbTextBox)s).Text;
                                    }
                                }
                            ) { X = 20, Y = Height };

                            textbox.SetText(obj.HasString() ? ((MacroObjectString)obj).Text : string.Empty);

                            Add(textbox);

                            break;
                    }

                    ForceSizeUpdate();
                    _control._databox.ReArrangeChildren();
                    _control._databox.ForceSizeUpdate();
                    _control.ForceSizeUpdate();
                }

                public override void OnButtonClick(int buttonID)
                {
                    switch (buttonID)
                    {
                        case (int)buttonsOption.RemoveBtn:

                            _control.Macro.Remove(_obj);
                            Dispose();
                            _control._databox.ReArrangeChildren();
                            _control._databox.ForceSizeUpdate();
                            _control.ForceSizeUpdate();
                            //_control.SetupMacroUI();
                            OnDelete?.Invoke(this, _obj);

                            break;
                    }
                }

                private void BoxOnOnOptionSelected(int selected, string val)
                {
                    WantUpdateSize = true;

                    MacroObject currentMacroObj = _obj;

                    if (selected == 0)
                    {
                        _control.Macro.Remove(currentMacroObj);

                        mainBox.Tag = null;

                        Dispose();

                        _control.SetupMacroUI();
                    }
                    else
                    {
                        MacroObject newMacroObj = Macro.Create((MacroType)selected);

                        _control.Macro.Insert(currentMacroObj, newMacroObj);
                        _control.Macro.Remove(currentMacroObj);

                        mainBox.Tag = newMacroObj;


                        for (int i = 2; i < Children.Count; i++)
                        {
                            Children[i]?.Dispose();
                        }

                        AddSubMacro(newMacroObj);
                    }
                }
            }
        }

        private class NameOverheadAssignControl : Control
        {
            private readonly HotkeyBox _hotkeyBox;
            private readonly Dictionary<NameOverheadOptions, CheckboxWithLabel> checkboxDict = new();

            private enum ButtonType
            {
                CheckAll,
                UncheckAll,
            }

            private World world;

            public NameOverheadAssignControl(World world, NameOverheadOption option)
            {
                this.world = world;
                Option = option;

                CanMove = true;

                Control c;
                c = AddLabel("Set hotkey:");

                _hotkeyBox = new HotkeyBox { X = c.Bounds.Right + 5 };

                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;

                Add(_hotkeyBox);

                Add
                (
                    c = new ModernButton(0, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate, "Uncheck all",
                        ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)ButtonType.UncheckAll, IsSelectable = false
                    }
                );

                Add
                (
                    new ModernButton(c.Bounds.Right + 5, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate,
                        "Check all", ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)ButtonType.CheckAll, IsSelectable = false
                    }
                );

                SetupOptionCheckboxes();

                UpdateCheckboxesByCurrentOptionFlags();
                UpdateValueInHotkeyBox();
            }

            private void SetupOptionCheckboxes()
            {
                int rightPosX = 200;
                Control c;
                PositionHelper.Reset();

                PositionHelper.Y = 100;

                c = AddLabel("Items");
                PositionHelper.PositionControl(c);

                c = AddCheckbox("Containers", NameOverheadOptions.Containers);
                PositionHelper.PositionControl(c);

                c = AddCheckbox("Gold", NameOverheadOptions.Gold);
                PositionHelper.PositionExact(c, rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Stackable", NameOverheadOptions.Stackable));
                PositionHelper.PositionExact(AddCheckbox("Locked down", NameOverheadOptions.LockedDown), rightPosX,
                    PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Moveable", NameOverheadOptions.Moveable));
                PositionHelper.PositionExact(AddCheckbox("Immoveable", NameOverheadOptions.Immoveable), rightPosX,
                    PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Other items", NameOverheadOptions.Other));


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Corpses"));

                PositionHelper.PositionControl(AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses));
                PositionHelper.PositionExact(AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses),
                    rightPosX, PositionHelper.LAST_Y);
                //AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, y);


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by type"));

                PositionHelper.PositionControl(AddCheckbox("Humanoid", NameOverheadOptions.Humanoid));
                PositionHelper.PositionExact(AddCheckbox("Monster", NameOverheadOptions.Monster), rightPosX,
                    PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Your Followers", NameOverheadOptions.OwnFollowers));
                PositionHelper.PositionExact(AddCheckbox("Yourself", NameOverheadOptions.Self), rightPosX,
                    PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Exclude yourself", NameOverheadOptions.ExcludeSelf));


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by notoriety"));

                CheckboxWithLabel cb;
                PositionHelper.PositionControl(cb = AddCheckbox("Innocent", NameOverheadOptions.Innocent));
                cb.TextLabel.Hue = CurrentProfile.InnocentHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Allied", NameOverheadOptions.Ally), rightPosX,
                    PositionHelper.LAST_Y);
                cb.TextLabel.Hue = CurrentProfile.FriendHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Attackable", NameOverheadOptions.Gray));
                cb.TextLabel.Hue = CurrentProfile.CanAttackHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Criminal", NameOverheadOptions.Criminal), rightPosX,
                    PositionHelper.LAST_Y);
                cb.TextLabel.Hue = CurrentProfile.CriminalHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Enemy", NameOverheadOptions.Enemy));
                cb.TextLabel.Hue = CurrentProfile.EnemyHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Murderer", NameOverheadOptions.Murderer), rightPosX,
                    PositionHelper.LAST_Y);
                cb.TextLabel.Hue = CurrentProfile.MurdererHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Invulnerable", NameOverheadOptions.Invulnerable));
                cb.TextLabel.Hue = CurrentProfile.InvulnerableHue;
            }

            private TextBox AddLabel(string name)
            {
                var label = TextBox.GetOne(name, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                    ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
                Add(label);

                return label;
            }

            private CheckboxWithLabel AddCheckbox(string checkboxName, NameOverheadOptions optionFlag)
            {
                var checkbox = new CheckboxWithLabel
                (
                    checkboxName, 0, true, (b) =>
                    {
                        if (b)
                            Option.NameOverheadOptionFlags |= optionFlag;
                        else
                            Option.NameOverheadOptionFlags &= ~optionFlag;

                        if (NameOverHeadManager.LastActiveNameOverheadOption.Replace("\\u0026", "&") == Option.Name)
                            NameOverHeadManager.ActiveOverheadOptions =
                                (NameOverheadOptions)Option.NameOverheadOptionFlags;
                    }
                );

                checkboxDict.Add(optionFlag, checkbox);

                Add(checkbox);

                return checkbox;
            }

            public NameOverheadOption Option { get; }

            private void UpdateValueInHotkeyBox()
            {
                if (Option == null || _hotkeyBox == null)
                {
                    return;
                }

                if (Option.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

                    if (Option.Alt)
                    {
                        mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                    }

                    if (Option.Shift)
                    {
                        mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                    }

                    if (Option.Ctrl)
                    {
                        mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                    }

                    _hotkeyBox.SetKey(Option.Key, mod);
                }
            }

            private void BoxOnHotkeyChanged(object sender, EventArgs e)
            {
                bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_SHIFT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_ALT) != SDL.SDL_Keymod.SDL_KMOD_NONE;
                bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.SDL_KMOD_CTRL) != SDL.SDL_Keymod.SDL_KMOD_NONE;

                if (_hotkeyBox.Key == SDL.SDL_Keycode.SDLK_UNKNOWN)
                    return;

                NameOverheadOption option = NameOverHeadManager.FindOptionByHotkey(_hotkeyBox.Key, alt, ctrl, shift);

                if (option == null)
                {
                    Option.Key = _hotkeyBox.Key;
                    Option.Shift = shift;
                    Option.Alt = alt;
                    Option.Ctrl = ctrl;

                    return;
                }

                if (Option == option)
                    return;

                UpdateValueInHotkeyBox();
                UIManager.Add(new MessageBoxGump(world, 250, 150,
                    string.Format(TazLang.Get("this_key_combination_already_exists"), option.Name), null));
            }

            private void BoxOnHotkeyCancelled(object sender, EventArgs e)
            {
                Option.Alt = Option.Ctrl = Option.Shift = false;
                Option.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
            }

            public override void OnButtonClick(int buttonID)
            {
                switch ((ButtonType)buttonID)
                {
                    case ButtonType.CheckAll:
                        Option.NameOverheadOptionFlags = ByteFlagHelper.AllBits<NameOverheadOptions>();
                        UpdateCheckboxesByCurrentOptionFlags();

                        break;

                    case ButtonType.UncheckAll:
                        Option.NameOverheadOptionFlags = 0x0;
                        UpdateCheckboxesByCurrentOptionFlags();

                        break;
                }
            }

            private void UpdateCheckboxesByCurrentOptionFlags()
            {
                foreach (KeyValuePair<NameOverheadOptions, CheckboxWithLabel> kvp in checkboxDict)
                {
                    NameOverheadOptions flag = kvp.Key;
                    CheckboxWithLabel checkbox = kvp.Value;

                    checkbox.IsChecked = ((NameOverheadOptions)Option.NameOverheadOptionFlags).HasFlag(flag);
                }
            }
        }

        #endregion

        private class ProfileLocationData
        {
            public readonly DirectoryInfo Server;
            public readonly DirectoryInfo Username;
            public readonly DirectoryInfo Character;

            public ProfileLocationData(string server, string username, string character)
            {
                this.Server = new DirectoryInfo(server);
                this.Username = new DirectoryInfo(username);
                this.Character = new DirectoryInfo(character);
            }

            public override string ToString() => Character.ToString();
        }

        private enum PAGE
        {
            None,
            General,
            Sound,
            Video,
            Macros,
            Tooltip,
            Speech,
            CombatSpells,
            Counters,
            InfoBar,
            Containers,
            Experimental,
            IgnoreList,
            NameplateOptions,
            TUOCooldowns,
            TUOOptions
        }
    }
}
