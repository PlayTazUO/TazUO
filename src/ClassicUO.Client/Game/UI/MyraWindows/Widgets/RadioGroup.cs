#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Theme;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     One option out of a mutually exclusive set, optionally owning the widgets that only mean
///     anything while it is the chosen one.
/// </summary>
/// <param name="Value">What selecting this option stands for.</param>
/// <param name="Label">The option's caption.</param>
/// <param name="Tooltip">Optional tooltip for the caption.</param>
/// <param name="Dependents">
///     Widgets indented under the option and enabled only while it is selected. A setting that only
///     applies to one branch belongs here rather than loose in the panel, where nothing would say
///     which branch it serves.
/// </param>
public sealed record RadioOption<T>(T Value, string Label, string? Tooltip = null, params Widget[] Dependents);

/// <summary>
///     A labelled set of radio options, each able to gate its own dependent widgets.
/// </summary>
/// <remarks>
///     The radio counterpart to <see cref="CheckBoxGroup" />, and for the same reason: an option that
///     governs other settings should own them visibly instead of silently enabling something
///     elsewhere in the panel. Reflects selections made through the UI only - like
///     <see cref="CheckBoxGroup" />, it does not watch the bound value for outside changes.
/// </remarks>
public sealed class RadioGroup<T> : VerticalStackPanel
{
    #region Public events

    /// <summary>Raised when the user picks a different option.</summary>
    public event EventHandler<T>? SelectionChanged;

    #endregion

    #region Public accessors

    /// <summary>The selected option's value.</summary>
    public T Selected { get; private set; }

    #endregion

    #region Private members

    /// <summary>Indent of an option's dependents, matching <see cref="CheckBoxGroup" />.</summary>
    private const int DEPENDENT_INDENT = 20;

    private readonly List<Branch> _branches = [];

    #endregion

    #region Ctor

    /// <summary>Builds the group and selects one of its options.</summary>
    /// <param name="label">Heading above the options. Null or whitespace omits it.</param>
    /// <param name="selected">Which option starts selected.</param>
    /// <param name="options">The options, in display order. Two or more are expected.</param>
    public RadioGroup(string? label, T selected, params RadioOption<T>[] options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Selected = selected;
        Spacing = MyraStyle.STANDARD_SPACING;

        if (!string.IsNullOrWhiteSpace(label))
            Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P));

        // Every RadioButton has to be a direct child of one parent for Myra's exclusivity to see its
        // siblings. Dependents live in the same panel; Myra skips anything that is not a RadioButton.
        var column = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            Margin = new Thickness(DEPENDENT_INDENT, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        foreach (RadioOption<T> option in options)
            AddOption(column, option);

        Widgets.Add(column);

        UpdateOptionStates();
        EnabledChanged += (_, _) => UpdateOptionStates();
    }

    #endregion

    #region Private methods

    /// <summary>Adds one option's radio, and its dependents beneath it, to the shared column.</summary>
    /// <param name="column">The column every radio must share for Myra's exclusivity to work.</param>
    /// <param name="option">The option to add.</param>
    private void AddOption(VerticalStackPanel column, RadioOption<T> option)
    {
        var caption = new MyraLabel(option.Label, MyraLabel.TextStyle.P);

        var radio = new RadioButton
        {
            Content = caption,
            IsPressed = EqualityComparer<T>.Default.Equals(option.Value, Selected)
        };

        if (option.Tooltip != null)
            radio.Tooltip = option.Tooltip;

        radio.PressedChanged += (_, _) =>
        {
            if (!radio.IsPressed)
                return;

            Selected = option.Value;
            UpdateOptionStates();
            SelectionChanged?.Invoke(this, option.Value);
        };

        ApplyRowWideHover(radio);

        column.Widgets.Add(radio);

        VerticalStackPanel? dependents = null;

        if (option.Dependents is { Length: > 0 })
        {
            dependents = new VerticalStackPanel
            {
                Spacing = MyraStyle.STANDARD_SPACING,
                Margin = new Thickness(DEPENDENT_INDENT, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            foreach (Widget dependent in option.Dependents)
                dependents.Widgets.Add(dependent);

            column.Widgets.Add(dependents);
        }

        // Captured rather than resolved later: the label carries the stylesheet's colour until the
        // first time an unselected option dims it, after which it is no longer there to read back.
        _branches.Add(new Branch(option.Value, dependents, caption, caption.TextColor));
    }

    /// <summary>
    ///     Widens the mark's hover response to the whole option row.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Myra picks the over image from the mark's own <c>IsMouseInside</c>, so out of the box only
    ///         the small circle reacts while the caption beside it - just as clickable - does nothing.
    ///         A selected option is unaffected either way, since the pressed image wins over both.
    ///     </para>
    ///     <para>
    ///         Written onto the mark's renderable rather than through <c>UncheckedImage</c>, which looks
    ///         like the tidier property but corrupts the mark: its setter runs
    ///         <c>Renderable = IsPressed ? checkedImage : uncheckedImage</c>, so assigning it while the
    ///         option is selected stamps the <i>checked</i> art in as the resting image. Myra never
    ///         refreshes that on a pressed change - it only flips an <c>IsPressed</c> flag the renderer
    ///         consults - so the option would keep drawing as checked after losing the selection, until
    ///         the next hover happened to rewrite it.
    ///     </para>
    /// </remarks>
    private static void ApplyRowWideHover(RadioButton radio)
    {
        ImageTextButtonStyle? style = Stylesheet.Current?.RadioButtonStyle;

        if (style?.ImageStyle?.OverImage is not { } over)
            return;

        IImage rest = style.ImageStyle.Image;

        radio.MouseEntered += (_, _) => radio.CheckImage.Renderable = over;
        radio.MouseLeft += (_, _) => radio.CheckImage.Renderable = rest;
    }

    /// <summary>
    ///     Brings every option's caption and dependents in line with the selection: an option that is
    ///     not in force reads as inactive rather than leaving the user to match captions to radios.
    /// </summary>
    private void UpdateOptionStates()
    {
        foreach (Branch branch in _branches)
        {
            bool isActive = Enabled && EqualityComparer<T>.Default.Equals(branch.Value, Selected);

            branch.Caption.TextColor = isActive ? branch.ActiveTextColor : MyraTheme.Current.DisabledText;

            if (branch.Dependents != null)
                branch.Dependents.Enabled = isActive;
        }
    }

    #endregion

    #region Nested types

    /// <summary>One option's mutable parts, held so the group can restate them on every selection change.</summary>
    /// <param name="Value">What the option stands for.</param>
    /// <param name="Dependents">Its dependent widgets, or null where it has none.</param>
    /// <param name="Caption">Its label.</param>
    /// <param name="ActiveTextColor">The caption's colour while selected, as the stylesheet gave it.</param>
    private sealed record Branch(T Value, Widget? Dependents, MyraLabel Caption, Color ActiveTextColor);

    #endregion
}
