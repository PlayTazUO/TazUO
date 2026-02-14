using System;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiControlDropDown(Combobox combobox, LegionAPI api) : ApiUiBaseControl(combobox)
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

    /// <summary>
    /// Add an onSelectionChanged callback to this dropdown control.
    /// The callback function will receive the selected index as a parameter.
    /// Example:
    /// ```py
    /// def on_select(index):
    ///   API.SysMsg(f"Selected index: {index}")
    ///
    /// dropdown = API.Gumps.CreateDropDown(100, ["first", "second", "third"], 0)
    /// dropdown.OnDropDownOptionSelected(on_select)
    ///
    /// while True:
    ///   API.ProcessCallbacks()
    /// ```
    /// </summary>
    /// <param name="onSelectionChanged">The callback function that receives the selected index</param>
    /// <returns>Returns this control so methods can be chained.</returns>
    public ApiUiControlDropDown OnDropDownOptionSelected(object onSelectionChanged)
    {
        if (!VerifyIntegrity() || onSelectionChanged == null || api == null || !api.CallbackChannel.CanInvoke(onSelectionChanged))
            return this;

        combobox.OnOptionSelected += (_, selectedIndex) =>
        {
            api?.ScheduleCallback(() =>
            {
                try
                {
                    api.CallbackChannel.Invoke(onSelectionChanged, selectedIndex);
                }
                catch (Exception ex)
                {
                    Game.GameActions.Print($"Script callback error: {ex}", Constants.HUE_ERROR);
                }
            });
        };

        return this;
    }
}
