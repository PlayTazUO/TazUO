using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;
using ClassicUO.Resources;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class NameplatesTab
{
    internal static IOptionSource GetContent() => GetNameplatesMenuTabs();

    private static OptionTabGroup GetNameplatesMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup()
            .AddTab(lang.ButtonGeneral, GetGeneralNameplatesSubTabContent, new SearchMetadata(lang.ButtonGeneral, Keywords: [kw.General]))
            .AddTab(lang.ButtonProfiles, GetProfilesSubTabContentSource, new SearchMetadata(lang.ButtonProfiles, Keywords: [kw.Profile]));
    }

    #region Profiles

    private static IOptionSource GetProfilesSubTabContentSource()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;
        return OptionsUi.Vertical(
            Option.Custom(GetProfilesSubTabContent, new SearchMetadata(lang.ButtonProfiles, Keywords: [kw.Profile]))
        ).WithSearch(new SearchMetadata(lang.ButtonProfiles, Tags: [kw.Nameplate, kw.Profile]));
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

        var currentHotkey = new HotkeySelection(profile.Key, mods);

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionTabCommons.StyledHorizontalSpaceBetween(
                [
                    new HotkeyInput(
                        existingSelection: currentHotkey,
                        onSelectionChanged: e => OnProfileHotkeyChanged(profile, e)
                    ) { Padding = new Thickness(MyraStyle.STANDARD_SPACING, 0, 0, 0) }
                ],
                [
                    OptionTabCommons.StyledVerticalSeparator(),
                    new MyraButton(
                        npLang.CheckAll,
                        () => profile.NameOverheadOptionFlags = EnumUtils.AllBits<NameOverheadOptions>()
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
        HotkeySelection value = e.NewValue;

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
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            Option.FontSelector(
                tuoLang.NameplateFont,
                new Accessor<string>(() => profile.NamePlateFont),
                s => profile.NamePlateFont = s,
                search: new SearchMetadata(tuoLang.NameplateFont, Keywords: [kw.Font])
            ),
            Option.Slider(
                tuoLang.SharedSize,
                5,
                50,
                new Accessor<float>(() => profile.NamePlateFontSize, f => profile.NamePlateFontSize = (int)f),
                search: new SearchMetadata(tuoLang.SharedSize, Keywords: [kw.Size])
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
            OptionsUi.Vertical(
                Option.Checkbox(
                    tuoLang.NameplatesAlsoActAsHealthBars,
                    new Accessor<bool>(() => profile.NamePlateHealthBar),
                    search: new SearchMetadata(tuoLang.NameplatesAlsoActAsHealthBars, Keywords: [kw.HealthBar, kw.HP])
                ),
                Option.Slider(
                    tuoLang.HpOpacity,
                    0,
                    100,
                    new Accessor<float>(() => profile.NamePlateHealthBarOpacity, f => profile.NamePlateHealthBarOpacity = (byte)f),
                    search: new SearchMetadata(tuoLang.HpOpacity, Keywords: [kw.HP, kw.Opacity])
                ),
                OptionsUi.Vertical(
                    Option.Checkbox(
                        tuoLang.HideNameplatesIfFullHealth,
                        new Accessor<bool>(() => profile.NamePlateHideAtFullHealth),
                        search: new SearchMetadata(tuoLang.HideNameplatesIfFullHealth, Keywords: [kw.Hide, kw.Full, kw.Health])
                    ),
                    Option.Checkbox(
                        tuoLang.OnlyInWarmode,
                        new Accessor<bool>(() => profile.NamePlateHideAtFullHealthInWarmode),
                        search: new SearchMetadata(tuoLang.OnlyInWarmode, Keywords: [kw.War, kw.Mode])
                    )
                )
            ),
            Option.Slider(
                tuoLang.BorderOpacity,
                0,
                100,
                new Accessor<float>(() => profile.NamePlateBorderOpacity, f => profile.NamePlateBorderOpacity = (byte)f),
                search: new SearchMetadata(tuoLang.BorderOpacity, Keywords: [kw.Border, kw.Opacity])
            ),
            Option.Slider(
                tuoLang.BackgroundOpacity,
                0,
                100,
                new Accessor<float>(() => profile.NamePlateOpacity, f => profile.NamePlateOpacity = (byte)f),
                search: new SearchMetadata(tuoLang.BackgroundOpacity, Keywords: [kw.Background, kw.Opacity])
            ),
            Option.Checkbox(
                tuoLang.AvoidOverlap,
                new Accessor<bool>(() => profile.NamePlateAvoidOverlap),
                search: new SearchMetadata(tuoLang.AvoidOverlap, Keywords: [kw.Overlap, kw.Avoid])
            )
        ).WithSearch(new SearchMetadata(lang.ButtonGeneral, Tags: [kw.Nameplate, kw.General]));
    }

    #endregion General Sub-Tab
}
