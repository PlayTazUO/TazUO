#nullable enable

using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>
/// The kind of CRUD operation that produced a <see cref="RuleCrudEventArgs{TRule}"/>.
/// </summary>
public enum RuleCrudEventType
{
    /// <summary>A new rule was created</summary>
    Create,
    /// <summary>An existing rule was edited</summary>
    Update,
    /// <summary>A rule was removed</summary>
    Delete
}

/// <summary>
/// Event data describing a create/update/delete operation performed on a rule via a configurator
/// </summary>
/// <typeparam name="TRule">The rule type the configurator operates on</typeparam>
public class RuleCrudEventArgs<TRule> : EventArgs where TRule : IRule
{
    /// <summary>The rule that was created, updated, or deleted</summary>
    public TRule Rule { get; }

    /// <summary>The operation that was performed</summary>
    public RuleCrudEventType Event { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleCrudEventArgs{TRule}"/> class
    /// </summary>
    /// <param name="rule">The rule affected by the operation.</param>
    /// <param name="eventType">The operation that was performed.</param>
    public RuleCrudEventArgs(TRule rule,  RuleCrudEventType eventType)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Rule = rule;
        Event = eventType;
    }
}

/// <summary>
/// Supplies the editing UI used by a <see cref="Rulebase{TRule}"/> to create or edit a single rule,
/// and reports back create/update/delete results plus when the editor UI is dismissed.
/// </summary>
/// <typeparam name="TRule">The rule type being configured</typeparam>
public interface IRuleConfigurator<TRule> where TRule : IRule
{
    /// <summary>Raised when a rule is created, updated, or deleted through the configurator</summary>
    event EventHandler<RuleCrudEventArgs<TRule>> Crud;

    /// <summary>Raised when the configurator's editing UI is closed without further action</summary>
    event EventHandler EditorClosed;

    /// <summary>
    /// Gets the widget used to edit (or create) the given rule.
    /// </summary>
    /// <param name="rule">The rule to edit, or a fresh instance when creating.</param>
    /// <param name="isEdit">True if editing an existing rule; false if creating a new one.</param>
    /// <returns>The configurator widget to display.</returns>
    Widget GetConfiguratorWidget(TRule rule, bool isEdit);
}
