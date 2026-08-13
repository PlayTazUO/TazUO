#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Logic;

/// <summary>
/// One line of a <see cref="LogicBuilder" />, laid out as
/// <c>[field] [operator] [value] [x]</c> with any switches on their own row beneath it:
/// <code>
/// [Field] [Operator] [Value]        [x]
/// [Case sensitive] [Ignore spaces]
/// </code>
/// The switches belong under the condition rather than trailing it, because they qualify the whole
/// comparison rather than the value alone - and inline they push the remove button off the end of
/// the row as soon as there is more than one of them.
/// <para>
/// Both dropdowns are type-to-filter boxes. The field list is as long as the consumer's schema, and
/// the operator list is short but changes with the field - and unlike the plain combo, these hide
/// their popup before reporting a choice, which is what makes rebuilding the row from inside the
/// handler safe.
/// </para>
/// </summary>
internal static class LogicConditionRow
{
    #region Private members

    private const int FIELD_MIN_WIDTH = 150;
    private const int FIELD_MAX_WIDTH = 340;
    private const int OPERATOR_MIN_WIDTH = 150;
    private const int OPERATOR_MAX_WIDTH = 220;

    /// <summary>Multiplication sign, U+1F5D9. Present in Noto Sans Symbols 2.</summary>
    private const string REMOVE_GLYPH = "🗙";

    private const int REMOVE_GLYPH_FONT_SIZE = 24;

    /// <summary>Indent for the switches, so they read as belonging to the line above.</summary>
    private const int FLAGS_INDENT = 12;

    /// <summary>
    /// Gap between the controls on a line. These are borderless inputs sitting side by side, and
    /// flush against one another they read as a single wide box rather than as three fields.
    /// </summary>
    private const int CONTROL_SPACING = 8;

    /// <summary>Wider still between switches: each is a box and a caption, so the gap has to beat
    /// the one inside a switch or the caption reads as belonging to the box on its right.</summary>
    private const int FLAG_SPACING = 18;

    #endregion

    #region Internal methods

    /// <summary>
    /// Builds the row.
    /// </summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="remove">Detaches this condition from its group.</param>
    /// <returns>The row.</returns>
    internal static Widget Build(LogicCondition condition, LogicEditorContext context, Action remove)
    {
        LogicField? field = Resolve(condition, context.Schema);
        LogicValueKind kind = field?.Kind ?? LogicValueKind.Text;

        var stack = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        stack.Widgets.Add(
            Row(
                FieldCombo(condition, context),
                OperatorCombo(condition, context, kind, field != null),
                LogicValueEditor.Build(condition, context, kind, field?.EnumType),
                RemoveButton(context, remove)
            )
        );

        Widget[] flags = [.. FlagChecks(condition, context, kind)];

        if (flags.Length > 0)
        {
            WrapPanel flagRow = Row(flags);
            flagRow.HorizontalSpacing = FLAG_SPACING;
            flagRow.Margin = new Myra.Graphics2D.Thickness(FLAGS_INDENT, 0, 0, 0);

            stack.Widgets.Add(flagRow);
        }

        return stack;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// One line of controls, top-aligned to the line's own axis. <see cref="WrapPanel.Aligned"/> is
    /// what makes any alignment take effect: unaligned, the panel arranges each child into a
    /// rectangle of exactly its own height, leaving vertical alignment nothing to resolve against.
    /// <para>
    /// Top rather than centred: the value cell can be a whole stack of list-entry rows once the
    /// operator takes a list, and centring a short combo against that tall stack floats it away from
    /// the field/operator combos beside it instead of sitting level with the first entry.
    /// </para>
    /// </summary>
    /// <param name="content">The row's controls, left to right.</param>
    /// <returns>The row.</returns>
    private static WrapPanel Row(params Widget[] content)
    {
        WrapPanel row = OptionTabCommons.StyledHorizontalWrapPanel(content);
        row.Aligned = true;
        row.HorizontalSpacing = CONTROL_SPACING;

        foreach (Widget widget in row.Widgets)
            widget.VerticalAlignment = VerticalAlignment.Top;

        return row;
    }

    /// <summary>
    /// The field a condition names. A key the schema does not know is left as it was persisted
    /// rather than silently re-pointed: the condition is dead either way, and re-pointing it would
    /// quietly start matching on something the user never asked about.
    /// </summary>
    private static LogicField? Resolve(LogicCondition condition, ILogicSchema schema) =>
        schema.Fields.FirstOrDefault(entry => string.Equals(entry.Key, condition.Field, StringComparison.OrdinalIgnoreCase));

    /// <summary>The field-name dropdown, offering every field the schema declares.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <returns>The combo.</returns>
    private static Widget FieldCombo(LogicCondition condition, LogicEditorContext context)
    {
        IReadOnlyList<LogicField> fields = context.Schema.Fields;
        LogicField? selected = Resolve(condition, context.Schema);

        ContainsLevenshteinComboBox combo = Combo(
            selected?.DisplayName,
            fields.Select(field => field.DisplayName),
            FIELD_MIN_WIDTH,
            FIELD_MAX_WIDTH,
            TazLang.Get("logic_hint_field", "Field name"),
            context,
            chosen =>
            {
                LogicField? picked = fields.FirstOrDefault(field => field.DisplayName == chosen);

                if (picked == null || picked.Key == condition.Field)
                    return;

                condition.Field = picked.Key;

                // The new field may not accept the operator the row was carrying, its switches may
                // differ, and its operand editor is a different widget - so the row is rebuilt
                // rather than patched.
                condition.Operator = LogicOperators.Coerce(condition.Operator, picked.Kind);

                // The operand was typed against the old field's kind - a boolean's "true", an
                // enum's member name, a number - and means nothing against the new one. Carrying it
                // over reads as leftover garbage in the new widget rather than an unset value.
                condition.Value = string.Empty;
                condition.Values = [];

                context.Rebuild();
            }
        );

        combo.TooltipSelector = name => fields.FirstOrDefault(field => field.DisplayName == name)?.Description ?? name;

        // The closed combo shows only the selected name, which the row-tooltip above never covers -
        // this is the backstop for a name still clipped at FIELD_MAX_WIDTH.
        combo.Tooltip = selected?.Description ?? selected?.DisplayName ?? string.Empty;

        return combo;
    }

    /// <summary>The comparison dropdown, offering only what <see cref="LogicOperators.For" /> allows
    /// for the current kind.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The row's current field kind, or Text before one is chosen.</param>
    /// <param name="hasField">Whether the row already names a field. Before one is chosen, what
    /// <see cref="LogicOperators.For" /> offers is only the default kind's operators, not this
    /// row's - so the combo is disabled rather than left open on an offer that does not mean
    /// anything yet.</param>
    /// <returns>The combo.</returns>
    private static Widget OperatorCombo(LogicCondition condition, LogicEditorContext context, LogicValueKind kind, bool hasField)
    {
        IReadOnlyList<LogicOperator> operators = LogicOperators.For(kind);

        ContainsLevenshteinComboBox combo = Combo(
            LogicText.Name(condition.Operator, kind),
            operators.Select(op => LogicText.Name(op, kind)),
            OPERATOR_MIN_WIDTH,
            OPERATOR_MAX_WIDTH,
            TazLang.Get("logic_hint_operator", "Comparison"),
            context,
            chosen =>
            {
                LogicOperator? picked = LogicText.ParseOperator(operators, chosen, kind);

                if (picked == null || picked == condition.Operator)
                    return;

                condition.Operator = picked.Value;

                // Both the switches on the row and the operand editor follow the operator - a list
                // operator swaps a single box for a list of them - so this is a shape change.
                context.Rebuild();
            }
        );

        combo.Enabled = !context.ReadOnly && hasField;

        return combo;
    }

    /// <summary>One checkbox-and-caption per switch <see cref="LogicText.ApplicableFlags" /> says
    /// applies to the current operator/kind pairing.</summary>
    /// <param name="condition">The condition being edited, written to in place.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="kind">The row's current field kind.</param>
    /// <returns>The switches, in declaration order.</returns>
    private static IEnumerable<Widget> FlagChecks(LogicCondition condition, LogicEditorContext context, LogicValueKind kind)
    {
        foreach (LogicConditionFlags flag in LogicText.ApplicableFlags(condition.Operator, kind))
        {
            LogicConditionFlags captured = flag;

            var check = MyraCheckButton.CreateWithCallback(
                condition.Flags.HasFlag(captured),
                on =>
                {
                    condition.Flags = on ? condition.Flags | captured : condition.Flags & ~captured;
                    context.Changed();
                }
            );

            StackPanel labelled = OptionTabCommons.StyledStackPanel(
                Orientation.Horizontal,
                check,
                new MyraLabel(LogicText.Name(captured), MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center }
            );

            // Disabled as a pair: disabling the box alone leaves its caption at full strength, which
            // reads as though it were still editable.
            labelled.Enabled = !context.ReadOnly;

            yield return labelled;
        }
    }

    /// <summary>Detaches this condition from its parent group.</summary>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="remove">Detaches this condition from its group.</param>
    /// <returns>The button.</returns>
    private static Widget RemoveButton(LogicEditorContext context, Action remove) =>
        new IconButton(
            REMOVE_GLYPH,
            remove,
            TazLang.Get("logic_removecondition", "Remove this condition"),
            glyphSize: REMOVE_GLYPH_FONT_SIZE
        )
        {
            Enabled = !context.ReadOnly
        };

    /// <summary>A type-to-filter dropdown shared by the field and operator combos.</summary>
    /// <param name="selected">The item to preselect, or null for none.</param>
    /// <param name="items">The offered items, in display order.</param>
    /// <param name="minWidth">Floor on the combo's width, so a short selection does not shrink the
    /// row.</param>
    /// <param name="maxWidth">Ceiling on the combo's width. Below it the combo auto-sizes to its
    /// content - Myra already measures the popup against every item - so this is what keeps a
    /// schema with a long display name from stretching the row rather than what sizes the common
    /// case.</param>
    /// <param name="hint">Placeholder shown while nothing is selected.</param>
    /// <param name="context">The builder's shared state and change callbacks.</param>
    /// <param name="onChosen">Called with the newly selected item's text.</param>
    /// <returns>The combo.</returns>
    private static ContainsLevenshteinComboBox Combo(
        string? selected,
        IEnumerable<string> items,
        int minWidth,
        int maxWidth,
        string hint,
        LogicEditorContext context,
        Action<string> onChosen
    )
    {
        // addSelectedItemIfMissing is off: what is offered comes from the live schema, so a name
        // that is not in it names something withdrawn and must not read as a valid choice.
        var combo = new ContainsLevenshteinComboBox(
            selected ?? string.Empty,
            items,
            chosen =>
            {
                if (chosen != null)
                    onChosen(chosen);
            },
            addSelectedItemIfMissing: false
        )
        {
            MinWidth = minWidth,
            MaxWidth = maxWidth,
            PlaceholderText = hint,
            Enabled = !context.ReadOnly
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        return combo;
    }

    #endregion
}
