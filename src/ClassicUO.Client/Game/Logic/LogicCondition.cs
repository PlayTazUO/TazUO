#nullable enable

using System.Collections.Generic;

namespace ClassicUO.Game.Logic;

/// <summary>
/// One comparison: a field, how to compare it, and what to compare it against.
/// <para>
/// The operand is always text, whatever the field's kind. Numbers and flags are parsed when the
/// condition runs rather than stored typed, which keeps the persisted form readable and
/// hand-editable, and lets the editor swap the input widget for the field's kind without the stored
/// shape changing under it.
/// </para>
/// </summary>
public sealed class LogicCondition : LogicNode
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "condition";

    /// <summary>
    /// The <see cref="LogicField.Key" /> being compared. A key the schema no longer knows makes the
    /// condition false rather than an error - a filter written against a newer build should narrow,
    /// not throw.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>How <see cref="Field" /> is compared against the operand.</summary>
    public LogicOperator Operator { get; set; } = LogicOperator.Contains;

    /// <summary>
    /// The operand, for the operators that take one value. Ignored by the list operators.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The operand, for <see cref="LogicOperator.IsAnyOf" /> and
    /// <see cref="LogicOperator.IsNoneOf" />. Held as a list rather than as one delimited string so
    /// that a value containing the delimiter is not a trap, and so the editor can offer a row per
    /// entry instead of asking the user to punctuate.
    /// </summary>
    public List<string> Values { get; set; } = [];

    /// <summary>Switches that change how the comparison is made. Ones the operator has no use for
    /// are ignored rather than cleared, so flipping between operators does not lose them.</summary>
    public LogicConditionFlags Flags { get; set; }

    /// <inheritdoc />
    public override LogicNode Clone() =>
        new LogicCondition
        {
            Join = Join,
            Field = Field,
            Operator = Operator,
            Value = Value,
            Values = [.. Values],
            Flags = Flags
        };
}
