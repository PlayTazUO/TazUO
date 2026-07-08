using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiButton(Button button) : ApiUiBaseControl(button)
{
    public int ButtonID => GetProp(() => button.ButtonID);

    /// <summary>
    /// Check if the button is currently down(clicked) generally, HasBeenClicked() is a better way to check for button presses.
    /// </summary>
    public bool IsClicked
    {
        get => GetProp(() => button.IsClicked);
        set => SetProp(() => button.IsClicked = value);
    }

    public int ButtonAction
    {
        get => GetProp(() => (int)button.ButtonAction);
        set => SetProp(() => button.ButtonAction = (ButtonAction)value);
    }

    public int ToPage
    {
        get => GetProp(() => button.ToPage);
        set => SetProp(() => button.ToPage = value);
    }

    public ushort ButtonGraphicNormal
    {
        get => GetProp(() => button.ButtonGraphicNormal);
        set => SetProp(() => button.ButtonGraphicNormal = value);
    }

    public ushort ButtonGraphicPressed
    {
        get => GetProp(() => button.ButtonGraphicPressed);
        set => SetProp(() => button.ButtonGraphicPressed = value);
    }

    public ushort ButtonGraphicOver
    {
        get => GetProp(() => button.ButtonGraphicOver);
        set => SetProp(() => button.ButtonGraphicOver = value);
    }

    public int Hue
    {
        get => GetProp(() => button.Hue);
        set => SetProp(() => button.Hue = value);
    }

    public bool FontCenter
    {
        get => GetProp(() => button.FontCenter);
        set => SetProp(() => button.FontCenter = value);
    }

    public bool ContainsByBounds
    {
        get => GetProp(() => button.ContainsByBounds);
        set => SetProp(() => button.ContainsByBounds = value);
    }

    public bool HasBeenClicked() => GetProp(button.HasBeenClicked);
}
