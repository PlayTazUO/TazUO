#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Logic;

/// <summary>
/// Runs a <see cref="LogicNode" /> tree against a subject, by recursive descent: a group asks each
/// of its children and combines the answers, a condition compares one field.
/// <para>
/// One instance per consumer rather than a static helper, because it caches the regexes its tree
/// compiles: a pattern is fixed for the tree's lifetime and rebuilding one per evaluation would be
/// paid on every event the owner is watching. The cache is scoped to the instance, so it is released
/// with whatever held it.
/// </para>
/// <para>
/// Nothing here throws on bad configuration. A field the schema does not know, an operand that is
/// not a number, a regex that will not compile - each makes its own condition false and leaves the
/// rest of the tree alone. A filter is user input, and the cost of being wrong about one has to be
/// that it stops matching, not that the client falls over on every packet.
/// </para>
/// </summary>
/// <typeparam name="TSubject">What the tree is evaluated against.</typeparam>
public sealed class LogicEvaluator<TSubject>
{
    #region Private members

    private readonly LogicSchema<TSubject> _schema;

    /// <summary>Compiled patterns, keyed by the pattern and its case handling. A null entry is a
    /// pattern that would not compile - cached too, so a broken one is reported once.</summary>
    private readonly Dictionary<(string Pattern, bool CaseSensitive), Regex?> _patterns = [];

    #endregion

    #region Ctor

    /// <summary>
    /// Builds an evaluator over one schema.
    /// </summary>
    /// <param name="schema">The fields conditions may name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema" /> is null.</exception>
    public LogicEvaluator(LogicSchema<TSubject> schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _schema = schema;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Whether a subject satisfies a tree.
    /// </summary>
    /// <param name="node">The tree, or null.</param>
    /// <param name="subject">What to evaluate against.</param>
    /// <returns>Whether it holds. A null or empty tree holds: a filter nobody has written anything
    /// into narrows nothing, and reading it as a contradiction would make a newly added filter
    /// silently dead instead of obviously unconfigured.</returns>
    public bool Evaluate(LogicNode? node, TSubject subject) =>
        node switch
        {
            null => true,
            LogicGroup group => EvaluateGroup(group, subject),
            LogicCondition condition => EvaluateCondition(condition, subject),
            _ => false
        };

    #endregion

    #region Private methods

    /// <summary>
    /// Folds a bracket's lines left to right, each combined with the one before it by its own
    /// connective. <c>a AND b OR c</c> is therefore <c>(a AND b) OR c</c> - what is read top to
    /// bottom is what is evaluated, with no operator binding tighter than another.
    /// </summary>
    private bool EvaluateGroup(LogicGroup group, TSubject subject)
    {
        if (group.IsEmpty)
            return true;

        // The first line has nothing above it, so it seeds the fold and its own connective is
        // ignored - which is exactly what the editor shows, no join being drawn above it.
        bool result = Evaluate(group.Children[0], subject);

        for (int i = 1; i < group.Children.Count; i++)
            result = Combine(result, group.Children[i].Join, Evaluate(group.Children[i], subject));

        return result;
    }

    /// <summary>
    /// Applies one join. Every connective is binary here, including the ones that read as
    /// quantifiers: chained, <see cref="LogicConnective.Xor" /> is odd parity rather than "exactly
    /// one of the whole bracket", because each join only ever sees the running result and the next
    /// line.
    /// </summary>
    /// <param name="left">The running result of everything above.</param>
    /// <param name="connective">The join.</param>
    /// <param name="right">The line being joined on.</param>
    /// <returns>The combined result.</returns>
    private static bool Combine(bool left, LogicConnective connective, bool right) =>
        connective switch
        {
            LogicConnective.And => left && right,
            LogicConnective.Or => left || right,
            LogicConnective.Xor => left ^ right,
            LogicConnective.Nand => !(left && right),
            LogicConnective.Nor => !(left || right),
            _ => false
        };

    private bool EvaluateCondition(LogicCondition condition, TSubject subject)
    {
        LogicField? field = _schema.Find(condition.Field);

        if (field == null || !_schema.TryResolve(condition.Field, subject, out object? raw))
            return false;

        if (!HasOperand(condition))
            return false;

        return field.Kind switch
        {
            LogicValueKind.Boolean => EvaluateBoolean(condition, raw),
            LogicValueKind.Integer or LogicValueKind.Decimal => EvaluateNumber(condition, raw),
            _ => EvaluateText(condition, raw)
        };
    }

    /// <summary>
    /// Whether the condition has been given anything to compare against. A half-written one is
    /// false whichever way round it is put, so an unfinished row can neither fire an effect on its
    /// own nor be smuggled past a negation.
    /// </summary>
    private static bool HasOperand(LogicCondition condition) =>
        LogicOperators.TakesList(condition.Operator)
            ? condition.Values.Any(entry => !string.IsNullOrEmpty(entry))
            : !string.IsNullOrEmpty(condition.Value);

    private bool EvaluateText(LogicCondition condition, object? raw)
    {
        bool trim = condition.Flags.HasFlag(LogicConditionFlags.TrimWhitespace);
        bool caseSensitive = condition.Flags.HasFlag(LogicConditionFlags.CaseSensitive);

        string subject = raw?.ToString() ?? string.Empty;
        string operand = condition.Value;

        if (trim)
        {
            subject = subject.Trim();
            operand = operand.Trim();
        }

        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return condition.Operator switch
        {
            LogicOperator.Is => string.Equals(subject, operand, comparison),
            LogicOperator.IsNot => !string.Equals(subject, operand, comparison),
            LogicOperator.Contains => subject.Contains(operand, comparison),
            LogicOperator.DoesNotContain => !subject.Contains(operand, comparison),
            LogicOperator.StartsWith => subject.StartsWith(operand, comparison),
            LogicOperator.EndsWith => subject.EndsWith(operand, comparison),
            LogicOperator.MatchesRegex => PatternFor(operand, caseSensitive)?.IsMatch(subject) == true,
            LogicOperator.IsAnyOf => ListEntries(condition, trim).Any(entry => string.Equals(subject, entry, comparison)),
            LogicOperator.IsNoneOf => !ListEntries(condition, trim).Any(entry => string.Equals(subject, entry, comparison)),
            _ => false
        };
    }

    private static bool EvaluateNumber(LogicCondition condition, object? raw)
    {
        if (!TryParseNumber(raw, out double subject))
            return false;

        if (LogicOperators.TakesList(condition.Operator))
        {
            bool present = ListEntries(condition, trim: true)
                .Any(entry => TryParseNumber(entry, out double candidate) && Equal(candidate, subject));

            return condition.Operator == LogicOperator.IsAnyOf ? present : !present;
        }

        if (!TryParseNumber(condition.Value, out double operand))
            return false;

        return condition.Operator switch
        {
            LogicOperator.Is => Equal(subject, operand),
            LogicOperator.IsNot => !Equal(subject, operand),
            LogicOperator.GreaterThan => subject > operand,
            LogicOperator.GreaterOrEqual => subject >= operand,
            LogicOperator.LessThan => subject < operand,
            LogicOperator.LessOrEqual => subject <= operand,
            _ => false
        };
    }

    /// <summary>
    /// A flag against a flag. The operand is whatever the editor wrote, which is
    /// <c>true</c>/<c>false</c>, but a hand-edited config may hold anything - an unreadable one is a
    /// mis-set condition rather than a false one, so it matches nothing.
    /// </summary>
    private static bool EvaluateBoolean(LogicCondition condition, object? raw)
    {
        if (!TryParseBoolean(raw, out bool subject) || !TryParseBoolean(condition.Value, out bool operand))
            return false;

        return condition.Operator switch
        {
            LogicOperator.Is => subject == operand,
            LogicOperator.IsNot => subject != operand,
            _ => false
        };
    }

    /// <summary>The list operand's entries, blanks dropped.</summary>
    private static IEnumerable<string> ListEntries(LogicCondition condition, bool trim) =>
        condition.Values
            .Where(entry => !string.IsNullOrEmpty(entry))
            .Select(entry => trim ? entry.Trim() : entry);

    /// <summary>
    /// Whether two numbers are the same. Scaled rather than absolute, because the fields worth
    /// comparing run from a resistance percentage to a serial, and an epsilon that suits one is
    /// meaningless for the other.
    /// </summary>
    private static bool Equal(double left, double right) =>
        Math.Abs(left - right) <= 1e-9 * Math.Max(1d, Math.Max(Math.Abs(left), Math.Abs(right)));

    /// <summary>
    /// Reads a value as a number. Hex with an <c>0x</c> prefix is accepted because the fields most
    /// worth comparing numerically - serials above all - are written that way everywhere else.
    /// </summary>
    /// <param name="value">The value, boxed or as text.</param>
    /// <param name="number">The number read.</param>
    /// <returns>Whether it parsed.</returns>
    private static bool TryParseNumber(object? value, out double number)
    {
        number = 0;

        switch (value)
        {
            case null:
                return false;

            case string text:
                return TryParseNumberText(text, out number);

            case IConvertible convertible:
                try
                {
                    number = convertible.ToDouble(CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                {
                    return false;
                }

            default:
                return TryParseNumberText(value.ToString() ?? string.Empty, out number);
        }
    }

    private static bool TryParseNumberText(string text, out double number)
    {
        number = 0;
        string trimmed = text.Trim();

        if (trimmed.Length == 0)
            return false;

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!ulong.TryParse(trimmed.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong hex))
                return false;

            number = hex;

            return true;
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryParseBoolean(object? value, out bool flag)
    {
        switch (value)
        {
            case bool direct:
                flag = direct;
                return true;

            case string text:
                return bool.TryParse(text.Trim(), out flag);

            default:
                flag = false;
                return value != null && bool.TryParse(value.ToString(), out flag);
        }
    }

    private Regex? PatternFor(string pattern, bool caseSensitive)
    {
        (string pattern, bool caseSensitive) key = (pattern, caseSensitive);

        if (_patterns.TryGetValue(key, out Regex? cached))
            return cached;

        RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        if (!caseSensitive)
            options |= RegexOptions.IgnoreCase;

        Regex? compiled = null;

        try
        {
            compiled = new Regex(pattern, options);
        }
        catch (ArgumentException e)
        {
            Log.Warn($"Logic condition pattern '{pattern}' is not a valid regex and will never match: {e.Message}");
        }

        _patterns[key] = compiled;

        return compiled;
    }

    #endregion
}
