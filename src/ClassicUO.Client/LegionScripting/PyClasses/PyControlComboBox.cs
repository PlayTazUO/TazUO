using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyControlDropDown(Combobox combobox) : PyBaseControl(combobox)
{
    /// <summary>
    /// Get the selected index of the dropdown. The first entry is 0.
    /// </summary>
    /// <returns></returns>
    public int GetSelectedIndex()
    {
        if (!VerifyIntegrity()) return 0;

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            if(combobox != null)
                return combobox.SelectedIndex;

            return 0;
        });
    }
}
