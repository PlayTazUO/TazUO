using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyNiceButton(NiceButton button) : PyBaseControl(button)
{
    public int ButtonParameter
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.ButtonParameter);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.ButtonParameter = value);
        }
    }

    public bool IsSelectable
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.IsSelectable);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.IsSelectable = value);
        }
    }

    public bool IsSelected
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.IsSelected);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.IsSelected = value);
        }
    }

    public bool DisplayBorder
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.DisplayBorder);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.DisplayBorder = value);
        }
    }

    public bool AlwaysShowBackground
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => button.AlwaysShowBackground);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.AlwaysShowBackground = value);
        }
    }

    public string Text
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => button.TextLabel.Text);
        }
        set
        {
            if (!VerifyIntegrity() || value == null) return;

            MainThreadQueue.InvokeOnMainThread(() => button.SetText(value));
        }
    }

    public void SetText(string text) => Text = text;

    public ushort TextHue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.TextLabel.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.TextLabel.Hue = value);
        }
    }

    public ushort BackgroundHue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => button.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => button.SetBackgroundHue(value));
        }
    }

    public void SetBackgroundHue(ushort hue) => BackgroundHue = hue;

    /// <summary>
    /// Sets the background color of the button. Pass null to clear.
    /// </summary>
    public void SetBackgroundColor(int? r, int? g, int? b, int? a = 255)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (r.HasValue && g.HasValue && b.HasValue)
            {
                button.BackgroundColor = new Color(r.Value, g.Value, b.Value, a ?? 255);
            }
            else
            {
                button.BackgroundColor = null;
            }
        });
    }

    /// <summary>
    /// Clears the background color of the button.
    /// </summary>
    public void ClearBackgroundColor()
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.InvokeOnMainThread(() => button.BackgroundColor = null);
    }
}
