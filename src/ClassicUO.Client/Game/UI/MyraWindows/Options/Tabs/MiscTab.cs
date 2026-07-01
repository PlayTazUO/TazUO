using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for miscellaneous settings that don't belong in another category, spread across multiple pages</summary>
public static class MiscTab
{
    /// <summary>Returns the paged option group containing miscellaneous settings pages</summary>
    internal static IOptionSource GetContent() => GetPages();

    private static OptionPageGroup GetPages()
    {
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return new OptionPageGroup(
            new SearchMetadata(Keywords: [kw.Misc, kw.Miscellaneous, kw.Other], Tags: [kw.Misc]),
            GetPage1,
            GetPage2,
            GetPage3
        );
    }

    private static OptionFragment GetPage1()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.MiscTabLang miscLang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
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
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.ShowSkillsChangedMessage), genLang.ShowSkillsChangedMsg),
                Option.Slider(
                    genLang.ChangeVolume,
                    0,
                    100,
                    new Accessor<float>(() => profile.ShowSkillsChangedDeltaValue, f => profile.ShowSkillsChangedDeltaValue = (int)f),
                    search: new SearchMetadata(genLang.ChangeVolume, Keywords: [kw.Skills, kw.Volume])
                )
            ).WithSearch(new SearchMetadata(miscLang.Label, Tags: [kw.Misc], Keywords: [kw.Skills])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.DisplaySkillBarOnChange), miscLang.DisplayProgressBarOnSkillChanges),
                Option.InputField(
                    TazLang.Get("uicommons_format"),
                    new Accessor<string>(() => profile.SkillBarFormat, s => profile.SkillBarFormat = s),
                    miscLang.SkillProgressBarFormatTooltip,
                    search: new SearchMetadata(TazLang.Get("uicommons_format"), Keywords: [kw.Skill, kw.Format])
                )
            ).WithSearch(new SearchMetadata(miscLang.Label, Tags: [kw.Misc], Keywords: [kw.Skill])),
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

    private static OptionFragment GetPage2()
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
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenCorpses), genLang.AutoOpenCorpse),
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
            ).WithSearch(new SearchMetadata(miscLang.Label, Tags: [kw.Misc], Keywords: [kw.Corpse])),
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
            Option.ComboBox(
                genLang.GridLoot,
                profile.GridLootType,
                [
                    genLang.GridLootOptDisable,
                    genLang.GridLootOptOnly,
                    genLang.GridLootOptBoth
                ],
                newValue => profile.GridLootType = newValue,
                genLang.GridLootOptOnlyTooltip,
                new SearchMetadata(genLang.GridLoot, Keywords: [kw.Grid, kw.Loot])
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

    private static OptionFragment GetPage3()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.MiscTabLang miscLang = Language.Instance.GetModernOptionsGumpLanguage.MiscTab;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
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
            Option.UIntegerInput(
                miscLang.SosGumpId,
                new Accessor<uint>(() => profile.SOSGumpID),
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
                ZLib.ManagedZlibForced,
                newValue =>
                {
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.MANAGED_ZLIB, newValue);
                    ZLib.SetForceManagedZlib(newValue);
                },
                miscLang.UseManagedZlibTooltip,
                new SearchMetadata(miscLang.UseManagedZlib, Keywords: [kw.Zlib, kw.Managed])
            ),
            Option.Checkbox(
                tuoLang.EnableASyncMapLoading,
                profile.EnableASyncMapLoading,
                newValue =>
                {
                    profile.EnableASyncMapLoading = newValue;
                    GameScene.Instance?.ASyncMapLoading = newValue;
                }
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = miscLang.HousingTransparency, LabelLink = "https://tazuo.org/wiki/tazuotrasparenthouses/" },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.ForceHouseTransparency), miscLang.EnableHouseTransparency),
                    Option.Slider(
                        TazLang.Get("uicommons_opacity"),
                        0,
                        255,
                        new Accessor<float>(() => profile.ForcedHouseTransparency, newValue => { profile.ForcedHouseTransparency = (byte)newValue; }),
                        search: new SearchMetadata(TazLang.Get("uicommons_opacity"), Keywords: [kw.House, kw.Opacity])
                    ),
                    Option.HuePicker(
                        TazLang.Get("uicommons_hue"),
                        new Accessor<ushort>(() => profile.ForcedTransparencyHouseTileHue, h => profile.ForcedTransparencyHouseTileHue = h),
                        search: new SearchMetadata(TazLang.Get("uicommons_hue"), Keywords: [kw.House, kw.Hue])
                    )
                ).WithSearch(new SearchMetadata(miscLang.HousingTransparency, Tags: [kw.Misc], Keywords: [kw.House, kw.Transparency]))
            )
        ).WithSearch(new SearchMetadata(miscLang.Label, Keywords: [kw.Misc, kw.Miscellaneous, kw.Other], Tags: [kw.Misc]));
    }
}
