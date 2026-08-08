#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
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

    /// <summary>Multiplication sign, U+1F5D9. Present in Noto Sans Symbols 2.</summary>
    private const string REMOVE_GLYPH = "🗙";

    private const int SMALL_GLYPH_SIZE = 20;
    private const int SMALL_BUTTON_SIZE = 22;

    /// <summary>Invariant, matching how the evaluator parses. A comma would be ambiguous beside the
    /// list operators regardless of locale.</summary>
    private const char DECIMAL_SEPARATOR = '.';

    #endregion

    #region Internal methods

    /// <summary>
    /// Builds the operand editor for a condition.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The chosen field's value kind.</param>
    /// <returns>The editor.</returns>
    internal static Widget Build(LogicCondition condition, LogicEditorContext context, LogicValueKind kind)
    {
        if (LogicOperators.TakesList(condition.Operator))
            return BuildList(condition, context, kind);

        return kind switch
        {
            LogicValueKind.Boolean => BuildBoolean(condition, context),
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
            _ => TazLang.Get("logic_value_text_tooltip", "The text to compare against.")
        };
    }

    #endregion

    #region Private methods

    private static Widget BuildScalar(LogicCondition condition, LogicEditorContext context, LogicValueKind kind)
    {
        return TextEntry(
            condition.Value,
            VALUE_WIDTH,
            kind,
            Tooltip(condition.Operator, kind),
            context,
            text =>
            {
                condition.Value = text;
                context.Changed();
            }
        );
    }

    /// <summary>
    /// A flag's operand is one of two values, so it is a check box rather than a box to type
    /// <c>true</c> into. Stored as the invariant lowercase words the model parses back.
    /// </summary>
    private static Widget BuildBoolean(LogicCondition condition, LogicEditorContext context)
    {
        bool current = bool.TryParse(condition.Value, out bool parsed) && parsed;

        MyraCheckButton check = MyraCheckButton.CreateWithCallback(
            current,
            on =>
            {
                condition.Value = on ? bool.TrueString.ToLowerInvariant() : bool.FalseString.ToLowerInvariant();
                context.Changed();
            }
        );

        check.Enabled = !context.ReadOnly;

        // A brand new boolean condition has an empty operand, which would evaluate as unconfigured
        // while the box plainly shows "unchecked". Writing it out makes the two agree from the start.
        if (string.IsNullOrEmpty(condition.Value))
            condition.Value = bool.FalseString.ToLowerInvariant();

        return OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            check,
            new MyraLabel(TazLang.Get("logic_boolean_true", "True"), MyraLabel.TextStyle.P)
            {
                VerticalAlignment = VerticalAlignment.Center
            }
        );
    }

    /// <summary>
    /// One row per value, with its own remove button, and an add button under them. The alternative
    /// - one box holding a comma-separated list - makes the separator part of the syntax, which then
    /// has to be escaped in any value that contains one.
    /// </summary>
    private static Widget BuildList(LogicCondition condition, LogicEditorContext context, LogicValueKind kind)
    {
        var panel = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Center
        };

        for (int i = 0; i < condition.Values.Count; i++)
            panel.Widgets.Add(ListEntryRow(condition, context, kind, i));

        panel.Widgets.Add(AddEntryButton(condition, context));

        return panel;
    }

    private static Widget ListEntryRow(LogicCondition condition, LogicEditorContext context, LogicValueKind kind, int index)
    {
        Widget entry = TextEntry(
            condition.Values[index],
            LIST_ENTRY_WIDTH,
            kind,
            Tooltip(condition.Operator, kind),
            context,
            text =>
            {
                // Re-checked rather than captured: a removal above this row shifts it down, and the
                // handler outlives the rebuild that would have replaced it.
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
    private static Widget TextEntry(
        string value,
        int width,
        LogicValueKind kind,
        string tooltip,
        LogicEditorContext context,
        Action<string> onChanged
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

        return input;
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
