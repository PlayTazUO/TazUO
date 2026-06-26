#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class FloatInputBox : NumericInputBox<float>
{
    public FloatInputBox() : base(null) { }

    public FloatInputBox(Action<float>? valueChangedCallback) : base(valueChangedCallback) { }

    protected override bool TryParse(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    protected override bool IsIntermediate(string text) =>
        string.IsNullOrEmpty(text) || text == "-" || text == "." || text == "-.";

    public override void OnChar(char c)
    {
        if (!char.IsDigit(c) && c != '-' && c != '.')
            return;

        base.OnChar(c);
    }
}
