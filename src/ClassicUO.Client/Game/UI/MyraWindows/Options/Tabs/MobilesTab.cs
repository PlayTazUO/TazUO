using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MobilesTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonCombatSpells, GetTabs);
    }

    private static MyraTabControl GetTabs()
    {
        ModernOptionsGumpLanguage.MobilesLangTab mobilesLangTab = Language.Instance.GetModernOptionsGumpLanguage.MobilesTab;

        var tabs = new MyraTabControl();
        tabs.AddTab(mobilesLangTab.Highlighting, GetHighlightingSection);
        tabs.AddTab(mobilesLangTab.Hues, GetEntityHueSettingSection);
        return tabs;
    }

    private static WrapPanel GetHighlightingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.ShowMobilesHP), genLang.ShowMobileHP),
                OptionsFactory.CreateComboBox(
                    genLang.MobileHPType,
                    profile.MobileHPType,
                    [genLang.HPTypePerc, genLang.HPTypeBar, genLang.HPTypeNBoth],
                    i => profile.MobileHPType = i
                ),
                OptionsFactory.CreateComboBox(
                    genLang.HPShowWhen,
                    profile.MobileHPShowWhen,
                    [genLang.HPShowWhen_Always, genLang.HPShowWhen_Less100, genLang.HPShowWhen_Smart],
                    i => profile.MobileHPShowWhen = i
                )
            ),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.HighlightMobilesByPoisoned),
                    genLang.HighlightPoisoned
                ),
                OptionsFactory.CreateHuePicker(
                    genLang.PoisonHighlightColor,
                    profile.PoisonHue,
                    h => profile.PoisonHue = h
                )
            ),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.HighlightMobilesByParalize),
                    genLang.HighlightPara
                ),
                OptionsFactory.CreateHuePicker(
                    genLang.ParaHighlightColor,
                    profile.ParalyzedHue,
                    h => profile.ParalyzedHue = h
                )
            ),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.HighlightMobilesByInvul),
                    genLang.HighlightInvul
                ),
                OptionsFactory.CreateHuePicker(
                    genLang.InvulHighlightColor,
                    profile.InvulnerableHue,
                    h => profile.InvulnerableHue = h
                )
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(
                genLang.IncomingMobiles,
                new Accessor<bool>(() => profile.ShowNewMobileNameIncoming)
            ),
            OptionsFactory.CreateCheckboxOption(
                genLang.IncomingCorpses,
                new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming)
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateComboBox(
                genLang.AuraUnderFeet,
                profile.AuraUnderFeetType,
                [
                    genLang.AuraOptDisabled,
                    genLang.AuroOptWarmode,
                    genLang.AuraOptCtrlShift,
                    genLang.AuraOptAlways
                ],
                i => profile.AuraUnderFeetType = i
            ),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.PartyAura), genLang.AuraForParty),
                OptionsFactory.CreateHuePicker(
                    genLang.AuraPartyColor,
                    profile.PartyAuraHue,
                    h => profile.PartyAuraHue = h
                )
            )
        );
    }

    private static VisualContainer GetEntityHueSettingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.CombatSpells spellLang = lang.GetCombatSpells;
        ModernOptionsGumpLanguage.MobilesLangTab mobLangTab = lang.MobilesTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = mobLangTab.HueMobileByNotoriety },
            OptionsFactory.CreateHuePicker(spellLang.InnocentColor, profile.InnocentHue, b => profile.InnocentHue = b),
            OptionsFactory.CreateHuePicker(spellLang.BeneficialSpell, profile.BeneficHue, b => profile.BeneficHue = b),
            OptionsFactory.CreateHuePicker(spellLang.FriendColor, profile.FriendHue, b => profile.FriendHue = b),
            OptionsFactory.CreateHuePicker(spellLang.HarmfulSpell, profile.HarmfulHue, b => profile.HarmfulHue = b),
            OptionsFactory.CreateHuePicker(spellLang.Criminal, profile.CriminalHue, b => profile.CriminalHue = b),
            OptionsFactory.CreateHuePicker(spellLang.NeutralSpell, profile.NeutralHue, b => profile.NeutralHue = b),
            OptionsFactory.CreateHuePicker(spellLang.CanBeAttackedHue, profile.CanAttackHue, b => profile.CanAttackHue = b),
            OptionsFactory.CreateHuePicker(spellLang.Murderer, profile.MurdererHue, b => profile.MurdererHue = b),
            OptionsFactory.CreateHuePicker(spellLang.Enemy, profile.EnemyHue, b => profile.EnemyHue = b)
        );
    }
}
