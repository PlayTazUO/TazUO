using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.CooldownBars;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CooldownBarsTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;
        return new OptionItem(cdLang.CooldownBarsLabel, GetSection);
    }

    private static WrapPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new VisualContainer(
                new VisualContainerProps { LabelText = cdLang.CustomCooldownBars },
                OptionsFactory.PropBoundNumericInput(
                    cdLang.PositionX,
                    new Accessor<int>(() => profile.CoolDownX),
                    0,
                    8192
                ),
                OptionsFactory.PropBoundNumericInput(
                    cdLang.PositionY,
                    new Accessor<int>(() => profile.CoolDownY),
                    0,
                    8192
                ),
                OptionsFactory.CreateCheckboxOption(cdLang.UseLastMovedBarPosition, new Accessor<bool>(() => profile.UseLastMovedCooldownPosition)),
                GetRuleEditor()
            )
        );
    }

    private static Rulebase<CooldownBarRule> GetRuleEditor()
    {
        var rb = new Rulebase<CooldownBarRule>()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TitleLabel = { Text = "Rules", HorizontalAlignment = HorizontalAlignment.Center }
        };

        rb.Columns.AddRange(
            [
                new RulebaseColumn<CooldownBarRule>
                {
                    Header = "Name",
                    Proportion = new Proportion(ProportionType.Auto),
                    CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.Name))
                },
                new RulebaseColumn<CooldownBarRule>
                {
                    Header = "Hue",
                    Proportion = new Proportion(ProportionType.Auto),
                    CellFactory = rule =>
                    {
                        var label = new MyraLabel(rule.Hue.ToString(), MyraLabel.TextStyle.P);
                        return OptionTabCommons.StyledStackPanel(
                            Orientation.Horizontal,
                            OptionsFactory.CreateHuePicker(
                                null,
                                rule.Hue,
                                newHue =>
                                {
                                    rule.Hue = newHue;
                                    label.Text = newHue.ToString();
                                }
                            ),
                            label
                        );
                    }
                },
                new RulebaseColumn<CooldownBarRule>
                {
                    Header = "Cooldown",
                    Proportion = new Proportion(ProportionType.Auto),
                    CellFactory = rule => OptionsFactory.PropBoundUIntInput(null, new Accessor<uint>(() => rule.Cooldown))
                },
                new RulebaseColumn<CooldownBarRule>
                {
                    Header = "Trigger Message Type",
                    Proportion = new Proportion(ProportionType.Auto),
                    CellFactory = rule =>
                    {
                        OptionItem box = OptionsFactory.CreateComboBox(
                            null,
                            rule.TriggerMessageType.ToString(),
                            Enum.GetNames<CooldownTriggerMessageType>(),
                            newValue => rule.TriggerMessageType = Enum.Parse<CooldownTriggerMessageType>(newValue)
                        );
                        box.Width = null;
                        box.MinWidth = 40;
                        return box;
                    }
                },
                new RulebaseColumn<CooldownBarRule>
                {
                    Header = "Trigger Message",
                    Proportion = new Proportion(ProportionType.Fill),
                    CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.TriggerMessage))
                }
            ]
        );


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
            !isNew
        );
    }
}
