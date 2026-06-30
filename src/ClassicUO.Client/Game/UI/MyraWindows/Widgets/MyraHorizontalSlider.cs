#nullable enable
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraHorizontalSlider : HorizontalSlider
{
    public override void OnMouseEntered()
    {
        if (!Enabled)
            return;
        base.OnMouseEntered();
    }

    public override void OnTouchEntered()
    {
        if (!Enabled)
            return;
        base.OnTouchEntered();
    }

    public override void OnMouseWheel(float delta)
    {
        if (!Enabled)
            return;
        base.OnMouseWheel(delta);
    }

    public override void OnTouchDown()
    {
        if (!Enabled)
            return;
        base.OnTouchDown();
    }
}
