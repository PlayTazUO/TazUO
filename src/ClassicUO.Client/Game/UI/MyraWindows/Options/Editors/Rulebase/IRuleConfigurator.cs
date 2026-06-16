#nullable enable

using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public enum RuleCrudEventType
{
    Create,
    Update,
    Delete
}

public class RuleCrudEventArgs<TRule> : EventArgs where TRule : IRule
{
    public TRule Rule { get; }
    public RuleCrudEventType Event { get; }

    public RuleCrudEventArgs(TRule rule,  RuleCrudEventType eventType)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Rule = rule;
        Event = eventType;
    }
}

public interface IRuleConfigurator<TRule> where TRule : IRule
{
    event EventHandler<RuleCrudEventArgs<TRule>> Crud;
    event EventHandler EditorClosed;
    Widget GetConfiguratorWidget(TRule rule, bool isEdit);
}
