using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyTiledGumpPic(GumpPicTiled gumpPicTiled) : PyBaseControl(gumpPicTiled)
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
