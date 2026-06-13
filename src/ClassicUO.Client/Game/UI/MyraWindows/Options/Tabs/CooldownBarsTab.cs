using System;
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
                    CellFactory = rule => OptionsFactory.PropBoundHuePicker(rule.Hue.ToString(), new Accessor<ushort>(() => rule.Hue))
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
        CoolDownBar.CoolDownConditionData.GetAllRules()
            .Select(CooldownBarRule.FromLegacyCondition)
            .ToArray()
            .ForEach(rb.Rules.Add);

        return rb;
    }
}
