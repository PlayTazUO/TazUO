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
        get
        {
            if (!VerifyIntegrity()) return string.Empty;
            return MainThreadQueue.InvokeOnMainThread(() => control.TextureName);
        }
        set
        {
            if (!VerifyIntegrity()) return;
            MainThreadQueue.EnqueueAction(() => control.TextureName = value);
        }
    }
}
