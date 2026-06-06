using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CombatTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonCombatSpells, GetTabs);
    }

    private static MyraTabControl GetTabs()
    {
        ModernOptionsGumpLanguage.CombatSpells lang = Language.Instance.GetModernOptionsGumpLanguage.GetCombatSpells;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.Combat, GetCombatSection);
        tabs.AddTab(lang.Spells, SpellsTab.GetContent);
        return tabs;
    }

    private static WrapPanel GetCombatSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.HoldTabForCombat, new Accessor<bool>(() => profile.HoldDownKeyTab)),
            OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeAttack, new Accessor<bool>(() => profile.EnabledCriminalActionQuery)),
            OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.QueryBeforeBeneficial, new Accessor<bool>(() => profile.EnabledBeneficialCriminalActionQuery)),
            OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.ShowBuffDurationOnOldStyleBuffBar, new Accessor<bool>(() => profile.BuffBarTime)),
            OptionsFactory.CreateCheckboxOption(lang.GetCombatSpells.EnableDPSCounter, new Accessor<bool>(() => profile.ShowDPS))
        );
    }
}
