#nullable enable

using System;

namespace ClassicUO.Game.Logic;

/// <summary>
/// What kind of value a field holds. Decides which operators the editor offers for it, what it
/// offers to type the operand into, and how that operand is read back when the condition runs.
/// </summary>
public enum LogicValueKind
{
    /// <summary>Free text, compared as written.</summary>
    Text,

    /// <summary>A whole number. Written in decimal, or in hex with an <c>0x</c> prefix.</summary>
    Integer,

    /// <summary>A number with a fractional part.</summary>
    Decimal,

    /// <summary>True or false.</summary>
    Boolean,

    /// <summary>One of a fixed set of named values. Offered as a dropdown rather than typed, since a
    /// hand-typed value could never match a real member.</summary>
    Enum
}

/// <summary>
/// How a field's value is compared against what the condition was given. Not every operator suits
/// every kind - see <see cref="LogicOperators.For" />.
/// </summary>
public enum LogicOperator
{
    /// <summary>Equal.</summary>
    Is,

    /// <summary>Not equal.</summary>
    IsNot,

    /// <summary>The field contains the value somewhere in it.</summary>
    Contains,

    /// <summary>The field does not contain the value anywhere in it.</summary>
    DoesNotContain,

    /// <summary>The field begins with the value.</summary>
    StartsWith,

    /// <summary>The field ends with the value.</summary>
    EndsWith,

    /// <summary>The value is a .NET regular expression the field must match.</summary>
    MatchesRegex,

    /// <summary>The field equals one of a list.</summary>
    IsAnyOf,

    /// <summary>The field equals none of a list.</summary>
    IsNoneOf,

    /// <summary>Numerically greater.</summary>
    GreaterThan,

    /// <summary>Numerically greater, or equal.</summary>
    GreaterOrEqual,

    /// <summary>Numerically smaller.</summary>
    LessThan,

    /// <summary>Numerically smaller, or equal.</summary>
    LessOrEqual
}

/// <summary>
/// How a bracket combines what is under it.
/// </summary>
public enum LogicConnective
{
    /// <summary>Every line must hold.</summary>
    And,

    /// <summary>At least one line must hold.</summary>
    Or,

    /// <summary>Exactly one line must hold.</summary>
    Xor,

    /// <summary>Not every line holds.</summary>
    Nand,

    /// <summary>No line holds.</summary>
    Nor
}

/// <summary>
/// Per-condition switches that change how the comparison is made rather than what it compares.
/// Which of these apply depends on both the operator and the field's kind, so the editor shows only
/// the ones that mean something for the current pairing - see <see cref="LogicOperators.FlagsFor" />.
/// </summary>
[Flags]
public enum LogicConditionFlags
{
    /// <summary>Nothing set.</summary>
    None = 0,

    /// <summary>Capitalisation must match exactly.</summary>
    CaseSensitive = 1 << 0,

    /// <summary>Leading and trailing whitespace is stripped from both sides before comparing.</summary>
    TrimWhitespace = 1 << 1
}
