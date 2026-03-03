#nullable enable
using System;
using ClassicUO.Assets;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraHSlider : Grid
{
    private OverlayLabel _valueLabel = new();
    private HorizontalSlider _slider = new();

    public bool RoundValues { get; set; } = true;
    /// <summary>
    /// This is only used when RoundValues is true
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
            var val = ValidateValues(value);
            _slider.Value = val;
            _valueLabel.Text = FormatValue(val);
        }
    }

    public event EventHandler<ValueChangedEventArgs<float>> ValueChangedByUser
    {
        add => _slider.ValueChangedByUser += value;
        remove => _slider.ValueChangedByUser -= value;
    }

    public MyraHSlider()
    {
        Build();
    }

    private float ValidateValues(float value)
    {
        if (!RoundValues) return value;

        value = Math.Clamp(value, Minimum, Maximum);
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
        _valueLabel.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 12);

        _slider.ValueChangedByUser += (_, _) => _valueLabel.Text = FormatValue(_slider.Value);
        _slider.ValueChanged += (sender, args) =>
        {
            Value = ValidateValues(args.NewValue); //This may get called twice: Value updated -> Event fired -> Value changes -> Event fired -> Value changes but the value is the same this time so this event isn't called again
        };

        Widgets.Add(_slider);
        SetRow(_slider, 0);
        SetColumn(_slider, 0);

        Widgets.Add(_valueLabel);
        SetRow(_valueLabel, 0);
        SetColumn(_valueLabel, 0);
    }

    public static MyraHSlider CreateSliderWithCallback(float min, float max, float value, Action<float>? onChanged)
    {
        var slider = new MyraHSlider { Minimum = min, Maximum = max, Value = value };

        if(onChanged != null)
            slider.ValueChangedByUser += (_, _) => onChanged(Math.Clamp(slider.Value, min, max));

        return slider;
    }

    public static HorizontalStackPanel SliderWithLabel(string label, out MyraHSlider slider, Action<float>? onChanged = null, float min = 0f, float max = 100f, float value = 0f)
    {
        HorizontalStackPanel stack = new();

        MyraHSlider s = slider = CreateSliderWithCallback(min, max, value, onChanged);
        stack.Widgets.Add(s);

        stack.Widgets.Add(new MyraLabel(label, MyraLabel.Style.P));

        return stack;
    }

    private static string FormatValue(float v) =>
        v == (int)v ? ((int)v).ToString() : v.ToString("F1");

    private sealed class OverlayLabel : Label
    {
        public override bool InputFallsThrough(Microsoft.Xna.Framework.Point localPos) => true;
    }
}
