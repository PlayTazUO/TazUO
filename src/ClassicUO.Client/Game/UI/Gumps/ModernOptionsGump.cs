using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL3;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using static ClassicUO.Game.Managers.AutoLootManager;
using ClassicUO.Game.UI.Gumps.GridHighLight;

namespace ClassicUO.Game.UI.Gumps
{
    public class ModernOptionsGump : BaseOptionsGump
    {
        private List<SettingsOption> options = new List<SettingsOption>();
        private Profile profile;
        private ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        public ModernOptionsGump(World world) : base(world, 900, 700, Language.Instance.GetModernOptionsGumpLanguage.OptionsTitle)
        {
            profile = ProfileManager.CurrentProfile;

            CenterXInScreen();
            CenterYInScreen();

            Build();
        }

        private void Build()
        {
            ModernButton b;
            MainContent.AddToLeft(b = CategoryButton(lang.ButtonGeneral, (int)PAGE.General, MainContent.LeftWidth));
            b.IsSelected = true;
            MainContent.AddToLeft(CategoryButton(lang.ButtonSound, (int)PAGE.Sound, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonVideo, (int)PAGE.Video, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonMacros, (int)PAGE.Macros, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonTooltips, (int)PAGE.Tooltip, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonSpeech, (int)PAGE.Speech, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonCombatSpells, (int)PAGE.CombatSpells, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonCounters, (int)PAGE.Counters, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonInfobar, (int)PAGE.InfoBar, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonContainers, (int)PAGE.Containers, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonExperimental, (int)PAGE.Experimental, MainContent.LeftWidth));

            MainContent.AddToLeft
            (
                b = new ModernButton(0, 0, MainContent.LeftWidth, 40, ButtonAction.Activate, lang.ButtonIgnoreList, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = 999
                }
            );

            b.MouseUp += (s, e) =>
            {
                UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                UIManager.Add(new IgnoreManagerGump(World));
            };

            MainContent.AddToLeft(CategoryButton(lang.ButtonNameplates, (int)PAGE.NameplateOptions, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonCooldowns, (int)PAGE.TUOCooldowns, MainContent.LeftWidth));
            MainContent.AddToLeft(CategoryButton(lang.ButtonTazUO, (int)PAGE.TUOOptions, MainContent.LeftWidth));

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

            foreach (SettingsOption option in options)
            {
                MainContent.AddToRight(option.FullControl, false, (int)option.OptionsPage);
            }

            ChangePage((int)PAGE.General);
        }

        private void BuildGeneral()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General

            page = ((int)PAGE.General + 1000);
            content.AddToLeft(SubCategoryButton(lang.ButtonGeneral, page, content.LeftWidth));

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.HighlightObjects, isChecked: profile.HighlightGameObjects, valueChanged: (b) => { profile.HighlightGameObjects = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.Pathfinding, isChecked: profile.EnablePathfind, valueChanged: (b) => { profile.EnablePathfind = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.ShiftPathfinding, isChecked: profile.UseShiftToPathfind, valueChanged: (b) => { profile.UseShiftToPathfind = b; }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.SingleClickPathfind, isChecked: profile.PathfindSingleClick, valueChanged: (b) => { profile.PathfindSingleClick = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AlwaysRun, isChecked: profile.AlwaysRun, valueChanged: (b) => { profile.AlwaysRun = b; }), true, page);
            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.RunUnlessHidden, isChecked: profile.AlwaysRunUnlessHidden, valueChanged: (b) => { profile.AlwaysRunUnlessHidden = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.AutoOpenDoors, isChecked: profile.AutoOpenDoors, valueChanged: (b) => { profile.AutoOpenDoors = b; }), true, page);

            content.Indent();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.AutoOpenPathfinding, isChecked: profile.SmoothDoors, valueChanged: (b) => { profile.SmoothDoors = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.AutoOpenCorpse, isChecked: profile.AutoOpenCorpses, valueChanged: (b) => { profile.AutoOpenCorpses = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetGeneral.CorpseOpenDistance, 0, ThemeSettings.SLIDER_WIDTH, 0, 5, profile.AutoOpenCorpseRange, (r) => { profile.AutoOpenCorpseRange = r; }), true, page
            );

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.CorpseSkipEmpty, isChecked: profile.SkipEmptyCorpse, valueChanged: (b) => { profile.SkipEmptyCorpse = b; }), true, page);

            content.AddToRight
            (
                new ComboBoxWithLabel
                (
                    World, lang.GetGeneral.CorpseOpenOptions, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.CorpseOptNone, lang.GetGeneral.CorpseOptNotTarg, lang.GetGeneral.CorpseOptNotHiding, lang.GetGeneral.CorpseOptBoth },
                    profile.CorpseOpenOptions, (s, n) => { profile.CorpseOpenOptions = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.OutRangeColor, isChecked: profile.NoColorObjectsOutOfRange, valueChanged: (b) => { profile.NoColorObjectsOutOfRange = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
                (c = new CheckboxWithLabel(lang.GetGeneral.SallosEasyGrab, isChecked: profile.SallosEasyGrab, valueChanged: (b) => { profile.SallosEasyGrab = b; }), true, page);

            c.SetTooltip(lang.GetGeneral.SallosTooltip);

            if (Client.Game.UO.Version > ClientVersion.CV_70796)
            {
                content.BlankLine();

                content.AddToRight
                (
                    new CheckboxWithLabel(lang.GetGeneral.ShowHouseContent, isChecked: profile.ShowHouseContent, valueChanged: (b) => { profile.ShowHouseContent = b; }), true, page
                );
            }

            if (Client.Game.UO.Version >= ClientVersion.CV_7090)
            {
                content.BlankLine();

                content.AddToRight
                (
                    new CheckboxWithLabel(lang.GetGeneral.SmoothBoat, isChecked: profile.UseSmoothBoatMovement, valueChanged: (b) => { profile.UseSmoothBoatMovement = b; }), true,
                    page
                );
            }

            content.BlankLine();

            #endregion

            #region Mobiles

            page = ((int)PAGE.General + 1001);
            content.AddToLeft(SubCategoryButton(lang.ButtonMobiles, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.ShowMobileHP, isChecked: profile.ShowMobilesHP, valueChanged: (b) => { profile.ShowMobilesHP = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.MobileHPType, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.HPTypePerc, lang.GetGeneral.HPTypeBar, lang.GetGeneral.HPTypeNBoth }, profile.MobileHPType,
                    (s, n) => { profile.MobileHPType = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.HPShowWhen, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.HPShowWhen_Always, lang.GetGeneral.HPShowWhen_Less100, lang.GetGeneral.HPShowWhen_Smart }, profile.MobileHPShowWhen,
                    (s, n) => { profile.MobileHPShowWhen = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.HighlightPoisoned, isChecked: profile.HighlightMobilesByPoisoned, valueChanged: (b) => { profile.HighlightMobilesByPoisoned = b; }), true, page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetGeneral.PoisonHighlightColor, profile.PoisonHue, (h) => { profile.PoisonHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.HighlightPara, isChecked: profile.HighlightMobilesByParalize, valueChanged: (b) => { profile.HighlightMobilesByParalize = b; }), true, page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetGeneral.ParaHighlightColor, profile.ParalyzedHue, (h) => { profile.ParalyzedHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.HighlightInvul, isChecked: profile.HighlightMobilesByInvul, valueChanged: (b) => { profile.HighlightMobilesByInvul = b; }),
                true, page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetGeneral.InvulHighlightColor, profile.InvulnerableHue, (h) => { profile.InvulnerableHue = h; }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.IncomingMobiles, isChecked: profile.ShowNewMobileNameIncoming, valueChanged: (b) => { profile.ShowNewMobileNameIncoming = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.IncomingCorpses, isChecked: profile.ShowNewCorpseNameIncoming, valueChanged: (b) => { profile.ShowNewCorpseNameIncoming = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.AuraUnderFeet, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.AuraOptDisabled, lang.GetGeneral.AuroOptWarmode, lang.GetGeneral.AuraOptCtrlShift, lang.GetGeneral.AuraOptAlways },
                    profile.AuraUnderFeetType, (s, n) => { profile.AuraUnderFeetType = s; }
                ), true, page
            );

            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.AuraForParty, isChecked: profile.PartyAura, valueChanged: (b) => { profile.PartyAura = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetGeneral.AuraPartyColor, profile.PartyAuraHue, (h) => { profile.PartyAuraHue = h; }), true, page);
            content.RemoveIndent();
            content.RemoveIndent();

            #endregion

            #region Gumps & Context

            page = ((int)PAGE.General + 1002);
            content.AddToLeft(SubCategoryButton(lang.ButtonGumpContext, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.DisableTopMenu, isChecked: profile.TopbarGumpIsDisabled, valueChanged: (b) => { profile.TopbarGumpIsDisabled = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.AltForAnchorsGumps, isChecked: profile.HoldDownKeyAltToCloseAnchored, valueChanged: (b) => { profile.HoldDownKeyAltToCloseAnchored = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.AltToMoveGumps, isChecked: profile.HoldAltToMoveGumps, valueChanged: (b) => { profile.HoldAltToMoveGumps = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    lang.GetGeneral.CloseEntireAnchorWithRClick, isChecked: profile.CloseAllAnchoredGumpsInGroupWithRightClick,
                    valueChanged: (b) => { profile.CloseAllAnchoredGumpsInGroupWithRightClick = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.OriginalSkillsGump, isChecked: profile.StandardSkillsGump, valueChanged: (b) => { profile.StandardSkillsGump = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.OldStatusGump, isChecked: profile.UseOldStatusGump, valueChanged: (b) => { profile.UseOldStatusGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.PartyInviteGump, isChecked: profile.PartyInviteGump, valueChanged: (b) => { profile.PartyInviteGump = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.ModernHealthBars, isChecked: profile.CustomBarsToggled, valueChanged: (b) => { profile.CustomBarsToggled = b; }), true, page
            );

            content.Indent();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.ModernHPBlackBG, isChecked: profile.CBBlackBGToggled, valueChanged: (b) => { profile.CBBlackBGToggled = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.SaveHPBars, isChecked: profile.SaveHealthbars, valueChanged: (b) => { profile.SaveHealthbars = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.CloseHPGumpsWhen, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.CloseHPOptDisable, lang.GetGeneral.CloseHPOptOOR, lang.GetGeneral.CloseHPOptDead, lang.GetGeneral.CloseHPOptBoth },
                    profile.CloseHealthBarType, (s, n) => { profile.CloseHealthBarType = s; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.GridLoot, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.GridLootOptDisable, lang.GetGeneral.GridLootOptOnly, lang.GetGeneral.GridLootOptBoth }, profile.GridLootType,
                    (s, n) => { profile.GridLootType = s; }
                ), true, page
            );

            c.SetTooltip(lang.GetGeneral.GridLootTooltip);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.ShiftContext, isChecked: profile.HoldShiftForContext, valueChanged: (b) => { profile.HoldShiftForContext = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.ShiftSplit, isChecked: profile.HoldShiftToSplitStack, valueChanged: (b) => { profile.HoldShiftToSplitStack = b; }), true, page
            );

            #endregion

            #region Misc

            page = ((int)PAGE.General + 1003);
            content.AddToLeft(SubCategoryButton(lang.ButtonMisc, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.EnableCOT, isChecked: profile.UseCircleOfTransparency, valueChanged: (b) => { profile.UseCircleOfTransparency = b; }), true,
                page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetGeneral.COTDistance, 0, ThemeSettings.SLIDER_WIDTH, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    profile.CircleOfTransparencyRadius, (r) => { profile.CircleOfTransparencyRadius = r; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.COTType, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.COTTypeOptFull, lang.GetGeneral.COTTypeOptGrad, lang.GetGeneral.COTTypeOptModern }, profile.CircleOfTransparencyType,
                    (s, n) => { profile.CircleOfTransparencyType = s; }
                ), true, page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.HideScreenshotMessage, isChecked: profile.HideScreenshotStoredInMessage, valueChanged: (b) => { profile.HideScreenshotStoredInMessage = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.ObjFade, isChecked: profile.UseObjectsFading, valueChanged: (b) => { profile.UseObjectsFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.TextFade, isChecked: profile.TextFading, valueChanged: (b) => { profile.TextFading = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.CursorRange, isChecked: profile.ShowTargetRangeIndicator, valueChanged: (b) => { profile.ShowTargetRangeIndicator = b; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.DragSelectHP, isChecked: profile.EnableDragSelect, valueChanged: (b) => { profile.EnableDragSelect = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.DragKeyMod, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt }, profile.DragSelectModifierKey,
                    (s, n) => { profile.DragSelectModifierKey = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.DragPlayersOnly, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt },
                    profile.DragSelect_PlayersModifier, (s, n) => { profile.DragSelect_PlayersModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.DragMobsOnly, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt },
                    profile.DragSelect_MonstersModifier, (s, n) => { profile.DragSelect_MonstersModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.DragNameplatesOnly, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.SharedNone, lang.GetGeneral.SharedCtrl, lang.GetGeneral.SharedShift, lang.GetGeneral.SharedAlt },
                    profile.DragSelect_NameplateModifier, (s, n) => { profile.DragSelect_NameplateModifier = s; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetGeneral.DragX, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, profile.DragSelectStartX,
                    (r) => { profile.DragSelectStartX = r; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetGeneral.DragY, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Scene.Camera.Bounds.Width, profile.DragSelectStartY,
                    (r) => { profile.DragSelectStartY = r; }
                ), true, page
            );

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.DragAnchored, isChecked: profile.DragSelectAsAnchor, valueChanged: (b) => { profile.DragSelectAsAnchor = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.ShowStatsChangedMsg, isChecked: profile.ShowStatsChangedMessage, valueChanged: (b) => { profile.ShowStatsChangedMessage = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetGeneral.ShowSkillsChangedMsg, isChecked: profile.ShowSkillsChangedMessage, valueChanged: (b) => { profile.ShowSkillsChangedMessage = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetGeneral.ChangeVolume, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.ShowSkillsChangedDeltaValue, (r) => { profile.ShowSkillsChangedDeltaValue = r; }),
                true, page
            );

            content.RemoveIndent();

            #endregion

            #region Terrain and statics

            page = ((int)PAGE.General + 1004);
            content.AddToLeft(SubCategoryButton(lang.ButtonTerrainStatics, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.HideRoof, isChecked: !profile.DrawRoofs, valueChanged: (b) => { profile.DrawRoofs = !b; }), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.TreesToStump, isChecked: profile.TreeToStumps, valueChanged: (b) => { profile.TreeToStumps = b; }), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetGeneral.HideVegetation, isChecked: profile.HideVegetation, valueChanged: (b) => { profile.HideVegetation = b; }), true, page);

            //content.BlankLine();

            //content.AddToRight(new CheckboxWithLabel("Mark cave tiles", isChecked: profile.EnableCaveBorder, valueChanged: (b) => { profile.EnableCaveBorder = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetGeneral.MagicFieldType, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetGeneral.MagicFieldOpt_Normal, lang.GetGeneral.MagicFieldOpt_Static, lang.GetGeneral.MagicFieldOpt_Tile }, profile.FieldsType,
                    (s, n) => { profile.FieldsType = s; }
                ), true, page
            );

            #endregion

            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.General));
        }

        private void BuildSound()
        {
            SettingsOption s;

            PositionHelper.Reset();

            options.Add
            (
                s = new SettingsOption
                    ("", new CheckboxWithLabel(lang.GetSound.EnableSound, 0, profile.EnableSound, (b) => { profile.EnableSound = b; }), MainContent.RightWidth, (int)PAGE.Sound)
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel(lang.GetSound.SharedVolume, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.SoundVolume, (i) => { profile.SoundVolume = i; }),
                    MainContent.RightWidth, (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                    ("", new CheckboxWithLabel(lang.GetSound.EnableMusic, 0, profile.EnableMusic, (b) => { profile.EnableMusic = b; }), MainContent.RightWidth, (int)PAGE.Sound)
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel(lang.GetSound.SharedVolume, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.MusicVolume, (i) => { profile.MusicVolume = i; }),
                    MainContent.RightWidth, (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSound.LoginMusic, 0, Settings.GlobalSettings.LoginMusic, (b) => { Settings.GlobalSettings.LoginMusic = b; }),
                    MainContent.RightWidth, (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel
                    (
                        lang.GetSound.SharedVolume, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, Settings.GlobalSettings.LoginMusicVolume,
                        (i) => { Settings.GlobalSettings.LoginMusicVolume = i; }
                    ), MainContent.RightWidth, (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSound.PlayFootsteps, 0, profile.EnableFootstepsSound, (b) => { profile.EnableFootstepsSound = b; }), MainContent.RightWidth,
                    (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSound.CombatMusic, 0, profile.EnableCombatMusic, (b) => { profile.EnableCombatMusic = b; }), MainContent.RightWidth,
                    (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSound.BackgroundMusic, 0, profile.ReproduceSoundsInBackground, (b) => { profile.ReproduceSoundsInBackground = b; }),
                    MainContent.RightWidth, (int)PAGE.Sound
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildVideo()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));

            #region Game window

            int page = ((int)PAGE.Video + 1000);
            content.AddToLeft(SubCategoryButton(lang.ButtonGameWindow, page, content.LeftWidth));

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetVideo.FPSCap, 0, ThemeSettings.SLIDER_WIDTH, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS, (r) =>
                    {
                        Settings.GlobalSettings.FPS = r;
                        Client.Game.SetRefreshRate(r);
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetVideo.BackgroundFPS, isChecked: profile.ReduceFPSWhenInactive, valueChanged: (b) => { profile.ReduceFPSWhenInactive = b; }), true,
                page
            );

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetVideo.EnableVSync, isChecked: profile.EnableVSync, valueChanged: (b) =>
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
                    lang.GetVideo.FullsizeViewport, isChecked: profile.GameWindowFullSize, valueChanged: (b) =>
                    {
                        profile.GameWindowFullSize = b;

                        if (b)
                        {
                            UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                            UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(new Point(-5, -5));
                            profile.GameWindowPosition = new Point(-5, -5);
                        }
                        else
                        {
                            UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(new Point(600, 480));
                            UIManager.GetGump<WorldViewportGump>()?.SetGameWindowPosition(new Point(25, 25));
                            profile.GameWindowPosition = new Point(25, 25);
                        }
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    lang.GetVideo.FullScreen, isChecked: profile.WindowBorderless, valueChanged: (b) =>
                    {
                        profile.WindowBorderless = b;
                        Client.Game.SetWindowBorderless(b);
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.LockViewport, isChecked: profile.GameWindowLock, valueChanged: (b) => { profile.GameWindowLock = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetVideo.ViewportX, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowPosition.X, (r) =>
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
                    lang.GetVideo.ViewportY, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowPosition.Y, (r) =>
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
                    lang.GetVideo.ViewportW, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Width, profile.GameWindowSize.X, (r) =>
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
                    lang.GetVideo.ViewportH, 0, ThemeSettings.SLIDER_WIDTH, 0, Client.Game.Window.ClientBounds.Height, profile.GameWindowSize.Y, (r) =>
                    {
                        profile.GameWindowSize = new Point(profile.GameWindowSize.X, r);
                        UIManager.GetGump<WorldViewportGump>()?.ResizeGameWindow(profile.GameWindowSize);
                    }
                ), true, page
            );

            #endregion

            #region Zoom

            page = ((int)PAGE.Video + 1001);
            content.AddToLeft(SubCategoryButton(lang.ButtonZoom, page, content.LeftWidth));
            content.ResetRightSide();

            int cameraZoomCount = (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin) / Client.Game.Scene.Camera.ZoomStep);
            int cameraZoomIndex = cameraZoomCount - (int)((Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom) / Client.Game.Scene.Camera.ZoomStep);

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetVideo.DefaultZoom, 0, ThemeSettings.SLIDER_WIDTH, 0, cameraZoomCount, cameraZoomIndex,
                    (r) => { profile.DefaultScale = Client.Game.Scene.Camera.Zoom = (r * Client.Game.Scene.Camera.ZoomStep) + Client.Game.Scene.Camera.ZoomMin; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetVideo.ZoomWheel, isChecked: profile.EnableMousewheelScaleZoom, valueChanged: (b) => { profile.EnableMousewheelScaleZoom = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetVideo.ReturnDefaultZoom, isChecked: profile.RestoreScaleAfterUnpressCtrl, valueChanged: (b) => { profile.RestoreScaleAfterUnpressCtrl = b; }), true,
                page
            );

            #endregion

            #region Lighting

            page = ((int)PAGE.Video + 1002);
            content.AddToLeft(SubCategoryButton(lang.ButtonLighting, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.AltLights, isChecked: profile.UseAlternativeLights, valueChanged: (b) => { profile.UseAlternativeLights = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    lang.GetVideo.CustomLLevel, isChecked: profile.UseCustomLightLevel, valueChanged: (b) =>
                    {
                        profile.UseCustomLightLevel = b;

                        if (b)
                        {
                            World.Light.Overall = profile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, profile.LightLevel) : profile.LightLevel;
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
                    lang.GetVideo.Level, 0, ThemeSettings.SLIDER_WIDTH, 0, 0x1E, 0x1E - profile.LightLevel, (r) =>
                    {
                        profile.LightLevel = (byte)(0x1E - r);

                        if (profile.UseCustomLightLevel)
                        {
                            World.Light.Overall = profile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, profile.LightLevel) : profile.LightLevel;
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
                    lang.GetVideo.LightType, 0, ThemeSettings.COMBO_BOX_WIDTH, new string[] { lang.GetVideo.LightType_Absolute, lang.GetVideo.LightType_Minimum },
                    profile.LightLevelType, (s, n) => { profile.LightLevelType = s; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.DarkNight, isChecked: profile.UseDarkNights, valueChanged: (b) => { profile.UseDarkNights = b; }), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.ColoredLight, isChecked: profile.UseColoredLights, valueChanged: (b) => { profile.UseColoredLights = b; }), true, page);

            #endregion

            #region Misc

            page = ((int)PAGE.Video + 1003);
            content.AddToLeft(SubCategoryButton(lang.ButtonMisc, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.EnableDeathScreen, isChecked: profile.EnableDeathScreen, valueChanged: (b) => { profile.EnableDeathScreen = b; }), true, page);

            content.Indent();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.BWDead, isChecked: profile.EnableBlackWhiteEffect, valueChanged: (b) => { profile.EnableBlackWhiteEffect = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    lang.GetVideo.MouseThread, isChecked: Settings.GlobalSettings.RunMouseInASeparateThread,
                    valueChanged: (b) => { Settings.GlobalSettings.RunMouseInASeparateThread = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetVideo.TargetAura, isChecked: profile.AuraOnMouse, valueChanged: (b) => { profile.AuraOnMouse = b; }), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.AnimWater, isChecked: profile.AnimatedWaterEffect, valueChanged: (b) => { profile.AnimatedWaterEffect = b; }), true, page);

            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel("Enable post processing effects", 0, profile.EnablePostProcessingEffects, (b) =>
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
                    ["point", "linear", "anisotropic", "xbr"],
                    profile.PostProcessingType,
                    (s, n) =>
                {
                    profile.PostProcessingType = (ushort)s;
                    Client.Game.GetScene<GameScene>()?.SetPostProcessingSettings();
                } ), true, page);;

            #endregion

            #region Shadows

            page = ((int)PAGE.Video + 1004);
            content.AddToLeft(SubCategoryButton(lang.ButtonShadows, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.EnableShadows, isChecked: profile.ShadowsEnabled, valueChanged: (b) => { profile.ShadowsEnabled = b; }), true, page);

            content.Indent();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetVideo.RockTreeShadows, isChecked: profile.ShadowsStatics, valueChanged: (b) => { profile.ShadowsStatics = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetVideo.TerrainShadowLevel, 0, ThemeSettings.SLIDER_WIDTH, Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL,
                    profile.TerrainShadowsLevel, (r) => { profile.TerrainShadowsLevel = r; }
                ), true, page
            );

            #endregion

            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.Video));
        }

        private void BuildMacros()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));
            int page = ((int)PAGE.Macros + 1000);
            int bParam = page + 1;

            #region New Macro

            ModernButton b;

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetMacros.NewMacro, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
            );

            b.MouseUp += (sender, e) =>
            {
                var dialog = new EntryDialog
                (
                    World, 250, 150, ResGumps.MacroName, name =>
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
                            nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, name, ThemeSettings.BUTTON_FONT_COLOR)
                            {
                                ButtonParameter = bParam++,
                                Tag = macroControl.Macro
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
                )
                {
                    CanCloseWithRightClick = true
                };

                UIManager.Add(dialog);
            };

            #endregion

            #region Delete Macro

            page = ((int)PAGE.Macros + 1001);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetMacros.DelMacro, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    var dialog = new QuestionGump
                    (
                        World, ResGumps.MacroDeleteConfirmation, b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (nb.Tag is Macro macro)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == macro)?.Dispose();
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
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, "Move Up", ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
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
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, "Move Down", ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
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
                    b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, macro.Name, ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = bParam++,
                        Tag = macro
                    }
                );

                content.ResetRightSide();
                content.AddToRight(new MacroControl(World, macro.Name), true, b.ButtonParameter);
            }

            b.IsSelected = true;
            content.ActivePage = b.ButtonParameter;

            #endregion

            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.Macros));
        }

        private void RebuildMacroButtons(LeftSideMenuRightSideContent content, ref int bParam)
        {
            Macro selectedMacro = null;
            ModernButton selectedButton = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);
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
                var b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, macro.Name, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = bParam++,
                    Tag = macro
                };

                content.AddToLeft(b);

                MacroControl macroControl = content.RightArea.FindControls<MacroControl>().FirstOrDefault(mc => mc.Macro == macro);
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
            var content = new mainScrollArea(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 1.0));
            int page = ((int)PAGE.InfoBar + 1000);

            #region Active Info Bar

            CheckboxWithLabel b;

            content.AddToLeft
            (
                b = new CheckboxWithLabel
                (
                    lang.GetInfoBars.ShowInfoBar, 0, profile.ShowInfoBar, (b) =>
                    {
                        profile.ShowInfoBar = b;
                        InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

                        if (b)
                        {
                            if (infoBarGump == null)
                            {
                                UIManager.Add
                                (
                                    new InfoBarGump(World)
                                    {
                                        X = 300,
                                        Y = 300
                                    }
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
                    lang.GetInfoBars.HighlightType, 0, ThemeSettings.COMBO_BOX_WIDTH,
                    new string[] { lang.GetInfoBars.HighLightOpt_TextColor, lang.GetInfoBars.HighLightOpt_ColoredBars }, profile.InfoBarHighlightType,
                    (i, s) => { profile.InfoBarHighlightType = i; }
                )
            );

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            #endregion

            #region Select type infobar

            var infoBarItems = new DataBox(0, 0, 0, 0)
            {
                AcceptMouseInput = true
            };

            ModernButton addItem;

            content.AddToLeft
            (
                addItem = new ModernButton(0, 0, 150, 40, ButtonAction.Activate, lang.GetInfoBars.AddItem, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = -1,
                    IsSelectable = true,
                    IsSelected = true
                }
            );

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            addItem.MouseUp += (s, e) =>
            {
                InfoBarItem ibi;
                var ibbc = new InfoBarBuilderControl(World, ibi = new InfoBarItem("HP", InfoBarVars.HP, 0x3B9), content);
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
                    lang.GetInfoBars.Label, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(100).MouseInput()
                ), 0, 135
            );

            content.AddToLeftText
            (
                TextBox.GetOne
                (
                    lang.GetInfoBars.Color, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(100).MouseInput()
                ), 120, 135
            );

            content.AddToLeftText
            (
                TextBox.GetOne
                (
                    lang.GetInfoBars.Data, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.DefaultCentered(100).MouseInput()
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


            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.InfoBar));
        }

        private void BuildTooltips()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetToolTips.EnableToolTips, 0, profile.UseTooltip, (b) => { profile.UseTooltip = b; }), MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel
                    (
                        lang.GetToolTips.ToolTipDelay, 0, ThemeSettings.SLIDER_WIDTH, 0, 1000, profile.TooltipDelayBeforeDisplay, (i) => { profile.TooltipDelayBeforeDisplay = i; }
                    ), MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new SliderWithLabel
                        (lang.GetToolTips.ToolTipBG, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.TooltipBackgroundOpacity, (i) => { profile.TooltipBackgroundOpacity = i; }),
                    MainContent.RightWidth, (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetToolTips.ToolTipFont, profile.TooltipTextHue, (h) => { profile.TooltipTextHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Tooltip
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildSpeech()
        {
            SettingsOption s, ss;
            PositionHelper.Reset();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.ScaleSpeechDelay, 0, profile.ScaleSpeechDelay, (b) => { profile.ScaleSpeechDelay = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel(lang.GetSpeech.SpeechDelay, 0, ThemeSettings.SLIDER_WIDTH, 0, 1000, profile.SpeechDelay, (i) => { profile.SpeechDelay = i; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.SaveJournalE, 0, profile.SaveJournalToFile, (b) => { profile.SaveJournalToFile = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.ChatEnterActivation, 0, profile.ActivateChatAfterEnter, (b) => { profile.ActivateChatAfterEnter = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.ChatEnterSpecial, 0, profile.ActivateChatAdditionalButtons, (b) => { profile.ActivateChatAdditionalButtons = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.ShiftEnterChat, 0, profile.ActivateChatShiftEnterSupport, (b) => { profile.ActivateChatShiftEnterSupport = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.ChatGradient, 0, profile.HideChatGradient, (b) => { profile.HideChatGradient = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.HideGuildChat, 0, profile.IgnoreGuildMessages, (b) => { profile.IgnoreGuildMessages = b; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetSpeech.HideAllianceChat, 0, profile.IgnoreAllianceMessages, (b) => { profile.IgnoreAllianceMessages = b; }),
                    MainContent.RightWidth, (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);


            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                    ("", new ModernColorPickerWithLabel(World, lang.GetSpeech.SpeechColor, profile.SpeechHue, (h) => { profile.SpeechHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add
            (
                s = new SettingsOption
                    ("", new ModernColorPickerWithLabel(World, lang.GetSpeech.YellColor, profile.YellHue, (h) => { profile.YellHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetSpeech.PartyColor, profile.PartyMessageHue, (h) => { profile.PartyMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetSpeech.AllianceColor, profile.AllyMessageHue, (h) => { profile.AllyMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add
            (
                s = new SettingsOption
                    ("", new ModernColorPickerWithLabel(World, lang.GetSpeech.EmoteColor, profile.EmoteHue, (h) => { profile.EmoteHue = h; }), MainContent.RightWidth, (int)PAGE.Speech)
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetSpeech.WhisperColor, profile.WhisperHue, (h) => { profile.WhisperHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionExact(s.FullControl, 200, ss.FullControl.Y);

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetSpeech.GuildColor, profile.GuildMessageHue, (h) => { profile.GuildMessageHue = h; }), MainContent.RightWidth,
                    (int)PAGE.Speech
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            ss = s;

            options.Add
            (
                s = new SettingsOption
                (
                    "", new ModernColorPickerWithLabel(World, lang.GetSpeech.CharColor, profile.ChatMessageHue, (h) => { profile.ChatMessageHue = h; }), MainContent.RightWidth,
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
            options.Add(new SettingsOption("", scroll, MainContent.RightWidth, (int)PAGE.CombatSpells));

            Control c;
            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.HoldTabForCombat, 0, profile.HoldDownKeyTab, (b) => { profile.HoldDownKeyTab = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add
                (c = new CheckboxWithLabel(lang.GetCombatSpells.QueryBeforeAttack, 0, profile.EnabledCriminalActionQuery, (b) => { profile.EnabledCriminalActionQuery = b; }));

            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add
            (
                c = new CheckboxWithLabel
                    (lang.GetCombatSpells.QueryBeforeBeneficial, 0, profile.EnabledBeneficialCriminalActionQuery, (b) => { profile.EnabledBeneficialCriminalActionQuery = b; })
            );

            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.EnableOverheadSpellFormat, 0, profile.EnabledSpellFormat, (b) => { profile.EnabledSpellFormat = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.EnableOverheadSpellHue, 0, profile.EnabledSpellHue, (b) => { profile.EnabledSpellHue = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.SingleClickForSpellIcons, 0, profile.CastSpellsByOneClick, (b) => { profile.CastSpellsByOneClick = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar, 0, profile.BuffBarTime, (b) => { profile.BuffBarTime = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.EnableFastSpellHotkeyAssigning, 0, profile.FastSpellsAssign, (b) => { profile.FastSpellsAssign = b; }));

            PositionHelper.PositionControl(c);
            c.SetTooltip(lang.GetCombatSpells.TooltipFastSpellAssign);

            PositionHelper.BlankLine();

            scroll.Add(c = new CheckboxWithLabel(lang.GetCombatSpells.EnableDPSCounter, 0, profile.ShowDPS, (b) => { profile.ShowDPS = b; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.InnocentColor, profile.InnocentHue, (h) => { profile.InnocentHue = h; }));

            PositionHelper.PositionControl(c);

            Control clast = c;
            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.BeneficialSpell, profile.BeneficHue, (h) => { profile.BeneficHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.FriendColor, profile.FriendHue, (h) => { profile.FriendHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.HarmfulSpell, profile.HarmfulHue, (h) => { profile.HarmfulHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.Criminal, profile.CriminalHue, (h) => { profile.CriminalHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.NeutralSpell, profile.NeutralHue, (h) => { profile.NeutralHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.CanBeAttackedHue, profile.CanAttackHue, (h) => { profile.CanAttackHue = h; }));
            PositionHelper.PositionControl(c);
            clast = c;

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.Murderer, profile.MurdererHue, (h) => { profile.MurdererHue = h; }));
            PositionHelper.PositionExact(c, 200, clast.Y);

            scroll.Add(c = new ModernColorPickerWithLabel(World, lang.GetCombatSpells.Enemy, profile.EnemyHue, (h) => { profile.EnemyHue = h; }));
            PositionHelper.PositionControl(c);

            PositionHelper.BlankLine();

            InputFieldWithLabel spellFormat = spellFormat = new InputFieldWithLabel
            (
                lang.GetCombatSpells.SpellOverheadFormat, 200, profile.SpellDisplayFormat, onTextChange: (s, e) => { profile.SpellDisplayFormat = ((InputField.StbTextBox)s).Text; }
            );

            scroll.Add(spellFormat);
            PositionHelper.PositionControl(spellFormat);
            spellFormat.SetTooltip(lang.GetCombatSpells.TooltipSpellFormat);
        }

        private void BuildCounters()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel
                    (
                        lang.GetCounters.EnableCounters, 0, profile.CounterBarEnabled, (b) =>
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

                            counterGump?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetCounters.HighlightItemsOnUse, 0, profile.CounterBarHighlightOnUse, (b) => { profile.CounterBarHighlightOnUse = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                        (lang.GetCounters.AbbreviatedValues, 0, profile.CounterBarDisplayAbbreviatedAmount, (b) => { profile.CounterBarDisplayAbbreviatedAmount = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCounters.AbbreviateIfAmountExceeds, new InputField
                    (
                        100, 40, text: profile.CounterBarAbbreviatedAmount.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
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

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                        (lang.GetCounters.HighlightRedWhenAmountIsLow, 0, profile.CounterBarHighlightOnAmount, (b) => { profile.CounterBarHighlightOnAmount = b; }),
                    MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCounters.HighlightRedIfAmountIsBelow, new InputField
                    (
                        100, 40, text: profile.CounterBarHighlightAmount.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
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

            options.Add(s = new SettingsOption(lang.GetCounters.CounterLayout, new Area(false), MainContent.RightWidth, (int)PAGE.Counters));
            PositionHelper.PositionControl(s.FullControl);

            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel
                    (
                        lang.GetCounters.GridSize, 0, ThemeSettings.SLIDER_WIDTH, 30, 100, profile.CounterBarCellSize, (v) =>
                        {
                            profile.CounterBarCellSize = v;
                            UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCounters.Rows, new InputField
                    (
                        100, 40, text: profile.CounterBarRows.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarRows = v;
                                UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Counters
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            SettingsOption ss = s;

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCounters.Columns, new InputField
                    (
                        100, 40, text: profile.CounterBarColumns.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CounterBarColumns = v;
                                UIManager.GetGump<CounterBarGump>()?.SetLayout(profile.CounterBarCellSize, profile.CounterBarRows, profile.CounterBarColumns);
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

            options.Add(s = new SettingsOption(lang.GetContainers.Description, new Area(false), MainContent.RightWidth, (int)PAGE.Containers));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            if (Client.Game.UO.Version >= ClientVersion.CV_705301)
            {
                options.Add
                (
                    s = new SettingsOption
                    (
                        "",
                        new ComboBoxWithLabel
                        (
                            World,
                            lang.GetContainers.CharacterBackpackStyle, 0, ThemeSettings.COMBO_BOX_WIDTH,
                            new string[]
                            {
                                lang.GetContainers.BackpackOpt_Default, lang.GetContainers.BackpackOpt_Suede, lang.GetContainers.BackpackOpt_PolarBear,
                                lang.GetContainers.BackpackOpt_GhoulSkin
                            }, profile.BackpackStyle, (i, s) => { profile.BackpackStyle = i; }
                        ), MainContent.RightWidth, (int)PAGE.Containers
                    )
                );

                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            options.Add
            (
                s = new SettingsOption
                (
                    "", new SliderWithLabel
                    (
                        lang.GetContainers.ContainerScale, 0, ThemeSettings.SLIDER_WIDTH, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC,
                        profile.ContainersScale, (i) =>
                        {
                            profile.ContainersScale = (byte)i;
                            UIManager.ContainerScale = (byte)i / 100f;

                            foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                            {
                                resizableGump.RequestUpdateContents();
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetContainers.AlsoScaleItems, 0, profile.ScaleItemsInsideContainers, (b) => { profile.ScaleItemsInsideContainers = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();
            PositionHelper.BlankLine();

            if (Client.Game.UO.Version >= ClientVersion.CV_706000)
            {
                options.Add
                (
                    s = new SettingsOption
                    (
                        "", new CheckboxWithLabel(lang.GetContainers.UseLargeContainerGumps, 0, profile.UseLargeContainerGumps, (b) => { profile.UseLargeContainerGumps = b; }),
                        MainContent.RightWidth, (int)PAGE.Containers
                    )
                );

                PositionHelper.PositionControl(s.FullControl);
                PositionHelper.BlankLine();
            }

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (
                        lang.GetContainers.DoubleClickToLootItemsInsideContainers, 0, profile.DoubleClickToLootInsideContainers,
                        (b) => { profile.DoubleClickToLootInsideContainers = b; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                        (lang.GetContainers.RelativeDragAndDropItemsInContainers, 0, profile.RelativeDragAndDropItems, (b) => { profile.RelativeDragAndDropItems = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                    (
                        lang.GetContainers.HighlightContainerOnGroundWhenMouseIsOverAContainerGump, 0, profile.HighlightContainerWhenSelected,
                        (b) => { profile.HighlightContainerWhenSelected = b; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();


            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetContainers.RecolorContainerGumpByWithContainerHue, 0, profile.HueContainerGumps, (b) => { profile.HueContainerGumps = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new CheckboxWithLabel
                        (lang.GetContainers.OverrideContainerGumpLocations, 0, profile.OverrideContainerLocation, (b) => { profile.OverrideContainerLocation = b; }),
                    MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    "",
                    new ComboBoxWithLabel
                (World,
                        lang.GetContainers.OverridePosition, 0, ThemeSettings.COMBO_BOX_WIDTH,
                        new string[]
                        {
                            lang.GetContainers.PositionOpt_NearContainer, lang.GetContainers.PositionOpt_TopRight, lang.GetContainers.PositionOpt_LastDraggedPosition,
                            lang.GetContainers.RememberEachContainer
                        }, profile.OverrideContainerLocationSetting, (i, s) => { profile.OverrideContainerLocationSetting = i; }
                    ), MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            ModernButton rebuildContainers;

            options.Add
            (
                s = new SettingsOption
                (
                    "", rebuildContainers = new ModernButton(0, 0, 130, 40, ButtonAction.Activate, lang.GetContainers.RebuildContainersTxt, ThemeSettings.BUTTON_FONT_COLOR, 999)
                    {
                        IsSelected = true,
                        IsSelectable = true
                    }, MainContent.RightWidth, (int)PAGE.Containers
                )
            );

            rebuildContainers.MouseUp += (s, e) => { World.ContainerManager.BuildContainerFile(true); };
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildExperimental()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetExperimental.DisableDefaultUoHotkeys, 0, profile.DisableDefaultHotkeys, (b) => { profile.DisableDefaultHotkeys = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetExperimental.DisableArrowsNumlockArrowsPlayerMovement, 0, profile.DisableArrowBtn, (b) => { profile.DisableArrowBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetExperimental.DisableTabToggleWarmode, 0, profile.DisableTabBtn, (b) => { profile.DisableTabBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetExperimental.DisableCtrlQWMessageHistory, 0, profile.DisableCtrlQWBtn, (b) => { profile.DisableCtrlQWBtn = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    "", new CheckboxWithLabel(lang.GetExperimental.DisableRightLeftClickAutoMove, 0, profile.DisableAutoMove, (b) => { profile.DisableAutoMove = b; }),
                    MainContent.RightWidth, (int)PAGE.Experimental
                )
            );

            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildNameplates()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));
            int page = ((int)PAGE.NameplateOptions + 1000);

            #region New entry

            ModernButton b;

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetNamePlates.NewEntry, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
            );

            b.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new
                (
                    World, 250, 150, lang.GetNamePlates.NameOverheadEntryName, name =>
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
                            nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, name, ThemeSettings.BUTTON_FONT_COLOR)
                            {
                                ButtonParameter = page + 1 + content.LeftArea.Children.Count,
                                Tag = option
                            }
                        );

                        nb.IsSelected = true;
                        content.ActivePage = nb.ButtonParameter;
                        World.NameOverHeadManager.AddOption(option);

                        content.AddToRight(new NameOverheadAssignControl(World, option), false, nb.ButtonParameter);
                    }
                )
                {
                    CanCloseWithRightClick = true
                };

                UIManager.Add(dialog);
            };

            #endregion

            #region Delete entry

            page = ((int)PAGE.Macros + 1001);

            content.AddToLeft
            (
                b = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.Activate, lang.GetNamePlates.DeleteEntry, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = page,
                    IsSelectable = false
                }
            );

            b.MouseUp += (ss, ee) =>
            {
                ModernButton nb = content.LeftArea.FindControls<ModernButton>().SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    var dialog = new QuestionGump
                    (
                        World, ResGumps.MacroDeleteConfirmation, b =>
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
                    nb = new ModernButton(0, 0, content.LeftWidth, 40, ButtonAction.SwitchPage, option.Name, ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = page + 1 + content.LeftArea.Children.Count,
                        Tag = option
                    }
                );

                content.AddToRight(new NameOverheadAssignControl(World, option), false, nb.ButtonParameter);
            }

            if (nb != null)
            {
                nb.IsSelected = true;
                content.ActivePage = nb.ButtonParameter;
            }

            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.NameplateOptions));
        }

        private void BuildCooldowns()
        {
            SettingsOption s;
            PositionHelper.Reset();

            options.Add(s = new SettingsOption(lang.GetCooldowns.CustomCooldownBars, new Area(false), MainContent.RightWidth, (int)PAGE.TUOCooldowns));
            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.Indent();

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCooldowns.PositionX, new InputField
                    (
                        100, 40, text: profile.CoolDownX.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CoolDownX = v;
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.TUOCooldowns
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    lang.GetCooldowns.PositionY, new InputField
                    (
                        100, 40, text: profile.CoolDownY.ToString(), numbersOnly: true, onTextChanges: (s, e) =>
                        {
                            if (int.TryParse(((InputField.StbTextBox)s).Text, out int v))
                            {
                                profile.CoolDownY = v;
                            }
                        }
                    ), MainContent.RightWidth, (int)PAGE.TUOCooldowns
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.BlankLine();

            options.Add
            (
                s = new SettingsOption
                (
                    string.Empty,
                    new CheckboxWithLabel(lang.GetCooldowns.UseLastMovedBarPosition, 0, profile.UseLastMovedCooldownPosition, (b) => { profile.UseLastMovedCooldownPosition = b; }),
                    MainContent.RightWidth, (int)PAGE.TUOCooldowns
                )
            );

            PositionHelper.PositionControl(s.FullControl);
            PositionHelper.RemoveIndent();

            PositionHelper.BlankLine();
            PositionHelper.BlankLine();

            options.Add(s = new SettingsOption(lang.GetCooldowns.Conditions, new Area(false), MainContent.RightWidth, (int)PAGE.TUOCooldowns));
            PositionHelper.PositionControl(s.FullControl);

            var conditionsDataBox = new DataBox(0, 0, 0, 0)
            {
                WantUpdateSize = true
            };

            ModernButton addcond;

            options.Add
            (
                s = new SettingsOption
                (
                    "", addcond = new ModernButton(0, 0, 175, 40, ButtonAction.Activate, lang.GetCooldowns.AddCondition, ThemeSettings.BUTTON_FONT_COLOR), MainContent.RightWidth,
                    (int)PAGE.TUOCooldowns
                )
            );

            addcond.MouseUp += (s, e) =>
            {
                CoolDownBar.CoolDownConditionData.GetConditionData(profile.CoolDownConditionCount, true);

                Gump g = UIManager.GetGump<ModernOptionsGump>();

                if (g != null)
                {
                    Point pos = g.Location;
                    g.Dispose();

                    g = new ModernOptionsGump(World)
                    {
                        Location = pos
                    };

                    g.ChangePage((int)PAGE.TUOCooldowns);
                    UIManager.Add(g);
                }
            };

            PositionHelper.PositionControl(s.FullControl);

            int count = profile.CoolDownConditionCount;

            for (int i = 0; i < count; i++)
            {
                conditionsDataBox.Add(GenConditionControl(i, MainContent.RightWidth - 19, false));
            }

            conditionsDataBox.ReArrangeChildren();
            conditionsDataBox.ForceSizeUpdate();

            var scroll = new ScrollArea(0, 0, MainContent.RightWidth, MainContent.Height - PositionHelper.Y)
            {
                CanMove = true,
                AcceptMouseInput = true
            };

            scroll.Add(conditionsDataBox);

            options.Add(s = new SettingsOption("", scroll, MainContent.RightWidth, (int)PAGE.TUOCooldowns));
            PositionHelper.PositionControl(s.FullControl);
        }

        private void BuildTazUO()
        {
            var content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));
            Control c;
            int page;

            #region General

            page = ((int)PAGE.TUOOptions + 1000);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.GridContainers, page, content.LeftWidth));

            content.AddToRight
                (new HttpClickableLink("Grid Containers Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-Containers", ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.EnableGridContainers, 0, profile.UseGridLayoutContainerGumps, (b) => { profile.UseGridLayoutContainerGumps = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.GridContainersDefaultToOldStyleView, 0, profile.GridContainersDefaultToOldStyleView, (b) => { profile.GridContainersDefaultToOldStyleView = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.GridContainerScale, 0, ThemeSettings.SLIDER_WIDTH, 50, 200, profile.GridContainersScale, (i) => { profile.GridContainersScale = (byte)i; }),
                true, page
            );

            content.Indent();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.AlsoScaleItems, 0, profile.GridContainerScaleItems, (b) => { profile.GridContainerScaleItems = b; }), true, page);

            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.GridItemBorderOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.GridBorderAlpha, (i) =>
                    {
                        profile.GridBorderAlpha = (byte)i;
                        GridContainer.GridItem.StaticGridContainerSettingUpdated();
                    }), true, page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetTazUO.BorderColor, profile.GridBorderHue, (h) =>
            {
                profile.GridBorderHue = h;
                GridContainer.GridItem.StaticGridContainerSettingUpdated();
            }), true, page);
            content.RemoveIndent();

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.ContainerOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.ContainerOpacity, (i) =>
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
                    World, lang.GetTazUO.BackgroundColor, profile.AltGridContainerBackgroundHue, (h) =>
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
                    lang.GetTazUO.UseContainersHue, 0, profile.Grid_UseContainerHue, (b) =>
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
                    lang.GetTazUO.SearchStyle, 0, ThemeSettings.COMBO_BOX_WIDTH, new string[] { lang.GetTazUO.OnlyShow, lang.GetTazUO.Highlight }, profile.GridContainerSearchMode,
                    (i, s) => { profile.GridContainerSearchMode = i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
                (c = new CheckboxWithLabel(lang.GetTazUO.EnableContainerPreview, 0, profile.GridEnableContPreview, (b) => { profile.GridEnableContPreview = b; }), true, page);

            c.SetTooltip(lang.GetTazUO.TooltipPreview);

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    lang.GetTazUO.MakeAnchorable, 0, profile.EnableGridContainerAnchor, (b) =>
                    {
                        profile.EnableGridContainerAnchor = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            c.SetTooltip(lang.GetTazUO.TooltipGridAnchor);

            content.BlankLine();

            content.AddToRight
            (
                new ComboBoxWithLabel
                (World,
                    lang.GetTazUO.ContainerStyle, 0, ThemeSettings.COMBO_BOX_WIDTH, Enum.GetNames(typeof(GridContainer.BorderStyle)), profile.Grid_BorderStyle, (i, s) =>
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
                    lang.GetTazUO.HideBorders, 0, profile.Grid_HideBorder, (b) =>
                    {
                        profile.Grid_HideBorder = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.DefaultGridRows, 0, ThemeSettings.SLIDER_WIDTH, 1, 20, profile.Grid_DefaultRows, (i) => { profile.Grid_DefaultRows = i; }), true,
                page
            );

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.DefaultGridColumns, 0, ThemeSettings.SLIDER_WIDTH, 1, 20, profile.Grid_DefaultColumns, (i) => { profile.Grid_DefaultColumns = i; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new HttpClickableLink
                    ("Grid Highlighting Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Grid-highlighting-based-on-item-properties", ThemeSettings.TEXT_FONT_COLOR), true, page
            );

            content.AddToRight
            (
                c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, lang.GetTazUO.GridHighlightSettings, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) => { GridHighlightMenu.Open(World); };

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.GridHighlightSize, 0, ThemeSettings.SLIDER_WIDTH, 1, 5, profile.GridHighlightSize, (i) => { profile.GridHighlightSize = i; }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.GridHighlightProperties, 0, profile.GridHighlightProperties, (b) => { profile.GridHighlightProperties = b; }),
                true, page
            );

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.GridHighlightShowRuleName, 0, profile.GridHighlightShowRuleName, (b) => { profile.GridHighlightShowRuleName = b; }),
                true, page
            );

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.GridDisableTargeting, 0, profile.DisableTargetingGridContainers, (b) => { profile.DisableTargetingGridContainers = b; }),
                true, page
            );

            #endregion

            #region Journal

            page = ((int)PAGE.TUOOptions + 1001);
            content.ResetRightSide();

            content.AddToRight(new HttpClickableLink("Journal Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Journal", ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Journal, page, content.LeftWidth));

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.MaxJournalEntries, 0, ThemeSettings.SLIDER_WIDTH, 100, 2000, profile.MaxJournalEntries, (i) => { profile.MaxJournalEntries = i; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.JournalOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.JournalOpacity, (i) =>
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
                    World, lang.GetTazUO.JournalBackgroundColor, profile.AltJournalBackgroundHue, (h) =>
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
                    lang.GetTazUO.JournalStyle, 0, ThemeSettings.COMBO_BOX_WIDTH, Enum.GetNames(typeof(ResizableJournal.BorderStyle)), profile.JournalStyle, (i, s) =>
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
                    lang.GetTazUO.JournalHideBorders, 0, profile.HideJournalBorder, (b) =>
                    {
                        profile.HideJournalBorder = b;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.HideTimestamp, 0, profile.HideJournalTimestamp, (b) => { profile.HideJournalTimestamp = b; }), true, page);
            content.BlankLine();
            content.AddToRight(c = new CheckboxWithLabel(lang.GetTazUO.JournalHideSystemPrefix, 0, profile.HideJournalSystemPrefix, (b) => { profile.HideJournalSystemPrefix = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    lang.GetTazUO.MakeAnchorable, 0, profile.JournalAnchorEnabled, (b) =>
                    {
                        profile.JournalAnchorEnabled = b;
                        ResizableJournal.UpdateJournalOptions();
                    }
                ), true, page
            );

            #endregion

            #region Modern paperdoll

            page = ((int)PAGE.TUOOptions + 1002);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.ModernPaperdoll, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new HttpClickableLink("Modern Paperdoll Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Alternate-Paperdoll", ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();

            content.AddToRight
                (c = new CheckboxWithLabel(lang.GetTazUO.EnableModernPaperdoll, 0, profile.UseModernPaperdoll, (b) => { profile.UseModernPaperdoll = b; }), true, page);

            content.Indent();
            content.BlankLine();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, lang.GetTazUO.PaperdollHue, profile.ModernPaperDollHue, (h) =>
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
                    World, lang.GetTazUO.DurabilityBarHue, profile.ModernPaperDollDurabilityHue, (h) =>
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
                    lang.GetTazUO.ShowDurabilityBarBelow, 0, ThemeSettings.SLIDER_WIDTH, 1, 100, profile.ModernPaperDoll_DurabilityPercent,
                    (i) => { profile.ModernPaperDoll_DurabilityPercent = i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    lang.GetTazUO.PaperdollAnchor, 0, profile.ModernPaperdollAnchorEnabled, (b) =>
                    {
                        profile.ModernPaperdollAnchorEnabled = b;
                        ModernPaperdoll.UpdateAllOptions();
                    }
                ), true, page
            );

            #endregion

            #region Nameplates

            page = ((int)PAGE.TUOOptions + 1003);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Nameplates, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new HttpClickableLink("Nameplates Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Nameplate-options", ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.NameplatesAlsoActAsHealthBars, 0, profile.NamePlateHealthBar, (b) => { profile.NamePlateHealthBar = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.HpOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateHealthBarOpacity, (i) => { profile.NamePlateHealthBarOpacity = (byte)i; }),
                true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.HideNameplatesIfFullHealth, 0, profile.NamePlateHideAtFullHealth, (b) => { profile.NamePlateHideAtFullHealth = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.OnlyInWarmode, 0, profile.NamePlateHideAtFullHealthInWarmode, (b) => { profile.NamePlateHideAtFullHealthInWarmode = b; }), true,
                page
            );

            content.RemoveIndent();
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.BorderOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateBorderOpacity, (i) => { profile.NamePlateBorderOpacity = (byte)i; }),
                true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.BackgroundOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.NamePlateOpacity, (i) => { profile.NamePlateOpacity = (byte)i; }), true, page
            );

            content.AddToRight
            (
                new CheckboxWithLabel("Avoid overlap", 0, profile.NamePlateAvoidOverlap, (b) => { profile.NamePlateAvoidOverlap = b; }), true, page
            );

            #endregion

            #region Mobiles

            page = ((int)PAGE.TUOOptions + 1004);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Mobiles, page, content.LeftWidth));
            content.ResetRightSide();
            content.AddToRight(c = new ModernColorPickerWithLabel(World, lang.GetTazUO.DamageToSelf, profile.DamageHueSelf, (h) => { profile.DamageHueSelf = h; }), true, page);

            content.AddToRight
            (
                c = new ModernColorPickerWithLabel(World, lang.GetTazUO.DamageToOthers, profile.DamageHueOther, (h) => { profile.DamageHueOther = h; })
                {
                    X = 250,
                    Y = c.Y
                }, false, page
            );

            content.AddToRight(c = new ModernColorPickerWithLabel(World, lang.GetTazUO.DamageToPets, profile.DamageHuePet, (h) => { profile.DamageHuePet = h; }), true, page);

            content.AddToRight
            (
                c = new ModernColorPickerWithLabel(World, lang.GetTazUO.DamageToAllies, profile.DamageHueAlly, (h) => { profile.DamageHueAlly = h; })
                {
                    X = 250,
                    Y = c.Y
                }, false, page
            );

            content.AddToRight
                (c = new ModernColorPickerWithLabel(World, lang.GetTazUO.DamageToLastAttack, profile.DamageHueLastAttck, (h) => { profile.DamageHueLastAttck = h; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.DisplayPartyChatOverPlayerHeads, 0, profile.DisplayPartyChatOverhead, (b) => { profile.DisplayPartyChatOverhead = b; }),
                true, page
            );

            c.SetTooltip(lang.GetTazUO.TooltipPartyChat);
            content.BlankLine();

            content.AddToRight
            (
                c = new SliderWithLabel
                    (lang.GetTazUO.OverheadTextWidth, 0, ThemeSettings.SLIDER_WIDTH, 0, 600, profile.OverheadChatWidth, (i) => { profile.OverheadChatWidth = i; }), true, page
            );

            c.SetTooltip(lang.GetTazUO.TooltipOverheadText);
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.BelowMobileHealthBarScale, 0, ThemeSettings.SLIDER_WIDTH, 1, 5, profile.HealthLineSizeMultiplier,
                    (i) => { profile.HealthLineSizeMultiplier = (byte)i; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                    (lang.GetTazUO.AutomaticallyOpenHealthBarsForLastAttack, 0, profile.OpenHealthBarForLastAttack, (b) => { profile.OpenHealthBarForLastAttack = b; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.UpdateOneBarAsLastAttack, 0, profile.UseOneHPBarForLastAttack, (b) => { profile.UseOneHPBarForLastAttack = b; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.HiddenPlayerOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.HiddenBodyAlpha, (i) => { profile.HiddenBodyAlpha = (byte)i; }), true, page
            );

            content.Indent();
            content.AddToRight(c = new ModernColorPickerWithLabel(World, lang.GetTazUO.HiddenPlayerHue, profile.HiddenBodyHue, (h) => { profile.HiddenBodyHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.RegularPlayerOpacity, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.PlayerConstantAlpha, (i) => { profile.PlayerConstantAlpha = i; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.AutoFollowDistance, 0, ThemeSettings.SLIDER_WIDTH, 1, 10, profile.AutoFollowDistance, (i) => { profile.AutoFollowDistance = i; }),
                true, page
            );

            content.Indent();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisableAutoFollow, 0, profile.DisableAutoFollowAlt, (i) => { profile.DisableAutoFollowAlt = i; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    lang.GetTazUO.DisableMouseInteractionsForOverheadText, 0, profile.DisableMouseInteractionOverheadText,
                    (b) => { profile.DisableMouseInteractionOverheadText = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.OverridePartyMemberHues, 0, profile.OverridePartyAndGuildHue, (b) => { profile.OverridePartyAndGuildHue = b; }), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.ShowTargetIndicator, isChecked: profile.ShowTargetIndicator, valueChanged: (b) => { profile.ShowTargetIndicator = b; }), true,
                page
            );

            content.BlankLine();

            content.AddToRight
                (c = new SliderWithLabel(lang.GetTazUO.TurnDelay, 0, ThemeSettings.SLIDER_WIDTH, 45, 120, profile.TurnDelay, i => profile.TurnDelay = (ushort)i), true, page);

            c.SetTooltip("This settting may cause throttling, Use with caution.");

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.IgnoreStaminaCheck, 0, profile.IgnoreStaminaCheck, (b) => profile.IgnoreStaminaCheck = b), true, page);

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.DisableGrayEnemies, 0, profile.DisableGrayEnemies, (b) => profile.DisableGrayEnemies = b), true, page);

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetGeneral.DisableDismountWarmode, 0, profile.DisableDismountInWarMode, (b) => profile.DisableDismountInWarMode = b), true, page);
            #endregion

            #region Misc

            page = ((int)PAGE.TUOOptions + 1005);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Misc, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new HttpClickableLink("Misc Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Miscellaneous", ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.DisableSystemChat, 0, profile.DisableSystemChat, (b) => { profile.DisableSystemChat = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetGeneral.AutoAvoidObstacules, isChecked: profile.AutoAvoidObstacules, valueChanged: (b) => { profile.AutoAvoidObstacules = b; }), true,
                page
            );

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableImprovedBuffGump, 0, profile.UseImprovedBuffBar, (b) => { profile.UseImprovedBuffBar = b; }), true, page);
            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetTazUO.BuffGumpHue, profile.ImprovedBuffBarHue, (h) => { profile.ImprovedBuffBarHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new ModernColorPickerWithLabel
                (
                    World, lang.GetTazUO.MainGameWindowBackground, profile.MainWindowBackgroundHue, (h) =>
                    {
                        profile.MainWindowBackgroundHue = h;
                        GameController.UpdateBackgroundHueShader();
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.EnableHealthIndicatorBorder, 0, profile.EnableHealthIndicator, (b) => { profile.EnableHealthIndicator = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.OnlyShowBelowHp, 0, ThemeSettings.SLIDER_WIDTH, 1, 100, (int)profile.ShowHealthIndicatorBelow * 100,
                    (i) => { profile.ShowHealthIndicatorBelow = i / 100f; }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.Size, 0, ThemeSettings.SLIDER_WIDTH, 1, 25, profile.HealthIndicatorWidth, (i) => { profile.HealthIndicatorWidth = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.SpellIconScale, 0, ThemeSettings.SLIDER_WIDTH, 50, 300, profile.SpellIconScale, (i) => { profile.SpellIconScale = i; }), true,
                page
            );

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.DisplayMatchingHotkeysOnSpellIcons, 0, profile.SpellIcon_DisplayHotkey, (b) => { profile.SpellIcon_DisplayHotkey = b; }), true,
                page
            );

            content.Indent();
            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetTazUO.HotkeyTextHue, profile.SpellIcon_HotkeyHue, (h) => { profile.SpellIcon_HotkeyHue = h; }), true, page);
            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                    (lang.GetTazUO.EnableGumpOpacityAdjustViaAltScroll, 0, profile.EnableAlphaScrollingOnGumps, (b) => { profile.EnableAlphaScrollingOnGumps = b; }), true, page
            );

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableAdvancedShopGump, 0, profile.UseModernShopGump, (b) => { profile.UseModernShopGump = b; }), true, page);
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.DisplaySkillProgressBarOnSkillChanges, 0, profile.DisplaySkillBarOnChange, (b) => { profile.DisplaySkillBarOnChange = b; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new InputFieldWithLabel
                    (lang.GetTazUO.TextFormat, ThemeSettings.INPUT_WIDTH, profile.SkillBarFormat, false, (s, e) => { profile.SkillBarFormat = ((InputField.StbTextBox)s).Text; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.EnableSpellIndicatorSystem, 0, profile.EnableSpellIndicators, (b) => { profile.EnableSpellIndicators = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                c = new ModernButton(0, 0, 200, 40, ButtonAction.Activate, lang.GetTazUO.ImportFromUrl, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true,
                    IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.Add
                    (
                        new InputRequest
                        (
                            World, lang.GetTazUO.InputRequestUrl, lang.GetTazUO.Download, lang.GetTazUO.Cancel, (r, s) =>
                            {
                                if (r == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(s))
                                {
                                    if (Uri.TryCreate(s, UriKind.Absolute, out Uri uri))
                                    {
                                        GameActions.Print(World, lang.GetTazUO.AttemptingToDownloadSpellConfig);

                                        Task.Factory.StartNew
                                        (() =>
                                        {
                                            try
                                            {
                                                using var httpClient = new HttpClient();
                                                string result = httpClient.GetStringAsync(uri).Result;

                                                    if (SpellVisualRangeManager.Instance.LoadFromString(result))
                                                    {
                                                        GameActions.Print(World, lang.GetTazUO.SuccesfullyDownloadedNewSpellConfig);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    GameActions.Print(World, string.Format(lang.GetTazUO.FailedToDownloadTheSpellConfigExMessage, ex.Message));
                                                }
                                            }
                                        );
                                    }
                                }
                            }, "https://github.com/PlayTazUO/TazUO/raw/refs/heads/dev/src/ClassicUO.Client/Game/Managers/DefaultSpellIndicatorConfig.json"
                        )
                        {
                            X = (Client.Game.Window.ClientBounds.Width >> 1) - 50,
                            Y = (Client.Game.Window.ClientBounds.Height >> 1) - 50
                        }
                    );
                }
            };

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                new CheckboxWithLabel
                (
                    lang.GetTazUO.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars, content.RightWidth - 30, profile.CloseHealthBarIfAnchored,
                    (b) => { profile.CloseHealthBarIfAnchored = b; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.EnableAutoResyncOnHangDetection, 0, profile.ForceResyncOnHang, (b) => { profile.ForceResyncOnHang = b; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.PlayerOffsetX, 0, ThemeSettings.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.X,
                    (i) => { profile.PlayerOffset = new Point(i, profile.PlayerOffset.Y); }
                ), true, page
            );

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.PlayerOffsetY, 0, ThemeSettings.SLIDER_WIDTH, -20, 20, profile.PlayerOffset.Y,
                    (i) => { profile.PlayerOffset = new Point(profile.PlayerOffset.X, i); }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new InputFieldWithLabel
                (
                    lang.GetTazUO.SOSGumpID, ThemeSettings.INPUT_WIDTH, profile.SOSGumpID.ToString(), true, (s, e) =>
                    {
                        if (uint.TryParse(((InputField.StbTextBox)s).Text, out uint id))
                        {
                            profile.SOSGumpID = id;
                        }
                    }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.UseWASDMovement, isChecked: profile.UseWASDInsteadArrowKeys, valueChanged: (e) => { profile.UseWASDInsteadArrowKeys = e; }),
                true, page
            );
            c.SetTooltip("This only works if you have enable chat by pressing enter, and chat disabled. Otherwise you will still be typing into your chatbar.");

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.ApplyBorderCaveTiles, isChecked: profile.EnableCaveBorder, valueChanged: (e) =>
                {
                    profile.EnableCaveBorder = e;
                    if(e)
                        StaticFilters.ApplyCaveTileBorder();
                }),
                true, page
            );
            c.SetTooltip("After disabling, you need to restart the client to revert to no borders.");

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.EnableASyncMapLoading, isChecked: profile.EnableASyncMapLoading, valueChanged: (e) =>
                {
                    profile.EnableASyncMapLoading = e;
                    if(GameScene.Instance != null)
                        GameScene.Instance.ASyncMapLoading = e;
                }),
                true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                c = new CheckboxWithLabel(lang.GetTazUO.ForceManagedZlib, isChecked: ZLib.ManagedZlibForced, valueChanged: (e) =>
                {
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.MANAGED_ZLIB, e);
                    ZLib.SetForceManagedZlib(e);
                }),
                true, page
            );
            c.SetTooltip("This may impact performance negatively, but some unix systems have issues using unmanaged zlibs.");

            #region HideHouses
            content.BlankLine();

            content.AddToRight(new HttpClickableLink("Houses Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.HideHouses", ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();
            content.AddToRight
            (
                c = new CheckboxWithLabel
                (
                    lang.GetTazUO.EnableHouseTransparency, 0, profile.ForceHouseTransparency, (b) =>
                    {
                        profile.ForceHouseTransparency = b;
                    }
                ), true, page
            );

            content.BlankLine();
            content.Indent();
            content.AddToRight
            (
                new ModernColorPickerWithLabel(World, lang.GetTazUO.HouseTransparencyTileHue, profile.ForcedTransparencyHouseTileHue, (h) => { profile.ForcedTransparencyHouseTileHue = h; }), true, page
            );
            content.RemoveIndent();

            content.BlankLine();
            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.ForcedHouseTransparencyLevel, 0, ThemeSettings.SLIDER_WIDTH, 0, 255, profile.ForcedHouseTransparency, (i) =>
                    {
                        profile.ForcedHouseTransparency = (byte)i;
                    }
                ), true, page
            );
            #endregion


            #endregion

            #region Tooltips

            page = ((int)PAGE.TUOOptions + 1006);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Tooltips, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
                (new CheckboxWithLabel(lang.GetTazUO.AlignTooltipsToTheLeftSide, 0, profile.LeftAlignToolTips, (b) => { profile.LeftAlignToolTips = b; }), true, page);

            content.Indent();

            content.AddToRight
            (
                new CheckboxWithLabel(lang.GetTazUO.AlignMobileTooltipsToCenter, 0, profile.ForceCenterAlignTooltipMobiles, (b) => { profile.ForceCenterAlignTooltipMobiles = b; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight(new ModernColorPickerWithLabel(World, lang.GetTazUO.BackgroundHue, profile.ToolTipBGHue, (h) => { profile.ToolTipBGHue = h; }), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new InputFieldWithLabel
                (
                    lang.GetTazUO.HeaderFormatItemName, ThemeSettings.INPUT_WIDTH, profile.TooltipHeaderFormat, false,
                    (s, e) => { profile.TooltipHeaderFormat = ((InputField.StbTextBox)s).Text; }
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
                (c = new CheckboxWithLabel(lang.GetTazUO.ForcedTooltips, 0, profile.ForceTooltipsOnOldClients, b => { profile.ForceTooltipsOnOldClients = b; }), true, page);

            c.SetTooltip("This feature relies on simulating single clicking items and is not a perfect solution.");

            content.BlankLine();

            content.AddToRight
                (new HttpClickableLink("Tooltip Overrides Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Tooltip-Override", ThemeSettings.TEXT_FONT_COLOR), true, page);

            NiceButton tooltipConfigButton;
            content.AddToRight(tooltipConfigButton = new NiceButton(0, 0, 150, 25, ButtonAction.Activate, "Open Tooltip Config")
            {
                IsSelectable = false,
                DisplayBorder = true
            }, true, page);

            tooltipConfigButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    UIManager.Add(new TooltipConfigGump());
                }
            };

            #endregion

            #region Font settings

            const int minFontSize = 5;
            const int maxFontSize = 50;
            page = ((int)PAGE.TUOOptions + 1007);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.FontSettings, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new HttpClickableLink("TTF Fonts Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.TTF-Fonts", ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight
                (new SliderWithLabel(lang.GetTazUO.TtfFontBorder, 0, ThemeSettings.SLIDER_WIDTH, 0, 2, profile.TextBorderSize, (i) => { profile.TextBorderSize = i; }), true, page);

            content.BlankLine();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                (
                    lang.GetTazUO.InfobarFont, ProfileManager.CurrentProfile.InfoBarFont, (i, s) =>
                    {
                        ProfileManager.CurrentProfile.InfoBarFont = s;
                        InfoBarGump.UpdateAllOptions();
                    }
                ), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.InfoBarFontSize, (i) =>
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
                    (lang.GetTazUO.SystemChatFont, ProfileManager.CurrentProfile.GameWindowSideChatFont, (i, s) => { ProfileManager.CurrentProfile.GameWindowSideChatFont = s; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.GameWindowSideChatFontSize, (i) => { profile.GameWindowSideChatFontSize = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                    (lang.GetTazUO.TooltipFont, ProfileManager.CurrentProfile.SelectedToolTipFont, (i, s) => { ProfileManager.CurrentProfile.SelectedToolTipFont = s; }), true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.SelectedToolTipFontSize, (i) => { profile.SelectedToolTipFontSize = i; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(lang.GetTazUO.OverheadFont, ProfileManager.CurrentProfile.OverheadChatFont, (i, s) => { ProfileManager.CurrentProfile.OverheadChatFont = s; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.OverheadChatFontSize, (i) => { profile.OverheadChatFontSize = i; }),
                true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector
                    (lang.GetTazUO.JournalFont, ProfileManager.CurrentProfile.SelectedTTFJournalFont, (i, s) => { ProfileManager.CurrentProfile.SelectedTTFJournalFont = s; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.SelectedJournalFontSize, (i) => { profile.SelectedJournalFontSize = i; }), true, page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(lang.GetTazUO.NameplateFont, ProfileManager.CurrentProfile.NamePlateFont, (i, s) => { ProfileManager.CurrentProfile.NamePlateFont = s; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.NamePlateFontSize, (i) => { profile.NamePlateFontSize = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            content.AddToRight
            (
                GenerateFontSelector(lang.GetTazUO.Optionsfont, ProfileManager.CurrentProfile.OptionsFont, (i, s) => { ProfileManager.CurrentProfile.OptionsFont = s; }),
                true, page
            );

            content.Indent();

            content.AddToRight
            (
                new SliderWithLabel(lang.GetTazUO.SharedSize, 0, ThemeSettings.SLIDER_WIDTH, minFontSize, maxFontSize, profile.OptionsFontSize, (i) => { profile.OptionsFontSize = i; }), true,
                page
            );

            content.RemoveIndent();
            content.BlankLine();

            #endregion

            #region Controller settings

            page = ((int)PAGE.TUOOptions + 1008);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Controller, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.EnableController, 0, profile.ControllerEnabled, (b) => profile.ControllerEnabled = b), true, page);
            content.BlankLine();

            content.AddToRight
                (new HttpClickableLink("Controller Support Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Controller-Support", ThemeSettings.TEXT_FONT_COLOR), true, page);

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                    (lang.GetTazUO.MouseSesitivity, 0, ThemeSettings.SLIDER_WIDTH, 1, 20, profile.ControllerMouseSensativity, (i) => { profile.ControllerMouseSensativity = i; }),
                true, page
            );

            #endregion

            #region Settings transfers

            page = ((int)PAGE.TUOOptions + 1009);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.SettingsTransfers, page, content.LeftWidth));
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

                        if (FileSystemHelper.RemoveInvalidChars(profile.ServerName) == FileSystemHelper.RemoveInvalidChars(Path.GetFileName(server)))
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
                    string.Format(lang.GetTazUO.SettingsWarning, locations.Count), ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.AddToRight
            (
                c = new ModernButton
                    (0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideAll, locations.Count - 1), ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true,
                    IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(locations);
                    GameActions.Print(World, string.Format(lang.GetTazUO.OverrideSuccess, locations.Count - 1), 32, Data.MessageType.System);
                }
            };

            content.AddToRight
            (
                c = new ModernButton
                (
                    0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, string.Format(lang.GetTazUO.OverrideSame, sameServerLocations.Count - 1),
                    ThemeSettings.BUTTON_FONT_COLOR
                )
                {
                    IsSelectable = true,
                    IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OverrideAllProfiles(sameServerLocations);
                    GameActions.Print(World, string.Format(lang.GetTazUO.OverrideSuccess, sameServerLocations.Count - 1), 32, Data.MessageType.System);
                }
            };

            content.AddToRight
            (
                c = new ModernButton(0, 0, content.RightWidth - 20, 40, ButtonAction.Activate, lang.GetTazUO.SetAsDefault, ThemeSettings.BUTTON_FONT_COLOR)
                {
                    IsSelectable = true,
                    IsSelected = true
                }, true, page
            );

            c.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ProfileManager.SetProfileAsDefault(ProfileManager.CurrentProfile);
                    GameActions.Print(World, lang.GetTazUO.SetAsDefaultSuccess, 32, Data.MessageType.System);
                }
            };

            #endregion

            #region Gump scaling

            page = ((int)PAGE.TUOOptions + 1010);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.GumpScaling, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight(new HttpClickableLink("Scaling Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Global-Scaling", ThemeSettings.TEXT_FONT_COLOR), true, page);
            content.BlankLine();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    lang.GetTazUO.ScalingInfo, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.BlankLine();

            content.AddToRight
            (
                new SliderWithLabel
                (
                    lang.GetTazUO.PaperdollGump, 0, ThemeSettings.SLIDER_WIDTH, 50, 300, (int)(profile.PaperdollScale * 100), (i) =>
                    {
                        //Must be cast even though VS thinks it's redundant.
                        double v = (double)i / (double)100;
                        profile.PaperdollScale = v > 0 ? v : 1f;
                    }
                ), true, page
            );

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.GlobalScaling, 0, profile.GlobalScaling, b => profile.GlobalScaling = b), true, page);

            SliderWithLabel s;
            content.AddToRight(s = new SliderWithLabel(lang.GetTazUO.GlobalScale, 0, ThemeSettings.SLIDER_WIDTH, 50, 175, (int)(profile.GlobalScale * 100), null), true, page);

            ModernButton b;
            content.AddToRight(b = new ModernButton(s.X + s.Width + 75, s.Y - 20, 75, 40, ButtonAction.Activate, "Apply", ThemeSettings.BUTTON_FONT_COLOR), false, page);

            b.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    float v = ((float)s.GetValue() / (float)100);

                    if (v <= 0 || v == 1f)
                        profile.GlobalScaling = false;

                    profile.GlobalScale = v > 0 ? v : 1f;
                }
            };

            #endregion

            #region Hidden layers

            page = ((int)PAGE.TUOOptions + 1011);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.VisibleLayers, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    lang.GetTazUO.VisLayersInfo, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.DefaultCentered(content.RightWidth - 20)
                ), true, page
            );

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.HiddenLayersEnabled, 0, profile.HiddenLayersEnabled, (b) => { profile.HiddenLayersEnabled = b; }), true, page);

            content.BlankLine();
            content.AddToRight(new CheckboxWithLabel(lang.GetTazUO.OnlyForYourself, 0, profile.HideLayersForSelf, (b) => { profile.HideLayersForSelf = b; }), true, page);

            content.BlankLine();

            bool rightSide = false;

            foreach (Layer layer in (Layer[])Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.Invalid || layer == Layer.Hair || layer == Layer.Beard || layer == Layer.Backpack || layer == Layer.ShopBuyRestock || layer == Layer.ShopBuy ||
                    layer == Layer.ShopSell || layer == Layer.Bank || layer == Layer.Face || layer == Layer.Talisman || layer == Layer.Mount)
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
                        )
                        {
                            X = 200,
                            Y = c.Y
                        }, false, page
                    );

                    rightSide = false;
                }
            }

            #endregion

            #region Hotkeys

            page = ((int)PAGE.TUOOptions + 1016);
            content.AddToLeft(SubCategoryButton(lang.GetTazUO.Hotkeys, page, content.LeftWidth));
            content.ResetRightSide();

            content.AddToRight
            (
                TextBox.GetOne
                (
                    "These are not configurable here, this is a list of hotkeys built into the client.\nThere may be missing hotkeys, please report them on our Discord.",
                    ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(content.RightWidth - 15)
                ), true, page
            );

            content.BlankLine();

            int ewidth = content.RightWidth - 15;

            //Gumps ish
            content.AddToRight(GenHotKeyDisplay("Move gumps", "ALT", ewidth, ProfileManager.CurrentProfile.HoldAltToMoveGumps), true, page);

            content.AddToRight(GenHotKeyDisplay("Detatch anchored gumps", "ALT", ewidth, ProfileManager.CurrentProfile.HoldAltToMoveGumps), true, page);
            content.AddToRight(GenHotKeyDisplay("Show lock button on various gumps", "ALT", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Hold to close anchored gumps", "ALT", ewidth, ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored), true, page);
            content.AddToRight(GenHotKeyDisplay("Lock gump if it's lockable", "ALT CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Show gump lock icon where applicable", "ALT HOVER", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Adjust gump opacity", "ALT SCROLL-WHEEL", ewidth, ProfileManager.CurrentProfile.EnableAlphaScrollingOnGumps), true, page);

            //Grid container
            content.AddToRight(GenHotKeyDisplay("Grid container - move multiple items", "ALT CLICK-ITEM", ewidth), true, page);

            content.AddToRight
            (
                GenHotKeyDisplay
                (
                    "Grid container - add item to autoloot", "SHIFT CLICK-ITEM", ewidth,
                    ProfileManager.CurrentProfile.EnableAutoLoot && !ProfileManager.CurrentProfile.HoldShiftForContext && !ProfileManager.CurrentProfile.HoldShiftToSplitStack
                ), true, page
            );

            content.AddToRight(GenHotKeyDisplay("Grid container - lock item in slot", "CTRL CLICK-ITEM", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Grid container - compare item to equipped", "CTRL HOVER", ewidth), true, page);


            content.AddToRight(GenHotKeyDisplay("Remove item from counterbar", "ALT RIGHT-CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Click a mobile to follow them", "ALT CLICK", ewidth, !ProfileManager.CurrentProfile.DisableAutoFollowAlt), true, page);
            content.AddToRight(GenHotKeyDisplay("Activate chat", "ENTER", ewidth, ProfileManager.CurrentProfile.ActivateChatAfterEnter), true, page);
            content.AddToRight(GenHotKeyDisplay("Split item stacks", "SHIFT", ewidth, ProfileManager.CurrentProfile.HoldShiftToSplitStack), true, page);
            content.AddToRight(GenHotKeyDisplay("Show name plates", "CTRL SHIFT", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Pathfinding", "SHIFT CLICK/DOUBLE-CLICK", ewidth, ProfileManager.CurrentProfile.UseShiftToPathfind), true, page);
            content.AddToRight(GenHotKeyDisplay("Buy/Sell all of an item at a shop", "SHIFT DOUBLE-CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Item drag - Lock in position", "CTRL SCROL-WHEEL", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Zoom window", "CTRL SCROL-WHEEL", ewidth, ProfileManager.CurrentProfile.EnableMousewheelScaleZoom), true, page);
            content.AddToRight(GenHotKeyDisplay("Scroll through messages sent in chat", "CTRL q/w", ewidth, !ProfileManager.CurrentProfile.DisableCtrlQWBtn), true, page);
            content.AddToRight(GenHotKeyDisplay("Auto-start xml gump from menu", "CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("World Map - Pathfind", "CTRL RIGHT-CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("World Map - Add Marker", "CTRL CLICK", ewidth), true, page);
            content.AddToRight(GenHotKeyDisplay("Screen shot gump/tooltip only", "CTRL PRINTSCREEN", ewidth), true, page);

            #endregion



            options.Add(new SettingsOption("", content, MainContent.RightWidth, (int)PAGE.TUOOptions));
        }

        public override void Dispose()
        {
            base.Dispose();
            ProfileManager.CurrentProfile?.Save(World, ProfileManager.ProfilePath);
        }

        private void OverrideAllProfiles(List<ProfileLocationData> allProfiles)
        {
            foreach (ProfileLocationData profile in allProfiles)
            {
                ProfileManager.CurrentProfile.Save(World, profile.ToString(), false);
            }
        }

        private ComboBoxWithLabel GenerateFontSelector(string label, string selectedFont = "", Action<int, string> onSelect = null)
        {
            string[] fontArray = TrueTypeLoader.Instance.Fonts;
            int selectedFontInd = Array.IndexOf(fontArray, selectedFont);

            return new ComboBoxWithLabel(World, label, 0, ThemeSettings.COMBO_BOX_WIDTH, fontArray, selectedFontInd, onSelect);
        }

        public Control GenConditionControl(int key, int width, bool createIfNotExists)
        {
            var data = CoolDownBar.CoolDownConditionData.GetConditionData(key, createIfNotExists);

            var main = new Area
            {
                Width = width
            };

            var _background = new AlphaBlendControl();
            main.Add(_background);

            var _delete = new ModernButton(1, 1, 30, 40, ButtonAction.Activate, "X", ThemeSettings.BUTTON_FONT_COLOR);
            _delete.SetTooltip("Delete this cooldown bar");

            _delete.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    CoolDownBar.CoolDownConditionData.RemoveCondition(key);

                    Gump g = UIManager.GetGump<ModernOptionsGump>();

                    if (g != null)
                    {
                        Point pos = g.Location;
                        g.Dispose();

                        g = new ModernOptionsGump(World)
                        {
                            Location = pos
                        };

                        g.ChangePage((int)PAGE.TUOCooldowns);
                        UIManager.Add(g);
                    }
                }
            };

            main.Add(_delete);


            var _hueLabel = TextBox.GetOne("Hue:", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.BUTTON_FONT_COLOR, TextBox.RTLOptions.Default());
            _hueLabel.X = _delete.X + _delete.Width + 5;
            _hueLabel.Y = 10;
            main.Add(_hueLabel);

            var _hueSelector = new ModernColorPickerWithLabel(World, string.Empty, data.hue)
            {
                X = _hueLabel.X + _hueLabel.Width + 5,
                Y = 10
            };

            main.Add(_hueSelector);

            var _name = new InputField(140, 40, text: data.label)
            {
                X = _hueSelector.X + _hueSelector.Width + 10,
                Y = 1
            };

            main.Add(_name);

            var _cooldownLabel = TextBox.GetOne
                ("Cooldown:", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.BUTTON_FONT_COLOR, TextBox.RTLOptions.Default());

            _cooldownLabel.X = _name.X + _name.Width + 10;
            _cooldownLabel.Y = 10;
            main.Add(_cooldownLabel);

            var _cooldown = new InputField(45, 40, numbersOnly: true, text: data.cooldown.ToString())
            {
                Y = 1
            };

            _cooldown.X = _cooldownLabel.X + _cooldownLabel.Width + 10;
            main.Add(_cooldown);

            var _message_type = new ComboBoxWithLabel(World, string.Empty, 0, 85, new string[] { "All", "Self", "Other" }, data.message_type)
            {
                X = _cooldown.X + _cooldown.Width + 10,
                Y = 10
            };

            main.Add(_message_type);

            var _conditionText = new InputField(main.Width - 50, 40, text: data.trigger)
            {
                X = 1,
                Y = _delete.Height + 5
            };

            main.Add(_conditionText);

            var _replaceIfExists = new CheckboxWithLabel(isChecked: data.replace_if_exists)
            {
                X = _conditionText.X + _conditionText.Width + 2,
                Y = _conditionText.Y + 5
            };

            _replaceIfExists.SetTooltip("Replace any active cooldown of this type with a new one if triggered again.");
            main.Add(_replaceIfExists);

            var _save = new ModernButton(0, 1, 40, 40, ButtonAction.Activate, "Save", ThemeSettings.BUTTON_FONT_COLOR);
            _save.X = main.Width - _save.Width;
            _save.IsSelectable = true;
            _save.IsSelected = true;

            _save.MouseUp += (s, e) =>
            {
                CoolDownBar.CoolDownConditionData.SaveCondition
                    (key, _hueSelector.Hue, _name.Text, _conditionText.Text, int.Parse(_cooldown.Text), false, _message_type.SelectedIndex, _replaceIfExists.IsChecked);
            };

            main.Add(_save);

            var _preview = new ModernButton(0, 1, 65, 40, ButtonAction.Activate, "Preview", ThemeSettings.BUTTON_FONT_COLOR);
            _preview.X = _save.X - _preview.Width - 15;
            _preview.IsSelectable = true;
            _preview.IsSelected = true;

            _preview.MouseUp += (s, e) =>
            {
                if (int.TryParse(_cooldown.Text, out int value))
                {
                    CoolDownBarManager.AddCoolDownBar(World, TimeSpan.FromSeconds(value), _name.Text, _hueSelector.Hue, _replaceIfExists.IsChecked);
                }
            };

            main.Add(_preview);

            main.ForceSizeUpdate();

            _background.Width = width;
            _background.Height = main.Height;

            return main;
        }

        public Control GenHotKeyDisplay(string text, string hotkey, int width, bool enabled = true)
        {
            var d = new Area(false);
            d.Add(TextBox.GetOne(text, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));

            var hk = TextBox.GetOne(hotkey, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            hk.X = width - hk.MeasuredSize.X;

            d.Add
            (
                new AlphaBlendControl()
                {
                    Width = hk.MeasuredSize.X,
                    Height = hk.MeasuredSize.Y,
                    X = width - hk.MeasuredSize.X
                }
            );

            d.Add(hk);

            d.ForceSizeUpdate();

            if (!enabled)
                d.Add
                (
                    new AlphaBlendControl(0.65f)
                    {
                        Width = d.Width,
                        Height = d.Height
                    }
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
                )
                {
                    X = 5
                };

                string[] dataVars = InfoBarManager.GetVars();

                varStat = new ComboBoxWithLabel
                (world,
                    string.Empty, 0, 170, dataVars, (int)item.var, onOptionSelected: (i, s) =>
                    {
                        item.var = (InfoBarVars)i;
                        UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    }
                )
                {
                    X = 200,
                    Y = 8
                };

                labelColor = new ModernColorPickerWithLabel
                (
                    world,
                    string.Empty, item.hue, (h) =>
                    {
                        item.hue = h;
                        UIManager.GetGump<InfoBarGump>()?.ResetItems();
                    }
                )
                {
                    X = 150,
                    Y = 10
                };


                var deleteButton = new ModernButton(390, 8, 60, 25, ButtonAction.Activate, "Delete", ThemeSettings.BUTTON_FONT_COLOR)
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
                        Control c = Children[i];

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
            private ScrollArea left, right;
            private int leftY, rightY = ThemeSettings.TOP_PADDING, leftX, rightX;

            public ScrollArea LeftArea => left;
            public ScrollArea RightArea => right;

            public new int ActivePage
            {
                get => base.ActivePage;
                set
                {
                    base.ActivePage = value;
                    right.ActivePage = value;
                }
            }

            public mainScrollArea(int width, int height, int leftWidth, int page = 0)
            {
                Width = width;
                Height = height;
                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;

                Add
                (
                    left = new ScrollArea(0, 0, leftWidth, height)
                    {
                        CanMove = true,
                        AcceptMouseInput = true
                    }, page
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
                        ModernOptionsGump.SetParentsForMatchingSearch(this, p);
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
                Add(_keyBinding = TextBox.GetOne("Hotkey", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));

                _hotkeyBox = new HotkeyBox();
                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;
                _hotkeyBox.X = _keyBinding.X + _keyBinding.Width + 5;


                Add(_hotkeyBox);

                Control c;

                Add
                (
                    c = new ModernButton(0, _hotkeyBox.Height + 3, 200, 40, ButtonAction.Activate, ResGumps.CreateMacroButton, ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.CreateNewMacro,
                        IsSelectable = true,
                        IsSelected = true
                    }
                );

                Add
                (
                    c = new ModernButton(c.Width + c.X + 10, c.Y, 200, 40, ButtonAction.Activate, ResGumps.MacroButtonEditor, ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.OpenButtonEditor,
                        IsSelectable = true,
                        IsSelected = true
                    }
                );

                Add(c = new Line(0, c.Y + c.Height + 5, 325, 1, Color.Gray.PackedValue));

                Add
                (
                    c = new ModernButton(0, c.Y + 5, 75, 40, ButtonAction.Activate, ResGumps.Add, ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)buttonsOption.AddBtn,
                        IsSelectable = false
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
                        UIManager.Add(new MessageBoxGump(world, 250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

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
                        UIManager.Add(new MessageBoxGump(world, 250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

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
                        UIManager.Add(new MessageBoxGump(world, 250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, macro.Name), null));

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

                    mainBox = new ComboBoxWithLabel(world, string.Empty, 0, 200, _items, (int)obj.Code, BoxOnOnOptionSelected)
                    {
                        Tag = obj
                    };

                    Add(mainBox);

                    Control c;

                    Add
                    (
                        c = new ModernButton(mainBox.Width + 10, 0, 75, 40, ButtonAction.Activate, ResGumps.Remove, ThemeSettings.BUTTON_FONT_COLOR)
                        {
                            ButtonParameter = (int)buttonsOption.RemoveBtn,
                            IsSelectable = false
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
                            )
                            {
                                Tag = obj,
                                X = 20,
                                Y = Height
                            };

                            Add(sub);

                            //Height += sub.Height;
                            break;

                        case 2:
                            var textbox = new InputField
                            (
                                400, 40, 0, 80, obj.HasString() ? ((MacroObjectString)obj).Text : string.Empty, false, (s, e) =>
                                {
                                    if (obj.HasString())
                                    {
                                        ((MacroObjectString)obj).Text = ((InputField.StbTextBox)s).Text;
                                    }
                                }
                            )
                            {
                                X = 20,
                                Y = Height
                            };

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

                _hotkeyBox = new HotkeyBox
                {
                    X = c.Bounds.Right + 5
                };

                _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
                _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;

                Add(_hotkeyBox);

                Add
                (
                    c = new ModernButton(0, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate, "Uncheck all", ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)ButtonType.UncheckAll,
                        IsSelectable = false
                    }
                );

                Add
                (
                    new ModernButton(c.Bounds.Right + 5, _hotkeyBox.Height + 3, 100, 40, ButtonAction.Activate, "Check all", ThemeSettings.BUTTON_FONT_COLOR)
                    {
                        ButtonParameter = (int)ButtonType.CheckAll,
                        IsSelectable = false
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
                PositionHelper.PositionExact(AddCheckbox("Locked down", NameOverheadOptions.LockedDown), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Moveable", NameOverheadOptions.Moveable));
                PositionHelper.PositionExact(AddCheckbox("Immoveable", NameOverheadOptions.Immoveable), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Other items", NameOverheadOptions.Other));


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Corpses"));

                PositionHelper.PositionControl(AddCheckbox("Monster corpses", NameOverheadOptions.MonsterCorpses));
                PositionHelper.PositionExact(AddCheckbox("Humanoid corpses", NameOverheadOptions.HumanoidCorpses), rightPosX, PositionHelper.LAST_Y);
                //AddCheckbox("Own corpses", NameOverheadOptions.OwnCorpses, 0, y);


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by type"));

                PositionHelper.PositionControl(AddCheckbox("Humanoid", NameOverheadOptions.Humanoid));
                PositionHelper.PositionExact(AddCheckbox("Monster", NameOverheadOptions.Monster), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Your Followers", NameOverheadOptions.OwnFollowers));
                PositionHelper.PositionExact(AddCheckbox("Yourself", NameOverheadOptions.Self), rightPosX, PositionHelper.LAST_Y);

                PositionHelper.PositionControl(AddCheckbox("Exclude yourself", NameOverheadOptions.ExcludeSelf));


                PositionHelper.BlankLine();
                PositionHelper.PositionControl(AddLabel("Mobiles by notoriety"));

                CheckboxWithLabel cb;
                PositionHelper.PositionControl(cb = AddCheckbox("Innocent", NameOverheadOptions.Innocent));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.InnocentHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Allied", NameOverheadOptions.Ally), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.FriendHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Attackable", NameOverheadOptions.Gray));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.CanAttackHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Criminal", NameOverheadOptions.Criminal), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.CriminalHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Enemy", NameOverheadOptions.Enemy));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.EnemyHue;
                PositionHelper.PositionExact(cb = AddCheckbox("Murderer", NameOverheadOptions.Murderer), rightPosX, PositionHelper.LAST_Y);
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.MurdererHue;

                PositionHelper.PositionControl(cb = AddCheckbox("Invulnerable", NameOverheadOptions.Invulnerable));
                cb.TextLabel.Hue = ProfileManager.CurrentProfile.InvulnerableHue;
            }

            private TextBox AddLabel(string name)
            {
                var label = TextBox.GetOne(name, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
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
                            Option.NameOverheadOptionFlags |= (int)optionFlag;
                        else
                            Option.NameOverheadOptionFlags &= ~(int)optionFlag;

                        if (NameOverHeadManager.LastActiveNameOverheadOption.Replace("\\u0026", "&") == Option.Name)
                            NameOverHeadManager.ActiveOverheadOptions = (NameOverheadOptions)Option.NameOverheadOptionFlags;
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
                UIManager.Add(new MessageBoxGump(world, 250, 150, string.Format(ResGumps.ThisKeyCombinationAlreadyExists, option.Name), null));
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
                        Option.NameOverheadOptionFlags = int.MaxValue;
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
