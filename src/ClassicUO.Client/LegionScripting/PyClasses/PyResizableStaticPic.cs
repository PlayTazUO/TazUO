using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyResizableStaticPic(ResizableStaticPic resizableStaticPic) : PyBaseControl(resizableStaticPic)
{
    public ushort Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.Hue = value);
        }
    }

    public uint Graphic
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.Graphic);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.Graphic = value);
        }
    }

    public bool DrawBorder
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.DrawBorder);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => resizableStaticPic.DrawBorder = value);
        }
    }
}
