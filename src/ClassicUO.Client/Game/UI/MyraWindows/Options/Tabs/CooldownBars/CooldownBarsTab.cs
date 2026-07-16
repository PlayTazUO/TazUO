using System.Collections.Generic;
using System.ComponentModel;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Collections;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;

/// <summary>Options tab source for the cooldown-bar rulebase editor (create and manage timed bars triggered by messages)</summary>
internal static partial class CooldownBarsTab
{
    /// <summary>Returns the option fragment containing the cooldown-bars editor and enable toggle</summary>
    internal static IOptionSource GetContent()
    {
        return OptionsUi.Vertical(
            GetSection()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_cooldownstab_cooldownbarslabel"), Tags: [TazLang.Get("mog_kw_cooldown"), TazLang.Get("mog_kw_timer")]));
    }

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_cooldownstab_customcooldownbars") },
            Option.IntegerInput(
                TazLang.Get("mog_cooldownstab_positionx"),
                new Accessor<int>(() => profile.CoolDownX),
                0,
                8192,
                search: new SearchMetadata(TazLang.Get("mog_cooldownstab_positionx"), Keywords: [TazLang.Get("mog_kw_position"), TazLang.Get("mog_kw_x")])
            ),
            Option.IntegerInput(
                TazLang.Get("mog_cooldownstab_positiony"),
                new Accessor<int>(() => profile.CoolDownY),
                0,
                8192,
                search: new SearchMetadata(TazLang.Get("mog_cooldownstab_positiony"), Keywords: [TazLang.Get("mog_kw_position"), TazLang.Get("mog_kw_y")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_cooldownstab_uselastmovedbarposition"),
                new Accessor<bool>(() => profile.UseLastMovedCooldownPosition),
                search: new SearchMetadata(TazLang.Get("mog_cooldownstab_uselastmovedbarposition"), Keywords: [TazLang.Get("mog_kw_last"), TazLang.Get("mog_kw_moved"), TazLang.Get("mog_kw_position")])
            ),
            Option.Custom(GetRuleEditor, new SearchMetadata(TazLang.Get("mog_cooldownstab_conditions"), Keywords: [TazLang.Get("mog_kw_condition"), TazLang.Get("mog_kw_rule")]))
        );
    }

    private static Rulebase<CooldownBarRule> GetRuleEditor()
    {

        var rb = new Rulebase<CooldownBarRule>
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TitleLabel = { Text = TazLang.Get("mog_cooldownstab_conditions"), HorizontalAlignment = HorizontalAlignment.Center }
        };

        rb.Columns.AddRange(GetRulebaseColumns());

        List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;
        for (uint i = 0; i < bars.Count; i++)
        {
            var rule = CooldownBarRule.FromEntry(i, bars[(int)i]);
            rb.Rules.Add(rule);
            rule.PropertyChanged += OnCooldownRuleChanged;
        }

        rb.RuleCrud += OnCooldownRuleCrud;

        // Note - we don't need to explicitly attach a 'Reordered' handler - when the rulebase reorders, it updates the 'Order' property which triggers a PropertyChanged event
        // that saves the changes.
        // Do keep in mind, however, that due to the backing nature of the store (index-based), the store is effectively 'overwritten' with these changes as the order change
        // basically causes an UPDATE to the item in the new order's slot; I.e., when moving item 1 up, the save occurs on index 0 so item 0 is overwritten and is basically
        // 'recovered' by the subsequent Order PropertyChanged event for ex-rule 0 (now rule #1)
        return rb;
    }

    private static void OnCooldownRuleCrud(object sender, RuleCrudEventArgs<CooldownBarRule> ruleCrudEventArgs)
    {
        switch (ruleCrudEventArgs.Event)
        {
            case RuleCrudEventType.Create:
                UpsertCooldownRule(ruleCrudEventArgs.Rule, true);
                break;
            case RuleCrudEventType.Update:
                UpsertCooldownRule(ruleCrudEventArgs.Rule, false);
                break;
            case RuleCrudEventType.Delete:
                ruleCrudEventArgs.Rule.PropertyChanged -= OnCooldownRuleChanged;
                CooldownBarsConfig.Current.RemoveAt((int)ruleCrudEventArgs.Rule.Order);
                break;
        }
    }

    private static void OnCooldownRuleChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not CooldownBarRule rule)
            return;

        UpsertCooldownRule(rule, false);
    }

    private static void UpsertCooldownRule(CooldownBarRule rule, bool isNew)
    {
        if (isNew)
            rule.PropertyChanged += OnCooldownRuleChanged;

        CooldownBarsConfig.Current.Upsert((int)rule.Order, rule.ToEntry(), isNew);
    }
}
