using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiRenderedMapArea(RenderedMapArea control) : ApiUiBaseControl(control)
{
    public float Alpha
    {
        get => GetProp(() => control.Alpha);
        set => SetProp(() => control.Alpha = value);
    }
}
