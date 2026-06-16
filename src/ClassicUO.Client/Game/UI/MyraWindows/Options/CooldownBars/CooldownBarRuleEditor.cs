#nullable enable

using System;
using ClassicUO.Common;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.CooldownBars;

public class CooldownBarRuleEditor : IRuleConfigurator<CooldownBarRule>
{
    public event EventHandler<RuleCrudEventArgs<CooldownBarRule>>? Crud;
    public event EventHandler? EditorClosed;

    public Widget GetConfiguratorWidget(CooldownBarRule rule, bool isEdit) =>
        OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            OptionsFactory.PropBoundInputField("Name", new Accessor<string>(() => rule.Name)),
            OptionsFactory.PropBoundHuePicker("Hue", new Accessor<ushort>(() => rule.Hue)),
            OptionsFactory.PropBoundUIntInput("Cooldown", new Accessor<uint>(() => rule.Cooldown)),
            new MyraButton("Accept",
                () => Crud?.Invoke(
                    this,
                    new RuleCrudEventArgs<CooldownBarRule>(rule, isEdit ? RuleCrudEventType.Update : RuleCrudEventType.Create)
                )
            ),
            new MyraButton("Cancel", () => EditorClosed?.Invoke(this, EventArgs.Empty))
        );
}
