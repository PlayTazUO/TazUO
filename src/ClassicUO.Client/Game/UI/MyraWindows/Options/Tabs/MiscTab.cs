using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MiscTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonCombatSpells, GetPages);
    }

    private static PageControl GetPages() => new(GetPage1(), GetPage2()) { RetainSizeWhenPaging = true };

    private static WrapPanel GetPage1()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;

        WrapPanel panel = OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseCircleOfTransparency), genLang.EnableCOT),
                OptionsFactory.CreateSliderOption(
                    genLang.COTDistance,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    profile.CircleOfTransparencyRadius,
                    f => profile.CircleOfTransparencyRadius = (int)f
                ),
                OptionsFactory.CreateComboBox(
                    genLang.COTType,
                    profile.CircleOfTransparencyType,
                    [
                        genLang.COTTypeOptFull,
                        genLang.COTTypeOptGrad,
                        genLang.COTTypeOptModern
                    ],
                    i => profile.CircleOfTransparencyType = i
                )
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(
                genLang.HideScreenshotMessage,
                new Accessor<bool>(() => profile.HideScreenshotStoredInMessage)
            ),
            OptionsFactory.CreateCheckboxOption(
                genLang.ObjFade,
                new Accessor<bool>(() => profile.UseObjectsFading)
            ),
            OptionsFactory.CreateCheckboxOption(
                genLang.TextFade,
                new Accessor<bool>(() => profile.TextFading)
            ),
            OptionsFactory.CreateCheckboxOption(
                genLang.CursorRange,
                new Accessor<bool>(() => profile.ShowTargetRangeIndicator)
            ),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableDragSelect), genLang.DragSelectHP),
                OptionsFactory.CreateComboBox(
                    genLang.DragKeyMod,
                    profile.DragSelectModifierKey,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelectModifierKey = i
                ),
                OptionsFactory.CreateComboBox(
                    genLang.DragPlayersOnly,
                    profile.DragSelect_PlayersModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_PlayersModifier = i
                ),
                OptionsFactory.CreateComboBox(
                    genLang.DragMobsOnly,
                    profile.DragSelect_MonstersModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_MonstersModifier = i
                ),
                OptionsFactory.CreateComboBox(
                    genLang.DragNameplatesOnly,
                    profile.DragSelect_NameplateModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_NameplateModifier = i
                ),
                OptionsFactory.CreateInputField(
                    genLang.DragX,
                    profile.DragSelectStartX.ToString(),
                    s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartX = result;
                    }
                ),
                OptionsFactory.CreateInputField(
                    genLang.DragY,
                    profile.DragSelectStartY.ToString(),
                    s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartY = result;
                    }
                )
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(
                genLang.ShowStatsChangedMsg,
                new Accessor<bool>(() => profile.ShowStatsChangedMessage)
            ),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.ShowSkillsChangedMessage),
                    genLang.ShowSkillsChangedMsg
                ),
                OptionsFactory.CreateSliderOption(
                    genLang.ChangeVolume,
                    0,
                    100,
                    profile.ShowSkillsChangedDeltaValue,
                    f => profile.ShowSkillsChangedDeltaValue = (int)f
                )
            )
        );

        panel.VerticalAlignment = VerticalAlignment.Top;
        return panel;
    }

    private static WrapPanel GetPage2()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.CombatSpells lang = Language.Instance.GetModernOptionsGumpLanguage.GetCombatSpells;

        WrapPanel panel = OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(genLang.HighlightObjects, new Accessor<bool>(() => profile.HighlightGameObjects)),
            OptionsFactory.CreateSpacer(),
            new OptionItem(genLang.AutoOpenCorpse, () => new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenCorpses), genLang.AutoOpenCorpse),
                OptionsFactory.CreateSliderOption(genLang.CorpseOpenDistance, 0, 5, profile.AutoOpenCorpseRange,
                    f => profile.AutoOpenCorpseRange = (int)f),
                OptionsFactory.CreateCheckboxOption(genLang.CorpseSkipEmpty, new Accessor<bool>(() => profile.SkipEmptyCorpse), genLang.CorpseSkipEmptyTooltip),
                OptionsFactory.CreateComboBox(genLang.CorpseOpenOptions, profile.CorpseOpenOptions, [
                    genLang.CorpseOptNone, genLang.CorpseOptNotTarg,
                    genLang.CorpseOptNotHiding, genLang.CorpseOptBoth
                ], i => profile.CorpseOpenOptions = i)
            )),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(genLang.OutRangeColor, new Accessor<bool>(() => profile.NoColorObjectsOutOfRange)),
            OptionsFactory.CreateCheckboxOption(genLang.SallosEasyGrab, new Accessor<bool>(() => profile.SallosEasyGrab), genLang.SallosTooltip),
            OptionsFactory.CreateCheckboxOption(genLang.ShowHouseContent, new Accessor<bool>(() => profile.ShowHouseContent), genLang.ClientVersionLimitedTooltip),
            OptionsFactory.CreateCheckboxOption(genLang.SmoothBoat, new Accessor<bool>(() => profile.UseSmoothBoatMovement), genLang.ClientVersionLimitedTooltip),
            GetExperimentalSection()
        );

        panel.VerticalAlignment = VerticalAlignment.Top;
        return panel;
    }

    private static VisualContainer GetExperimentalSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Experimental experimentalLang = Language.Instance.GetModernOptionsGumpLanguage.GetExperimental;

        return new VisualContainer(
            new VisualContainerProps { LabelText = lang.ButtonExperimental },
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableDefaultUoHotkeys,
                new Accessor<bool>(() => profile.DisableDefaultHotkeys)
            ),
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableArrowsNumlockArrowsPlayerMovement,
                new Accessor<bool>(() => profile.DisableArrowBtn)
            ),
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableTabToggleWarmode,
                new Accessor<bool>(() => profile.DisableTabBtn)
            ),
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableCtrlQWMessageHistory,
                new Accessor<bool>(() => profile.DisableCtrlQWBtn)
            ),
            OptionsFactory.CreateCheckboxOption(
                experimentalLang.DisableRightLeftClickAutoMove,
                new Accessor<bool>(() => profile.DisableAutoMove)
            )
        );
    }
}
