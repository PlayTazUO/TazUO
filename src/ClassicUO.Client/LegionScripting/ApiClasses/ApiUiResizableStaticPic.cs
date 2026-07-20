using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiResizableStaticPic(ResizableStaticPic resizableStaticPic) : ApiUiBaseControl(resizableStaticPic)
{
    public ushort Hue
    {
        get => GetProp(() => resizableStaticPic.Hue);
        set => SetProp(() => resizableStaticPic.Hue = value);
    }

    public uint Graphic
    {
        get => GetProp(() => resizableStaticPic.Graphic);
        set => SetProp(() => resizableStaticPic.Graphic = value);
    }

    public bool DrawBorder
    {
        get => GetProp(() => resizableStaticPic.DrawBorder);
        set => SetProp(() => resizableStaticPic.DrawBorder = value);
    }
}
