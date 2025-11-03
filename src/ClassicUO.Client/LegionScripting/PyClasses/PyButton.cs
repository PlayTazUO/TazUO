using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyButton(Button button) : PyBaseControl(button)
{
    public int ButtonID
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ButtonID);
        }
    }

    /// <summary>
    /// Check if the button is currently down(clicked) generally, HasBeenClicked() is a better way to check for button presses.
    /// </summary>
    public bool IsClicked
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.IsClicked);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.IsClicked = value);
        }
    }

    public int ButtonAction
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => (int)button.ButtonAction);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ButtonAction = (ButtonAction)value);
        }
    }

    public int ToPage
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ToPage);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ToPage = value);
        }
    }

    public ushort ButtonGraphicNormal
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicNormal);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicNormal = value);
        }
    }

    public ushort ButtonGraphicPressed
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicPressed);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicPressed = value);
        }
    }

    public ushort ButtonGraphicOver
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicOver);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ButtonGraphicOver = value);
        }
    }

    public int Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.Hue = value);
        }
    }

    public bool FontCenter
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.FontCenter);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.FontCenter = value);
        }
    }

    public bool ContainsByBounds
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.ContainsByBounds);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ContainsByBounds = value);
        }
    }

    public bool HasBeenClicked()
    {
        if (!VerifyIntegrity()) return false;

        return MainThreadQueue.InvokeOnMainThread(button.HasBeenClicked);
    }
}
