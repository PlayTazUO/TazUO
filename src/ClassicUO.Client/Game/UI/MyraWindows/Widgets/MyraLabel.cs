using ClassicUO.Assets;
using FontStashSharp;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraLabel : Label
{
    public MyraLabel(string text, int fontSize)
    {
        Text = text;

        Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, fontSize);
    }

    public MyraLabel(string text, Style style)
    {
        Text = text;

        var styleSheet = Stylesheet.Current.LabelStyle.Clone() as LabelStyle;

        if(styleSheet == null) return;

        switch (style)
        {
            case Style.H1:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 20);
                break;
            case Style.H2:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 18);
                break;
            case Style.H3:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 16);
                break;
            default:
            case Style.P:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, 14);
                break;
        }

        ApplyLabelStyle(styleSheet);
    }

    public enum Style
    {
        H1,
        H2,
        H3,
        P
    }
}
