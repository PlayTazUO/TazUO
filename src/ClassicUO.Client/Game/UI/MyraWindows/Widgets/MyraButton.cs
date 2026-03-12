#nullable enable
using System;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraButton : Button
{
    private readonly Action? _onClick;

    public string Text { get; }

    public MyraButton(string text, Action? onClick = null)
    {
        _onClick = onClick;
        Text = text;
        Margin = new Thickness(2);
        VerticalAlignment = VerticalAlignment.Center;

        Build();
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();
        _onClick?.Invoke();
    }

    private void Build() => Content = new MyraLabel(Text, MyraLabel.TextStyle.P);
}
