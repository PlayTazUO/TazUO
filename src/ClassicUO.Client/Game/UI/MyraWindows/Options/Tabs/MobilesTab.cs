using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for mobile-entity highlighting and hue settings</summary>
public static class MobilesTab
{
    /// <summary>Returns the tab group containing highlighting and hue sub-tabs</summary>
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
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.ShowMobilesHP), lang.ShowMobileHP),
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
            ).WithSearch(new SearchMetadata(Keywords: [kw.HP, kw.Health])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByPoisoned), lang.HighlightPoisoned),
                Option.HuePicker(
                    genLang.PoisonHighlightColor,
                    new Accessor<ushort>(() => profile.PoisonHue, h => profile.PoisonHue = h),
                    new SearchMetadata(genLang.PoisonHighlightColor, Keywords: [kw.Poison, kw.Hue])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Highlight, kw.Poison])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByParalize), lang.HighlightPara),
                Option.HuePicker(
                    genLang.ParaHighlightColor,
                    new Accessor<ushort>(() => profile.ParalyzedHue, h => profile.ParalyzedHue = h),
                    new SearchMetadata(genLang.ParaHighlightColor, Keywords: [kw.Paralyze, kw.Hue])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Highlight, kw.Paralyze])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HighlightMobilesByInvul), lang.HighlightInvul),
                Option.HuePicker(
                    genLang.InvulHighlightColor,
                    new Accessor<ushort>(() => profile.InvulnerableHue, h => profile.InvulnerableHue = h),
                    new SearchMetadata(genLang.InvulHighlightColor, Keywords: [kw.Invulnerable, kw.Hue])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Highlight, kw.Invulnerable])),
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
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.PartyAura), lang.AuraForParty),
                Option.HuePicker(
                    genLang.AuraPartyColor,
                    new Accessor<ushort>(() => profile.PartyAuraHue, h => profile.PartyAuraHue = h),
                    new SearchMetadata(genLang.AuraPartyColor, Keywords: [kw.Aura, kw.Party, kw.Hue])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Aura, kw.Party])),
            Option.Checkbox(
                genLang.DisableGrayEnemies,
                new Accessor<bool>(() => profile.DisableGrayEnemies),
                search: new SearchMetadata(genLang.DisableGrayEnemies, Keywords: [kw.Enemy, kw.Disable])
            )
        ).WithSearch(new SearchMetadata(lang.Label, [kw.Mobile, kw.Health]));
    }

    private static IOptionSource GetEntityHueSettingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.CombatTabLang combatLang = lang.CombatTab;
        ModernOptionsGumpLanguage.MobilesTabLang mobLang = lang.MobilesTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = mobLang.Hues.HueMobileByNotoriety },
                Option.HuePicker(
                    combatLang.Spells.InnocentColor,
                    new Accessor<ushort>(() => profile.InnocentHue),
                    new SearchMetadata(combatLang.Spells.InnocentColor, Keywords: [kw.Notoriety, kw.Innocent])
                ),
                Option.HuePicker(
                    combatLang.Spells.BeneficialSpell,
                    new Accessor<ushort>(() => profile.BeneficHue),
                    new SearchMetadata(combatLang.Spells.BeneficialSpell, Keywords: [kw.Notoriety, kw.Beneficial])
                ),
                Option.HuePicker(
                    combatLang.Spells.FriendColor,
                    new Accessor<ushort>(() => profile.FriendHue),
                    new SearchMetadata(combatLang.Spells.FriendColor, Keywords: [kw.Notoriety, kw.Friend])
                ),
                Option.HuePicker(
                    combatLang.Spells.HarmfulSpell,
                    new Accessor<ushort>(() => profile.HarmfulHue),
                    new SearchMetadata(combatLang.Spells.HarmfulSpell, Keywords: [kw.Notoriety, kw.Harmful])
                ),
                Option.HuePicker(
                    combatLang.Spells.Criminal,
                    new Accessor<ushort>(() => profile.CriminalHue),
                    new SearchMetadata(combatLang.Spells.Criminal, Keywords: [kw.Notoriety, kw.Criminal])
                ),
                Option.HuePicker(
                    combatLang.Spells.NeutralSpell,
                    new Accessor<ushort>(() => profile.NeutralHue),
                    new SearchMetadata(combatLang.Spells.NeutralSpell, Keywords: [kw.Notoriety, kw.Neutral])
                ),
                Option.HuePicker(
                    combatLang.Spells.CanBeAttackedHue,
                    new Accessor<ushort>(() => profile.CanAttackHue),
                    new SearchMetadata(combatLang.Spells.CanBeAttackedHue, Keywords: [kw.Notoriety, kw.Attack])
                ),
                Option.HuePicker(
                    combatLang.Spells.Murderer,
                    new Accessor<ushort>(() => profile.MurdererHue),
                    new SearchMetadata(combatLang.Spells.Murderer, Keywords: [kw.Notoriety, kw.Murderer])
                ),
                Option.HuePicker(
                    combatLang.Spells.Enemy,
                    new Accessor<ushort>(() => profile.EnemyHue),
                    new SearchMetadata(combatLang.Spells.Enemy, Keywords: [kw.Notoriety, kw.Enemy])
                )
            ).WithSearch(new SearchMetadata(Tags: [kw.Mobile, kw.Notoriety])),
            GetDamageHuesSection(),
            GetPlayerVisibilitySection()
        );
    }

    private static OptionFragment GetPlayerVisibilitySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.MobilesTabLang.HuesSection hueLang = lang.MobilesTab.Hues;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = hueLang.PlayerVisibility },
                Option.Slider(
                    tuoLang.HiddenPlayerOpacity,
                    0,
                    100,
                    new Accessor<byte>(() => profile.HiddenBodyAlpha),
                    search: new SearchMetadata(tuoLang.HiddenPlayerOpacity, Keywords: [kw.Hidden])
                ),
                Option.Slider(
                    tuoLang.RegularPlayerOpacity,
                    0,
                    100,
                    new Accessor<int>(() => profile.PlayerConstantAlpha)
                ),
                Option.HuePicker(
                    tuoLang.HiddenPlayerHue,
                    new Accessor<ushort>(() => profile.HiddenBodyHue),
                    new SearchMetadata(tuoLang.HiddenPlayerHue, Keywords: [kw.Hue])
                ),
                Option.Checkbox(
                    tuoLang.OverridePartyMemberHues,
                    new Accessor<bool>(() => profile.OverridePartyAndGuildHue),
                    search: new SearchMetadata(tuoLang.OverridePartyMemberHues, Keywords: [kw.Party])
                )
            ).AsSearchGroup()
            .WithSearch(new SearchMetadata(Keywords: [kw.Player, kw.Opacity, kw.Hidden]));
    }

    private static OptionFragment GetDamageHuesSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;
        ModernOptionsGumpLanguage.MobilesTabLang.HuesSection hueLang = lang.MobilesTab.Hues;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = kw.Damage, LabelTooltip = hueLang.DamageHuesTooltip },
                Option.HuePicker(
                    tuoLang.DamageToSelf,
                    new Accessor<ushort>(() => profile.DamageHueSelf),
                    new SearchMetadata(tuoLang.DamageToSelf, Keywords: [kw.Self])
                ),
                Option.HuePicker(
                    tuoLang.DamageToOthers,
                    new Accessor<ushort>(() => profile.DamageHueOther),
                    new SearchMetadata(tuoLang.DamageToOthers, Keywords: [kw.Other])
                ),
                Option.HuePicker(
                    tuoLang.DamageToPets,
                    new Accessor<ushort>(() => profile.DamageHuePet),
                    new SearchMetadata(tuoLang.DamageToPets, Keywords: [kw.Pet])
                ),
                Option.HuePicker(
                    tuoLang.DamageToAllies,
                    new Accessor<ushort>(() => profile.DamageHueAlly),
                    new SearchMetadata(tuoLang.DamageToAllies, Keywords: [kw.Ally])
                ),
                Option.HuePicker(
                    tuoLang.DamageToLastAttack,
                    new Accessor<ushort>(() => profile.DamageHueLastAttck),
                    new SearchMetadata(tuoLang.DamageToLastAttack, Keywords: [kw.Last, kw.Attack])
                )
            ).AsSearchGroup()
            .WithSearch(new SearchMetadata(Keywords: [kw.Damage]));
    }
}
