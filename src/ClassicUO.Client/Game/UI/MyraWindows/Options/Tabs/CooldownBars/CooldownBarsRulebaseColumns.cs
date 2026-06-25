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
                HeaderTooltip = cdLang.NameTooltip,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.Name))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.Hue,
                HeaderTooltip = cdLang.HueTooltip,
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
                HeaderTooltip = cdLang.CooldownTooltip,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundUIntInput(null, new Accessor<uint>(() => rule.Cooldown))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.TriggerMessageType,
                HeaderTooltip = cdLang.TriggerMessageTypeTooltip,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule =>
                {
                    Widget box = OptionTabCommons.CreateOptionsComboBox(
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
                HeaderTooltip = cdLang.TriggerMessageTooltip,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.TriggerMessage))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = cdLang.ReplaceExisting,
                HeaderTooltip = cdLang.ReplaceExistingTooltip,
                CellContentAlignment = HorizontalAlignment.Center,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => MyraCheckButton.CreatePropBoundCheckButton(new Accessor<bool>(() => rule.ReplaceExisting))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = kwLang.Preview,
                HeaderTooltip = cdLang.PreviewTooltip,
                Proportion = new Proportion(ProportionType.Fill),
                CellFactory = rule => new BasicButton(() =>
                {
                    CoolDownBarManager.AddCoolDownBar(World.Instance, TimeSpan.FromSeconds(rule.Cooldown), rule.Name, rule.Hue, true);
                }) { Width = 45, Height = 18 }
            }
        ];
    }
}
