using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class HealthBarsTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(
            lang.ButtonGameplay,
            () => OptionTabCommons.StyledVerticalWrapPanel(
                GetMainSection(),
                OptionsFactory.CreateSpacer(),
                GetDragSection()
            ));
    }

    private static WrapPanel GetMainSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), genLang.ModernHealthBars),
                OptionsFactory.CreateCheckboxOption(genLang.ModernHPBlackBG, new Accessor<bool>(() => profile.CBBlackBGToggled))
            ),
            OptionsFactory.CreateCheckboxOption(genLang.SaveHPBars, new Accessor<bool>(() => profile.SaveHealthbars)),
            OptionsFactory.CreateComboBox(genLang.CloseHPGumpsWhen, profile.CloseHealthBarType, [
                    genLang.CloseHPOptDisable, genLang.CloseHPOptOOR,
                    genLang.CloseHPOptDead, genLang.CloseHPOptBoth
                ], b => profile.CloseHealthBarType = b
            ),
            OptionsFactory.CreateCheckboxOption(
                tuoLang.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars,
                new Accessor<bool>(() => profile.CloseHealthBarIfAnchored)
            ),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableHealthIndicator), tuoLang.EnableHealthIndicatorBorder),
                OptionsFactory.PropBoundSliderOption(tuoLang.OnlyShowBelowHp, new Accessor<float>(() => profile.ShowHealthIndicatorBelow), 0, 100),
                OptionsFactory.PropBoundSliderOption(tuoLang.Size, new Accessor<int>(() => profile.HealthIndicatorWidth), 1, 25)
            )
        );
    }

    private static VisualContainer GetDragSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        return new VisualContainer(
            new VisualContainerProps { LabelText = "Dragging" },
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
            )
        );
    }
}
