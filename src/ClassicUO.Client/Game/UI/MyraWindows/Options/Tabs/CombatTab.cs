using ClassicUO.Common;
using ClassicUO.Configuration;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for combat and spell settings</summary>
public static class CombatTab
{
    /// <summary>Returns the tab group containing combat and spells sub-tabs</summary>
    internal static IOptionSource GetContent() => GetTabs();

    private static OptionTabGroup GetTabs()
    {

        return new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_combattab_combat_label"),
                GetCombatSection,
                new SearchMetadata(TazLang.Get("mog_combattab_combat_label"), Keywords: [TazLang.Get("mog_kw_combat"), TazLang.Get("mog_kw_attack"), TazLang.Get("mog_kw_battle")])
            )
            .AddTab(
                TazLang.Get("mog_combattab_spells_spelllabel"),
                SpellsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_combattab_spells_spelllabel"), Keywords: [TazLang.Get("mog_kw_spell"), TazLang.Get("mog_kw_magic"), TazLang.Get("mog_kw_cast")])
            );
    }

    private static IOptionSource GetCombatSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_combattab_combat_holdtabforcombat"),
                new Accessor<bool>(() => profile.HoldDownKeyTab),
                search: new SearchMetadata(TazLang.Get("mog_combattab_combat_holdtabforcombat"), Keywords: [TazLang.Get("mog_kw_tab")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_combat_querybeforeattack"),
                new Accessor<bool>(() => profile.EnabledCriminalActionQuery),
                search: new SearchMetadata(TazLang.Get("mog_combattab_combat_querybeforeattack"), Keywords: [TazLang.Get("mog_kw_criminal"), TazLang.Get("mog_kw_query")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_combat_querybeforebeneficial"),
                new Accessor<bool>(() => profile.EnabledBeneficialCriminalActionQuery),
                search: new SearchMetadata(TazLang.Get("mog_combattab_combat_querybeforebeneficial"), Keywords: [TazLang.Get("mog_kw_beneficial"), TazLang.Get("mog_kw_criminal"), TazLang.Get("mog_kw_query")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_combat_showbuffdurationonoldstylebuffbar"),
                new Accessor<bool>(() => profile.BuffBarTime),
                search: new SearchMetadata(TazLang.Get("mog_combattab_combat_showbuffdurationonoldstylebuffbar"), Keywords: [TazLang.Get("mog_kw_buff"), TazLang.Get("mog_kw_duration"), TazLang.Get("mog_kw_time")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_combat_enabledpscounter"),
                new Accessor<bool>(() => profile.ShowDPS),
                search: new SearchMetadata(TazLang.Get("mog_combattab_combat_enabledpscounter"), Keywords: [TazLang.Get("mog_kw_dps"), TazLang.Get("mog_kw_damage")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_showtargetindicator"),
                new Accessor<bool>(() => profile.ShowTargetIndicator),
                search: new SearchMetadata(TazLang.Get("mog_general_showtargetindicator"), Keywords: [TazLang.Get("mog_kw_target"), TazLang.Get("mog_kw_indicator")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_ignorestaminacheck"),
                new Accessor<bool>(() => profile.IgnoreStaminaCheck),
                search: new SearchMetadata(TazLang.Get("mog_general_ignorestaminacheck"), Keywords: [TazLang.Get("mog_kw_stamina"), TazLang.Get("mog_kw_disable")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_general_disabledismountwarmode"),
                new Accessor<bool>(() => profile.DisableDismountInWarMode),
                search: new SearchMetadata(TazLang.Get("mog_general_disabledismountwarmode"), Keywords: [TazLang.Get("mog_kw_dismount"), TazLang.Get("mog_kw_warmode")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_combattab_combat_label"), [TazLang.Get("mog_kw_combat"), TazLang.Get("mog_kw_battle")]));
    }
}
