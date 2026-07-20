using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class BasicButton : Button
{
    public Action OnClick { get; set; }

    public BasicButton(Action onClick)
    {
        OnClick = onClick;
        DisabledBackground = Background;
        VerticalAlignment = VerticalAlignment.Center;
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();

        if (Enabled)
            OnClick?.Invoke();
    }
}
