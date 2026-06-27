using System.ComponentModel;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
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
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;
        return OptionsUi.Vertical(
            GetSection()
        ).WithSearch(new SearchMetadata(cdLang.CooldownBarsLabel, Tags: [kw.Cooldown, kw.Timer]));
    }

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = cdLang.CustomCooldownBars },
            Option.IntegerInput(
                cdLang.PositionX,
                new Accessor<int>(() => profile.CoolDownX),
                0,
                8192,
                search: new SearchMetadata(cdLang.PositionX, Keywords: [kw.Position, kw.X])
            ),
            Option.IntegerInput(
                cdLang.PositionY,
                new Accessor<int>(() => profile.CoolDownY),
                0,
                8192,
                search: new SearchMetadata(cdLang.PositionY, Keywords: [kw.Position, kw.Y])
            ),
            Option.Checkbox(
                cdLang.UseLastMovedBarPosition,
                new Accessor<bool>(() => profile.UseLastMovedCooldownPosition),
                search: new SearchMetadata(cdLang.UseLastMovedBarPosition, Keywords: [kw.Last, kw.Moved, kw.Position])
            ),
            Option.Custom(GetRuleEditor, new SearchMetadata(cdLang.Conditions, Keywords: [kw.Condition, kw.Rule]))
        );
    }

    private static Rulebase<CooldownBarRule> GetRuleEditor()
    {
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;

        var rb = new Rulebase<CooldownBarRule>
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TitleLabel = { Text = cdLang.Conditions, HorizontalAlignment = HorizontalAlignment.Center }
        };

        rb.Columns.AddRange(GetRulebaseColumns());

        CoolDownBar.CoolDownConditionData[] cooldownRules = CoolDownBar.CoolDownConditionData.GetAllRules();
        for (uint i = 0; i < cooldownRules.Length; i++)
        {
            var rule = CooldownBarRule.FromLegacyCondition(i, cooldownRules[i]);
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
                CoolDownBar.CoolDownConditionData.RemoveCondition((int)ruleCrudEventArgs.Rule.Order);
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

        CoolDownBar.CoolDownConditionData.SaveCondition(
            (int)rule.Order,
            rule.Hue,
            rule.Name,
            rule.TriggerMessage,
            (int)rule.Cooldown,
            isNew,
            (int)rule.TriggerMessageType,
            rule.ReplaceExisting
        );
    }
}
