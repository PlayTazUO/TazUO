using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Inherits from PyCheckbox
/// </summary>
/// <param name="radioButton"></param>
public class PyRadioButton(RadioButton radioButton) : PyCheckbox(radioButton)
{
    /// <summary>
    /// Gets or sets the group index of the radio button.
    /// Radio buttons with the same group index are mutually exclusive.
    /// Used in python API
    /// </summary>
    public int GroupIndex
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => radioButton.GroupIndex);
        }
        set
        {
            if (VerifyIntegrity())
                MainThreadQueue.EnqueueAction(() => radioButton.GroupIndex = value);
        }
    }

    /// <summary>
    /// Gets the group index of the radio button.
    /// Radio buttons with the same group index are mutually exclusive.
    /// Used in python API
    /// </summary>
    /// <returns>The group index</returns>
    public int GetGroupIndex()
    {
        if (!VerifyIntegrity()) return 0;

        return MainThreadQueue.InvokeOnMainThread(() => radioButton.GroupIndex);
    }

    /// <summary>
    /// Sets the group index of the radio button.
    /// Radio buttons with the same group index are mutually exclusive.
    /// Used in python API
    /// </summary>
    /// <param name="groupIndex">The group index to set</param>
    public void SetGroupIndex(int groupIndex)
    {
        if (VerifyIntegrity())
            MainThreadQueue.EnqueueAction(() => radioButton.GroupIndex = groupIndex);
    }
}
