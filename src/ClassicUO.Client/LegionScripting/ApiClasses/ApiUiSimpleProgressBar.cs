using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiSimpleProgressBar(SimpleProgressBar progressBar) : ApiUiBaseControl(progressBar)
{
    /// <summary>
    /// Sets the progress value for the progress bar.
    /// </summary>
    /// <param name="value">The current value</param>
    /// <param name="max">The maximum value</param>
    public void SetProgress(float value, float max)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.EnqueueAction(() => progressBar.SetProgress(value, max));
    }
}
