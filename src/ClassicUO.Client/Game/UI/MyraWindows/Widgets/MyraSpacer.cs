using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraSpacer : Widget
{
    public MyraSpacer(int width, int height)
    {
        Padding = new Thickness(width, height);
    }

    public MyraSpacer(int width, int height, IBrush background) : this(width, height)
    {
        Background = background;
    }
}
