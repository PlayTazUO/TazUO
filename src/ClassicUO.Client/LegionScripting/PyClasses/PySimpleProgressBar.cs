using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PySimpleProgressBar(SimpleProgressBar progressBar) : PyBaseControl(progressBar)
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
