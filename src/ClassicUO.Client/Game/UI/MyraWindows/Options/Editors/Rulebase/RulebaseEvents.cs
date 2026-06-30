namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>
/// Event data raised when a rule is moved within a <see cref="Rulebase{TRule}"/> (e.g. via the
/// move up/down/top/bottom toolbar buttons).
/// </summary>
/// <typeparam name="TRule">The rule type that was reordered.</typeparam>
/// <param name="rule">The rule that moved.</param>
/// <param name="oldOrder">The rule's index before the move.</param>
/// <param name="newOrder">The rule's index after the move.</param>
public sealed class RulebaseOrderChangedEventArgs<TRule>(TRule rule, int oldOrder, int newOrder) where TRule : IRule
{
    /// <summary>The rule that was reordered</summary>
    public TRule Rule { get; } = rule;

    /// <summary>The rule's index before the move</summary>
    public int OldOrder { get; } = oldOrder;

    /// <summary>The rule's index after the move</summary>
    public int NewOrder { get; } = newOrder;
}
