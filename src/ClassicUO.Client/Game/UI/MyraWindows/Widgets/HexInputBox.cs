#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     An <see cref="IntegerInputBox" /> that also accepts "0x.." hex text, for a graphic ID
///     pasted from a tool that displays it that way.
/// </summary>
public sealed class HexInputBox : IntegerInputBox
{
    protected override bool IsCharAllowed(char c) => MyraInputBox.HueInputFilter(c);

    protected override bool TryParse(string text, out int value) => text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? int.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
        : base.TryParse(text, out value);

    protected override bool IsIntermediate(string text) =>
        base.IsIntermediate(text) || text.Equals("0x", StringComparison.OrdinalIgnoreCase);
}
