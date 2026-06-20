#nullable enable

using System;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal static class Option
{
    public static OptionEntry Checkbox(string? label, Accessor<bool> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry Checkbox(string label, bool value, Action<bool> onValueChanged, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreateWithCallback(value, onValueChanged, label, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry HuePicker(string label, Accessor<ushort> backingProperty, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundHuePicker(label, backingProperty), search ?? new SearchMetadata(label));

    public static OptionEntry Slider(string label, float min, float max, Accessor<float> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    public static OptionEntry Slider(string label, int min, int max, Accessor<int> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    public static OptionEntry Slider(string label, byte min, byte max, Accessor<byte> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    public static OptionEntry ComboBox<TEnum>(string label, Accessor<TEnum> backingProperty, string? tooltip = null, SearchMetadata? search = null)
        where TEnum : struct, Enum =>
        new(() => OptionsFactory.CreateComboBox(
                label,
                backingProperty.Get().ToInt(),
                Enum.GetNames<TEnum>(),
                v => backingProperty.Set((TEnum)(object)v), // This is some ugly casting right here...
                tooltip
            ),
            search ?? new SearchMetadata(label)
        );

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
                v => backingProperty.Set((TEnum)(object)v), // This is some ugly casting right here...
                tooltip
            ),
            search ?? new SearchMetadata(label)
        );

        return option;
    }

    public static OptionEntry ComboBox(string label, int value, string[] options, Action<int> onChange, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => OptionsFactory.CreateComboBox(label, value, options, onChange, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry InputField(string label, Accessor<string> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundInputField(label, backingProperty, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry NumericInput(
        string label,
        Accessor<int> backingProperty,
        int? min = 0,
        int? max = 1_000_000,
        string? tooltip = null,
        SearchMetadata? search = null
    ) =>
        new(() => OptionsFactory.PropBoundNumericInput(label, backingProperty, min, max, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry Button(string label, Action onClick, SearchMetadata? search = null) =>
        new(() => new MyraButton(label, onClick), search ?? new SearchMetadata(label));

    public static OptionEntry FontSelector(
        string label,
        Accessor<string> backingProperty,
        Action<string>? onSelectionChanged = null,
        SearchMetadata? search = null
    ) =>
        new(() => OptionTabCommons.StyledFontSelector(label, backingProperty, onSelectionChanged), search ?? new SearchMetadata(label));

    public static OptionEntry Custom(Func<Widget> render, SearchMetadata search) => new(render, search);

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
