#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Marks a <see cref="BuffTriggerMode" /> property as the one <see cref="BuffTriggerPicker" /> should
///     edit, and names the siblings that go with it.
///     <para>
///         Named rather than found by convention, the same way <see cref="FalloffEditorAttribute" /> is: the
///         picker writes to both, and a property found by guessing at its name would fail silently the day
///         one was renamed.
///     </para>
/// </summary>
/// <param name="buffTypeProperty">Sibling <see cref="short" /> holding the watched buff.</param>
/// <param name="durationSecondsProperty">
///     Sibling <see cref="float" /> holding the duration, meaningless
///     and hidden under <see cref="BuffTriggerMode.Active" />.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BuffTriggerEditorAttribute(string buffTypeProperty, string durationSecondsProperty) : Attribute
{
    /// <summary>The sibling holding the watched buff.</summary>
    public string BuffTypeProperty { get; } = buffTypeProperty;

    /// <summary>The sibling holding the configured duration.</summary>
    public string DurationSecondsProperty { get; } = durationSecondsProperty;
}

/// <summary>The properties one <see cref="BuffTriggerPicker" /> edits, resolved off its attribute.</summary>
/// <param name="Mode">Which moment of the buff's life fires the rule.</param>
/// <param name="BuffType">The buff being watched.</param>
/// <param name="DurationSeconds">The configured duration.</param>
public readonly record struct BuffTriggerProperties(
    PropertyInfo Mode,
    PropertyInfo? BuffType,
    PropertyInfo? DurationSeconds
);

/// <summary>
///     Edits a buff trigger's whole shape: which moment fires it, which buff, and - for the two momentary
///     modes - how long the effect runs.
///     <para>
///         One editor rather than three grid rows, because the duration is conditional: it means nothing
///         under <see cref="BuffTriggerMode.Active" />, where the buff's own span already decides that.
///         Shown always, it would read as a knob the mode silently ignores.
///     </para>
/// </summary>
public sealed class BuffTriggerPicker : VerticalStackPanel
{
    #region Private members

    private const int SPACING = 4;

    private readonly Dictionary<string, BuffTriggerMode> _byLabel = [];

    private readonly object _owner;

    private readonly BuffTriggerProperties _properties;

    private readonly HorizontalStackPanel? _durationRow;

    #endregion

    #region Ctor

    /// <param name="owner">The object holding the edited properties.</param>
    /// <param name="properties">The properties to edit.</param>
    /// <param name="listWidth">Width for the mode and buff-name lists.</param>
    /// <param name="numberWidth">Width for the numeric fields.</param>
    public BuffTriggerPicker(object owner, BuffTriggerProperties properties, int listWidth, int numberWidth)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        _properties = properties;

        Spacing = SPACING;

        foreach (BuffTriggerMode mode in Enum.GetValues<BuffTriggerMode>())
            _byLabel[DisplayName(mode)] = mode;

        BuffTriggerMode mode0 = CurrentMode();

        var modes = new ContainsLevenshteinComboBox(
            DisplayName(mode0),
            _byLabel.Keys,
            OnModeChosen,
            false
        ) { VerticalAlignment = VerticalAlignment.Center, Width = listWidth };

        MyraStyle.ApplySearchComboBoxPopupBorder(modes);

        Widgets.Add(modes);

        HorizontalStackPanel? typeRow = BuffTypeRow(properties.BuffType, listWidth, numberWidth);

        if (typeRow != null)
            Widgets.Add(typeRow);

        _durationRow = DurationRow(properties.DurationSeconds, numberWidth);

        if (_durationRow != null)
            Widgets.Add(_durationRow);

        ApplyModeVisibility(mode0);
    }

    #endregion

    #region Internal methods

    /// <summary>What one mode is called in the list.</summary>
    /// <param name="mode">The mode to name.</param>
    /// <returns>Its display name.</returns>
    internal static string DisplayName(BuffTriggerMode mode) =>
        mode switch
        {
            BuffTriggerMode.Added => TazLang.Get("overlaytrigger_buff_mode_added", "Buff added"),
            BuffTriggerMode.Removed => TazLang.Get("overlaytrigger_buff_mode_removed", "Buff removed"),
            BuffTriggerMode.Active => TazLang.Get("overlaytrigger_buff_mode_active", "Buff active"),
            _ => mode.ToString()
        };

    #endregion

    #region Private methods

    private BuffTriggerMode CurrentMode() =>
        _properties.Mode.GetValue(_owner) is BuffTriggerMode stored ? stored : BuffTriggerMode.Added;

    private HorizontalStackPanel? BuffTypeRow(PropertyInfo? property, int listWidth, int numberWidth)
    {
        if (property == null)
            return null;

        var picker = new BuffTypePicker(
            property.GetValue(_owner) is short stored ? stored : (short)0,
            numberWidth,
            listWidth
        ) { VerticalAlignment = VerticalAlignment.Center };

        picker.TypeChanged += (_, buffType) => property.SetValue(_owner, buffType);

        return new HorizontalStackPanel
        {
            Spacing = SPACING,
            Widgets =
            {
                new MyraLabel(ParameterMetadata.LabelFor(property), MyraLabel.TextStyle.P)
                {
                    VerticalAlignment = VerticalAlignment.Center, Tooltip = ParameterMetadata.TooltipFor(property)
                },
                picker
            }
        };
    }

    private HorizontalStackPanel? DurationRow(PropertyInfo? property, int width)
    {
        if (property == null)
            return null;

        var input = new FloatInputBox
        {
            MinValue = 0f,
            Width = width,
            VerticalAlignment = VerticalAlignment.Center,
            Value = property.GetValue(_owner) is float stored ? stored : 0f,
            Tooltip = ParameterMetadata.TooltipFor(property)
        };

        input.ValueChanged += (_, args) => property.SetValue(_owner, args.NewValue);

        return new HorizontalStackPanel
        {
            Spacing = SPACING,
            Widgets =
            {
                new MyraLabel(ParameterMetadata.LabelFor(property), MyraLabel.TextStyle.P)
                {
                    VerticalAlignment = VerticalAlignment.Center, Tooltip = ParameterMetadata.TooltipFor(property)
                },
                input
            }
        };
    }

    private void OnModeChosen(string? label)
    {
        if (label == null || !_byLabel.TryGetValue(label, out BuffTriggerMode mode))
            return;

        _properties.Mode.SetValue(_owner, mode);
        ApplyModeVisibility(mode);
    }

    /// <summary>
    ///     Shows the duration field only for the modes it means something to. Hidden rather than
    ///     disabled: a greyed field still reads as something that ought to apply.
    /// </summary>
    /// <param name="mode">The mode now chosen.</param>
    private void ApplyModeVisibility(BuffTriggerMode mode)
    {
        if (_durationRow != null)
            _durationRow.Visible = mode != BuffTriggerMode.Active;
    }

    #endregion
}
