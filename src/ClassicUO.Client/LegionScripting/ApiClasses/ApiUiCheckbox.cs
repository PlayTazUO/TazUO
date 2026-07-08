using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Inherits from ApiUiBaseControl
/// </summary>
/// <param name="checkbox"></param>
public class ApiUiCheckbox(Checkbox checkbox) : ApiUiBaseControl(checkbox)
{
    /// <summary>
    /// Gets the checked state of the checkbox.
    /// Used in python API
    /// </summary>
    public bool IsChecked
    {
        get => GetProp(() => checkbox.IsChecked);
        set => SetProp(() => checkbox.IsChecked = value);
    }

    /// <summary>
    /// Gets the checked state of the checkbox.
    /// Used in python API
    /// </summary>
    /// <returns>True if the checkbox is checked, false otherwise</returns>
    public bool GetIsChecked() => GetProp(() => checkbox.IsChecked);

    /// <summary>
    /// Sets the checked state of the checkbox.
    /// Used in python API
    /// </summary>
    /// <param name="isChecked">True to check the checkbox, false to uncheck it</param>
    public void SetIsChecked(bool isChecked) => SetProp(() => checkbox.IsChecked = isChecked);

    /// <summary>
    /// Gets the text label displayed next to the checkbox.
    /// Used in python API
    /// </summary>
    public string Text => GetProp(() => checkbox.Text, string.Empty);

    /// <summary>
    /// Gets the text label displayed next to the checkbox.
    /// Used in python API
    /// </summary>
    /// <returns>The checkbox label text</returns>
    public string GetText() => GetProp(() => checkbox.Text, string.Empty);
}
