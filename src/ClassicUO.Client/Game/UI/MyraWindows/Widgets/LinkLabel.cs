using System;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class LinkLabel : MyraLabel
{
    private string _link;

    public LinkLabel(
        string text,
        string link,
        int fontSize
    ) : base(text, fontSize)
    {
        Init(link);
    }

    public LinkLabel(
        string text,
        string link,
        TextStyle style = TextStyle.P,
        AlignMode align = AlignMode.Left
    ) : base(text, style, align)
    {
        Init(link);
    }

    protected void Init(string link)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(link);
        _link = link;
        TextColor = Color.DodgerBlue;
        OverTextColor = Color.DeepSkyBlue;
    }

    public override void OnTouchDown()
    {
        base.OnTouchDown();
        PlatformHelper.LaunchBrowser(_link);
        TextColor = Color.MediumPurple;
    }
}
