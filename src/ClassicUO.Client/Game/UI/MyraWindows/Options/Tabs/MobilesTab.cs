using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MobilesTab
{
    internal static IOptionSource GetContent() => GetTabs();

    private static OptionTabGroup GetTabs()
    {
        ModernOptionsGumpLanguage.MobilesTabLang mobilesLang = Language.Instance.GetModernOptionsGumpLanguage.MobilesTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return new OptionTabGroup()
            .AddTab(
                mobilesLang.Highlighting.Label,
                GetHighlightingSection,
                new SearchMetadata(mobilesLang.Highlighting.Label, Keywords: [kw.Highlight])
            )
            .AddTab(
                mobilesLang.Hues.Label,
                GetEntityHueSettingSection,
                new SearchMetadata(mobilesLang.Hues.Label, Keywords: [kw.Hue, kw.Color])
            );
    }

    private static IOptionSource GetHighlightingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.MobilesTabLang mobLang = Language.Instance.GetModernOptionsGumpLanguage.MobilesTab;
        ModernOptionsGumpLanguage.MobilesTabLang.HighlightingSection lang = mobLang.Highlighting;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            OptionsUi.Vertical(
                Option.Checkbox(
                    lang.ShowMobileHP,
                    new Accessor<bool>(() => profile.ShowMobilesHP),
                    search: new SearchMetadata(lang.ShowMobileHP, Keywords: [kw.HP, kw.Health])
                ),
                Option.ComboBox(
                    lang.MobileHPType,
                    profile.MobileHPType,
                    [genLang.HPTypePerc, genLang.HPTypeBar, genLang.HPTypeNBoth],
                    i => profile.MobileHPType = i,
                    search: new SearchMetadata(lang.MobileHPType, Keywords: [kw.HP, kw.Health, kw.Type])
                ),
                Option.ComboBox(
                    lang.HPShowWhen,
                    profile.MobileHPShowWhen,
                    [genLang.HPShowWhen_Always, genLang.HPShowWhen_Less100, genLang.HPShowWhen_Smart],
                    i => profile.MobileHPShowWhen = i,
                    search: new SearchMetadata(lang.HPShowWhen, Keywords: [kw.HP, kw.Health, kw.Show])
                )
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    lang.HighlightPoisoned,
                    new Accessor<bool>(() => profile.HighlightMobilesByPoisoned),
                    search: new SearchMetadata(lang.HighlightPoisoned, Keywords: [kw.Poison])
                ),
                Option.HuePicker(
                    genLang.PoisonHighlightColor,
                    new Accessor<ushort>(() => profile.PoisonHue, h => profile.PoisonHue = h),
                    search: new SearchMetadata(genLang.PoisonHighlightColor, Keywords: [kw.Poison, kw.Hue])
                )
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    lang.HighlightPara,
                    new Accessor<bool>(() => profile.HighlightMobilesByParalize),
                    search: new SearchMetadata(lang.HighlightPara, Keywords: [kw.Paralyze])
                ),
                Option.HuePicker(
                    genLang.ParaHighlightColor,
                    new Accessor<ushort>(() => profile.ParalyzedHue, h => profile.ParalyzedHue = h),
                    search: new SearchMetadata(genLang.ParaHighlightColor, Keywords: [kw.Paralyze, kw.Hue])
                )
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    lang.HighlightInvul,
                    new Accessor<bool>(() => profile.HighlightMobilesByInvul),
                    search: new SearchMetadata(lang.HighlightInvul, Keywords: [kw.Invulnerable])
                ),
                Option.HuePicker(
                    genLang.InvulHighlightColor,
                    new Accessor<ushort>(() => profile.InvulnerableHue, h => profile.InvulnerableHue = h),
                    search: new SearchMetadata(genLang.InvulHighlightColor, Keywords: [kw.Invulnerable, kw.Hue])
                )
            ),
            Option.Checkbox(
                lang.IncomingMobiles,
                new Accessor<bool>(() => profile.ShowNewMobileNameIncoming),
                search: new SearchMetadata(lang.IncomingMobiles, Keywords: [kw.Incoming, kw.Mobile])
            ),
            Option.Checkbox(
                lang.IncomingCorpses,
                new Accessor<bool>(() => profile.ShowNewCorpseNameIncoming),
                search: new SearchMetadata(lang.IncomingCorpses, Keywords: [kw.Incoming, kw.Corpse])
            ),
            Option.ComboBox(
                lang.AuraUnderFeet,
                profile.AuraUnderFeetType,
                [
                    genLang.AuraOptDisabled,
                    genLang.AuroOptWarmode,
                    genLang.AuraOptCtrlShift,
                    genLang.AuraOptAlways
                ],
                i => profile.AuraUnderFeetType = i,
                search: new SearchMetadata(lang.AuraUnderFeet, Keywords: [kw.Aura])
            ),
            OptionsUi.Vertical(
                Option.Checkbox(
                    lang.AuraForParty,
                    new Accessor<bool>(() => profile.PartyAura),
                    search: new SearchMetadata(lang.AuraForParty, Keywords: [kw.Aura, kw.Party])
                ),
                Option.HuePicker(
                    genLang.AuraPartyColor,
                    new Accessor<ushort>(() => profile.PartyAuraHue, h => profile.PartyAuraHue = h),
                    search: new SearchMetadata(genLang.AuraPartyColor, Keywords: [kw.Aura, kw.Party, kw.Hue])
                )
            )
        ).WithSearch(new SearchMetadata(lang.Label, Tags: [kw.Mobile, kw.Health]));
    }

    private static IOptionSource GetEntityHueSettingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.CombatTabLang combatLang = lang.CombatTab;
        ModernOptionsGumpLanguage.MobilesTabLang mobLang = lang.MobilesTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = mobLang.Hues.HueMobileByNotoriety },
            Option.HuePicker(
                combatLang.Spells.InnocentColor,
                new Accessor<ushort>(() => profile.InnocentHue, b => profile.InnocentHue = b),
                search: new SearchMetadata(combatLang.Spells.InnocentColor, Keywords: [kw.Notoriety, kw.Innocent])
            ),
            Option.HuePicker(
                combatLang.Spells.BeneficialSpell,
                new Accessor<ushort>(() => profile.BeneficHue, b => profile.BeneficHue = b),
                search: new SearchMetadata(combatLang.Spells.BeneficialSpell, Keywords: [kw.Notoriety, kw.Beneficial])
            ),
            Option.HuePicker(
                combatLang.Spells.FriendColor,
                new Accessor<ushort>(() => profile.FriendHue, b => profile.FriendHue = b),
                search: new SearchMetadata(combatLang.Spells.FriendColor, Keywords: [kw.Notoriety, kw.Friend])
            ),
            Option.HuePicker(
                combatLang.Spells.HarmfulSpell,
                new Accessor<ushort>(() => profile.HarmfulHue, b => profile.HarmfulHue = b),
                search: new SearchMetadata(combatLang.Spells.HarmfulSpell, Keywords: [kw.Notoriety, kw.Harmful])
            ),
            Option.HuePicker(
                combatLang.Spells.Criminal,
                new Accessor<ushort>(() => profile.CriminalHue, b => profile.CriminalHue = b),
                search: new SearchMetadata(combatLang.Spells.Criminal, Keywords: [kw.Notoriety, kw.Criminal])
            ),
            Option.HuePicker(
                combatLang.Spells.NeutralSpell,
                new Accessor<ushort>(() => profile.NeutralHue, b => profile.NeutralHue = b),
                search: new SearchMetadata(combatLang.Spells.NeutralSpell, Keywords: [kw.Notoriety, kw.Neutral])
            ),
            Option.HuePicker(
                combatLang.Spells.CanBeAttackedHue,
                new Accessor<ushort>(() => profile.CanAttackHue, b => profile.CanAttackHue = b),
                search: new SearchMetadata(combatLang.Spells.CanBeAttackedHue, Keywords: [kw.Notoriety, kw.Attack])
            ),
            Option.HuePicker(
                combatLang.Spells.Murderer,
                new Accessor<ushort>(() => profile.MurdererHue, b => profile.MurdererHue = b),
                search: new SearchMetadata(combatLang.Spells.Murderer, Keywords: [kw.Notoriety, kw.Murderer])
            ),
            Option.HuePicker(
                combatLang.Spells.Enemy,
                new Accessor<ushort>(() => profile.EnemyHue, b => profile.EnemyHue = b),
                search: new SearchMetadata(combatLang.Spells.Enemy, Keywords: [kw.Notoriety, kw.Enemy])
            )
        ).WithSearch(new SearchMetadata(mobLang.Hues.Label, Tags: [kw.Mobile, kw.Health]));
    }
}
