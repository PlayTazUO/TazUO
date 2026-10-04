#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

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

    private readonly List<(T Value, Widget? Dependents)> _branches = [];

    #endregion

    #region Ctor

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

        UpdateDependentEnablement();
        EnabledChanged += (_, _) => UpdateDependentEnablement();
    }

    #endregion

    #region Private methods

    private void AddOption(VerticalStackPanel column, RadioOption<T> option)
    {
        var radio = new RadioButton
        {
            Content = new MyraLabel(option.Label, MyraLabel.TextStyle.P),
            IsPressed = EqualityComparer<T>.Default.Equals(option.Value, Selected),
        };

        if (option.Tooltip != null)
            radio.Tooltip = option.Tooltip;

        radio.PressedChanged += (_, _) =>
        {
            if (!radio.IsPressed)
                return;

            Selected = option.Value;
            UpdateDependentEnablement();
            SelectionChanged?.Invoke(this, option.Value);
        };

        column.Widgets.Add(radio);

        if (option.Dependents is not { Length: > 0 })
        {
            _branches.Add((option.Value, null));
            return;
        }

        var dependents = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            Margin = new Thickness(DEPENDENT_INDENT, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        foreach (Widget dependent in option.Dependents)
            dependents.Widgets.Add(dependent);

        column.Widgets.Add(dependents);
        _branches.Add((option.Value, dependents));
    }

    private void UpdateDependentEnablement()
    {
        foreach ((T value, Widget? dependents) in _branches)
        {
            if (dependents != null)
                dependents.Enabled = Enabled && EqualityComparer<T>.Default.Equals(value, Selected);
        }
    }

    #endregion
}
