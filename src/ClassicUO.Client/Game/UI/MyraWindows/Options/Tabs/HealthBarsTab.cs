using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for health-bar display and drag-to-lock settings</summary>
public static class HealthBarsTab
{
    /// <summary>Returns the option fragment for health-bar appearance and drag-lock configuration</summary>
    internal static IOptionSource GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;
        return OptionsUi.Vertical(
            GetMainSection(),
            GetDragSection()
        ).WithSearch(new SearchMetadata(lang.ButtonHealthBars, Tags: [kw.HealthBar, kw.HP]));
    }

    private static OptionFragment GetMainSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        string usePartyHealthBarsLabel = TazLang.Get("healthbar_usepartystyle", "Use party health bar style for party members");

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), genLang.ModernHealthBars),
                Option.Checkbox(
                    genLang.ModernHPBlackBG,
                    new Accessor<bool>(() => profile.CBBlackBGToggled),
                    search: new SearchMetadata(genLang.ModernHPBlackBG, Keywords: [kw.Black, kw.Background])
                )
            ).WithSearch(new SearchMetadata(genLang.ModernHealthBars, Keywords: [kw.Modern])),
            Option.Checkbox(
                usePartyHealthBarsLabel,
                new Accessor<bool>(() => profile.UsePartyHealthBars),
                search: new SearchMetadata(usePartyHealthBarsLabel, Keywords: [kw.Party, kw.HealthBar])
            ),
            Option.Checkbox(
                genLang.SaveHPBars,
                new Accessor<bool>(() => profile.SaveHealthbars),
                search: new SearchMetadata(genLang.SaveHPBars, Keywords: [kw.Save])
            ),
            Option.ComboBox(genLang.CloseHPGumpsWhen, profile.CloseHealthBarType, [
                    genLang.CloseHPOptDisable, genLang.CloseHPOptOOR,
                    genLang.CloseHPOptDead, genLang.CloseHPOptBoth
                ], b => profile.CloseHealthBarType = b,
                search: new SearchMetadata(genLang.CloseHPGumpsWhen, Keywords: [kw.Close])
            ),
            Option.Checkbox(
                tuoLang.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars,
                new Accessor<bool>(() => profile.CloseHealthBarIfAnchored),
                search: new SearchMetadata(tuoLang.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars, Keywords: [kw.Close, kw.Anchor])
            ),
            Option.Slider(
                tuoLang.BelowMobileHealthBarScale,
                1,
                5,
                new Accessor<int>(() => profile.HealthLineSizeMultiplier),
                search: new SearchMetadata(tuoLang.BelowMobileHealthBarScale, Keywords: [kw.Below, kw.Scale])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableHealthIndicator), tuoLang.HealthBarIndicator),
                Option.Slider(
                    tuoLang.OnlyShowBelowHp,
                    0,
                    100,
                    new Accessor<float>(() => profile.ShowHealthIndicatorBelow),
                    search: new SearchMetadata(tuoLang.OnlyShowBelowHp, Keywords: [kw.HP])
                ),
                Option.Slider(
                    tuoLang.Size,
                    1,
                    25,
                    new Accessor<float>(() => profile.HealthIndicatorWidth, f => profile.HealthIndicatorWidth = (int)f),
                    search: new SearchMetadata(tuoLang.Size, Keywords: [kw.Size])
                )
            ).WithSearch(new SearchMetadata(tuoLang.HealthBarIndicator, Keywords: [kw.Indicator, kw.Border])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.OpenHealthBarForLastAttack), tuoLang.AutomaticallyOpenHealthBarsForLastAttack),
                Option.Checkbox(
                    tuoLang.UpdateOneBarAsLastAttack,
                    new Accessor<bool>(() => profile.UseOneHPBarForLastAttack)
                )
            ).WithSearch(new SearchMetadata(tuoLang.AutomaticallyOpenHealthBarsForLastAttack, Keywords: [kw.Last, kw.Attack]))
        );
    }

    private static OptionFragment GetDragSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = genLang.DraggingSectionLabel },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableDragSelect), genLang.DragSelectHP),
                Option.Checkbox(
                    genLang.DragAnchored,
                    new Accessor<bool>(() => profile.DragSelectAsAnchor),
                    search: new SearchMetadata(genLang.DragAnchored, Keywords: [kw.Drag, kw.Select])
                ),
                Option.ComboBox(
                    genLang.DragKeyMod,
                    profile.DragSelectModifierKey,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelectModifierKey = i,
                    search: new SearchMetadata(genLang.DragKeyMod, Keywords: [kw.Drag, kw.Modifier])
                ),
                Option.ComboBox(
                    genLang.DragPlayersOnly,
                    profile.DragSelect_PlayersModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_PlayersModifier = i,
                    search: new SearchMetadata(genLang.DragPlayersOnly, Keywords: [kw.Drag, kw.Player])
                ),
                Option.ComboBox(
                    genLang.DragMobsOnly,
                    profile.DragSelect_MonstersModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_MonstersModifier = i,
                    search: new SearchMetadata(genLang.DragMobsOnly, Keywords: [kw.Drag, kw.Monster])
                ),
                Option.ComboBox(
                    genLang.DragNameplatesOnly,
                    profile.DragSelect_NameplateModifier,
                    [
                        genLang.SharedNone,
                        genLang.SharedCtrl,
                        genLang.SharedShift,
                        genLang.SharedAlt
                    ],
                    i => profile.DragSelect_NameplateModifier = i,
                    search: new SearchMetadata(genLang.DragNameplatesOnly, Keywords: [kw.Drag, kw.Nameplate])
                ),
                Option.InputField(
                    genLang.DragX,
                    new Accessor<string>(() => profile.DragSelectStartX.ToString(), s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartX = result;
                    }),
                    search: new SearchMetadata(genLang.DragX, Keywords: [kw.Drag, kw.X])
                ),
                Option.InputField(
                    genLang.DragY,
                    new Accessor<string>(() => profile.DragSelectStartY.ToString(), s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartY = result;
                    }),
                    search: new SearchMetadata(genLang.DragY, Keywords: [kw.Drag, kw.Y])
                )
            ).WithSearch(new SearchMetadata(genLang.DragSelectHP, Keywords: [kw.Drag, kw.Select]))
        );
    }
}
