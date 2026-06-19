#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal static class Option
{
    public static OptionEntry Checkbox(string label, Accessor<bool> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry Checkbox(string label, bool value, Action<bool> onValueChanged, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreateWithCallback(value, onValueChanged, label, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry HuePicker(string label, Accessor<ushort> backingProperty, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundHuePicker(label, backingProperty), search ?? new SearchMetadata(label));

    public static OptionEntry Slider(string label, float min, float max, Accessor<float> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    public static OptionEntry Slider(string label, int min, int max, Accessor<int> backingProperty, bool labelOnLeft = false, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundSliderOption(label, backingProperty, min, max, labelOnLeft), search ?? new SearchMetadata(label));

    public static OptionEntry ComboBox<TValue>(
        string label,
        TValue value,
        IEnumerable<TValue> options,
        Action<TValue> onChange,
        string? tooltip = null,
        SearchMetadata? search = null
    ) where TValue : IEquatable<TValue> =>
        new(() => OptionsFactory.CreateComboBox(label, value, options, onChange, tooltip), search ?? new SearchMetadata(label));

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
}
