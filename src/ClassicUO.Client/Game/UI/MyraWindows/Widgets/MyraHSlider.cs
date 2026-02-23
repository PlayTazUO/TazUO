using System;
using ClassicUO.Assets;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraHSlider : Grid
{
    private readonly OverlayLabel _valueLabel;
    private readonly HorizontalSlider _slider;

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
            _slider.Value = value;
            _valueLabel.Text = FormatValue(value);
        }
    }

    public event EventHandler<ValueChangedEventArgs<float>> ValueChangedByUser
    {
        add => _slider.ValueChangedByUser += value;
        remove => _slider.ValueChangedByUser -= value;
    }

    public MyraHSlider()
    {
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        RowsProportions.Add(new Proportion(ProportionType.Auto));

        _slider = new();
        _slider.ValueChangedByUser += (_, _) => _valueLabel.Text = FormatValue(_slider.Value);

        _valueLabel = new OverlayLabel
        {
            Text = "0",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 12),
        };

        Widgets.Add(_slider);
        SetRow(_slider, 0);
        SetColumn(_slider, 0);

        Widgets.Add(_valueLabel);
        SetRow(_valueLabel, 0);
        SetColumn(_valueLabel, 0);
    }

    public static MyraHSlider CreateSliderWithCallback(float min, float max, float value, Action<float> onChanged)
    {
        var slider = new MyraHSlider { Minimum = min, Maximum = max, Value = value };
        slider.ValueChangedByUser += (_, _) => onChanged(Math.Clamp(slider.Value, min, max));
        return slider;
    }

    private static string FormatValue(float v) =>
        v == (int)v ? ((int)v).ToString() : v.ToString("F1");

    private sealed class OverlayLabel : Label
    {
        public override bool InputFallsThrough(Microsoft.Xna.Framework.Point localPos) => true;
    }
}
