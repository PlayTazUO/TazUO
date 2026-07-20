using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiLegionTexture(LegionTexturePic control) : ApiUiBaseControl(control)
{
    /// <summary>
    /// The name of the texture being displayed, as it appears in the ZIP archive.
    /// </summary>
    public string TextureName
    {
        get => GetProp(() => control.TextureName, string.Empty);
        set => SetProp(() => control.TextureName = value);
    }
}
