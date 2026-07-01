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
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
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
                search: new SearchMetadata(lang.Combat.EnableDPSCounter, Keywords: [kw.Dps, kw.Damage])
            ),
            Option.Checkbox(
                genLang.ShowTargetIndicator,
                new Accessor<bool>(() => profile.ShowTargetIndicator),
                search: new SearchMetadata(genLang.ShowTargetIndicator, Keywords: [kw.Target, kw.Indicator])
            ),
            Option.Checkbox(
                genLang.IgnoreStaminaCheck,
                new Accessor<bool>(() => profile.IgnoreStaminaCheck),
                search: new SearchMetadata(genLang.IgnoreStaminaCheck, Keywords: [kw.Stamina, kw.Disable])
            ),
            Option.Checkbox(
                genLang.DisableDismountWarmode,
                new Accessor<bool>(() => profile.DisableDismountInWarMode),
                search: new SearchMetadata(genLang.DisableDismountWarmode, Keywords: [kw.Dismount, kw.Warmode])
            )
        ).WithSearch(new SearchMetadata(lang.Combat.Label, [kw.Combat, kw.Battle]));
    }
}
