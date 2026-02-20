using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

public static class MyraStyle
{
    public static void SetDefault()
    {
        WindowStyle style = Stylesheet.Current.WindowStyle;

        style.Background = new SolidBrush(new Color(0.118f, 0.115f, 0.143f, 0.75f));
        style.Border = new SolidBrush(new Color(0.667f, 0.412f, 0.051f, 0.9f));
    }
}