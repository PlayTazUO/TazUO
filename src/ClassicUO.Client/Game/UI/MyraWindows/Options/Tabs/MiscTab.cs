using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>
///     Options tab source for miscellaneous settings that don't belong in another category, spread across multiple
///     pages
/// </summary>
public static class MiscTab
{
    /// <summary>Returns the paged option group containing miscellaneous settings pages</summary>
    internal static IOptionSource GetContent() => GetPages();

    private static OptionPageGroup GetPages() => new(
        new SearchMetadata(Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous"), TazLang.Get("mog_kw_other")],
            Tags: [TazLang.Get("mog_kw_misc")]),
        GetPage1,
        GetPage2,
        GetPage3,
        GetExperimentalSection
    );

    private static OptionFragment GetPage1()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_general_hidescreenshotmessage"),
                new Accessor<bool>(() => profile.HideScreenshotStoredInMessage),
                search: new SearchMetadata(TazLang.Get("mog_general_hidescreenshotmessage"), Keywords: [TazLang.Get("mog_misctab_labelscreenshot")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_objfade"),
                new Accessor<bool>(() => profile.UseObjectsFading),
                search: new SearchMetadata(TazLang.Get("mog_general_objfade"), Keywords: [TazLang.Get("mog_kw_fade"), TazLang.Get("mog_kw_object")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_textfade"),
                new Accessor<bool>(() => profile.TextFading),
                search: new SearchMetadata(TazLang.Get("mog_general_textfade"), Keywords: [TazLang.Get("mog_kw_fade"), TazLang.Get("mog_kw_text")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_cursorrange"),
                new Accessor<bool>(() => profile.ShowTargetRangeIndicator),
                search: new SearchMetadata(TazLang.Get("mog_general_cursorrange"), Keywords: [TazLang.Get("mog_kw_cursor"), TazLang.Get("mog_kw_range")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_showstatschangedmsg"),
                new Accessor<bool>(() => profile.ShowStatsChangedMessage),
                search: new SearchMetadata(TazLang.Get("mog_general_showstatschangedmsg"), Keywords: [TazLang.Get("mog_kw_stats"), TazLang.Get("mog_kw_changed")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.ShowSkillsChangedMessage), TazLang.Get("mog_general_showskillschangedmsg")),
                Option.Slider(
                    TazLang.Get("mog_general_changevolume"),
                    0,
                    100,
                    new Accessor<float>(() => profile.ShowSkillsChangedDeltaValue, f => profile.ShowSkillsChangedDeltaValue = (int)f),
                    search: new SearchMetadata(TazLang.Get("mog_general_changevolume"), Keywords: [TazLang.Get("mog_kw_skills"), TazLang.Get("mog_kw_volume")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_label"), [TazLang.Get("mog_kw_misc")], [TazLang.Get("mog_kw_skills")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.DisplaySkillBarOnChange), TazLang.Get("mog_misctab_displayprogressbaronskillchanges")),
                Option.InputField(
                    TazLang.Get("uicommons_format"),
                    new Accessor<string>(() => profile.SkillBarFormat, s => profile.SkillBarFormat = s),
                    TazLang.Get("mog_misctab_skillprogressbarformattooltip"),
                    new SearchMetadata(TazLang.Get("uicommons_format"), Keywords: [TazLang.Get("mog_kw_skill"), TazLang.Get("mog_kw_format")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_label"), [TazLang.Get("mog_kw_misc")], [TazLang.Get("mog_kw_skill")])),
            Option.Checkbox(
                TazLang.Get("mog_general_shiftcontext"),
                new Accessor<bool>(() => profile.HoldShiftForContext),
                search: new SearchMetadata(TazLang.Get("mog_general_shiftcontext"), Keywords: [TazLang.Get("mog_kw_shift"), TazLang.Get("mog_kw_context")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_shiftsplit"),
                new Accessor<bool>(() => profile.HoldShiftToSplitStack),
                search: new SearchMetadata(TazLang.Get("mog_general_shiftsplit"), Keywords: [TazLang.Get("mog_kw_shift"), TazLang.Get("mog_kw_split")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_label"),
            Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous"), TazLang.Get("mog_kw_other")], Tags: [TazLang.Get("mog_kw_misc")]));
    }

    private static OptionFragment GetPage2()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_general_highlightobjects"),
                new Accessor<bool>(() => profile.HighlightGameObjects),
                search: new SearchMetadata(TazLang.Get("mog_general_highlightobjects"), Keywords: [TazLang.Get("mog_kw_highlight")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_outrangecolor"),
                new Accessor<bool>(() => profile.NoColorObjectsOutOfRange),
                search: new SearchMetadata(TazLang.Get("mog_general_outrangecolor"), Keywords: [TazLang.Get("mog_kw_range"), TazLang.Get("mog_kw_color")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_salloseasygrab"),
                new Accessor<bool>(() => profile.SallosEasyGrab),
                TazLang.Get("mog_general_sallostooltip"),
                new SearchMetadata(TazLang.Get("mog_general_salloseasygrab"), Keywords: [TazLang.Get("mog_kw_sallos"), TazLang.Get("mog_kw_grab")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_showhousecontent"),
                new Accessor<bool>(() => profile.ShowHouseContent),
                TazLang.Get("mog_general_clientversionlimitedtooltip"),
                new SearchMetadata(TazLang.Get("mog_general_showhousecontent"), Keywords: [TazLang.Get("mog_kw_house"), TazLang.Get("mog_kw_content")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_smoothboat"),
                new Accessor<bool>(() => profile.UseSmoothBoatMovement),
                TazLang.Get("mog_general_clientversionlimitedtooltip"),
                new SearchMetadata(TazLang.Get("mog_general_smoothboat"), Keywords: [TazLang.Get("mog_kw_boat"), TazLang.Get("mog_kw_smooth")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_label"),
            Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous"), TazLang.Get("mog_kw_other")], Tags: [TazLang.Get("mog_kw_misc")]));
    }

    private static OptionFragment GetPage3()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Button(
                TazLang.Get("mog_misctab_manageignorelistbuttonlabel"),
                () =>
                {
                    UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                    UIManager.Add(new IgnoreManagerGump(World.Instance));
                },
                new SearchMetadata(TazLang.Get("mog_misctab_manageignorelistbuttonlabel"), Keywords: [TazLang.Get("mog_kw_ignore"), TazLang.Get("mog_kw_entity")])
            ),
            Option.UIntegerInput(
                TazLang.Get("mog_misctab_sosgumpid"),
                new Accessor<uint>(() => profile.SOSGumpID),
                tooltip: TazLang.Get("mog_misctab_sosgumpidlabeltooltip"),
                search: new SearchMetadata(TazLang.Get("mog_misctab_sosgumpid"), Keywords: [TazLang.Get("mog_kw_sos"), TazLang.Get("mog_kw_gump")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_enableautoresynconhangdetection"),
                new Accessor<bool>(() => profile.ForceResyncOnHang),
                TazLang.Get("mog_misctab_enableautoresynconhangdetectiontooltip"),
                new SearchMetadata(TazLang.Get("mog_misctab_enableautoresynconhangdetection"), Keywords: [TazLang.Get("mog_kw_resync"), TazLang.Get("mog_kw_hang")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_usemanagedzlib"),
                ZLib.ManagedZlibForced,
                newValue =>
                {
                    _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.MANAGED_ZLIB, newValue);
                    ZLib.SetForceManagedZlib(newValue);
                },
                TazLang.Get("mog_misctab_usemanagedzlibtooltip"),
                new SearchMetadata(TazLang.Get("mog_misctab_usemanagedzlib"), Keywords: [TazLang.Get("mog_kw_zlib"), TazLang.Get("mog_kw_managed")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_enableasyncmaploading"),
                profile.EnableASyncMapLoading,
                newValue =>
                {
                    profile.EnableASyncMapLoading = newValue;
                    GameScene.Instance?.ASyncMapLoading = newValue;
                }
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_misctab_housingtransparency"), LabelLink = "https://tazuo.org/wiki/tazuotrasparenthouses/" },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.ForceHouseTransparency), TazLang.Get("mog_misctab_enablehousetransparency")),
                    Option.Slider(
                        TazLang.Get("uicommons_opacity"),
                        0,
                        255,
                        new Accessor<float>(() => profile.ForcedHouseTransparency, newValue => { profile.ForcedHouseTransparency = (byte)newValue; }),
                        search: new SearchMetadata(TazLang.Get("uicommons_opacity"), Keywords: [TazLang.Get("mog_kw_house"), TazLang.Get("mog_kw_opacity")])
                    ),
                    Option.HuePicker(
                        TazLang.Get("uicommons_hue"),
                        new Accessor<ushort>(() => profile.ForcedTransparencyHouseTileHue, h => profile.ForcedTransparencyHouseTileHue = h),
                        new SearchMetadata(TazLang.Get("uicommons_hue"), Keywords: [TazLang.Get("mog_kw_house"), TazLang.Get("mog_kw_hue")])
                    )
                ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_housingtransparency"), [TazLang.Get("mog_kw_misc")],
                    [TazLang.Get("mog_kw_house"), TazLang.Get("mog_kw_transparency")]))
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_label"),
            Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous"), TazLang.Get("mog_kw_other")], Tags: [TazLang.Get("mog_kw_misc")]));
    }

    private static OptionFragment GetExperimentalSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_misctab_experimental_label") },
            Option.Checkbox(
                TazLang.Get("mog_misctab_experimental_disabledefaultuohotkeys"),
                new Accessor<bool>(() => profile.DisableDefaultHotkeys),
                search: new SearchMetadata(TazLang.Get("mog_misctab_experimental_disabledefaultuohotkeys"),
                    Keywords: [TazLang.Get("mog_kw_hotkey"), TazLang.Get("mog_kw_disable")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_experimental_disablearrowsnumlockarrowsplayermovement"),
                new Accessor<bool>(() => profile.DisableArrowBtn),
                search: new SearchMetadata(TazLang.Get("mog_misctab_experimental_disablearrowsnumlockarrowsplayermovement"),
                    Keywords: [TazLang.Get("mog_kw_arrow"), TazLang.Get("mog_kw_movement")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_experimental_disabletabtogglewarmode"),
                new Accessor<bool>(() => profile.DisableTabBtn),
                search: new SearchMetadata(TazLang.Get("mog_misctab_experimental_disabletabtogglewarmode"),
                    Keywords: [TazLang.Get("mog_kw_tab"), TazLang.Get("mog_kw_warmode")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_experimental_disablectrlqwmessagehistory"),
                new Accessor<bool>(() => profile.DisableCtrlQWBtn),
                search: new SearchMetadata(TazLang.Get("mog_misctab_experimental_disablectrlqwmessagehistory"),
                    Keywords: [TazLang.Get("mog_kw_history"), TazLang.Get("mog_kw_message")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_misctab_experimental_disablerightleftclickautomove"),
                new Accessor<bool>(() => profile.DisableAutoMove),
                search: new SearchMetadata(TazLang.Get("mog_misctab_experimental_disablerightleftclickautomove"),
                    Keywords: [TazLang.Get("mog_kw_automove"), TazLang.Get("mog_kw_click")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_misctab_experimental_label"),
            Keywords: [TazLang.Get("mog_kw_experimental"), TazLang.Get("mog_kw_beta"), TazLang.Get("mog_kw_test")], Tags: [TazLang.Get("mog_kw_experimental")]));
    }

}
