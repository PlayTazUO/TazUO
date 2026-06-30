#nullable enable

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>
/// Represents a single row/entry managed by a <see cref="Rulebase{TRule}"/> table.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Sort position of the rule within its rulebase. Lower values appear first.
    /// </summary>
    uint Order { get; set; }

    /// <summary>
    /// Whether the rule is currently active.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Whether the rule can be edited
    /// </summary>
    bool CanEdit { get; set; }

    /// <summary>
    /// Whether the rule can be removed
    /// </summary>
    bool CanDelete { get; set; }

    /// <summary>
    /// Serves as an 'OnRuleDeleted' hook for implementors to release any resources tied to the rule.
    /// </summary>
    /// <param name="rule">The rule being deleted</param>
    static virtual void DeleteRule(IRule rule)
    {
    }
}
