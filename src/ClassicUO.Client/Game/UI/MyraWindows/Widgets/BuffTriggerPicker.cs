#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Marks a <see cref="BuffTriggerMode" /> property as the one <see cref="BuffTriggerPicker" /> should
///     edit, and names the siblings that go with it. Named rather than found by convention, as
///     <see cref="FalloffEditorAttribute" /> is: a guessed name would fail silently on a rename.
/// </summary>
/// <param name="buffTypesProperty">Sibling <c>List&lt;short&gt;</c> holding the watched buffs.</param>
/// <param name="durationSecondsProperty">
///     Sibling <see cref="float" /> holding the duration, hidden under
///     <see cref="BuffTriggerMode.Active" />.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BuffTriggerEditorAttribute(string buffTypesProperty, string durationSecondsProperty) : Attribute
{
    /// <summary>The sibling holding the watched buffs.</summary>
    public string BuffTypesProperty { get; } = buffTypesProperty;

    /// <summary>The sibling holding the configured duration.</summary>
    public string DurationSecondsProperty { get; } = durationSecondsProperty;
}

/// <summary>The properties one <see cref="BuffTriggerPicker" /> edits, resolved off its attribute.</summary>
/// <param name="Mode">Which moment of the buff's life fires the rule.</param>
/// <param name="BuffTypes">The buffs being watched.</param>
/// <param name="DurationSeconds">The configured duration.</param>
public readonly record struct BuffTriggerProperties(
    PropertyInfo Mode,
    PropertyInfo? BuffTypes,
    PropertyInfo? DurationSeconds
);

/// <summary>
///     Edits a buff trigger's whole shape: which moment fires it, which buff, and - for the two momentary
///     modes - how long the effect runs.
///     <para>
///         One editor rather than three grid rows: the mode decides whether the duration applies at all
///         and whether one buff is watched or a set, and flat rows would read as knobs it ignores.
///     </para>
/// </summary>
public sealed class BuffTriggerPicker : VerticalStackPanel
{
    #region Private members

    private const int SPACING = 4;

    private readonly Dictionary<string, BuffTriggerMode> _byLabel = [];

    private readonly object _owner;

    private readonly BuffTriggerProperties _properties;

    private readonly int _listWidth;

    private readonly int _numberWidth;

    /// <summary>Holds the buff editor, rebuilt on a mode change - the two modes need different
    /// widgets.</summary>
    private readonly Panel? _buffTypesHost;

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
        _listWidth = listWidth;
        _numberWidth = numberWidth;

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

        if (properties.BuffTypes != null)
        {
            _buffTypesHost = new Panel();
            Widgets.Add(_buffTypesHost);
        }

        _durationRow = DurationRow(properties.DurationSeconds, numberWidth);

        if (_durationRow != null)
            Widgets.Add(_durationRow);

        ApplyMode(mode0);
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

    /// <summary>Builds the buff editor for one mode: multi-select for the momentary modes, a single
    /// picker for <see cref="BuffTriggerMode.Active" />, which brackets one buff's own lifetime.</summary>
    /// <param name="property">The sibling holding the watched buffs.</param>
    /// <param name="mode">The mode the editor is being built for.</param>
    /// <returns>The row to show, or null where there is no property to edit.</returns>
    private HorizontalStackPanel? BuffTypesRow(PropertyInfo? property, BuffTriggerMode mode)
    {
        if (property == null)
            return null;

        List<short> stored = property.GetValue(_owner) as List<short> ?? [];

        bool single = mode == BuffTriggerMode.Active;

        Widget editor = single ? SingleBuffPicker(property, stored) : MultiBuffPicker(property, stored);

        return new HorizontalStackPanel
        {
            Spacing = SPACING,
            Widgets =
            {
                new MyraLabel(ParameterMetadata.LabelFor(property), MyraLabel.TextStyle.P)
                {
                    VerticalAlignment = single ? VerticalAlignment.Center : VerticalAlignment.Top,
                    Tooltip = ParameterMetadata.TooltipFor(property)
                },
                editor
            }
        };
    }

    private Widget MultiBuffPicker(PropertyInfo property, List<short> stored)
    {
        var picker = new IndexedListPicker(
            0,
            BuffTypePicker.CatalogueEntries,
            _numberWidth,
            _listWidth,
            stored.Select(type => (int)type),
            short.MinValue,
            short.MaxValue
        ) { VerticalAlignment = VerticalAlignment.Top };

        picker.ItemsChanged += (_, _) =>
            property.SetValue(_owner, picker.PickedItems.Select(type => (short)type).ToList());

        return picker;
    }

    private Widget SingleBuffPicker(PropertyInfo property, List<short> stored)
    {
        var picker = new IndexedComboPicker(
            stored.Count > 0 ? stored[0] : 0,
            BuffTypePicker.CatalogueEntries,
            short.MinValue,
            short.MaxValue
        ) { VerticalAlignment = VerticalAlignment.Center };

        picker.NumberInput.Width = _numberWidth;
        picker.NameList.Width = _listWidth;

        picker.ValueChanged += (_, value) => property.SetValue(_owner, new List<short> { (short)value });

        return picker;
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
        ApplyMode(mode);
    }

    /// <summary>Fits the editor to <paramref name="mode" />: shows the duration row only where it means
    /// something - hidden, not greyed, which would still read as applicable - and rebuilds the buff
    /// editor.</summary>
    /// <param name="mode">The mode now chosen.</param>
    private void ApplyMode(BuffTriggerMode mode)
    {
        if (_durationRow != null)
            _durationRow.Visible = mode != BuffTriggerMode.Active;

        if (_buffTypesHost == null)
            return;

        // Into Active: drop all but the first, so the config matches what the single picker shows.
        if (mode == BuffTriggerMode.Active && _properties.BuffTypes?.GetValue(_owner) is List<short> { Count: > 1 } stored)
            _properties.BuffTypes.SetValue(_owner, new List<short> { stored[0] });

        _buffTypesHost.Widgets.Clear();

        HorizontalStackPanel? row = BuffTypesRow(_properties.BuffTypes, mode);

        if (row != null)
            _buffTypesHost.Widgets.Add(row);
    }

    #endregion
}
