using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.CooldownBars;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class CooldownBarsTab
{
    internal static OptionItem GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
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
        var rb = new Rulebase<CooldownBarRule>(new CooldownBarRuleEditor())
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            TitleLabel = { Text = "Rules", HorizontalAlignment = HorizontalAlignment.Center }
        };

        rb.Columns.Add(new RulebaseColumn<CooldownBarRule>
        {
            Header = "Name",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => new MyraLabel(rule.Name, MyraLabel.TextStyle.P)
        });

        rb.Columns.Add(new RulebaseColumn<CooldownBarRule>
        {
            Header = "Hue",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => OptionsFactory.PropBoundHuePicker(rule.Hue.ToString(), new Accessor<ushort>(() => rule.Hue)),
        });

        rb.Columns.Add(new RulebaseColumn<CooldownBarRule>
        {
            Header = "Cooldown",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = rule => new MyraLabel(rule.Cooldown.ToString(), MyraLabel.TextStyle.P)
        });

        rb.Columns.Add(new RulebaseColumn<CooldownBarRule>
        {
            Header = "Trigger",
            Proportion = new Proportion(ProportionType.Fill),
            CellFactory = rule => new MyraLabel(rule.TriggerMessageType.ToString(), MyraLabel.TextStyle.P)
        });

        CoolDownBar.CoolDownConditionData.GetAllRules()
            .Select(CooldownBarRule.FromLegacyCondition)
            .ToArray()
            .ForEach(rb.Rules.Add);

        return rb;
    }
}
