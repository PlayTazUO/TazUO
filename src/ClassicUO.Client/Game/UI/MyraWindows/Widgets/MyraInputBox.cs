#nullable enable
using System;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraInputBox : TextBox
{
    public static readonly Func<char, bool> HueInputFilter =
        c => char.IsDigit(c) || c == '-' || c == 'x' || c == 'X'
             || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    public static readonly Func<char, bool> DigitInputFilter = char.IsDigit;

    public Func<char, bool>? InputFilter { get; set; }

    /// <summary>Invoked when the box loses keyboard focus (used to normalize/commit numeric input).</summary>
    public Action? LostFocus { get; set; }

    public MyraInputBox()
    {
        VerticalAlignment = VerticalAlignment.Center;
    }

    public override void OnChar(char c)
    {
        if (InputFilter != null && !InputFilter(c))
            return;

        base.OnChar(c);
    }

    public override void OnLostKeyboardFocus()
    {
        base.OnLostKeyboardFocus();
        LostFocus?.Invoke();
    }

    public static bool TryParseHue(string? text, out ushort hue)
    {
        if (text == "-1")
        {
            hue = ushort.MaxValue;
            return true;
        }

        if (StringHelper.TryParseInt(text ?? "", out int h) && h >= 0 && h <= ushort.MaxValue)
        {
            hue = (ushort)h;
            return true;
        }

        hue = 0;
        return false;
    }

    public static WrapPanel LabeledHorizontalStackPanel(
        string? labelText,
        out MyraInputBox input,
        int width = 150,
        string? text = null,
        string? hintText = null,
        string? tooltip = null
    )
    {
        WrapPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalSpacing = 4,
            VerticalSpacing = 4
        };

        if (!string.IsNullOrWhiteSpace(labelText))
            row.Widgets.Add(new MyraLabel(labelText, MyraLabel.TextStyle.P) { Tooltip = tooltip });

        input = new MyraInputBox { Text = text ?? "", HintText = hintText ?? "", Width = width, Tooltip = tooltip };
        row.Widgets.Add(input);
        return row;
    }

    /// <summary>
    /// Builds a labeled integer input field that only accepts digits and is validated against
    /// <paramref name="min"/>/<paramref name="max"/>. In-range values are committed live as the
    /// user types; the displayed text is clamped and normalized when the field loses focus.
    /// </summary>
    public static HorizontalStackPanel NumberWithLabel(
        string labelText,
        int value,
        int min,
        int max,
        Action<int> onChanged,
        int width = 110,
        string? tooltip = null
    )
    {
        var box = new MyraInputBox
        {
            Text = Math.Clamp(value, min, max).ToString(),
            Width = width,
            Tooltip = tooltip,
            InputFilter = DigitInputFilter
        };

        // Commit live only when the typed value is already within range; don't rewrite the text
        // mid-edit so the user can type toward large numbers (e.g. "500000") without clamping jumps.
        box.TextChangedByUser += (_, _) =>
        {
            if (int.TryParse(box.Text, out int v) && v >= min && v <= max)
                onChanged(v);
        };

        // On focus loss, finalize: clamp out-of-range / empty input and sync the displayed text.
        box.LostFocus = () =>
        {
            int clamped = int.TryParse(box.Text, out int v) ? Math.Clamp(v, min, max) : min;
            onChanged(clamped);

            string normalized = clamped.ToString();
            if (box.Text != normalized)
                box.Text = normalized;
        };

        var row = new HorizontalStackPanel { Spacing = 4 };
        row.Widgets.Add(box);
        row.Widgets.Add(new MyraLabel(labelText, MyraLabel.TextStyle.P));
        return row;
    }

    public static MyraInputBox Hue(ushort value, int? width = 80, string? tooltip = "Set to -1 for any hue.")
    {
        var box = new MyraInputBox
        {
            Text = value == ushort.MaxValue ? "-1" : $"0x{value:X}",
            Width = width,
            Tooltip = tooltip
        };
        box.InputFilter = HueInputFilter;
        return box;
    }
}
