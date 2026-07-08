using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiGumpPic(GumpPic gumpPic) : ApiUiBaseControl(gumpPic)
{
    public ushort Graphic
    {
        get => GetProp(() => gumpPic.Graphic);
        set => SetProp(() => gumpPic.Graphic = value);
    }

    public ushort Hue
    {
        get => GetProp(() => gumpPic.Hue);
        set => SetProp(() => gumpPic.Hue = value);
    }

    public bool IsPartialHue
    {
        get => GetProp(() => gumpPic.IsPartialHue);
        set => SetProp(() => gumpPic.IsPartialHue = value);
    }

    public bool ContainsByBounds
    {
        get => GetProp(() => gumpPic.ContainsByBounds);
        set => SetProp(() => gumpPic.ContainsByBounds = value);
    }
}
