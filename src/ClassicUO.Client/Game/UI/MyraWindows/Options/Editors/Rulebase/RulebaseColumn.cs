#nullable enable

using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>
///     Describes a single column of a <see cref="RulebaseTableView{TRule}" />: its header, sizing,
///     visibility, and how to build the cell widget for a given rule.
/// </summary>
/// <typeparam name="TRule">The rule type the table displays.</typeparam>
public sealed class RulebaseColumn<TRule> where TRule : IRule
{
    /// <summary>The text shown in the column header</summary>
    public string Header { get; init; } = string.Empty;

    /// <summary>An optional tooltip to display when hovering over the header</summary>
    public string HeaderTooltip { get; init; } = string.Empty;

    /// <summary>The grid proportion used to size this column relative to others</summary>
    public Proportion Proportion { get; init; } = new(ProportionType.Fill, 1);

    /// <summary>Whether the column is currently shown in the table</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Builds the cell widget displayed for a given rule in this column</summary>
    public Func<TRule, Widget> CellFactory { get; init; } = _ => new Label();

    /// <summary>Horizontal alignment applied to each cell widget within this column</summary>
    public HorizontalAlignment CellContentAlignment { get; init; } = HorizontalAlignment.Stretch;
}
