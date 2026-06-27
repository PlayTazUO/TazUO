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

    private static OptionTabGroup GetNameplatesMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup()
            .AddTab(lang.ButtonGeneral, GetGeneralNameplatesSubTabContent, new SearchMetadata(lang.ButtonGeneral, Keywords: [kw.General]))
            .AddTab(lang.ButtonProfiles, GetProfilesSubTabContentSource,
                new SearchMetadata()); // Empty metadata to disable search; Doesn't render well in the results page.
    }

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
            NameOverHeadManager.GetAllOptions()
        );
        return profileEditor;
    }

    private static WrapPanel GetEditorForProfile(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

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
                    ) { Padding = new Thickness(MyraStyle.STANDARD_SPACING, 0, 0, 0) }
                ],
                [
                    OptionTabCommons.StyledVerticalSeparator(),
                    new MyraButton(
                        npLang.CheckAll,
                        () => profile.NameOverheadOptionFlags = ByteFlagHelper.AllBits<NameOverheadOptions>()
                    ),
                    new MyraButton(
                        npLang.UncheckAll,
                        () => profile.NameOverheadOptionFlags = NameOverheadOptions.None
                    )
                ]
            ),
            settingsPanel
        );
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

    private static VisualContainer GetItemsBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.Items },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Containers,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Containers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Stackable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Stackable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Moveable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Moveable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.OtherItems,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Other
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Gold,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gold
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.LockedDown,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.LockedDown
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Immovable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Immoveable
            )
        );
    }

    private static VisualContainer GetCorpseBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;
        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.Corpses },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Monster,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.MonsterCorpses
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Humanoid,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.HumanoidCorpses
            )
        );
    }

    private static VisualContainer GetMobilesByTypeBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.MobilesByType },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Humanoid,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Humanoid
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.YourFollowers,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.OwnFollowers
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.ExcludeYourself,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.ExcludeSelf
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Monster,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Monster
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Yourself,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Self
            )
        );
    }

    private static VisualContainer GetMobilesByNotorietyBoxesPanel(NameOverheadOption profile)
    {
        ModernOptionsGumpLanguage.NamePlatesOptionsTab npLang = Language.Instance.GetModernOptionsGumpLanguage.GetNamePlates.OptionsTab;
        return new VisualContainer(
            new VisualContainerProps { LabelText = npLang.MobilesByNotoriety },
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Innocent,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Innocent
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Attackable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Gray
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Enemy,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Enemy
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Invulnerable,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Invulnerable
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Allied,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Ally
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Criminal,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Criminal
            ),
            OptionsFactory.CreatePropBoundBitFlagCheckBox(
                npLang.Murderer,
                new Accessor<NameOverheadOptions>(() => profile.NameOverheadOptionFlags),
                NameOverheadOptions.Murderer
            )
        );
    }

    #endregion Profiles

    #region General Sub-Tab

    private static IOptionSource GetGeneralNameplatesSubTabContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kwLang = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Horizontal(GeneralSettingsLeftSide(), GeneralSettingsRightSide())
            .WithSearch(new SearchMetadata(lang.ButtonGeneral, [kwLang.Nameplate, kwLang.General]));
    }

    private static OptionFragment GeneralSettingsLeftSide()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;

        const string locNameplatesHealth = "nameplate_health_";
        string nameWidthLabel = TazLang.Get("nameplate_width", "Name width");
        string heightLabel = TazLang.Get("nameplate_height", "Height");
        string cornerRadiusLabel = TazLang.Get("nameplate_cornerradius", "Corner radius");
        string separateHealthBarWidthLabel = TazLang.Get("nameplate_separatehealthbarwidth", tuoLang.SeparateHealthBarWidth);
        string healthBarWidthLabel = TazLang.Get("nameplate_healthbarwidth", tuoLang.HealthBarWidth);
        string splitHealthBarLabel = TazLang.Get("nameplate_splithealthbar", tuoLang.SplitHealthBar);

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = kw.Appearance },
                Option.IntegerInput(
                    heightLabel,
                    new Accessor<int>(() => profile.NamePlateHeight),
                    0,
                    80,
                    search: new SearchMetadata(heightLabel, Keywords: [kw.Height])
                ),
                Option.IntegerInput(
                    nameWidthLabel,
                    new Accessor<int>(() => profile.NamePlateFixedWidth),
                    60,
                    300,
                    search: new SearchMetadata(nameWidthLabel, Keywords: [kw.Width])
                ),
                Option.IntegerInput(
                    cornerRadiusLabel,
                    new Accessor<int>(() => profile.NamePlateCornerRadius),
                    0,
                    40,
                    search: new SearchMetadata(cornerRadiusLabel, Keywords: [kw.Corner, kw.Radius])
                ),
                Option.FontSelector(
                    tuoLang.NameplateFont,
                    new Accessor<string>(() => profile.NamePlateFont),
                    s => profile.NamePlateFont = s,
                    new SearchMetadata(tuoLang.NameplateFont, Keywords: [kw.Font])
                ),
                Option.Slider(
                    kw.Size,
                    5,
                    50,
                    new Accessor<int>(() => profile.NamePlateFontSize),
                    search: new SearchMetadata(tuoLang.SharedSize, Keywords: [kw.Size])
                )
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = tuoLang.BackgroundColor },
                Option.Slider(
                    kw.Red,
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundR),
                    search: new SearchMetadata(kw.Red, Keywords: [kw.Red, kw.Color])
                ),
                Option.Slider(
                    kw.Green,
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundG),
                    search: new SearchMetadata(kw.Green, Keywords: [kw.Green, kw.Color])
                ),
                Option.Slider(
                    kw.Blue,
                    0,
                    255,
                    new Accessor<byte>(() => profile.NamePlateBackgroundB),
                    search: new SearchMetadata(kw.Blue, Keywords: [kw.Blue, kw.Color])
                ),
                Option.Slider(
                    tuoLang.BackgroundOpacity,
                    0,
                    100,
                    new Accessor<byte>(() => profile.NamePlateOpacity),
                    search: new SearchMetadata(tuoLang.BackgroundOpacity, Keywords: [kw.Background, kw.Opacity])
                ),
                Option.ComboBox(
                    kw.Mode,
                    new Accessor<NamePlateBackgroundMode>(() => profile.NamePlateBackgroundMode),
                    search: new SearchMetadata(kw.Mode, Keywords: [kw.Mode, kw.Background])
                )
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = kw.HealthBar },
                Option.LComboBox(
                    kw.Mode,
                    new Accessor<NamePlateHealthBarMode>(() => profile.NamePlateHealthBarMode),
                    locNameplatesHealth,
                    search: new SearchMetadata(kw.Mode, Keywords: [kw.HealthBar, kw.Mode])
                ),
                Option.Checkbox(
                    separateHealthBarWidthLabel,
                    new Accessor<bool>(() => profile.NamePlateUseFixedHealthBarWidth),
                    search: new SearchMetadata(separateHealthBarWidthLabel, Keywords: [kw.Fixed, kw.Width, kw.HealthBar])
                ),
                Option.IntegerInput(
                    healthBarWidthLabel,
                    new Accessor<int>(() => profile.NamePlateHealthBarFixedWidth),
                    60,
                    300,
                    search: new SearchMetadata(healthBarWidthLabel, Keywords: [kw.HealthBar, kw.Width])
                ),
                Option.Checkbox(
                    splitHealthBarLabel,
                    new Accessor<bool>(() => profile.NamePlateSplitHealthBar),
                    search: new SearchMetadata(splitHealthBarLabel, Keywords: [kw.HealthBar, kw.Split])
                )
            )
        );
    }

    private static OptionFragment GeneralSettingsRightSide()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        const string locNameplatesHealth = "nameplate_health_";
        string fixedWidthLabel = TazLang.Get("nameplate_fixedwidth", tuoLang.FixedWidth);
        string showWordOfDeathIconLabel = TazLang.Get("nameplate_showwordofdeathicon", tuoLang.ShowWordOfDeathIcon);
        string presetLabel = TazLang.Get("nameplate_preset", kw.Preset);

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = kw.Misc },
                Option.Checkbox(
                    fixedWidthLabel,
                    new Accessor<bool>(() => profile.NamePlateUseFixedWidth),
                    search: new SearchMetadata(fixedWidthLabel, Keywords: [kw.Fixed, kw.Width])
                ),
                Option.Checkbox(
                    showWordOfDeathIconLabel,
                    new Accessor<bool>(() => profile.NamePlateShowWordOfDeathIcon),
                    search: new SearchMetadata(showWordOfDeathIconLabel, Keywords: [kw.Icon, kw.Death])
                ),
                Option.LComboBox(
                    presetLabel,
                    new Accessor<NamePlatePreset>(() => profile.NamePlatePreset),
                    locNameplatesHealth,
                    search: new SearchMetadata(presetLabel, Keywords: [kw.Preset])
                ),
                Option.ComboBox(
                    genLang.DragNameplatesOnly,
                    profile.DragSelect_NameplateModifier,
                    [genLang.SharedNone, genLang.SharedCtrl, genLang.SharedShift, genLang.SharedAlt],
                    i => profile.DragSelect_NameplateModifier = i,
                    search: new SearchMetadata(genLang.DragNameplatesOnly, Keywords: [kw.Drag, kw.Modifier])
                ),
                Option.Checkbox(
                    genLang.IncomingMobiles,
                    new Accessor<bool>(() => profile.ShowNewMobileNameIncoming),
                    search: new SearchMetadata(genLang.IncomingMobiles, Keywords: [kw.Incoming, kw.Mobile])
                ),
                Option.Checkbox(
                    genLang.IncomingCorpses,
                    new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming),
                    search: new SearchMetadata(genLang.IncomingCorpses, Keywords: [kw.Incoming, kw.Corpse])
                ),
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHealthBar), tuoLang.NameplatesAlsoActAsHealthBars),
                    Option.Slider(
                        tuoLang.HpOpacity,
                        0,
                        100,
                        new Accessor<byte>(() => profile.NamePlateHealthBarOpacity),
                        search: new SearchMetadata(tuoLang.HpOpacity, Keywords: [kw.HP, kw.Opacity])
                    ),
                    OptionsUi.CheckBoxGroup(
                        new PropertyBinder(new Accessor<bool>(() => profile.NamePlateHideAtFullHealth), tuoLang.HideNameplatesIfFullHealth),
                        Option.Checkbox(
                            tuoLang.OnlyInWarmode,
                            new Accessor<bool>(() => profile.NamePlateHideAtFullHealthInWarmode),
                            search: new SearchMetadata(tuoLang.OnlyInWarmode, Keywords: [kw.War, kw.Mode])
                        )
                    ).WithSearch(new SearchMetadata(Tags: [kw.Nameplate], Keywords: [kw.Hide, kw.Health]))
                ).WithSearch(new SearchMetadata(Tags: [kw.Nameplate], Keywords: [kw.HealthBar, kw.HP])),
                Option.Slider(
                    tuoLang.BorderOpacity,
                    0,
                    100,
                    new Accessor<byte>(() => profile.NamePlateBorderOpacity),
                    search: new SearchMetadata(tuoLang.BorderOpacity, Keywords: [kw.Border, kw.Opacity])
                ),
                Option.Checkbox(
                    tuoLang.AvoidOverlap,
                    new Accessor<bool>(() => profile.NamePlateAvoidOverlap),
                    search: new SearchMetadata(tuoLang.AvoidOverlap, Keywords: [kw.Overlap, kw.Avoid])
                )
            )
        );
    }

    #endregion General Sub-Tab
}
