using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for nameplate display settings and profile-based nameplate configuration</summary>
public static class NameplatesTab
{
    /// <summary>Returns the tab group containing general nameplate settings and profile sub-tabs</summary>
    internal static IOptionSource GetContent() => GetNameplatesMenuTabs();

    private static OptionTabGroup GetNameplatesMenuTabs() =>
        new OptionTabGroup()
            .AddTab(TazLang.Get("mog_buttongeneral"), GetGeneralNameplatesSubTabContent, new SearchMetadata(TazLang.Get("mog_buttongeneral"), Keywords: [TazLang.Get("mog_kw_general")]))
            .AddTab(TazLang.Get("mog_buttonprofiles"), GetProfilesSubTabContentSource,
                new SearchMetadata()); // Empty metadata to disable search; Doesn't render well in the results page.

    #region Profiles

    private static IOptionSource GetProfilesSubTabContentSource()
    {
        // The profile editor is not 'searchable' right now since it doesn't really fit well in the search results page.
        OptionFragment panel = OptionsUi.Vertical(
            Option.Custom(GetProfilesSubTabContent)
        );
        panel.InheritsSearch = false;
        return panel;
    }

    private static Widget GetProfilesSubTabContent()
    {
        var profileEditor = new ProfileEditor<NameOverheadOption>(
            GetEditorForProfile,
            name =>
            {
                var newProfile = new NameOverheadOption(name);
                World.Instance.NameOverHeadManager.AddOption(newProfile);
                return newProfile;
            },
            profile =>
            {
                World.Instance.NameOverHeadManager.RemoveOption(profile);
            },
            NameOverHeadManager.GetAllOptions(),
            profile =>
            {
                World.Instance.NameOverHeadManager.HandleRenamedOption(profile);
            }
        );
        return profileEditor;
    }

    private static WrapPanel GetEditorForProfile(NameOverheadOption profile)
    {

        WrapPanel settingsPanel = OptionTabCommons.StyledHorizontalWrapPanel(
            GetItemsBoxesPanel(profile),
            GetCorpseBoxesPanel(profile),
            GetMobilesByTypeBoxesPanel(profile),
            GetMobilesByNotorietyBoxesPanel(profile)
        );
        settingsPanel.HorizontalAlignment = HorizontalAlignment.Left;
        settingsPanel.Aligned = false;
        settingsPanel.UniformSizing = false;


        // Note that these coalesce both left and right mod keys. Might want to improve specifically later.
        SDL.SDL_Keymod mods = profile.Alt ? SDL.SDL_Keymod.SDL_KMOD_ALT : 0;
        mods |= profile.Ctrl ? SDL.SDL_Keymod.SDL_KMOD_CTRL : 0;
        mods |= profile.Shift ? SDL.SDL_Keymod.SDL_KMOD_SHIFT : 0;

        var currentHotkey = new HotkeyBinding(profile.Key, mods);

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionTabCommons.StyledHorizontalSpaceBetween(
                [
                    new HotkeyInput(
                        existingSelection: currentHotkey,
                        onSelectionChanged: e => OnProfileHotkeyChanged(profile, e),
                        capturesMouseEvents: false
                    )
                    {
                        Padding = new Thickness(MyraStyle.STANDARD_SPACING, 0, 0, 0),
                        Tooltip = TazLang.Get("mog_nameplates_optionstab_hotkeyinputtooltip")
                    }
                ],
                [
                    OptionTabCommons.StyledVerticalSeparator(),
                    new MyraButton(
                        TazLang.Get("mog_nameplates_optionstab_checkall"),
                        () => profile.NameOverheadOptionFlags = ByteFlagHelper.AllBits<NameOverheadOptions>()
                    ),
                    new MyraButton(
                        TazLang.Get("mog_nameplates_optionstab_uncheckall"),
                        () => profile.NameOverheadOptionFlags = NameOverheadOptions.None
                    )
                ]
            ),
            settingsPanel,
            GetSearchFieldsPanel(profile)
        );
    }

    /// <summary>
    ///     Builds the search / negative-search input rows for a nameplate profile. The values are stored on
    ///     the profile so they persist per nameplate option (see <see cref="NameOverheadOption.Search" />).
    /// </summary>
    private static WrapPanel GetSearchFieldsPanel(NameOverheadOption profile)
    {
        WrapPanel searchRow = MyraInputBox.LabeledHorizontalStackPanel(
            "Search:",
            out MyraInputBox searchInput,
            width: 200,
            text: profile.Search,
            hintText: "Only show matching",
            tooltip: "Only show nameplates matching this text.\nSeparate multiple terms with ';'"
        );
        // Commit on focus loss so the editor isn't rebuilt on every keystroke (which would drop focus).
        searchInput.LostFocus = () => profile.Search = searchInput.Text;

        WrapPanel negativeSearchRow = MyraInputBox.LabeledHorizontalStackPanel(
            "Hide search:",
            out MyraInputBox negativeSearchInput,
            width: 200,
            text: profile.NegativeSearch,
            hintText: "Hide matching",
            tooltip: "Hide nameplates matching this text (opposite of search).\nSeparate multiple terms with ';'"
        );
        negativeSearchInput.LostFocus = () => profile.NegativeSearch = negativeSearchInput.Text;

        WrapPanel panel = OptionTabCommons.StyledVerticalWrapPanel(searchRow, negativeSearchRow);
        panel.Margin = new Thickness(0, MyraStyle.STANDARD_SPACING, 0, 0);
        return panel;
    }

    private static void OnProfileHotkeyChanged(NameOverheadOption profile, SelectionChangedEventArgs e)
    {
        HotkeyBinding value = e.NewValue;

        // We have to check for hotkey conflicts first.
        NameOverheadOption option = NameOverHeadManager.FindOptionByHotkey(value.Key, value.Alt, value.Ctrl, value.Shift);

        // If there are none, simply update the profile with the new hotkey.
        if (option == null || option == profile || value.IsEmpty)
        {
            profile.Key = value.Key;
            profile.Alt = value.Alt;
            profile.Ctrl = value.Ctrl;
            profile.Shift = value.Shift;
            return;
        }

        // Otherwise, raise a notice
        UIManager.Add(new MessageBoxGump(
                World.Instance,
                250,
                150,
                string.Format(ResGumps.ThisKeyCombinationAlreadyExists, option.Name),
                null
            )
        );
    }

    private static VisualContainer GetItemsBoxesPanel(NameOverheadOption profile) =>
        new(
            new VisualContainerProps { LabelText = TazLang.Get("mog_nameplates_optionstab_items") },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_containers"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Containers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_stackable"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Stackable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_moveable"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Moveable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_otheritems"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Other
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_gold"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gold
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_lockeddown"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.LockedDown
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_immovable"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Immoveable
            )
        );

    private static VisualContainer GetCorpseBoxesPanel(NameOverheadOption profile) =>
        new(
            new VisualContainerProps { LabelText = TazLang.Get("mog_nameplates_optionstab_corpses") },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_monster"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.MonsterCorpses
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_humanoid"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.HumanoidCorpses
            )
        );

    private static VisualContainer GetMobilesByTypeBoxesPanel(NameOverheadOption profile) =>
        new(
            new VisualContainerProps { LabelText = TazLang.Get("mog_nameplates_optionstab_mobilesbytype") },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_humanoid"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Humanoid
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_yourfollowers"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.OwnFollowers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_excludeyourself"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.ExcludeSelf
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_monster"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Monster
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_yourself"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Self
            )
        );

    private static VisualContainer GetMobilesByNotorietyBoxesPanel(NameOverheadOption profile) =>
        new(
            new VisualContainerProps { LabelText = TazLang.Get("mog_nameplates_optionstab_mobilesbynotoriety") },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_innocent"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Innocent
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_attackable"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gray
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_enemy"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Enemy
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_invulnerable"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Invulnerable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_allied"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Ally
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_criminal"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Criminal
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                TazLang.Get("mog_nameplates_optionstab_murderer"),
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Murderer
            )
        );

    #endregion Profiles

    #region General Sub-Tab

    private static IOptionSource GetGeneralNameplatesSubTabContent() =>
        OptionsUi.Horizontal(GeneralSettingsLeftSide(), GeneralSettingsRightSide())
            .WithSearch(new SearchMetadata(TazLang.Get("mog_buttongeneral"), [TazLang.Get("mog_kw_nameplate"), TazLang.Get("mog_kw_general")]));

    private static OptionFragment GeneralSettingsLeftSide()
    {
        Profile profile = ProfileManager.CurrentProfile;

        const string locNameplatesHealth = "nameplate_health_";
        string nameWidthLabel = TazLang.Get("nameplate_width", "Name width");
        string heightLabel = TazLang.Get("nameplate_height", "Height");
        string cornerRadiusLabel = TazLang.Get("nameplate_cornerradius", "Corner radius");
        string separateHealthBarWidthLabel = TazLang.Get("nameplate_separatehealthbarwidth", TazLang.Get("mog_tazuo_separatehealthbarwidth"));
        string healthBarWidthLabel = TazLang.Get("nameplate_healthbarwidth", TazLang.Get("mog_tazuo_healthbarwidth"));
        string splitHealthBarLabel = TazLang.Get("nameplate_splithealthbar", TazLang.Get("mog_tazuo_splithealthbar"));

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_kw_appearance") },
                Option.IntegerInput(
                    heightLabel,
                    new Accessor<int>(() => profile.NamePlateHeight),
                    0,
                    80,
                    search: new SearchMetadata(heightLabel, Keywords: [TazLang.Get("mog_kw_height")])
                ),
                Option.IntegerInput(
                    nameWidthLabel,
                    new Accessor<int>(() => profile.NamePlateFixedWidth),
                    60,
                    300,
                    search: new SearchMetadata(nameWidthLabel, Keywords: [TazLang.Get("mog_kw_width")])
                ),
                Option.IntegerInput(
                    cornerRadiusLabel,
                    new Accessor<int>(() => profile.NamePlateCornerRadius),
                    0,
                    40,
                    search: new SearchMetadata(cornerRadiusLabel, Keywords: [TazLang.Get("mog_kw_corner"), TazLang.Get("mog_kw_radius")])
                ),
                Option.FontSelector(
                    TazLang.Get("mog_tazuo_nameplatefont"),
                    new Accessor<string>(() => profile.NamePlateFont),
                    s => profile.NamePlateFont = s,
                    new SearchMetadata(TazLang.Get("mog_tazuo_nameplatefont"), Keywords: [TazLang.Get("mog_kw_font")])
                ),
                Option.Slider(
                    TazLang.Get("mog_kw_size"),
                    5,
                    50,
                    new Accessor<int>(() => profile.NamePlateFontSize),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_sharedsize"), Keywords: [TazLang.Get("mog_kw_size")])
                )
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_tazuo_backgroundcolor") },
                Option.Slider(
                    TazLang.Get("mog_kw_red"),
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundR),
                    search: new SearchMetadata(TazLang.Get("mog_kw_red"), Keywords: [TazLang.Get("mog_kw_red"), TazLang.Get("mog_kw_color")])
                ),
                Option.Slider(
                    TazLang.Get("mog_kw_green"),
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundG),
                    search: new SearchMetadata(TazLang.Get("mog_kw_green"), Keywords: [TazLang.Get("mog_kw_green"), TazLang.Get("mog_kw_color")])
                ),
                Option.Slider(
                    TazLang.Get("mog_kw_blue"),
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundB),
                    search: new SearchMetadata(TazLang.Get("mog_kw_blue"), Keywords: [TazLang.Get("mog_kw_blue"), TazLang.Get("mog_kw_color")])
                ),
                Option.Slider(
                    TazLang.Get("mog_tazuo_backgroundopacity"),
                    0,
                    100,
                    new Accessor<byte>(() => profile.NamePlateOpacity),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_backgroundopacity"), Keywords: [TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_opacity")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_kw_mode"),
                    new Accessor<NamePlateBackgroundMode>(() => profile.NamePlateBackgroundMode),
                    search: new SearchMetadata(TazLang.Get("mog_kw_mode"), Keywords: [TazLang.Get("mog_kw_mode"), TazLang.Get("mog_kw_background")])
                )
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_kw_healthbar") },
                Option.LComboBox(
                    TazLang.Get("mog_kw_mode"),
                    new Accessor<NamePlateHealthBarMode>(() => profile.NamePlateHealthBarMode),
                    locNameplatesHealth,
                    search: new SearchMetadata(TazLang.Get("mog_kw_mode"), Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_mode")])
                ),
                Option.Checkbox(
                    separateHealthBarWidthLabel,
                    new Accessor<bool>(() => profile.NamePlateUseFixedHealthBarWidth),
                    search: new SearchMetadata(separateHealthBarWidthLabel, Keywords: [TazLang.Get("mog_kw_fixed"), TazLang.Get("mog_kw_width"), TazLang.Get("mog_kw_healthbar")])
                ),
                Option.IntegerInput(
                    healthBarWidthLabel,
                    new Accessor<int>(() => profile.NamePlateHealthBarFixedWidth),
                    60,
                    300,
                    search: new SearchMetadata(healthBarWidthLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_width")])
                ),
                Option.Checkbox(
                    splitHealthBarLabel,
                    new Accessor<bool>(() => profile.NamePlateSplitHealthBar),
                    search: new SearchMetadata(splitHealthBarLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_split")])
                )
            )
        );
    }

    private static OptionFragment GeneralSettingsRightSide()
    {
        Profile profile = ProfileManager.CurrentProfile;

        const string locNameplatesHealth = "nameplate_health_";
        string fixedWidthLabel = TazLang.Get("nameplate_fixedwidth", TazLang.Get("mog_tazuo_fixedwidth"));
        string showWordOfDeathIconLabel = TazLang.Get("nameplate_showwordofdeathicon", TazLang.Get("mog_tazuo_showwordofdeathicon"));
        string presetLabel = TazLang.Get("nameplate_preset", TazLang.Get("mog_kw_preset"));

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_kw_misc") },
                Option.Checkbox(
                    fixedWidthLabel,
                    new Accessor<bool>(() => profile.NamePlateUseFixedWidth),
                    search: new SearchMetadata(fixedWidthLabel, Keywords: [TazLang.Get("mog_kw_fixed"), TazLang.Get("mog_kw_width")])
                ),
                Option.Checkbox(
                    showWordOfDeathIconLabel,
                    new Accessor<bool>(() => profile.NamePlateShowWordOfDeathIcon),
                    search: new SearchMetadata(showWordOfDeathIconLabel, Keywords: [TazLang.Get("mog_kw_icon"), TazLang.Get("mog_kw_death")])
                ),
                Option.LComboBox(
                    presetLabel,
                    new Accessor<NamePlatePreset>(() => profile.NamePlatePreset),
                    locNameplatesHealth,
                    search: new SearchMetadata(presetLabel, Keywords: [TazLang.Get("mog_kw_preset")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_general_incomingmobiles"),
                    new Accessor<bool>(() => profile.ShowNewMobileNameIncoming),
                    search: new SearchMetadata(TazLang.Get("mog_general_incomingmobiles"), Keywords: [TazLang.Get("mog_kw_incoming"), TazLang.Get("mog_kw_mobile")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_general_incomingcorpses"),
                    new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming),
                    search: new SearchMetadata(TazLang.Get("mog_general_incomingcorpses"), Keywords: [TazLang.Get("mog_kw_incoming"), TazLang.Get("mog_kw_corpse")])
                ),
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHealthBar), TazLang.Get("mog_tazuo_nameplatesalsoactashealthbars")),
                    Option.Slider(
                        TazLang.Get("mog_tazuo_hpopacity"),
                        0,
                        100,
                        new Accessor<byte>(() => profile.NamePlateHealthBarOpacity),
                        search: new SearchMetadata(TazLang.Get("mog_tazuo_hpopacity"), Keywords: [TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_opacity")])
                    ),
                    OptionsUi.CheckBoxGroup(
                        new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHideAtFullHealth), TazLang.Get("mog_tazuo_hidenameplatesiffullhealth")),
                        Option.Checkbox(
                            TazLang.Get("mog_tazuo_onlyinwarmode"),
                            new Accessor<bool>(() => profile.NamePlateHideAtFullHealthInWarmode),
                            search: new SearchMetadata(TazLang.Get("mog_tazuo_onlyinwarmode"), Keywords: [TazLang.Get("mog_kw_war"), TazLang.Get("mog_kw_mode")])
                        )
                    ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_nameplate")], Keywords: [TazLang.Get("mog_kw_hide"), TazLang.Get("mog_kw_health")]))
                ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_nameplate")], Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_hp")])),
                Option.Slider(
                    TazLang.Get("mog_tazuo_borderopacity"),
                    0,
                    100,
                    new Accessor<byte>(() => profile.NamePlateBorderOpacity),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_borderopacity"), Keywords: [TazLang.Get("mog_kw_border"), TazLang.Get("mog_kw_opacity")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_tazuo_avoidoverlap"),
                    new Accessor<bool>(() => profile.NamePlateAvoidOverlap),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_avoidoverlap"), Keywords: [TazLang.Get("mog_kw_overlap"), TazLang.Get("mog_kw_avoid")])
                )
            )
        );
    }

    #endregion General Sub-Tab
}
