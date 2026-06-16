
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

public static class OptionsFactory
{
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
            EnumUtils.HasFlag(backingProperty.Get(), relevantFragment),
            enabled =>
            {
                backingProperty.Set(enabled
                    ? EnumUtils.AddFlag(backingProperty.Get(), relevantFragment)
                    : EnumUtils.RemoveFlag(backingProperty.Get(), relevantFragment)
                );
            }
        );
    }

    internal static OptionItem CreateCheckboxOption(string label, bool enabled, Action<bool> onChange,
        string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreateWithCallback(enabled, onChange, label, tooltip));

    internal static OptionItem CreateCheckboxOption(string label, Accessor<bool> backingProperty, string? tooltip = null) =>
        new(label, () => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip));

    internal static OptionItem PropBoundSliderOption(string label, Accessor<float> backingProperty, float min, float max, bool labelOnLeft = false) =>
        CreateSliderOption(label, min, max, backingProperty.Get(), backingProperty.Set, labelOnLeft);

    internal static OptionItem PropBoundSliderOption(string label, Accessor<int> backingProperty, int min, int max, bool labelOnLeft = false) =>
        CreateSliderOption(label, min, max, backingProperty.Get(), value => backingProperty.Set((int)value), labelOnLeft);

    internal static OptionItem CreateSliderOption(
        string label,
        float min,
        float max,
        float value,
        Action<float> onChange,
        bool labelOnLeft = false
    ) =>
        new(label, () => LabeledHorizontalSlider.SliderWithLabel(label, out _, onChange, min, max, value, labelOnLeft));

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

    internal static OptionItem CreateComboBox<TValue>(
        string? label,
        TValue value,
        IEnumerable<TValue> options,
        Action<TValue> onChange,
        string? tooltip = null
    ) where TValue : IEquatable<TValue> =>
        new(label ?? string.Empty, () => OptionTabCommons.CreateOptionsComboBox(label, value, options, onChange, tooltip)) { VerticalAlignment = VerticalAlignment.Center };

    internal static OptionItem PropBoundHuePicker(string? label, Accessor<ushort> backingProperty) =>
        CreateHuePicker(label, backingProperty.Get(), backingProperty.Set, 20);

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

    internal static OptionItem PropBoundInputField(string? label, Accessor<string> backingProp, string? tooltip = null) =>
        CreateInputField(label, backingProp.Get(), backingProp.Set, tooltip);

    internal static OptionItem CreateInputField(string? label, string text, Action<string> onChange, string? tooltip = null) => new(label ?? string.Empty, () =>
    {
        WrapPanel wid = MyraInputBox.LabeledHorizontalStackPanel(label, out MyraInputBox inputBox, text: text, tooltip: tooltip);
        inputBox.TextChangedByUser += (_, _) => onChange(inputBox.Text);
        return wid;
    });

    internal static OptionItem PropBoundNumericInput(
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

    internal static OptionItem PropBoundUIntInput(
        string? label,
        Accessor<uint> backingProp,
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
            () => new LabeledUIntInput(label, backingProp.Get(), setter) { MaxValue = max, Tooltip = tooltip, InputBoxMinWidth = 60 }
        );
    }

    internal static OptionItem CreateSpacer() => new(string.Empty, () => new MyraSpacer(1, 4), skipSearch: true);

    public static Accessor<T> ConditionalWrapAccessor<T>(Accessor<T> accessor, Action<T>? onChange = null)
    {
        if (onChange == null)
            return accessor;

        return new Accessor<T>(
            accessor.Get,
            value =>
            {
                accessor.Set(value);
                onChange(value);
            }
        );
    }
}

