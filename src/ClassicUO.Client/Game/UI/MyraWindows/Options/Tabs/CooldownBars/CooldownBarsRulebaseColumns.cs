using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;

internal static partial class CooldownBarsTab
{
    private static RulebaseColumn<CooldownBarRule>[] GetRulebaseColumns()
    {
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;
        ModernOptionsGumpLanguage.KeywordsLang kwLang = Language.Instance.GetModernOptionsGumpLanguage.Kw;
        return
        [
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.Name,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.Name))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.Hue,
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
                Header = cdLang.Cooldown,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundUIntInput(null, new Accessor<uint>(() => rule.Cooldown))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.TriggerMessageType,
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
                Header = cdLang.TriggerMessage,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.TriggerMessage))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = kwLang.Preview,
                Proportion = new Proportion(ProportionType.Fill),
                CellFactory = rule => new BasicButton(() =>
                {
                    CoolDownBarManager.AddCoolDownBar(World.Instance, TimeSpan.FromSeconds(rule.Cooldown), rule.Name, rule.Hue, true);
                }) { Width = 45, Height = 18 }
            }
        ];
    }
}
