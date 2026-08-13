#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Logic;

/// <summary>
/// What each operator can be used on, and which switches mean anything for it. The editor asks this
/// rather than listing every operator against every field: an offer that cannot work is a bug report
/// waiting to happen.
/// </summary>
public static class LogicOperators
{
    #region Private members

    /// <summary>
    /// Numbers are compared as numbers, so the substring operators are withheld: "contains 5" on a
    /// serial reads as a digit search, which is never what was meant.
    /// </summary>
    private static readonly LogicOperator[] _textOperators =
    [
        LogicOperator.Contains,
        LogicOperator.DoesNotContain,
        LogicOperator.Is,
        LogicOperator.IsNot,
        LogicOperator.StartsWith,
        LogicOperator.EndsWith,
        LogicOperator.MatchesRegex,
        LogicOperator.IsAnyOf,
        LogicOperator.IsNoneOf
    ];

    private static readonly LogicOperator[] _numberOperators =
    [
        LogicOperator.Is,
        LogicOperator.IsNot,
        LogicOperator.GreaterThan,
        LogicOperator.GreaterOrEqual,
        LogicOperator.LessThan,
        LogicOperator.LessOrEqual,
        LogicOperator.IsAnyOf,
        LogicOperator.IsNoneOf
    ];

    /// <summary>Two values, so there is nothing between equality and its negation to offer.</summary>
    private static readonly LogicOperator[] _booleanOperators =
    [
        LogicOperator.Is,
        LogicOperator.IsNot
    ];

    /// <summary>A closed set of named values, so only equality makes sense - substring and ordering
    /// operators have nothing to compare.</summary>
    private static readonly LogicOperator[] _enumOperators =
    [
        LogicOperator.Is,
        LogicOperator.IsNot,
        LogicOperator.IsAnyOf,
        LogicOperator.IsNoneOf
    ];

    /// <summary>
    /// Operators whose comparison is textual, and so the only ones the flags have anything to say
    /// about. Case and whitespace are properties of comparing strings; a number parses the same
    /// either way.
    /// </summary>
    private static readonly LogicConditionFlags _textFlags =
        LogicConditionFlags.CaseSensitive | LogicConditionFlags.TrimWhitespace;

    #endregion

    #region Public methods

    /// <summary>
    /// The operators that can be applied to a field of the given kind.
    /// </summary>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The operators, in the order the editor should offer them.</returns>
    public static IReadOnlyList<LogicOperator> For(LogicValueKind kind) =>
        kind switch
        {
            LogicValueKind.Integer or LogicValueKind.Decimal => _numberOperators,
            LogicValueKind.Boolean => _booleanOperators,
            LogicValueKind.Enum => _enumOperators,
            _ => _textOperators
        };

    /// <summary>Whether a kind is compared as a number.</summary>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>Whether it is numeric.</returns>
    public static bool IsNumeric(LogicValueKind kind) =>
        kind is LogicValueKind.Integer or LogicValueKind.Decimal;

    /// <summary>
    /// The flags that change anything for an operator on a field of the given kind. Everything else
    /// is left off the editor rather than shown and ignored.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The applicable flags, possibly <see cref="LogicConditionFlags.None" />.</returns>
    public static LogicConditionFlags FlagsFor(LogicOperator op, LogicValueKind kind) =>
        kind == LogicValueKind.Text && Supports(op, kind) ? _textFlags : LogicConditionFlags.None;

    /// <summary>
    /// Whether an operator is usable on a field of the given kind.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>Whether the pairing is offered.</returns>
    public static bool Supports(LogicOperator op, LogicValueKind kind) => For(kind).Contains(op);

    /// <summary>
    /// An operator that works on the given kind, preferring one already chosen. Used when the field
    /// changes under a condition and the operator it carried no longer applies.
    /// </summary>
    /// <param name="preferred">The operator currently set.</param>
    /// <param name="kind">The field's new value kind.</param>
    /// <returns>The preferred operator, or the kind's first one.</returns>
    public static LogicOperator Coerce(LogicOperator preferred, LogicValueKind kind) =>
        Supports(preferred, kind) ? preferred : For(kind)[0];

    /// <summary>
    /// Whether an operator reads <see cref="LogicCondition.Values" /> rather than
    /// <see cref="LogicCondition.Value" />.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <returns>Whether the operand is a list.</returns>
    public static bool TakesList(LogicOperator op) =>
        op is LogicOperator.IsAnyOf or LogicOperator.IsNoneOf;

    #endregion
}
