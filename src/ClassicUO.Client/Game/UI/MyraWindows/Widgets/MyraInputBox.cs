#nullable enable
using System;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraInputBox : TextBox
{
    public static readonly Func<char, bool> HueInputFilter =
        c => char.IsDigit(c) || c == '-' || c == 'x' || c == 'X'
             || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    public Func<char, bool>? InputFilter { get; set; }

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

    public static HorizontalStackPanel WithLabel(
        string labelText,
        out MyraInputBox input,
        int width = 150,
        string? text = null,
        string? hintText = null,
        string? tooltip = null
    )
    {
        var row = new HorizontalStackPanel { Spacing = 4 };
        row.Widgets.Add(new MyraLabel(labelText, MyraLabel.TextStyle.P));
        input = new MyraInputBox {Text = text ?? "", HintText = hintText ?? "", Width = width, Tooltip = tooltip };
        row.Widgets.Add(input);
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
