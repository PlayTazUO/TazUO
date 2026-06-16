namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public sealed class RulebaseOrderChangedEventArgs<TRule>(TRule rule, int oldOrder, int newOrder) where TRule : IRule
{
    public TRule Rule { get; } = rule;
    public int OldOrder { get; } = oldOrder;
    public int NewOrder { get; } = newOrder;
}
