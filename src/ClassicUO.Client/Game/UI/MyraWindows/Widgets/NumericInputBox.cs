#nullable enable
using System;
using System.Globalization;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public abstract class NumericInputBox<T> : GenericInputBox<T> where T : struct, IComparable<T>, IFormattable
{
    public T? MinValue { get; set; }
    public T? MaxValue { get; set; }

    protected NumericInputBox() : base(null) { }

    protected NumericInputBox(Action<T>? valueChangedCallback) : base(valueChangedCallback) { }

    protected override string FormatValue(T value) => value.ToString(null, CultureInfo.InvariantCulture);

    protected override bool Validate(T value, out T validatedValue)
    {
        validatedValue = value;
        bool valid = true;
        if (MinValue.HasValue && value.CompareTo(MinValue.Value) < 0)
        {
            validatedValue = MinValue.Value;
            valid = false;
        }
        else if (MaxValue.HasValue && value.CompareTo(MaxValue.Value) > 0)
        {
            validatedValue = MaxValue.Value;
            valid = false;
        }

        return valid;
    }
}
