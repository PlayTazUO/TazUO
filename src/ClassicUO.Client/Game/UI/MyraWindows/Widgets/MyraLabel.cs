using ClassicUO.Assets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraLabel : Label
{
    public MyraLabel(string text, int fontSize)
    {
        Wrap = true;
        Text = text;

        Font = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.EMBEDDED_FONT, fontSize);
    }

    public MyraLabel(string text, TextStyle style, AlignMode align = AlignMode.Left)
    {
        Wrap = true;
        Text = text;
        VerticalAlignment = VerticalAlignment.Center;

        var styleSheet = Stylesheet.Current.LabelStyle.Clone() as LabelStyle;
        if (styleSheet == null) return;

        switch (style)
        {
            case TextStyle.H1:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 22);
                break;
            case TextStyle.H2:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 20);
                break;
            case TextStyle.H3:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 18);
                styleSheet.Padding = new Thickness(4, 2);
                break;
            case TextStyle.H4:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 16);
                styleSheet.Padding = new Thickness(3, 1);
                break;
            case TextStyle.H5:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 14);
                styleSheet.Padding = new Thickness(3, 1);
                break;
            case TextStyle.H6:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 12);
                styleSheet.Padding = new Thickness(2, 0);
                break;
            case TextStyle.TableHeader:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO_BOLD, 16);
                styleSheet.Padding = new Thickness(4, 0);
                styleSheet.Margin = new Thickness(2, 0);
                break;
            case TextStyle.P:
            default:
                styleSheet.Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO, 16);
                styleSheet.Padding = new Thickness(4, 2);
                break;
        }

        ApplyLabelStyle(styleSheet);
        HorizontalAlignment = align switch
        {
            AlignMode.Center => HorizontalAlignment.Center,
            AlignMode.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
    }

    public enum TextStyle
    {
        H1,
        H2,
        H3,
        H4,
        H5,
        H6,
        P,
        TableHeader
    }

    public enum AlignMode
    {
        Left,
        Center,
        Right
    }
}
