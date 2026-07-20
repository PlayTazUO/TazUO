#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class UIntInputBox : NumericInputBox<uint>
{
    public UIntInputBox() : base(null) { }

    public UIntInputBox(Action<uint>? valueChangedCallback) : base(valueChangedCallback) { }

    protected override bool TryParse(string text, out uint value) =>
        uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    protected override bool IsIntermediate(string text) => string.IsNullOrEmpty(text);

    public override void OnChar(char c)
    {
        if (!char.IsDigit(c))
            return;

        base.OnChar(c);
    }
}
