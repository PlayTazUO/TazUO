using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiTiledGumpPic(GumpPicTiled gumpPicTiled) : ApiUiBaseControl(gumpPicTiled)
{
    public ushort Graphic
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => gumpPicTiled.Graphic);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => gumpPicTiled.Graphic = value);
        }
    }

    public ushort Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => gumpPicTiled.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => gumpPicTiled.Hue = value);
        }
    }
}
