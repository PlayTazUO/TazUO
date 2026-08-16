
#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Low-level factory methods that create concrete <see cref="OptionItem"/> widgets for use
/// in the options UI. Higher-level entry-point factories live in <see cref="Option"/> and
/// <see cref="OptionsUi"/>
/// </summary>
public static class OptionsFactory
{
    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a checkbox whose checked state controls
    /// a single flag bit within an <see langword="enum"/> flags property
    /// </summary>
    /// <typeparam name="TEnum">The flags enum type</typeparam>
    /// <param name="label">The checkbox label text</param>
    /// <param name="backingProperty">Accessor for the underlying flags value</param>
    /// <param name="relevantFragment">The specific flag bit this checkbox controls</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the bit-flag checkbox</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relevantFragment"/> is not a defined member of <typeparamref name="TEnum"/>
    /// </exception>
    internal static OptionItem CreatePropBoundBitFlagCheckBox<TEnum>(
        string label,
        Accessor<TEnum> backingProperty,
        TEnum relevantFragment
    ) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(relevantFragment))
            throw new ArgumentException($"The value {relevantFragment} is not a member of {typeof(TEnum)}");

        return CreateCheckboxOption(
            label,
            Utility.ByteFlagHelper.HasFlag(backingProperty.Get(), relevantFragment),
            enabled =>
            {
                backingProperty.Set(enabled
                    ? Utility.ByteFlagHelper.AddFlag(backingProperty.Get(), relevantFragment)
                    : Utility.ByteFlagHelper.RemoveFlag(backingProperty.Get(), relevantFragment)
                );
            }
        );
    }

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a checkbox with an explicit initial value
    /// and change callback
    /// </summary>
    /// <param name="label">The checkbox label text</param>
    /// <param name="enabled">The initial checked state</param>
    /// <param name="onChange">Callback invoked whenever the checked state changes</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the checkbox</returns>
    internal static OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange,
        string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a checkbox bound to a <see cref="bool"/>
    /// property via an <see cref="Accessor{T}"/>
    /// </summary>
    /// <param name="label">The checkbox label text</param>
    /// <param name="backingProperty">Accessor for the underlying boolean value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the checkbox</returns>
    internal static OptionItem CreateCheckboxOption(string label, Accessor<bool> backingProperty, string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip));

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled horizontal slider bound to a
    /// <see cref="float"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="backingProperty">Accessor for the underlying float value</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the slider</returns>
    /// <param name="decimalPlaces">Precision to round to; zero keeps the slider on whole numbers.</param>
    internal static OptionItem PropBoundSliderOption(
        string label,
        Accessor<float> backingProperty,
        float min,
        float max,
        bool labelOnLeft = false,
        int decimalPlaces = 0
    ) =>
        CreateSliderOption(label, min, max, backingProperty.Get(), backingProperty.Set, labelOnLeft, decimalPlaces);

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled horizontal slider bound to an
    /// <see cref="int"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="backingProperty">Accessor for the underlying integer value</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the slider</returns>
    internal static OptionItem PropBoundSliderOption(string label, Accessor<int> backingProperty, int min, int max, bool labelOnLeft = false) =>
        CreateSliderOption(label, min, max, backingProperty.Get(), value => backingProperty.Set((int)value), labelOnLeft);

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled horizontal slider bound to a
    /// <see cref="byte"/> property
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="backingProperty">Accessor for the underlying byte value</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the slider</returns>
    internal static OptionItem PropBoundSliderOption(string label, Accessor<byte> backingProperty, byte min, byte max, bool labelOnLeft = false) =>
        CreateSliderOption(label, min, max, backingProperty.Get(), value => backingProperty.Set((byte)value), labelOnLeft);

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled horizontal slider with explicit
    /// value and change callback
    /// </summary>
    /// <param name="label">The slider label text</param>
    /// <param name="min">The minimum slider value</param>
    /// <param name="max">The maximum slider value</param>
    /// <param name="value">The initial slider value</param>
    /// <param name="onChange">Callback invoked with the new value whenever the slider moves</param>
    /// <param name="labelOnLeft">When <see langword="true"/>, the label is placed to the left of the slider</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the slider</returns>
    internal static OptionItem CreateSliderOption(
        string label,
        float min,
        float max,
        float value,
        Action<float> onChange,
        bool labelOnLeft = false,
        int decimalPlaces = 0
    ) =>
        new(label, () => LabeledHorizontalSlider.SliderWithLabel(label, out _, onChange, min, max, value, labelOnLeft, decimalPlaces));

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled combo box with string option labels
    /// and an integer index selection
    /// </summary>
    /// <param name="label">The label displayed beside the combo box</param>
    /// <param name="value">The initially selected index</param>
    /// <param name="options">The display strings for each item</param>
    /// <param name="onChange">Callback invoked with the newly selected index</param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the combo box</returns>
    internal static OptionItem CreateComboBox(string label, int value, string[] options, Action<int> onChange,
        string? tooltip = null)
    {
        var comboView = new ComboView
        {
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (tooltip != null)
            comboView.Tooltip = tooltip;

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            comboView.ListView.Widgets.Add(new Label { Text = option, Tag = i });
        }

        comboView.ListView.SelectedIndex = value;

        comboView.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (comboView.ListView.SelectedIndex != null)
                onChange(comboView.ListView.SelectedIndex.Value);
        };

        return new OptionItem(label, () => new MyraLabel(label, MyraLabel.TextStyle.P).PlaceBefore(comboView));
    }

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled combo box for any equatable value type,
    /// mapping items by value identity rather than index
    /// </summary>
    /// <typeparam name="TValue">The value type of each option; must implement <see cref="IEquatable{T}"/></typeparam>
    /// <param name="label">The optional label displayed beside the combo box</param>
    /// <param name="value">The initially selected value</param>
    /// <param name="options">The available options</param>
    /// <param name="onChange">Callback invoked with the newly selected value</param>
    /// <param name="tooltip">Optional tooltip text on the combo box</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the combo box</returns>
    internal static OptionItem CreateComboBox<TValue>(
        string? label,
        TValue value,
        IEnumerable<TValue> options,
        Action<TValue> onChange,
        string? tooltip = null
    ) where TValue : IEquatable<TValue> =>
        new(label ?? string.Empty, () => OptionTabCommons.CreateOptionsComboBox(label, value, options, onChange, tooltip)) { VerticalAlignment = VerticalAlignment.Center };

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a hue-picker swatch bound to a
    /// <see cref="ushort"/> hue property
    /// </summary>
    /// <param name="label">Optional label; when non-empty it is placed to the right of the swatch</param>
    /// <param name="backingProperty">Accessor for the underlying hue value</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the hue picker</returns>
    internal static OptionItem PropBoundHuePicker(string? label, Accessor<ushort> backingProperty) =>
        CreateHuePicker(label, backingProperty.Get(), backingProperty.Set, 20);

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a clickable hue-picker swatch that opens
    /// a <see cref="ModernColorPicker"/> on touch
    /// </summary>
    /// <param name="label">Optional label; when non-empty it is placed to the right of the swatch</param>
    /// <param name="hue">The initially displayed hue</param>
    /// <param name="onChange">Callback invoked with the newly chosen hue</param>
    /// <param name="maxSize">Maximum pixel size of the swatch texture</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the hue picker</returns>
    internal static OptionItem CreateHuePicker(string? label, ushort hue, Action<ushort> onChange, int maxSize = 36) =>
        new(label ?? string.Empty, () =>
        {
            var textureButton = new MyraArtTexture(0x0FAB, hue, maxSize) { Tooltip = $"Current hue: {hue}" };
            textureButton.TouchUp += (_, _) =>
            {
                if (!textureButton.Enabled)
                    return;

                UIManager.GetGump<ModernColorPicker>()?.Dispose();
                UIManager.Add(new ModernColorPicker(
                    World.Instance,
                    newHue =>
                    {
                        textureButton.SetColorByHue(newHue);
                        onChange(newHue);
                    },
                    isClickable: true
                ));
            };

            if (string.IsNullOrWhiteSpace(label))
                return textureButton;

            return textureButton.PlaceBefore(new MyraLabel(label, MyraLabel.TextStyle.P));
        });

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled text input bound to a
    /// <see cref="string"/> property
    /// </summary>
    /// <param name="label">Optional label displayed beside the input</param>
    /// <param name="backingProp">Accessor for the underlying string value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the text input</returns>
    internal static OptionItem PropBoundInputField(string? label, Accessor<string> backingProp, string? tooltip = null) =>
        CreateInputField(label, backingProp.Get(), backingProp.Set, tooltip);

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled text input with an explicit
    /// initial value and change callback
    /// </summary>
    /// <param name="label">Optional label displayed beside the input</param>
    /// <param name="text">The initial text value</param>
    /// <param name="onChange">Callback invoked with the new text whenever it changes</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the text input</returns>
    internal static OptionItem CreateInputField(string? label, string text, Action<string> onChange, string? tooltip = null) => new(label ?? string.Empty, () =>
    {
        WrapPanel wid = MyraInputBox.LabeledHorizontalStackPanel(label, out MyraInputBox inputBox, text: text, tooltip: tooltip);
        inputBox.TextChangedByUser += (_, _) => onChange(inputBox.Text);
        return wid;
    });

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled integer spinner bound to an
    /// <see cref="int"/> property, with an optional post-set callback
    /// </summary>
    /// <param name="label">The label displayed beside the spinner</param>
    /// <param name="backingProp">Accessor for the underlying integer value</param>
    /// <param name="min">Minimum allowed value</param>
    /// <param name="max">Maximum allowed value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="onAfterUpdate">Optional callback invoked after the value is saved to the accessor</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the integer spinner</returns>
    internal static OptionItem PropBoundIntInput(
        string label,
        Accessor<int> backingProp,
        int? min = 0,
        int? max = 1_000_000,
        string? tooltip = null,
        Action<int>? onAfterUpdate = null
    )
    {
        Action<int> setter = onAfterUpdate == null
            ? backingProp.Set
            : newValue =>
            {
                backingProp.Set(newValue);
                onAfterUpdate.Invoke(newValue);
            };

        return new OptionItem(
            label,
            () => new LabeledIntegerInput(label, backingProp.Get(), setter) { MinValue = min, MaxValue = max, Tooltip = tooltip, InputBoxMinWidth = 60 }
        );
    }

    /// <summary>
    /// Creates an <see cref="OptionItem"/> containing a labeled unsigned integer spinner bound to a
    /// <see cref="uint"/> property, with an optional post-set callback
    /// </summary>
    /// <param name="label">The optional label displayed beside the spinner</param>
    /// <param name="backingProp">Accessor for the underlying unsigned integer value</param>
    /// <param name="min">Minimum allowed value</param>
    /// <param name="max">Maximum allowed value</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="onAfterUpdate">Optional callback invoked after the value is saved to the accessor</param>
    /// <returns>An <see cref="OptionItem"/> wrapping the unsigned integer spinner</returns>
    internal static OptionItem PropBoundUIntInput(
        string? label,
        Accessor<uint> backingProp,
        uint? min = 0,
        uint? max = null,
        string? tooltip = null,
        Action<uint>? onAfterUpdate = null
    )
    {
        Action<uint> setter = onAfterUpdate == null
            ? backingProp.Set
            : newValue =>
            {
                backingProp.Set(newValue);
                onAfterUpdate.Invoke(newValue);
            };

        return new OptionItem(
            label ?? string.Empty,
            () => new LabeledUIntInput(label, backingProp.Get(), setter) { MaxValue = max, MinValue = min, Tooltip = tooltip, InputBoxMinWidth = 60 }
        );
    }

    /// <summary>Creates a non-searchable vertical spacer <see cref="OptionItem"/></summary>
    /// <returns>An <see cref="OptionItem"/> wrapping a small spacer widget</returns>
    internal static OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 4), skipSearch: true);
}
