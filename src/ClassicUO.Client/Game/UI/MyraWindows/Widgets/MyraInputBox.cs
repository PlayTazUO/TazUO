#nullable enable
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraInputBox : TextBox
{
    public MyraInputBox()
    {
        VerticalAlignment = VerticalAlignment.Center;
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
        => new() { Text = value == ushort.MaxValue ? "-1" : value.ToString(), Width = width, Tooltip = tooltip };
}
