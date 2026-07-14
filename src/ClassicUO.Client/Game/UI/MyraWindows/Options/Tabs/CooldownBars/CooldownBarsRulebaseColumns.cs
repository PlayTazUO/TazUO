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
        return
        [
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_cooldownstab_name"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_nametooltip"),
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.Name))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_cooldownstab_hue"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_huetooltip"),
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
                Header = TazLang.Get("mog_cooldownstab_cooldown"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_cooldowntooltip"),
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundUIntInput(null, new Accessor<uint>(() => rule.Cooldown))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_cooldownstab_triggermessagetype"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_triggermessagetypetooltip"),
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
                Header = TazLang.Get("mog_cooldownstab_triggermessage"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_triggermessagetooltip"),
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => OptionsFactory.PropBoundInputField(null, new Accessor<string>(() => rule.TriggerMessage))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_cooldownstab_replaceexisting"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_replaceexistingtooltip"),
                CellContentAlignment = HorizontalAlignment.Center,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => MyraCheckButton.CreatePropBoundCheckButton(new Accessor<bool>(() => rule.ReplaceExisting))
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_kw_preview"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_previewtooltip"),
                Proportion = new Proportion(ProportionType.Fill),
                CellFactory = rule => new BasicButton(() =>
                {
                    CoolDownBarManager.AddCoolDownBar(World.Instance, TimeSpan.FromSeconds(rule.Cooldown), rule.Name, rule.Hue, true);
                }) { Width = 45, Height = 18 }
            }
        ];
    }
}
