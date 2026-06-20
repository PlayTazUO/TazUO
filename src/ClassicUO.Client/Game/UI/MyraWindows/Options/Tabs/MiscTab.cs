using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MiscTab
{
    internal static IOptionSource GetContent() => GetTabs();

    private static OptionTabGroup GetTabs()
    {
        ModernOptionsGumpLanguage.MiscTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;

        return new OptionTabGroup()
            .AddTab(lang.GeneralLabel, GetPage1, new SearchMetadata(lang.GeneralLabel))
            .AddTab(lang.InteractionLabel, GetPage2, new SearchMetadata(lang.InteractionLabel))
            .AddTab(lang.AdvancedLabel, GetPage3, new SearchMetadata(lang.AdvancedLabel));
    }

    private static IOptionSource GetPage1()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.MiscTabLang miscLang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = genLang.EnableCOT },
                Option.Checkbox(
                    genLang.EnableCOT,
                    new Accessor<bool>(() => profile.UseCircleOfTransparency),
                    search: new SearchMetadata(genLang.EnableCOT, Keywords: [kw.Cot, kw.Circle])
                ),
                Option.Slider(
                    genLang.COTDistance,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    new Accessor<float>(() => profile.CircleOfTransparencyRadius, f => profile.CircleOfTransparencyRadius = (int)f),
                    search: new SearchMetadata(genLang.COTDistance, Keywords: [kw.Cot, kw.Distance])
                ),
                Option.ComboBox(
                    genLang.COTType,
                    profile.CircleOfTransparencyType,
                    [genLang.COTTypeOptFull, genLang.COTTypeOptGrad, genLang.COTTypeOptModern],
                    i => profile.CircleOfTransparencyType = i,
                    search: new SearchMetadata(genLang.COTType, Keywords: [kw.Cot, kw.Type])
                )
            ),
            Option.Checkbox(
                genLang.HideScreenshotMessage,
                new Accessor<bool>(() => profile.HideScreenshotStoredInMessage),
                search: new SearchMetadata(genLang.HideScreenshotMessage, Keywords: [miscLang.LabelScreenshot])
            ),
            Option.Checkbox(
                genLang.ObjFade,
                new Accessor<bool>(() => profile.UseObjectsFading),
                search: new SearchMetadata(genLang.ObjFade, Keywords: [kw.Fade, kw.Object])
            ),
            Option.Checkbox(
                genLang.TextFade,
                new Accessor<bool>(() => profile.TextFading),
                search: new SearchMetadata(genLang.TextFade, Keywords: [kw.Fade, kw.Text])
            ),
            Option.Checkbox(
                genLang.CursorRange,
                new Accessor<bool>(() => profile.ShowTargetRangeIndicator),
                search: new SearchMetadata(genLang.CursorRange, Keywords: [kw.Cursor, kw.Range])
            ),
            Option.Checkbox(
                genLang.ShowStatsChangedMsg,
                new Accessor<bool>(() => profile.ShowStatsChangedMessage),
                search: new SearchMetadata(genLang.ShowStatsChangedMsg, Keywords: [kw.Stats, kw.Changed])
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    genLang.ShowSkillsChangedMsg,
                    new Accessor<bool>(() => profile.ShowSkillsChangedMessage),
                    search: new SearchMetadata(genLang.ShowSkillsChangedMsg, Keywords: [kw.Skills, kw.Changed])
                ),
                Option.Slider(
                    genLang.ChangeVolume,
                    0,
                    100,
                    new Accessor<float>(() => profile.ShowSkillsChangedDeltaValue, f => profile.ShowSkillsChangedDeltaValue = (int)f),
                    search: new SearchMetadata(genLang.ChangeVolume, Keywords: [kw.Skills, kw.Volume])
                )
            ),
            Option.Checkbox(
                genLang.ShiftContext,
                new Accessor<bool>(() => profile.HoldShiftForContext),
                search: new SearchMetadata(genLang.ShiftContext, Keywords: [kw.Shift, kw.Context])
            ),
            Option.Checkbox(
                genLang.ShiftSplit,
                new Accessor<bool>(() => profile.HoldShiftToSplitStack),
                search: new SearchMetadata(genLang.ShiftSplit, Keywords: [kw.Shift, kw.Split])
            )
        ).WithSearch(new SearchMetadata(miscLang.Label, Keywords: [kw.Misc, kw.Miscellaneous, kw.Other], Tags: [kw.Misc]));
    }

    private static IOptionSource GetPage2()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.MiscTabLang miscLang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                genLang.HighlightObjects,
                new Accessor<bool>(() => profile.HighlightGameObjects),
                search: new SearchMetadata(genLang.HighlightObjects, Keywords: [kw.Highlight])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = genLang.AutoOpenCorpse },
                Option.Checkbox(
                    genLang.AutoOpenCorpse,
                    new Accessor<bool>(() => profile.AutoOpenCorpses),
                    search: new SearchMetadata(genLang.AutoOpenCorpse, Keywords: [kw.Corpse, kw.Auto])
                ),
                Option.Slider(
                    genLang.CorpseOpenDistance,
                    0,
                    5,
                    new Accessor<float>(() => profile.AutoOpenCorpseRange, f => profile.AutoOpenCorpseRange = (int)f),
                    search: new SearchMetadata(genLang.CorpseOpenDistance, Keywords: [kw.Corpse, kw.Distance])
                ),
                Option.Checkbox(
                    genLang.CorpseSkipEmpty,
                    new Accessor<bool>(() => profile.SkipEmptyCorpse),
                    genLang.CorpseSkipEmptyTooltip,
                    search: new SearchMetadata(genLang.CorpseSkipEmpty, Keywords: [kw.Corpse, kw.Empty])
                ),
                Option.ComboBox(
                    genLang.CorpseOpenOptions,
                    profile.CorpseOpenOptions,
                    [genLang.CorpseOptNone, genLang.CorpseOptNotTarg, genLang.CorpseOptNotHiding, genLang.CorpseOptBoth],
                    i => profile.CorpseOpenOptions = i,
                    search: new SearchMetadata(genLang.CorpseOpenOptions, Keywords: [kw.Corpse, kw.Type])
                )
            ),
            Option.Checkbox(
                genLang.OutRangeColor,
                new Accessor<bool>(() => profile.NoColorObjectsOutOfRange),
                search: new SearchMetadata(genLang.OutRangeColor, Keywords: [kw.Range, kw.Color])
            ),
            Option.Checkbox(
                genLang.SallosEasyGrab,
                new Accessor<bool>(() => profile.SallosEasyGrab),
                genLang.SallosTooltip,
                search: new SearchMetadata(genLang.SallosEasyGrab, Keywords: [kw.Sallos, kw.Grab])
            ),
            Option.Checkbox(
                genLang.ShowHouseContent,
                new Accessor<bool>(() => profile.ShowHouseContent),
                genLang.ClientVersionLimitedTooltip,
                search: new SearchMetadata(genLang.ShowHouseContent, Keywords: [kw.House, kw.Content])
            ),
            Option.Checkbox(
                genLang.SmoothBoat,
                new Accessor<bool>(() => profile.UseSmoothBoatMovement),
                genLang.ClientVersionLimitedTooltip,
                search: new SearchMetadata(genLang.SmoothBoat, Keywords: [kw.Boat, kw.Smooth])
            ),
            GetExperimentalSection()
        ).WithSearch(new SearchMetadata(miscLang.Label, Keywords: [kw.Misc, kw.Miscellaneous, kw.Other], Tags: [kw.Misc]));
    }

    private static OptionFragment GetExperimentalSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.MiscTabLang.ExperimentalSection expLang = lang.MiscTab.Experimental;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = expLang.Label },
            Option.Checkbox(
                expLang.DisableDefaultUoHotkeys,
                new Accessor<bool>(() => profile.DisableDefaultHotkeys),
                search: new SearchMetadata(expLang.DisableDefaultUoHotkeys, Keywords: [kw.Hotkey, kw.Disable])
            ),
            Option.Checkbox(
                expLang.DisableArrowsNumlockArrowsPlayerMovement,
                new Accessor<bool>(() => profile.DisableArrowBtn),
                search: new SearchMetadata(expLang.DisableArrowsNumlockArrowsPlayerMovement, Keywords: [kw.Arrow, kw.Movement])
            ),
            Option.Checkbox(
                expLang.DisableTabToggleWarmode,
                new Accessor<bool>(() => profile.DisableTabBtn),
                search: new SearchMetadata(expLang.DisableTabToggleWarmode, Keywords: [kw.Tab, kw.Warmode])
            ),
            Option.Checkbox(
                expLang.DisableCtrlQWMessageHistory,
                new Accessor<bool>(() => profile.DisableCtrlQWBtn),
                search: new SearchMetadata(expLang.DisableCtrlQWMessageHistory, Keywords: [kw.History, kw.Message])
            ),
            Option.Checkbox(
                expLang.DisableRightLeftClickAutoMove,
                new Accessor<bool>(() => profile.DisableAutoMove),
                search: new SearchMetadata(expLang.DisableRightLeftClickAutoMove, Keywords: [kw.AutoMove, kw.Click])
            )
        ).WithSearch(new SearchMetadata(expLang.Label, Keywords: [kw.Experimental, kw.Beta, kw.Test], Tags: [kw.Experimental]));
    }

    private static IOptionSource GetPage3()
    {
        Profile profile = ProfileManager.CurrentProfile;
        UiCommonsLanguage uiLang = Language.Instance.UiCommons;
        ModernOptionsGumpLanguage.MiscTabLang miscLang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Button(
                miscLang.ManageIgnoreListButtonLabel,
                () =>
                {
                    UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                    UIManager.Add(new IgnoreManagerGump(World.Instance));
                },
                search: new SearchMetadata(miscLang.ManageIgnoreListButtonLabel, Keywords: [kw.Ignore, kw.Entity])
            ),
            Option.NumericInput(
                miscLang.SosGumpId,
                new Accessor<int>(() => (int)profile.SOSGumpID, i => profile.SOSGumpID = (uint)i),
                tooltip: miscLang.SosGumpIdLabelTooltip,
                search: new SearchMetadata(miscLang.SosGumpId, Keywords: [kw.SOS, kw.Gump])
            ),
            Option.Checkbox(
                miscLang.EnableAutoResyncOnHangDetection,
                new Accessor<bool>(() => profile.ForceResyncOnHang),
                miscLang.EnableAutoResyncOnHangDetectionTooltip,
                search: new SearchMetadata(miscLang.EnableAutoResyncOnHangDetection, Keywords: [kw.Resync, kw.Hang])
            ),
            Option.Checkbox(
                miscLang.UseManagedZlib,
                new Accessor<bool>(() => profile.ForceResyncOnHang),
                miscLang.UseManagedZlibTooltip,
                search: new SearchMetadata(miscLang.UseManagedZlib, Keywords: [kw.Zlib, kw.Managed])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = miscLang.HousingTransparency, LabelLink = "https://tazuo.org/wiki/tazuotrasparenthouses/" },
                Option.Checkbox(
                    miscLang.EnableHouseTransparency,
                    new Accessor<bool>(() => profile.ForceHouseTransparency),
                    search: new SearchMetadata(miscLang.EnableHouseTransparency, Keywords: [kw.House, kw.Transparency])
                ),
                Option.Slider(
                    uiLang.Opacity,
                    0,
                    255,
                    new Accessor<float>(() => profile.ForcedHouseTransparency, newValue => { profile.ForcedHouseTransparency = (byte)newValue; }),
                    search: new SearchMetadata(uiLang.Opacity, Keywords: [kw.House, kw.Opacity])
                ),
                Option.HuePicker(
                    uiLang.Hue,
                    new Accessor<ushort>(() => profile.ForcedTransparencyHouseTileHue, h => profile.ForcedTransparencyHouseTileHue = h),
                    search: new SearchMetadata(uiLang.Hue, Keywords: [kw.House, kw.Hue])
                )
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    miscLang.DisplayProgressBarOnSkillChanges,
                    new Accessor<bool>(() => profile.DisplaySkillBarOnChange),
                    search: new SearchMetadata(miscLang.DisplayProgressBarOnSkillChanges, Keywords: [kw.Skill, kw.Progress])
                ),
                Option.InputField(
                    uiLang.Format,
                    new Accessor<string>(() => profile.SkillBarFormat, s => profile.SkillBarFormat = s),
                    miscLang.SkillProgressBarFormatTooltip,
                    search: new SearchMetadata(uiLang.Format, Keywords: [kw.Skill, kw.Format])
                )
            )
        ).WithSearch(new SearchMetadata(miscLang.Label, Keywords: [kw.Misc, kw.Miscellaneous, kw.Other], Tags: [kw.Misc]));
    }
}
