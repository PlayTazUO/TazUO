#nullable enable

using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public sealed class RulebaseColumn<TRule> where TRule : IRule
{
    public string Header { get; init; } = string.Empty;
    public Proportion Proportion { get; init; } = new(ProportionType.Fill, 1);
    public bool Visible { get; set; } = true;
    public Func<TRule, Widget> CellFactory { get; init; } = _ => new Label();
}
