#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     An <see cref="IntegerInputBox" /> that also accepts "0x.." hex text.
///     <para>
///         Signed 32-bit, so the range tops out at <c>0x7FFFFFFF</c> once
///         <see cref="IntegerInputBox.MinValue" /> is at or above zero. Graphic IDs and UO serials stay
///         inside it; a field needing the full unsigned range needs its own box.
///     </para>
/// </summary>
public sealed class HexIntInputBox : IntegerInputBox
{
    protected override bool IsCharAllowed(char c) => MyraInputBox.HueInputFilter(c);

    protected override bool TryParse(string text, out int value) => text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? int.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
        : base.TryParse(text, out value);

    protected override bool IsIntermediate(string text) =>
        base.IsIntermediate(text) || text.Equals("0x", StringComparison.OrdinalIgnoreCase);
}
