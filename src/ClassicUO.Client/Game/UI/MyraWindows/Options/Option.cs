#nullable enable

using ClassicUO.Common;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal static class Option
{
    public static OptionEntry Checkbox(string label, Accessor<bool> backingProperty, string? tooltip = null, SearchMetadata? search = null) =>
        new(() => MyraCheckButton.CreatePropBoundCheckButton(backingProperty, label, tooltip), search ?? new SearchMetadata(label));

    public static OptionEntry HuePicker(string label, Accessor<ushort> backingProperty, SearchMetadata? search = null) =>
        new(() => OptionsFactory.PropBoundHuePicker(label, backingProperty), search ?? new SearchMetadata(label));

    public static OptionEntry Spacer() => new(() => new MyraSpacer(1, 4));
}
