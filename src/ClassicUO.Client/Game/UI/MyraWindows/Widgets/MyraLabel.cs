using ClassicUO.Assets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraLabel : Label
{
    public MyraLabel(string text, int fontSizeOffset)
    {
        Wrap = true;
        Text = text;

        Font = MyraStyle.GetUiFont(fontSizeOffset);
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
                styleSheet.Font = MyraStyle.GetUiFont(6);
                break;
            case TextStyle.H2:
                styleSheet.Font = MyraStyle.GetUiFont(4);
                break;
            case TextStyle.H3:
                styleSheet.Font = MyraStyle.GetUiFont(2);
                styleSheet.Padding = new Thickness(4, 2);
                break;
            case TextStyle.H4:
                styleSheet.Font = MyraStyle.GetUiFont(0);
                styleSheet.Padding = new Thickness(3, 1);
                break;
            case TextStyle.H5:
                styleSheet.Font = MyraStyle.GetUiFont(-2);
                styleSheet.Padding = new Thickness(3, 1);
                break;
            case TextStyle.H6:
                styleSheet.Font = MyraStyle.GetUiFont(-4);
                styleSheet.Padding = new Thickness(2, 0);
                break;
            case TextStyle.TableHeader:
                styleSheet.Font = MyraStyle.GetUiFont(-2);
                styleSheet.Padding = new Thickness(4, 0);
                styleSheet.Margin = new Thickness(2, 0);
                break;
            case TextStyle.P:
            default:
                styleSheet.Font = MyraStyle.UiFont;
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
