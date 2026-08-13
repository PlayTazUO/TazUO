#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Logic;

/// <summary>
/// Reading names for the logic model's enums. Kept out of the model itself, which stays free of both
/// the localization system and any notion of being displayed - the same tree is evaluated on the
/// packet thread, where none of this is wanted.
/// </summary>
internal static class LogicText
{
    #region Private members

    /// <summary>
    /// Names are looked up once per builder rebuild and matched back by string when a combo reports
    /// a choice, so they are resolved through here rather than inline: a name and its reverse lookup
    /// drifting apart would silently stop selections from taking.
    /// </summary>
    private static readonly Dictionary<LogicOperator, (string Key, string Fallback)> _operators = new()
    {
        [LogicOperator.Is] = ("logic_op_is", "Is"),
        [LogicOperator.IsNot] = ("logic_op_isnot", "Is not"),
        [LogicOperator.Contains] = ("logic_op_contains", "Contains"),
        [LogicOperator.DoesNotContain] = ("logic_op_doesnotcontain", "Does not contain"),
        [LogicOperator.StartsWith] = ("logic_op_startswith", "Starts with"),
        [LogicOperator.EndsWith] = ("logic_op_endswith", "Ends with"),
        [LogicOperator.MatchesRegex] = ("logic_op_matchesregex", "Matches regex"),
        [LogicOperator.IsAnyOf] = ("logic_op_isanyof", "Is any of"),
        [LogicOperator.IsNoneOf] = ("logic_op_isnoneof", "Is none of"),
        [LogicOperator.GreaterThan] = ("logic_op_greaterthan", "Is greater than"),
        [LogicOperator.GreaterOrEqual] = ("logic_op_greaterorequal", "Is at least"),
        [LogicOperator.LessThan] = ("logic_op_lessthan", "Is less than"),
        [LogicOperator.LessOrEqual] = ("logic_op_lessorequal", "Is at most")
    };

    /// <summary>
    /// What a numeric comparison is called. Symbols rather than words: on a number the operators
    /// form an ordered set that reads at a glance as <c>== != &gt; &gt;= &lt; &lt;=</c>, where the
    /// spelled-out forms have to be read one at a time to be told apart.
    /// </summary>
    private static readonly Dictionary<LogicOperator, (string Key, string Fallback)> _numericOperators = new()
    {
        [LogicOperator.Is] = ("logic_numop_is", "=="),
        [LogicOperator.IsNot] = ("logic_numop_isnot", "!="),
        [LogicOperator.GreaterThan] = ("logic_numop_greaterthan", ">"),
        [LogicOperator.GreaterOrEqual] = ("logic_numop_greaterorequal", ">="),
        [LogicOperator.LessThan] = ("logic_numop_lessthan", "<"),
        [LogicOperator.LessOrEqual] = ("logic_numop_lessorequal", "<=")
    };

    private static readonly Dictionary<LogicConnective, (string Key, string Fallback)> _connectives = new()
    {
        [LogicConnective.And] = ("logic_connective_and", "AND"),
        [LogicConnective.Or] = ("logic_connective_or", "OR"),
        [LogicConnective.Xor] = ("logic_connective_xor", "XOR"),
        [LogicConnective.Nand] = ("logic_connective_nand", "NAND"),
        [LogicConnective.Nor] = ("logic_connective_nor", "NOR")
    };

    private static readonly Dictionary<LogicConditionFlags, (string Key, string Fallback)> _flags = new()
    {
        [LogicConditionFlags.CaseSensitive] = ("logic_flag_casesensitive", "Case sensitive"),
        [LogicConditionFlags.TrimWhitespace] = ("logic_flag_trimwhitespace", "Ignore surrounding spaces")
    };

    /// <summary>
    /// Splits a declared name at each word boundary: before a capital that follows a lowercase
    /// letter or digit, and before the last capital of a run of them (so an acronym stays together -
    /// "HPRegen" reads as "HP Regen", not "H P Regen").
    /// </summary>
    private static readonly Regex _enumWordBoundary = new(
        @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
        RegexOptions.Compiled
    );

    /// <summary>
    /// What one join does. Worded as a join of two sides rather than as a statement about the whole
    /// bracket, which is what these are: everything above the join, combined with the line below it.
    /// </summary>
    private static readonly Dictionary<LogicConnective, (string Key, string Fallback)> _connectiveTooltips = new()
    {
        [LogicConnective.And] = ("logic_connective_and_tooltip", "Holds when everything above and the line below both hold."),
        [LogicConnective.Or] = ("logic_connective_or_tooltip", "Holds when either side holds."),
        [LogicConnective.Xor] = ("logic_connective_xor_tooltip", "Holds when exactly one side holds, not both."),
        [LogicConnective.Nand] = ("logic_connective_nand_tooltip", "Holds unless both sides hold."),
        [LogicConnective.Nor] = ("logic_connective_nor_tooltip", "Holds only when neither side holds.")
    };

    #endregion

    #region Internal methods

    /// <summary>
    /// What an operator is called on a field of the given kind.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>Its reading name.</returns>
    internal static string Name(LogicOperator op, LogicValueKind kind)
    {
        if (LogicOperators.IsNumeric(kind) && _numericOperators.TryGetValue(op, out (string Key, string Fallback) numeric))
            return TazLang.Get(numeric.Key, numeric.Fallback);

        return Resolve(_operators, op, op.ToString());
    }

    internal static string Name(LogicConnective connective) => Resolve(_connectives, connective, connective.ToString());

    internal static string Name(LogicConditionFlags flag) => Resolve(_flags, flag, flag.ToString());

    /// <summary>
    /// How one member of an enum field's backing type reads in the editor. Mechanical, not localized
    /// - the member names are declared by whatever game type the field wraps, not by this project's
    /// language file, so there is nothing to look up.
    /// </summary>
    /// <param name="memberName">The member's declared name, as <see cref="Enum.GetNames" /> reports
    /// it.</param>
    /// <returns>The name split into words.</returns>
    internal static string EnumMemberName(string memberName) => _enumWordBoundary.Replace(memberName, " ");

    internal static string Tooltip(LogicConnective connective) => Resolve(_connectiveTooltips, connective, string.Empty);

    /// <summary>
    /// The operator a displayed name came from.
    /// </summary>
    /// <param name="candidates">The operators that were offered.</param>
    /// <param name="displayName">The name the combo reported.</param>
    /// <param name="kind">The field's value kind, which is what decided the names.</param>
    /// <returns>The operator, or null where the name matches none of them.</returns>
    internal static LogicOperator? ParseOperator(
        IEnumerable<LogicOperator> candidates,
        string displayName,
        LogicValueKind kind
    ) =>
        Parse(candidates, displayName, op => Name(op, kind));

    /// <summary>
    /// The enum value a displayed name came from.
    /// </summary>
    /// <typeparam name="TValue">The enum being named.</typeparam>
    /// <param name="candidates">The values that were offered.</param>
    /// <param name="displayName">The name the combo reported.</param>
    /// <param name="name">Produces a value's displayed name.</param>
    /// <returns>The value, or null where the name matches none of them.</returns>
    internal static TValue? Parse<TValue>(
        IEnumerable<TValue> candidates,
        string displayName,
        Func<TValue, string> name
    ) where TValue : struct
    {
        foreach (TValue candidate in candidates)
        {
            if (string.Equals(name(candidate), displayName, StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Every key these tables declare. The names here are not attributes, so nothing else can find
    /// them - and a key with no entry behind it renders its English fallback and looks perfectly
    /// correct.
    /// </summary>
    /// <returns>The keys.</returns>
    internal static IEnumerable<string> DeclaredKeys() =>
        _operators.Values
            .Concat(_numericOperators.Values)
            .Concat(_connectives.Values)
            .Concat(_connectiveTooltips.Values)
            .Concat(_flags.Values)
            .Select(entry => entry.Key);

    /// <summary>The flags a condition uses, split out one per checkbox.</summary>
    /// <param name="op">The operator.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The individual flags, in declaration order.</returns>
    internal static IEnumerable<LogicConditionFlags> ApplicableFlags(LogicOperator op, LogicValueKind kind)
    {
        LogicConditionFlags applicable = LogicOperators.FlagsFor(op, kind);

        return _flags.Keys.Where(flag => applicable.HasFlag(flag));
    }

    #endregion

    #region Private methods

    private static string Resolve<TKey>(Dictionary<TKey, (string Key, string Fallback)> table, TKey value, string fallback)
        where TKey : notnull =>
        table.TryGetValue(value, out (string Key, string Fallback) entry) ? TazLang.Get(entry.Key, entry.Fallback) : fallback;

    #endregion
}
