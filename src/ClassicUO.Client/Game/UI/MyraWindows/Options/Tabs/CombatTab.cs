using ClassicUO.Common;
using ClassicUO.Configuration;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CombatTab
{
    internal static IOptionSource GetContent() => GetTabs();

    private static OptionTabGroup GetTabs()
    {
        ModernOptionsGumpLanguage.CombatTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.CombatTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return new OptionTabGroup()
            .AddTab(
                lang.Combat.Label,
                GetCombatSection,
                new SearchMetadata(lang.Combat.Label, Keywords: [kw.Combat, kw.Attack, kw.Battle])
            )
            .AddTab(
                lang.Spells.SpellLabel,
                SpellsTab.GetContent,
                new SearchMetadata(lang.Spells.SpellLabel, Keywords: [kw.Spell, kw.Magic, kw.Cast])
            );
    }

    private static IOptionSource GetCombatSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.CombatTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.CombatTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                lang.Combat.HoldTabForCombat,
                new Accessor<bool>(() => profile.HoldDownKeyTab),
                search: new SearchMetadata(lang.Combat.HoldTabForCombat, Keywords: [kw.Tab])
            ),
            Option.Checkbox(
                lang.Combat.QueryBeforeAttack,
                new Accessor<bool>(() => profile.EnabledCriminalActionQuery),
                search: new SearchMetadata(lang.Combat.QueryBeforeAttack, Keywords: [kw.Criminal, kw.Query])
            ),
            Option.Checkbox(
                lang.Combat.QueryBeforeBeneficial,
                new Accessor<bool>(() => profile.EnabledBeneficialCriminalActionQuery),
                search: new SearchMetadata(lang.Combat.QueryBeforeBeneficial, Keywords: [kw.Beneficial, kw.Criminal, kw.Query])
            ),
            Option.Checkbox(
                lang.Combat.ShowBuffDurationOnOldStyleBuffBar,
                new Accessor<bool>(() => profile.BuffBarTime),
                search: new SearchMetadata(lang.Combat.ShowBuffDurationOnOldStyleBuffBar, Keywords: [kw.Buff, kw.Duration, kw.Time])
            ),
            Option.Checkbox(
                lang.Combat.EnableDPSCounter,
                new Accessor<bool>(() => profile.ShowDPS),
                search: new SearchMetadata(lang.Combat.EnableDPSCounter, Keywords: [kw.DPS, kw.Damage])
            )
        ).WithSearch(new SearchMetadata(lang.Combat.Label, [kw.Combat, kw.Battle]));
    }
}
