#nullable enable
using System;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class LabeledHorizontalSlider : Grid
{
    private readonly OverlayLabel _valueLabel = new();
    private readonly MyraHorizontalSlider _slider = new();

    public bool RoundValues { get; set; } = true;

    /// <summary>
    ///     This is only used when RoundValues is true
    /// </summary>
    public int DecimalPlaces { get; set; } = 0;

    public float Minimum
    {
        get => _slider.Minimum;
        set => _slider.Minimum = value;
    }

    public float Maximum
    {
        get => _slider.Maximum;
        set => _slider.Maximum = value;
    }

    public float Value
    {
        get => _slider.Value;
        set
        {
            float val = ValidateValues(value);
            _slider.Value = val;
            _valueLabel.Text = FormatValue(val);
        }
    }

    public float WheelStep
    {
        get => _slider.WheelStep;
        set => _slider.WheelStep = value;
    }

    public event EventHandler<ValueChangedEventArgs<float>> ValueChangedByUser
    {
        add => _slider.ValueChangedByUser += value;
        remove => _slider.ValueChangedByUser -= value;
    }

    public LabeledHorizontalSlider()
    {
        Build();
    }

    private float ValidateValues(float value)
    {
        value = Math.Clamp(value, Minimum, Maximum);

        if (!RoundValues) return value;

        value = (float)Math.Round(value, DecimalPlaces);
        return value;
    }

    private void Build()
    {
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Auto));

        _valueLabel.Text = "0";
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _valueLabel.VerticalAlignment = VerticalAlignment.Center;
        _valueLabel.Font = MyraStyle.GetUiFont(-2);

        _slider.WheelAdjustment = true;
        _slider.WheelStep = 1f;
        _slider.ValueChangedByUser += (_, _) => _valueLabel.Text = FormatValue(_slider.Value);
        _slider.ValueChanged += (sender, args) =>
        {
            Value = ValidateValues(args
                .NewValue); //This may get called twice: Value updated -> Event fired -> Value changes -> Event fired -> Value changes but the value is the same this time so this event isn't called again
        };

        Widgets.Add(_slider);
        SetRow(_slider, 0);
        SetColumn(_slider, 0);

        Widgets.Add(_valueLabel);
        SetRow(_valueLabel, 0);
        SetColumn(_valueLabel, 0);
    }

    /// <param name="decimalPlaces">Precision to round to. Zero keeps the slider on whole numbers;
    /// anything else also steps the mouse wheel by that precision, since a step of one would take a
    /// fractional slider from end to end.</param>
    public static LabeledHorizontalSlider CreateSliderWithCallback(
        float min,
        float max,
        float value,
        Action<float>? onChanged,
        int decimalPlaces = 0
    )
    {
        var slider = new LabeledHorizontalSlider
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimalPlaces,
            Value = value,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (decimalPlaces > 0)
            slider.WheelStep = MathF.Pow(10f, -decimalPlaces);

        if (onChanged != null)
            slider.ValueChangedByUser += (_, _) => onChanged(Math.Clamp(slider.Value, min, max));

        return slider;
    }

    public static HorizontalStackPanel SliderWithLabel(
        string label,
        out LabeledHorizontalSlider slider,
        Action<float>? onChanged = null,
        float min = 0f,
        float max = 100f,
        float value = 0f,
        bool labelOnLeft = false,
        int decimalPlaces = 0
    )
    {
        HorizontalStackPanel stack = new() { VerticalAlignment = VerticalAlignment.Center, Spacing = MyraStyle.STANDARD_SPACING };
        LabeledHorizontalSlider s = slider = CreateSliderWithCallback(min, max, value, onChanged, decimalPlaces);
        if (labelOnLeft)
        {
            stack.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P));
            stack.Widgets.Add(s);
        }
        else
        {
            stack.Widgets.Add(s);
            stack.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P));
        }

        return stack;
    }

    private string FormatValue(float v) =>
        v == (int)v ? ((int)v).ToString() : v.ToString($"F{Math.Max(DecimalPlaces, 1)}");

    private sealed class OverlayLabel : Label
    {
        public override bool InputFallsThrough(Point localPos) => true;
    }
}
