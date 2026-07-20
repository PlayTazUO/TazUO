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
                CellFactory = rule => CreateExclusiveCheckbox(
                    rule,
                    r => r.ReplaceExisting,
                    (r, v) => r.ReplaceExisting = v,
                    r => r.SkipIfExists = false,
                    nameof(CooldownBarRule.ReplaceExisting)
                )
            },
            new RulebaseColumn<CooldownBarRule>
            {
                Header = TazLang.Get("mog_cooldownstab_skipifexists"),
                HeaderTooltip = TazLang.Get("mog_cooldownstab_skipifexiststooltip"),
                CellContentAlignment = HorizontalAlignment.Center,
                Proportion = new Proportion(ProportionType.Auto),
                CellFactory = rule => CreateExclusiveCheckbox(
                    rule,
                    r => r.SkipIfExists,
                    (r, v) => r.SkipIfExists = v,
                    r => r.ReplaceExisting = false,
                    nameof(CooldownBarRule.SkipIfExists)
                )
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

    /// <summary>
    /// Creates a checkbox bound to <paramref name="get"/>/<paramref name="set"/> that, when checked, clears the
    /// mutually-exclusive sibling option via <paramref name="clearOther"/>. The checkbox's visual state is kept in
    /// sync with the backing rule so that clearing one option from the other's handler updates both boxes.
    /// </summary>
    private static MyraCheckButton CreateExclusiveCheckbox(
        CooldownBarRule rule,
        Func<CooldownBarRule, bool> get,
        Action<CooldownBarRule, bool> set,
        Action<CooldownBarRule> clearOther,
        string propertyName)
    {
        MyraCheckButton cb = MyraCheckButton.CreateWithCallback(get(rule), isChecked =>
        {
            set(rule, isChecked);

            if (isChecked)
                clearOther(rule);
        });

        // The checkbox binding is one-way (it only writes on user input), so mirror programmatic changes to the
        // backing property - e.g. when the sibling option clears this one - back onto the checkbox's visual state.
        rule.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == propertyName && cb.IsChecked != get(rule))
                cb.IsChecked = get(rule);
        };

        return cb;
    }
}
