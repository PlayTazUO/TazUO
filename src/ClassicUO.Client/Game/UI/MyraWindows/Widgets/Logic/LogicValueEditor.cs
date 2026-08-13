#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Logic;

/// <summary>
/// The right-hand side of a condition: whatever it takes to type the operand for one field kind.
/// <para>
/// A text box for text, a digits-only box for a number, a check box for a flag, and a row per entry
/// for the list operators. Offering a bare text box for all of them is what makes a filter editor
/// feel like a config file - the widget is the clearest statement of what the field will accept.
/// </para>
/// </summary>
internal static class LogicValueEditor
{
    #region Private members

    private const int VALUE_WIDTH = 190;
    private const int LIST_ENTRY_WIDTH = 150;
    private const int BOOLEAN_WIDTH = 110;

    /// <summary>Ceiling for a value-editor combo (enum scalar/list-entry dropdowns). Below it the
    /// combo auto-sizes to its content, same as the field/operator combos - this is only what stops
    /// a schema with a long enum member name from stretching the row.</summary>
    private const int VALUE_MAX_WIDTH = 320;

    /// <inheritdoc cref="VALUE_MAX_WIDTH" />
    private const int LIST_ENTRY_MAX_WIDTH = 260;

    /// <summary>Multiplication sign, U+1F5D9. Present in Noto Sans Symbols 2.</summary>
    private const string REMOVE_GLYPH = "🗙";

    private const int SMALL_GLYPH_SIZE = 20;
    private const int SMALL_BUTTON_SIZE = 22;

    /// <summary>Invariant, matching how the evaluator parses. A comma would be ambiguous beside the
    /// list operators regardless of locale.</summary>
    private const char DECIMAL_SEPARATOR = '.';

    /// <summary>Border for a regex box whose pattern will not compile. One shared brush, not one per
    /// box, since it never changes.</summary>
    private static readonly IBrush _invalidRegexBorder = new SolidBrush(Color.Red);

    #endregion

    #region Internal methods

    /// <summary>
    /// Builds the operand editor for a condition.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The chosen field's value kind.</param>
    /// <param name="enumType">The backing enum, for <see cref="LogicValueKind.Enum" />; ignored
    /// otherwise.</param>
    /// <returns>The editor.</returns>
    internal static Widget Build(LogicCondition condition, LogicEditorContext context, LogicValueKind kind, Type? enumType = null)
    {
        if (LogicOperators.TakesList(condition.Operator))
            return BuildList(condition, context, kind, enumType);

        return kind switch
        {
            LogicValueKind.Boolean => BuildBoolean(condition, context),
            LogicValueKind.Enum when enumType != null => BuildEnumScalar(condition, context, enumType),
            _ => BuildScalar(condition, context, kind)
        };
    }

    /// <summary>
    /// The hint shown in an empty operand box. Without one an empty filter is a row of blank boxes
    /// that say nothing about what belongs in them. Deliberately the same for every kind - what the
    /// box will accept is the tooltip's job, and a hint that changes with the operator makes the row
    /// look like it rearranged itself.
    /// </summary>
    /// <returns>The hint text.</returns>
    internal static string Hint() => TazLang.Get("logic_hint_value", "Field value");

    /// <summary>
    /// What the box will accept, spelled out. The hint says which box this is; this says what goes
    /// in it.
    /// </summary>
    /// <param name="op">The operator chosen.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The tooltip text.</returns>
    internal static string Tooltip(LogicOperator op, LogicValueKind kind)
    {
        if (op == LogicOperator.MatchesRegex)
            return TazLang.Get("logic_value_regex_tooltip", "A .NET regular expression.");

        return kind switch
        {
            LogicValueKind.Integer =>
                TazLang.Get("logic_value_integer_tooltip", "A whole number. Hexadecimal is accepted with an 0x prefix."),
            LogicValueKind.Decimal => TazLang.Get("logic_value_decimal_tooltip", "A number."),
            LogicValueKind.Enum => TazLang.Get("logic_value_enum_tooltip", "One of this field's fixed values."),
            _ => TazLang.Get("logic_value_text_tooltip", "The text to compare against.")
        };
    }

    #endregion

    #region Private methods

    /// <summary>A single typed or dropdown box, for every kind that is not a flag or a closed set of
    /// named values.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The editor.</returns>
    private static Widget BuildScalar(LogicCondition condition, LogicEditorContext context, LogicValueKind kind) =>
        TextEntry(
            condition.Value,
            VALUE_WIDTH,
            kind,
            Tooltip(condition.Operator, kind),
            context,
            text =>
            {
                condition.Value = text;
                context.Changed();
            },
            validateRegex: condition.Operator == LogicOperator.MatchesRegex
        );

    /// <summary>
    /// A flag's operand is one of two values, so it is a small closed dropdown rather than a box to
    /// type <c>true</c> into. Stored as the invariant lowercase words the model parses back.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <returns>The editor.</returns>
    private static Widget BuildBoolean(LogicCondition condition, LogicEditorContext context)
    {
        string trueLabel = TazLang.Get("logic_boolean_true", "True");
        string falseLabel = TazLang.Get("logic_boolean_false", "False");

        bool current = bool.TryParse(condition.Value, out bool parsed) && parsed;

        // A brand new boolean condition has an empty operand, which would evaluate as unconfigured
        // while the dropdown plainly shows a choice already made. Writing it out makes the two agree.
        if (string.IsNullOrEmpty(condition.Value))
            condition.Value = bool.FalseString.ToLowerInvariant();

        var combo = new ContainsLevenshteinComboBox(
            current ? trueLabel : falseLabel,
            [falseLabel, trueLabel],
            chosen =>
            {
                if (chosen == null)
                    return;

                condition.Value = chosen == trueLabel ? bool.TrueString.ToLowerInvariant() : bool.FalseString.ToLowerInvariant();
                context.Changed();
            },
            addSelectedItemIfMissing: false
        )
        {
            MinWidth = BOOLEAN_WIDTH,
            Enabled = !context.ReadOnly
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        return combo;
    }

    /// <summary>
    /// A closed set of named values is a dropdown rather than a box to type into - free text could
    /// never match a real member, and typos would fail silently rather than refuse to be entered.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="enumType">The field's backing enum.</param>
    /// <returns>The editor.</returns>
    private static Widget BuildEnumScalar(LogicCondition condition, LogicEditorContext context, Type enumType) =>
        EnumCombo(
            condition.Value,
            VALUE_WIDTH,
            VALUE_MAX_WIDTH,
            enumType,
            context,
            picked =>
            {
                condition.Value = picked;
                context.Changed();
            }
        );

    /// <summary>
    /// A dropdown of an enum's members, shown humanized but reporting back the member's declared
    /// name - what <see cref="LogicEvaluator{TSubject}" /> compares against, since the resolved field
    /// value is read through <see cref="object.ToString" /> too.
    /// </summary>
    /// <param name="current">The condition's stored operand - a member's declared name, or empty for
    /// none chosen yet.</param>
    /// <param name="minWidth">Floor on the combo's width.</param>
    /// <param name="maxWidth">Ceiling on the combo's width; below it the combo auto-sizes to its
    /// content.</param>
    /// <param name="enumType">The enum <paramref name="current" /> is a member name of.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="onPicked">Called with the newly chosen member's declared name.</param>
    /// <returns>The combo.</returns>
    private static ContainsLevenshteinComboBox EnumCombo(
        string current,
        int minWidth,
        int maxWidth,
        Type enumType,
        LogicEditorContext context,
        Action<string> onPicked
    )
    {
        string[] names = Enum.GetNames(enumType);
        string? selected = names.FirstOrDefault(name => string.Equals(name, current, StringComparison.OrdinalIgnoreCase));
        string selectedDisplay = selected == null ? string.Empty : LogicText.EnumMemberName(selected);

        var combo = new ContainsLevenshteinComboBox(
            selectedDisplay,
            names.Select(LogicText.EnumMemberName),
            chosen =>
            {
                string? picked = chosen == null
                    ? null
                    : names.FirstOrDefault(name => LogicText.EnumMemberName(name) == chosen);

                if (picked != null)
                    onPicked(picked);
            },
            addSelectedItemIfMissing: false
        )
        {
            MinWidth = minWidth,
            MaxWidth = maxWidth,
            PlaceholderText = Hint(),
            Enabled = !context.ReadOnly,
            // The closed combo shows only the selected name, clipped at maxWidth for a long one -
            // this is the hover backstop, same as the field combo's.
            Tooltip = selectedDisplay
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        return combo;
    }

    /// <summary>
    /// One row per value, with its own remove button, and an add button above them. The alternative
    /// - one box holding a comma-separated list - makes the separator part of the syntax, which then
    /// has to be escaped in any value that contains one.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The field's value kind - decides each row's editor.</param>
    /// <param name="enumType">The field's backing enum, for <see cref="LogicValueKind.Enum" />;
    /// ignored otherwise.</param>
    /// <returns>The editor.</returns>
    private static Widget BuildList(LogicCondition condition, LogicEditorContext context, LogicValueKind kind, Type? enumType)
    {
        var panel = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Top
        };

        // First, not last: pinned here it stays level with the field/operator combos beside it -
        // added last, it would jump down the moment the first row pushed in above it.
        panel.Widgets.Add(AddEntryButton(condition, context));

        for (int i = 0; i < condition.Values.Count; i++)
            panel.Widgets.Add(ListEntryRow(condition, context, kind, enumType, i));

        return panel;
    }

    /// <summary>One value's editor and its own remove button, side by side.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The field's value kind - decides the entry's editor.</param>
    /// <param name="enumType">The field's backing enum, for <see cref="LogicValueKind.Enum" />;
    /// ignored otherwise.</param>
    /// <param name="index">This row's position in <see cref="LogicCondition.Values" />.</param>
    /// <returns>The row.</returns>
    private static Widget ListEntryRow(LogicCondition condition, LogicEditorContext context, LogicValueKind kind, Type? enumType, int index)
    {
        // Re-checked rather than captured in the callback: a removal above this row shifts it down,
        // and the handler outlives the rebuild that would have replaced it.
        Widget entry = kind == LogicValueKind.Enum && enumType != null
            ? EnumCombo(
                condition.Values[index],
                LIST_ENTRY_WIDTH,
                LIST_ENTRY_MAX_WIDTH,
                enumType,
                context,
                picked =>
                {
                    if (index < condition.Values.Count)
                    {
                        condition.Values[index] = picked;
                        context.Changed();
                    }
                }
            )
            : TextEntry(
                condition.Values[index],
                LIST_ENTRY_WIDTH,
                kind,
                Tooltip(condition.Operator, kind),
                context,
                text =>
                {
                    if (index < condition.Values.Count)
                    {
                        condition.Values[index] = text;
                        context.Changed();
                    }
                }
            );

        var remove = new IconButton(
            REMOVE_GLYPH,
            () =>
            {
                if (context.ReadOnly || index >= condition.Values.Count)
                    return;

                condition.Values.RemoveAt(index);
                context.Rebuild();
            },
            TazLang.Get("logic_removevalue", "Remove this value"),
            SMALL_BUTTON_SIZE,
            SMALL_GLYPH_SIZE
        )
        {
            Enabled = !context.ReadOnly
        };

        return OptionTabCommons.StyledStackPanel(Orientation.Horizontal, entry, remove);
    }

    /// <summary>Appends a blank entry to the list.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <returns>The button.</returns>
    private static Widget AddEntryButton(LogicCondition condition, LogicEditorContext context) =>
        new MyraButton(
            TazLang.Get("logic_addvalue", "Add value"),
            () =>
            {
                if (context.ReadOnly)
                    return;

                condition.Values.Add(string.Empty);
                context.Rebuild();
            }
        )
        {
            Enabled = !context.ReadOnly,
            HorizontalAlignment = HorizontalAlignment.Left
        };

    /// <summary>
    /// A box for one operand. Numeric kinds get an input filter rather than validation after the
    /// fact, so a field that cannot hold letters never shows any.
    /// </summary>
    /// <param name="value">The operand's current text.</param>
    /// <param name="width">Fixed width for the box - unlike the combos, there is no auto-measured
    /// content to size it against.</param>
    /// <param name="kind">The field's value kind, for its input filter and tooltip.</param>
    /// <param name="tooltip">What the box will accept, shown while the pattern is valid or the kind
    /// is not regex-checked.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="onChanged">Called with the box's text on every keystroke.</param>
    /// <param name="validateRegex">Whether the operand is a regular expression the evaluator will
    /// compile, and so should be flagged live if it will not.</param>
    /// <returns>The box.</returns>
    private static Widget TextEntry(
        string value,
        int width,
        LogicValueKind kind,
        string tooltip,
        LogicEditorContext context,
        Action<string> onChanged,
        bool validateRegex = false
    )
    {
        var input = new MyraInputBox
        {
            Text = value,
            Width = width,
            HintText = Hint(),
            Tooltip = tooltip,
            Enabled = !context.ReadOnly,
            VerticalAlignment = VerticalAlignment.Center,
            InputFilter = FilterFor(kind)
        };

        // Written through per keystroke rather than on focus loss: the builder edits a model the
        // owner is holding, and committing later would lose whatever was typed into the last box
        // before a Save button was clicked.
        input.TextChanged += (_, _) => onChanged(input.Text ?? string.Empty);

        if (validateRegex)
            ApplyRegexValidation(input, tooltip);

        return input;
    }

    /// <summary>
    /// Flags a pattern the evaluator will refuse. Without this a bad regex is silent until the rule
    /// stops matching - <see cref="LogicEvaluator{TSubject}" /> logs a warning and treats it as never
    /// matching rather than throwing, which is right for a filter already saved, but gives an editor
    /// nothing to show while it is still being typed.
    /// </summary>
    /// <param name="input">The regex operand's box, subscribed to for the rest of its life.</param>
    /// <param name="validTooltip">What the box's tooltip reads while the pattern compiles - restored
    /// once a pattern that did not compile is fixed.</param>
    private static void ApplyRegexValidation(MyraInputBox input, string validTooltip)
    {
        // Captured once, before anything here can have touched it, so an invalid pattern can be
        // undone exactly - not guessed back to whatever "normal" looks like.
        IBrush defaultBorder = input.Border;
        string invalidTooltip = TazLang.Get("logic_value_regex_invalid", "Not a valid regular expression.");
        bool invalid = !IsValidRegex(input.Text);

        // Guarded on an actual state change, not run unconditionally on every keystroke: reassigning
        // Border while the box holds keyboard focus and mid-edit text is what left it rendering
        // nothing at all until the row was rebuilt from scratch.
        void Revalidate()
        {
            bool nowInvalid = !IsValidRegex(input.Text);

            if (nowInvalid == invalid)
                return;

            invalid = nowInvalid;
            input.Border = invalid ? _invalidRegexBorder : defaultBorder;
            input.Tooltip = invalid ? invalidTooltip : validTooltip;
        }

        if (invalid)
        {
            input.Border = _invalidRegexBorder;
            input.Tooltip = invalidTooltip;
        }

        input.TextChanged += (_, _) => Revalidate();
    }

    /// <summary>An empty pattern is an unfinished condition, not an invalid one - the evaluator's own
    /// <c>HasOperand</c> check is what refuses to match on it, so this only judges what could compile.</summary>
    /// <param name="pattern">The typed operand.</param>
    /// <returns>Whether it is empty or compiles as a <see cref="Regex" />.</returns>
    private static bool IsValidRegex(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        try
        {
            _ = new Regex(pattern);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// What a box of this kind will accept a character of.
    /// </summary>
    /// <param name="kind">The field's value kind.</param>
    /// <returns>The filter, or null for one that accepts anything.</returns>
    private static Func<char, bool>? FilterFor(LogicValueKind kind) =>
        kind switch
        {
            // Hex digits and the x of the prefix as well as the decimal digits, since a serial is
            // written 0x40001234 everywhere else in the client.
            LogicValueKind.Integer => static character =>
                char.IsAsciiHexDigit(character) || character is 'x' or 'X' or '-',
            LogicValueKind.Decimal => static character =>
                char.IsAsciiDigit(character) || character == '-' || character == DECIMAL_SEPARATOR,
            _ => null
        };

    #endregion
}
