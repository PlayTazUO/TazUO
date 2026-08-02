#nullable enable

using System;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Typed factory methods for creating <see cref="OptionEntry"/> instances backed by
/// common UI controls such as checkboxes, sliders, combo boxes, and color pickers
/// </summary>
internal static class Option
{
    /// <summary>
    /// Creates a checkbox entry bound to a <see cref="bool"/> property via an <see cref="Accessor{T}"/>
    /// </summary>
    /// <param name="label">The checkbox label text</param>
    /// <param name="backingProperty">Accessor for the underlying boolean value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the checkbox widget</returns>
    public static OptionEntry Checkbox(string? label, Accessor<bool> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a checkbox entry with an explicit value and change callback
    /// </summary>
    /// <param name="label">The checkbox label text</param>
    /// <param name="value">The initial checked state</param>
    /// <param name="onValueChanged">Callback invoked whenever the checked state changes</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the checkbox widget</returns>
    public static OptionEntry Checkbox(string label, bool value, Action<bool> onValueChanged, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreateWithCallback(value, onValueChanged, label, tooltip), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a checkbox entry bound to a <see cref="bool"/> property that displays a confirmation
    /// warning dialog whenever the user enables it. Declining the dialog reverts the checkbox back to
    /// its unchecked state.
    /// </summary>
    /// <param name="label">The checkbox label text</param>
    /// <param name="backingProperty">Accessor for the underlying boolean value</param>
    /// <param name="warningTitle">Title shown on the confirmation dialog</param>
    /// <param name="warningMessage">Warning message shown in the confirmation dialog body</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the checkbox widget</returns>
    public static OptionEntry CheckboxWithEnableWarning(
        string label,
        Accessor<bool> backingProperty,
        string warningTitle,
        string warningMessage,
        string? tooltip = null,
        SearchMetadata? search = null) =>
        new(() =>
        {
            var cb = MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip);

            cb.IsCheckedChanged += (_, _) =>
            {
                if (!cb.IsChecked)
                    return;

                UIManager.Add(new ConfirmationModal(
                    warningTitle,
                    warningMessage,
                    confirmed =>
                    {
                        if (!confirmed)
                            cb.IsChecked = false;
                    }));
            };

            return cb;
        }, search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a hue-picker entry bound to a <see cref="ushort"/> hue property
    /// </summary>
    /// <param name="label">The label displayed beside the color swatch</param>
    /// <param name="backingProperty">Accessor for the underlying hue value</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the hue-picker widget</returns>
    public static OptionEntry HuePicker(string label, Accessor<ushort> backingProperty, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundHuePicker(label, backingProperty), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a hue-picker entry
    /// </summary>
    /// <param name="label">The label displayed beside the color swatch</param>
    /// <param name="value">The current hue value</param>
    /// <param name="onChanged">An action to invoke when a hue is selected</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the hue-picker widget</returns>
    public static OptionEntry HuePicker(string label, ushort value, Action<ushort> onChanged, SearchMetadata? search = null) =>
        new(() => OptionsFactory.CreateHuePicker(label, value, onChanged, 20), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled horizontal slider entry bound to a <see cref="float"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="backingProperty">Accessor for the underlying float value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the slider widget</returns>
    public static OptionEntry Slider(string label, float min, float max, Accessor<float> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled horizontal slider entry bound to a <see cref="ushort"/> property
    /// </summary>
    /// <param name="label">The slider label</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="backingProperty">Accessor for the underlying float value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the slider widget</returns>
    public static OptionEntry Slider(
        string label,
        ushort min,
        ushort max,
        Accessor<ushort> backingProperty,
        bool labelOnLeft = false,
        SearchMetadata? search = null
    ) =>
        new(
            () => OptionsFactory.CreateSliderOption(
                label,
                min,
                max,
                backingProperty.Get(),
                value => backingProperty.Set((ushort)value),
                labelOnLeft
            ),
            search ?? new SearchMetadata(label)
        );

    /// <summary>
    /// Creates a labeled horizontal slider entry bound to an <see cref="int"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="backingProperty">Accessor for the underlying integer value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the slider widget</returns>
    public static OptionEntry Slider(string label, int min, int max, Accessor<int> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled horizontal slider entry bound to a <see cref="byte"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="backingProperty">Accessor for the underlying byte value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the slider widget</returns>
    public static OptionEntry Slider(string label, byte min, byte max, Accessor<byte> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled combo box entry bound to an <see langword="enum"/> property, using
    /// the enum member names as option labels
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="backingProperty">Accessor for the underlying enum value</param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the combo box widget</returns>
    public static OptionEntry ComboBox<TEnum>(string label, Accessor<TEnum> backingProperty, string? tooltip = null, SearchMetadata? search = null)
        where TEnum : struct, Enum =>
        new(() => OptionsFactory.CreateComboBox(
                label,
                backingProperty.Get().ToInt(),
                Enum.GetNames<TEnum>(),
                v => backingProperty.Set((TEnum)Enum.ToObject(typeof(TEnum), v)),
                tooltip
            ),
            search ?? new SearchMetadata(label)
        );

    /// <summary>
    /// Creates a labeled combo box entry bound to an <see langword="enum"/> property, using
    /// localized display names looked up via a prefix in the TazLang string table.
    /// Falls back to the raw enum member name when no localization is found.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="backingProperty">Accessor for the underlying enum value</param>
    /// <param name="optionLocalizationPrefix">
    /// Optional prefix prepended to each lowercased enum name to form a TazLang lookup key.
    /// When <see langword="null"/>, the lowercased name is used directly as the key.
    /// </param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the combo box widget</returns>
    public static OptionEntry LComboBox<TEnum>(
        string label,
        Accessor<TEnum> backingProperty,
        string? optionLocalizationPrefix = null,
        string? tooltip = null,
        SearchMetadata? search = null
    )
        where TEnum : struct, Enum
    {
        var option = new OptionEntry(() => OptionsFactory.CreateComboBox(
                label,
                backingProperty.Get().ToInt(),
                LocalizeEnumWithFallback<TEnum>(optionLocalizationPrefix),
                v => backingProperty.Set((TEnum)Enum.ToObject(typeof(TEnum), v)),
                tooltip
            ),
            search ?? new SearchMetadata(label)
        );

        return option;
    }

    /// <summary>
    /// Creates a labeled combo box entry with explicit options and a change callback
    /// </summary>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="value">The initially selected index</param>
    /// <param name="options">The display strings for each combo box item</param>
    /// <param name="onChange">Callback invoked with the newly selected index</param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the combo box widget</returns>
    public static OptionEntry ComboBox(string label, int value, string[] options, Action<int> onChange, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => OptionsFactory.CreateComboBox(label, value, options, onChange, tooltip), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled text input entry bound to a <see cref="string"/> property
    /// </summary>
    /// <param name="label">The label displayed beside the input field</param>
    /// <param name="backingProperty">Accessor for the underlying string value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the input field widget</returns>
    public static OptionEntry InputField(string label, Accessor<string> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundInputField(label, backingProperty, tooltip), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled integer spinner entry bound to an <see cref="int"/> property
    /// </summary>
    /// <param name="label">The label displayed beside the spinner</param>
    /// <param name="backingProperty">Accessor for the underlying integer value</param>
    /// <param name="min">Minimum allowed value</param>
    /// <param name="max">Maximum allowed value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the integer input widget</returns>
    public static OptionEntry IntegerInput(
        string label,
        Accessor<int> backingProperty,
        int? min = 0,
        int? max = 1_000_000,
        string? tooltip = null,
        SearchMetadata? search = null
    ) =>
        new(() => OptionsFactory.PropBoundIntInput(label, backingProperty, min, max, tooltip), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled unsigned integer spinner entry bound to a <see cref="uint"/> property
    /// </summary>
    /// <param name="label">The label displayed beside the spinner</param>
    /// <param name="backingProperty">Accessor for the underlying unsigned integer value</param>
    /// <param name="min">Minimum allowed value</param>
    /// <param name="max">Maximum allowed value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the unsigned integer input widget</returns>
    public static OptionEntry UIntegerInput(
        string label,
        Accessor<uint> backingProperty,
        uint? min = 0,
        uint? max = 1_000_000,
        string? tooltip = null,
        SearchMetadata? search = null
    ) =>
        new(() => OptionsFactory.PropBoundUIntInput(label, backingProperty, min, max, tooltip), search ?? new SearchMetadata(label));

    /// <summary>Creates a clickable button entry</summary>
    /// <param name="label">The button label text</param>
    /// <param name="onClick">Action invoked when the button is clicked</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping a <see cref="MyraButton"/></returns>
    public static OptionEntry Button(string label, Action onClick, SearchMetadata? search = null) =>
        new(() => new MyraButton(label, onClick), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates a labeled font-selector combo box entry bound to a <see cref="string"/> font-name property
    /// </summary>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="backingProperty">Accessor for the underlying font name string</param>
    /// <param name="onSelectionChanged">Optional callback invoked after the new font name is saved</param>
    /// <param name="search">Optional search metadata; defaults to metadata seeded from <paramref name="label"/></param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the font-selector widget</returns>
    public static OptionEntry FontSelector(
        string label,
        Accessor<string> backingProperty,
        Action<string>? onSelectionChanged = null,
        SearchMetadata? search = null
    ) =>
        new(() => OptionTabCommons.StyledFontSelector(label, backingProperty, onSelectionChanged), search ?? new SearchMetadata(label));

    /// <summary>
    /// Creates an entry whose widget is produced by an arbitrary factory,
    /// with optional search metadata
    /// </summary>
    /// <param name="render">Factory that produces the custom widget</param>
    /// <param name="search">Optional search metadata for this entry</param>
    /// <returns>An <see cref="OptionEntry"/> wrapping the custom widget</returns>
    public static OptionEntry Custom(Func<Widget> render, SearchMetadata? search = null) => new(render, search);

    /// <summary>Creates a non-searchable vertical spacer entry</summary>
    /// <returns>An <see cref="OptionEntry"/> wrapping a small spacer widget</returns>
    public static OptionEntry Spacer() => new(() => new MyraSpacer(1, 4));

    private static string[] LocalizeEnumWithFallback<TEnum>(string? localizationPrefix = null) where TEnum : struct, Enum
    {
        Func<string, string> getLocKey;
        if (string.IsNullOrWhiteSpace(localizationPrefix))
            getLocKey = name => name.ToLowerInvariant();
        else
            getLocKey = name => $"{localizationPrefix}{name.ToLowerInvariant()}";

        return Enum.GetNames<TEnum>().Select(name =>
        {
            string localized = TazLang.Get(getLocKey(name));
            return string.IsNullOrWhiteSpace(localized) ? name : localized;
        }).ToArray();
    }
}
