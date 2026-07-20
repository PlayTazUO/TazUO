#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class IntegerInputBox : NumericInputBox<int>
{
    public IntegerInputBox() : base(null) { }

    public IntegerInputBox(Action<int>? valueChangedCallback) : base(valueChangedCallback) { }

    protected override bool TryParse(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    protected override bool IsIntermediate(string text) => string.IsNullOrEmpty(text) || text == "-";

    public override void OnChar(char c)
    {
        if (!char.IsDigit(c) && c != '-')
            return;

        base.OnChar(c);
    }
}
