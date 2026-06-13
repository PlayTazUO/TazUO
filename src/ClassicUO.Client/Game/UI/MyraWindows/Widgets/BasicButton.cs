using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class BasicButton : Button
{
    private readonly Action _onClick;

    public BasicButton(Action onClick)
    {
        _onClick = onClick;
        DisabledBackground = Background;
        VerticalAlignment = VerticalAlignment.Center;
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();

        if (Enabled)
            _onClick?.Invoke();
    }
}
