using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for mobile-entity highlighting, hue, and health bar settings</summary>
public static class MobilesTab
{
    /// <summary>Returns the tab group containing highlighting, hue, and health bar sub-tabs</summary>
    internal static IOptionSource GetContent() => GetTabs();

    private static OptionTabGroup GetTabs()
    {

        return new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_mobilestab_highlighting_label"),
                GetHighlightingSection,
                new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_label"), Keywords: [TazLang.Get("mog_kw_highlight")])
            )
            .AddTab(
                TazLang.Get("mog_mobilestab_hues_label"),
                GetEntityHueSettingSection,
                new SearchMetadata(TazLang.Get("mog_mobilestab_hues_label"), Keywords: [TazLang.Get("mog_kw_hue"), TazLang.Get("mog_kw_color")])
            )
            .AddTab(
                TazLang.Get("mog_buttonhealthbars"),
                HealthBarsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_buttonhealthbars"), [TazLang.Get("mog_kw_healthbar"), TazLang.Get("mog_kw_hp")])
            )
            .AddTab(
                TazLang.Get("mog_mobilestab_misc_label"),
                GetMiscSection,
                new SearchMetadata(TazLang.Get("mog_mobilestab_misc_label"), Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous")])
            );
    }

    private static IOptionSource GetMiscSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_mobilestab_highlighting_incomingmobiles"),
                new Accessor<bool>(() => profile.ShowNewMobileNameIncoming),
                search: new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_incomingmobiles"), Keywords: [TazLang.Get("mog_kw_incoming"), TazLang.Get("mog_kw_mobile")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_mobilestab_highlighting_incomingcorpses"),
                new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming),
                search: new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_incomingcorpses"), Keywords: [TazLang.Get("mog_kw_incoming"), TazLang.Get("mog_kw_corpse")])
            ),
            GetCorpseOpeningSection(),
            GetPlayerVisibilitySection()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_mobilestab_misc_label"), Tags: [TazLang.Get("mog_kw_misc")]));
    }

    private static OptionFragment GetCorpseOpeningSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_mobilestab_misc_corpseopening") },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenCorpses), TazLang.Get("mog_general_autoopencorpse")),
                    Option.Slider(
                        TazLang.Get("mog_general_corpseopendistance"),
                        0,
                        5,
                        new Accessor<float>(() => profile.AutoOpenCorpseRange, f => profile.AutoOpenCorpseRange = (int)f),
                        search: new SearchMetadata(TazLang.Get("mog_general_corpseopendistance"),
                            Keywords: [TazLang.Get("mog_kw_corpse"), TazLang.Get("mog_kw_distance")])
                    ),
                    Option.ComboBox(
                        TazLang.Get("mog_general_corpseopenoptions"),
                        profile.CorpseOpenOptions,
                        [
                            TazLang.Get("mog_general_corpseoptnone"), TazLang.Get("mog_general_corpseoptnottarg"), TazLang.Get("mog_general_corpseoptnothiding"),
                            TazLang.Get("mog_general_corpseoptboth")
                        ],
                        i => profile.CorpseOpenOptions = i,
                        search: new SearchMetadata(TazLang.Get("mog_general_corpseopenoptions"), Keywords: [TazLang.Get("mog_kw_corpse"), TazLang.Get("mog_kw_type")])
                    )
                ).WithSearch(new SearchMetadata(TazLang.Get("mog_mobilestab_misc_corpseopening"), [TazLang.Get("mog_kw_misc")], [TazLang.Get("mog_kw_corpse")]))
            ).AsSearchGroup()
            .WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_corpse"), TazLang.Get("mog_kw_open")]));
    }

    private static IOptionSource GetHighlightingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByPoisoned), TazLang.Get("mog_mobilestab_highlighting_highlightpoisoned")),
                Option.HuePicker(
                    TazLang.Get("mog_general_poisonhighlightcolor"),
                    new Accessor<ushort>(() => profile.PoisonHue, h => profile.PoisonHue = h),
                    new SearchMetadata(TazLang.Get("mog_general_poisonhighlightcolor"), Keywords: [TazLang.Get("mog_kw_poison"), TazLang.Get("mog_kw_hue")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_poison")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByParalize), TazLang.Get("mog_mobilestab_highlighting_highlightpara")),
                Option.HuePicker(
                    TazLang.Get("mog_general_parahighlightcolor"),
                    new Accessor<ushort>(() => profile.ParalyzedHue, h => profile.ParalyzedHue = h),
                    new SearchMetadata(TazLang.Get("mog_general_parahighlightcolor"), Keywords: [TazLang.Get("mog_kw_paralyze"), TazLang.Get("mog_kw_hue")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_paralyze")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByInvul), TazLang.Get("mog_mobilestab_highlighting_highlightinvul")),
                Option.HuePicker(
                    TazLang.Get("mog_general_invulhighlightcolor"),
                    new Accessor<ushort>(() => profile.InvulnerableHue, h => profile.InvulnerableHue = h),
                    new SearchMetadata(TazLang.Get("mog_general_invulhighlightcolor"), Keywords: [TazLang.Get("mog_kw_invulnerable"), TazLang.Get("mog_kw_hue")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_invulnerable")])),
            Option.ComboBox(
                TazLang.Get("mog_mobilestab_highlighting_auraunderfeet"),
                profile.AuraUnderFeetType,
                [
                    TazLang.Get("mog_general_auraoptdisabled"),
                    TazLang.Get("mog_general_aurooptwarmode"),
                    TazLang.Get("mog_general_auraoptctrlshift"),
                    TazLang.Get("mog_general_auraoptalways")
                ],
                i => profile.AuraUnderFeetType = i,
                search: new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_auraunderfeet"), Keywords: [TazLang.Get("mog_kw_aura")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.PartyAura), TazLang.Get("mog_mobilestab_highlighting_auraforparty")),
                Option.HuePicker(
                    TazLang.Get("mog_general_aurapartycolor"),
                    new Accessor<ushort>(() => profile.PartyAuraHue, h => profile.PartyAuraHue = h),
                    new SearchMetadata(TazLang.Get("mog_general_aurapartycolor"), Keywords: [TazLang.Get("mog_kw_aura"), TazLang.Get("mog_kw_party"), TazLang.Get("mog_kw_hue")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_aura"), TazLang.Get("mog_kw_party")])),
            Option.Checkbox(
                TazLang.Get("mog_general_disablegrayenemies"),
                new Accessor<bool>(() => profile.DisableGrayEnemies),
                search: new SearchMetadata(TazLang.Get("mog_general_disablegrayenemies"), Keywords: [TazLang.Get("mog_kw_enemy"), TazLang.Get("mog_kw_disable")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_mobilestab_highlighting_label"), [TazLang.Get("mog_kw_mobile"), TazLang.Get("mog_kw_health")]));
    }

    private static IOptionSource GetEntityHueSettingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_mobilestab_hues_huemobilebynotoriety") },
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_innocentcolor"),
                    new Accessor<ushort>(() => profile.InnocentHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_innocentcolor"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_innocent")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_beneficialspell"),
                    new Accessor<ushort>(() => profile.BeneficHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_beneficialspell"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_beneficial")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_friendcolor"),
                    new Accessor<ushort>(() => profile.FriendHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_friendcolor"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_friend")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_harmfulspell"),
                    new Accessor<ushort>(() => profile.HarmfulHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_harmfulspell"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_harmful")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_criminal"),
                    new Accessor<ushort>(() => profile.CriminalHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_criminal"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_criminal")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_neutralspell"),
                    new Accessor<ushort>(() => profile.NeutralHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_neutralspell"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_neutral")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_canbeattackedhue"),
                    new Accessor<ushort>(() => profile.CanAttackHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_canbeattackedhue"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_attack")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_murderer"),
                    new Accessor<ushort>(() => profile.MurdererHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_murderer"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_murderer")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_enemy"),
                    new Accessor<ushort>(() => profile.EnemyHue),
                    new SearchMetadata(TazLang.Get("mog_combattab_spells_enemy"), Keywords: [TazLang.Get("mog_kw_notoriety"), TazLang.Get("mog_kw_enemy")])
                )
            ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_mobile"), TazLang.Get("mog_kw_notoriety")])),
            GetDamageHuesSection()
        );
    }

    private static OptionFragment GetPlayerVisibilitySection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_mobilestab_hues_playervisibility") },
                Option.Slider(
                    TazLang.Get("mog_tazuo_hiddenplayeropacity"),
                    0,
                    100,
                    new Accessor<byte>(() => profile.HiddenBodyAlpha),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_hiddenplayeropacity"), Keywords: [TazLang.Get("mog_kw_hidden")])
                ),
                Option.Slider(
                    TazLang.Get("mog_tazuo_regularplayeropacity"),
                    0,
                    100,
                    new Accessor<int>(() => profile.PlayerConstantAlpha)
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_hiddenplayerhue"),
                    new Accessor<ushort>(() => profile.HiddenBodyHue),
                    new SearchMetadata(TazLang.Get("mog_tazuo_hiddenplayerhue"), Keywords: [TazLang.Get("mog_kw_hue")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_tazuo_overridepartymemberhues"),
                    new Accessor<bool>(() => profile.OverridePartyAndGuildHue),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_overridepartymemberhues"), Keywords: [TazLang.Get("mog_kw_party")])
                )
            ).AsSearchGroup()
            .WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_player"), TazLang.Get("mog_kw_opacity"), TazLang.Get("mog_kw_hidden")]));
    }

    private static OptionFragment GetDamageHuesSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_kw_damage"), LabelTooltip = TazLang.Get("mog_mobilestab_hues_damagehuestooltip") },
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_damagetoself"),
                    new Accessor<ushort>(() => profile.DamageHueSelf),
                    new SearchMetadata(TazLang.Get("mog_tazuo_damagetoself"), Keywords: [TazLang.Get("mog_kw_self")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_damagetoothers"),
                    new Accessor<ushort>(() => profile.DamageHueOther),
                    new SearchMetadata(TazLang.Get("mog_tazuo_damagetoothers"), Keywords: [TazLang.Get("mog_kw_other")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_damagetopets"),
                    new Accessor<ushort>(() => profile.DamageHuePet),
                    new SearchMetadata(TazLang.Get("mog_tazuo_damagetopets"), Keywords: [TazLang.Get("mog_kw_pet")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_damagetoallies"),
                    new Accessor<ushort>(() => profile.DamageHueAlly),
                    new SearchMetadata(TazLang.Get("mog_tazuo_damagetoallies"), Keywords: [TazLang.Get("mog_kw_ally")])
                ),
                Option.HuePicker(
                    TazLang.Get("mog_tazuo_damagetolastattack"),
                    new Accessor<ushort>(() => profile.DamageHueLastAttck),
                    new SearchMetadata(TazLang.Get("mog_tazuo_damagetolastattack"), Keywords: [TazLang.Get("mog_kw_last"), TazLang.Get("mog_kw_attack")])
                )
            ).AsSearchGroup()
            .WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_damage")]));
    }
}
