using System;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class BasicButton : Button
{
    public Action OnClick { get; set; }

    public BasicButton(Action onClick)
    {
        OnClick = onClick;
        VerticalAlignment = VerticalAlignment.Center;
    }

    public override void OnTouchDown(TouchEventArgs args)
    {
        base.OnTouchDown(args);

        if (Enabled)
            OnClick?.Invoke();
    }
}
