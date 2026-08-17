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

    /// <summary>Which characters the box accepts while typing. Overridden by subclasses that parse a
    /// wider format (e.g. hex) than plain decimal.</summary>
    protected virtual bool IsCharAllowed(char c) => char.IsDigit(c) || c == '-';

    public override void OnChar(char c)
    {
        if (!IsCharAllowed(c))
            return;

        base.OnChar(c);
    }
}
