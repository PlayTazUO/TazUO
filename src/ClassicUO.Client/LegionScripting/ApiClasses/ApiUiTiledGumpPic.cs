using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiTiledGumpPic(GumpPicTiled gumpPicTiled) : ApiUiBaseControl(gumpPicTiled)
{
    public ushort Graphic
    {
        get => GetProp(() => gumpPicTiled.Graphic);
        set => SetProp(() => gumpPicTiled.Graphic = value);
    }

    public ushort Hue
    {
        get => GetProp(() => gumpPicTiled.Hue);
        set => SetProp(() => gumpPicTiled.Hue = value);
    }
}
