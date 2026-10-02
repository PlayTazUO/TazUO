using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for health-bar display and drag-to-lock settings</summary>
public static class HealthBarsTab
{
    /// <summary>Returns the option fragment for health-bar appearance and drag-lock configuration</summary>
    internal static IOptionSource GetContent() => OptionsUi.Vertical(
            GetMainSection(),
            GetHealthBars(),
            GetLastAttackSection(),
            GetDragSection()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_buttonhealthbars"), Tags: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_hp")]));

    private static OptionFragment GetMainSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        string simpleHealthbar = TazLang.Get("mog_mobilestab_healthbars_simple", "Simple healthbar");
        string showMobileNameOverhead = TazLang.Get("mog_mobilestab_shownameoverhead", "Always show name/title above mobiles");

        return OptionsUi.Vertical(
            Option.Checkbox(
                simpleHealthbar,
                new Accessor<bool>(() => profile.ShowMobileHealthbar),
                search: new SearchMetadata(simpleHealthbar, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_mobile")])
            ),
            Option.Checkbox(
                showMobileNameOverhead,
                new Accessor<bool>(() => profile.ShowMobileNameOverhead),
                search: new SearchMetadata(showMobileNameOverhead, Keywords: [TazLang.Get("mog_kw_mobile"), TazLang.Get("mog_kw_name")])
            ),
            OptionsUi.VisualContainer(
                    new VisualContainerProps { LabelText = TazLang.Get("mog_tazuo_mobilehealthindicator") },
                    OptionsUi.CheckBoxGroup(
                        new PropertyBinder(new Accessor<bool>(() => profile.ShowMobilesHP), TazLang.Get("mog_mobilestab_highlighting_showmobilehp")),
                        Option.ComboBox(
                            TazLang.Get("mog_mobilestab_highlighting_mobilehptype"),
                            profile.MobileHPType,
                            [TazLang.Get("mog_general_hptypeperc"), TazLang.Get("mog_general_hptypebar"), TazLang.Get("mog_general_hptypenboth")],
                            i => profile.MobileHPType = i,
                            search: new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_mobilehptype"), Keywords: [TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_health"), TazLang.Get("mog_kw_type")])
                        ),
                        Option.ComboBox(
                            TazLang.Get("mog_mobilestab_highlighting_hpshowwhen"),
                            profile.MobileHPShowWhen,
                            [TazLang.Get("mog_general_hpshowwhen_always"), TazLang.Get("mog_general_hpshowwhen_less100"), TazLang.Get("mog_general_hpshowwhen_smart")],
                            i => profile.MobileHPShowWhen = i,
                            search: new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_hpshowwhen"), Keywords: [TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_health"), TazLang.Get("mog_kw_show")])
                        )
                    ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_health")])),
                    Option.Slider(
                        TazLang.Get("mog_tazuo_belowmobilehealthbarscale"),
                        1,
                        5,
                        new Accessor<int>(() => profile.HealthLineSizeMultiplier),
                        search: new SearchMetadata(TazLang.Get("mog_tazuo_belowmobilehealthbarscale"), Keywords: [TazLang.Get("mog_kw_below"), TazLang.Get("mog_kw_scale")])
                    )
                ).AsSearchGroup()
                .WithSearch(new SearchMetadata(TazLang.Get("mog_tazuo_mobilehealthindicator"), Keywords: [TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_health"), TazLang.Get("mog_kw_scale")]))
        );
    }

    private static OptionFragment GetHealthBars()
    {
        Profile profile = ProfileManager.CurrentProfile;

        string usePartyHealthBarsLabel = TazLang.Get("healthbar_usepartystyle", "Use party health bar style for party members");
        string healCureAllLabel = TazLang.Get("healthbar_healcureall", "Show heal/cure buttons on all health bars (except invulnerable)");
        string healCureFriendsLabel = TazLang.Get("healthbar_healcurefriends", "Show heal/cure buttons on friends list health bars");
        string healCurePetsLabel = TazLang.Get("healthbar_healcurepets", "Show heal/cure buttons on pet health bars");
        string quickHealActionLabel = TazLang.Get("healthbar_quickhealaction", "Quick heal action");
        string quickCureActionLabel = TazLang.Get("healthbar_quickcureaction", "Quick cure action");

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("healthbars_floating_section") },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.CustomBarsToggled), TazLang.Get("mog_general_modernhealthbars")),
                Option.Checkbox(
                    TazLang.Get("mog_general_modernhpblackbg"),
                    new Accessor<bool>(() => profile.CBBlackBGToggled),
                    search: new SearchMetadata(TazLang.Get("mog_general_modernhpblackbg"), Keywords: [TazLang.Get("mog_kw_black"), TazLang.Get("mog_kw_background")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_general_modernhealthbars"), Keywords: [TazLang.Get("mog_kw_modern")])),
            Option.Checkbox(
                usePartyHealthBarsLabel,
                new Accessor<bool>(() => profile.UsePartyHealthBars),
                search: new SearchMetadata(usePartyHealthBarsLabel, Keywords: [TazLang.Get("mog_kw_party"), TazLang.Get("mog_kw_healthbar")])
            ),
            Option.Checkbox(
                healCureAllLabel,
                new Accessor<bool>(() => profile.ShowHealCureButtonsAllHealthbars),
                search: new SearchMetadata(healCureAllLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_heal")])
            ),
            Option.Checkbox(
                healCureFriendsLabel,
                new Accessor<bool>(() => profile.ShowHealCureButtonsFriends),
                search: new SearchMetadata(healCureFriendsLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_heal")])
            ),
            Option.Checkbox(
                healCurePetsLabel,
                new Accessor<bool>(() => profile.ShowHealCureButtonsPets),
                search: new SearchMetadata(healCurePetsLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_heal")])
            ),
            Option.LComboBox(
                quickHealActionLabel,
                new Accessor<HealthBarQuickAction>(() => profile.QuickHealAction),
                "healthbar_quickaction_",
                search: new SearchMetadata(quickHealActionLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_heal")])
            ),
            Option.LComboBox(
                quickCureActionLabel,
                new Accessor<HealthBarQuickAction>(() => profile.QuickCureAction),
                "healthbar_quickaction_",
                search: new SearchMetadata(quickCureActionLabel, Keywords: [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_cure")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_savehpbars"),
                new Accessor<bool>(() => profile.SaveHealthbars),
                search: new SearchMetadata(TazLang.Get("mog_general_savehpbars"), Keywords: [TazLang.Get("mog_kw_save")])
            ),
            Option.ComboBox(TazLang.Get("mog_general_closehpgumpswhen"), profile.CloseHealthBarType, [
                    TazLang.Get("mog_general_closehpoptdisable"), TazLang.Get("mog_general_closehpoptoor"),
                    TazLang.Get("mog_general_closehpoptdead"), TazLang.Get("mog_general_closehpoptboth")
                ], b => profile.CloseHealthBarType = b,
                search: new SearchMetadata(TazLang.Get("mog_general_closehpgumpswhen"), Keywords: [TazLang.Get("mog_kw_close")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_alsocloseanchoredhealthbarswhenautoclosinghealthbars"),
                new Accessor<bool>(() => profile.CloseHealthBarIfAnchored),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_alsocloseanchoredhealthbarswhenautoclosinghealthbars"), Keywords: [TazLang.Get("mog_kw_close"), TazLang.Get("mog_kw_anchor")])
            )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_kw_healthbar"), [TazLang.Get("mog_kw_heal")]));
    }

    private static OptionFragment GetLastAttackSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        string openLabel = TazLang.Get("mog_tazuo_automaticallyopenhealthbarsforlastattack");
        string updateOneLabel = TazLang.Get("mog_tazuo_updateonebaraslastattack");
        string stackLabel = TazLang.Get("mog_tazuo_stackhealthbarsforlastattack");
        string anchorLabel = TazLang.Get("mog_tazuo_anchorhealthbarsforlastattack");

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_tazuo_lastattackhealthbars") },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.OpenHealthBarForLastAttack), openLabel),
                Option.Custom(
                    () => BuildLastAttackToggles(profile, updateOneLabel, stackLabel, anchorLabel),
                    new SearchMetadata(
                        $"{updateOneLabel} {stackLabel} {anchorLabel}",
                        Keywords: [TazLang.Get("mog_kw_last"), TazLang.Get("mog_kw_attack"), TazLang.Get("mog_kw_stack"), TazLang.Get("mog_kw_anchor")]
                    )
                )
            ).WithSearch(new SearchMetadata(openLabel, Keywords: [TazLang.Get("mog_kw_last"), TazLang.Get("mog_kw_attack")]))
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_tazuo_lastattackhealthbars"), Keywords: [TazLang.Get("mog_kw_last"), TazLang.Get("mog_kw_attack"), TazLang.Get("mog_kw_healthbar")]));
    }

    /// <summary>
    /// Builds the mutually-exclusive "one bar" and "stack bars" toggles, with the anchor toggle
    /// nested under stacking. The two toggles intercept their own clicks so only one can be on.
    /// </summary>
    private static Widget BuildLastAttackToggles(Profile profile, string updateOneLabel, string stackLabel, string anchorLabel)
    {
        GatedCheckBox updateOne = null!;
        GatedCheckBox stack = null!;

        updateOne = new GatedCheckBox(updateOneLabel, profile.UseOneHPBarForLastAttack, (newValue, commit) =>
        {
            profile.UseOneHPBarForLastAttack = newValue;

            if (newValue)
            {
                profile.StackHealthBarsForLastAttack = false;
                stack.IsChecked = false;
            }

            commit(newValue);
        });

        stack = new GatedCheckBox(stackLabel, profile.StackHealthBarsForLastAttack, (newValue, commit) =>
        {
            profile.StackHealthBarsForLastAttack = newValue;

            if (newValue)
            {
                profile.UseOneHPBarForLastAttack = false;
                updateOne.IsChecked = false;
            }

            commit(newValue);
        });

        MyraCheckButton anchor = MyraCheckButton.CreatePropBoundCheckButton(
            new Accessor<bool>(() => profile.AnchorHealthBarsForLastAttack), anchorLabel);
        anchor.Margin = new Thickness(20, 0, 0, 0);
        anchor.Enabled = profile.StackHealthBarsForLastAttack;

        stack.IsCheckedChanged += (_, _) =>
        {
            anchor.Enabled = stack.IsChecked;

            if (!stack.IsChecked)
            {
                profile.AnchorHealthBarsForLastAttack = false;
                anchor.IsChecked = false;
            }
        };

        var panel = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        panel.Widgets.Add(updateOne);
        panel.Widgets.Add(stack);
        panel.Widgets.Add(anchor);
        return panel;
    }

    private static OptionFragment GetDragSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_general_draggingsectionlabel") },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableDragSelect), TazLang.Get("mog_general_dragselecthp")),
                Option.Checkbox(
                    TazLang.Get("mog_general_draganchored"),
                    new Accessor<bool>(() => profile.DragSelectAsAnchor),
                    search: new SearchMetadata(TazLang.Get("mog_general_draganchored"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_select")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_general_dragkeymod"),
                    profile.DragSelectModifierKey,
                    [
                        TazLang.Get("mog_general_sharednone"),
                        TazLang.Get("mog_general_sharedctrl"),
                        TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    ],
                    i => profile.DragSelectModifierKey = i,
                    search: new SearchMetadata(TazLang.Get("mog_general_dragkeymod"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_modifier")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_general_dragplayersonly"),
                    profile.DragSelect_PlayersModifier,
                    [
                        TazLang.Get("mog_general_sharednone"),
                        TazLang.Get("mog_general_sharedctrl"),
                        TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    ],
                    i => profile.DragSelect_PlayersModifier = i,
                    search: new SearchMetadata(TazLang.Get("mog_general_dragplayersonly"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_player")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_general_dragmobsonly"),
                    profile.DragSelect_MonstersModifier,
                    [
                        TazLang.Get("mog_general_sharednone"),
                        TazLang.Get("mog_general_sharedctrl"),
                        TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    ],
                    i => profile.DragSelect_MonstersModifier = i,
                    search: new SearchMetadata(TazLang.Get("mog_general_dragmobsonly"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_monster")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_general_dragnameplatesonly"),
                    profile.DragSelect_NameplateModifier,
                    [
                        TazLang.Get("mog_general_sharednone"),
                        TazLang.Get("mog_general_sharedctrl"),
                        TazLang.Get("mog_general_sharedshift"),
                        TazLang.Get("mog_general_sharedalt")
                    ],
                    i => profile.DragSelect_NameplateModifier = i,
                    search: new SearchMetadata(TazLang.Get("mog_general_dragnameplatesonly"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_nameplate")])
                ),
                Option.InputField(
                    TazLang.Get("mog_general_dragx"),
                    new Accessor<string>(() => profile.DragSelectStartX.ToString(), s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartX = result;
                    }),
                    search: new SearchMetadata(TazLang.Get("mog_general_dragx"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_x")])
                ),
                Option.InputField(
                    TazLang.Get("mog_general_dragy"),
                    new Accessor<string>(() => profile.DragSelectStartY.ToString(), s =>
                    {
                        if (int.TryParse(s, out int result))
                            profile.DragSelectStartY = result;
                    }),
                    search: new SearchMetadata(TazLang.Get("mog_general_dragy"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_y")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_general_dragselecthp"), Keywords: [TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_select")]))
        );
    }
}
