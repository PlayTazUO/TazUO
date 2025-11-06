using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyTextBox(TextBox textBox) : PyBaseControl(textBox)
{
    public string Text
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => textBox.Text);
        }
        set
        {
            if (!VerifyIntegrity() || value == null) return;

            MainThreadQueue.InvokeOnMainThread(() => textBox.Text = value);
        }
    }

    public int Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => textBox.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => textBox.Hue = value);
        }
    }

    public string Font
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => textBox.Font);
        }
        set
        {
            if (!VerifyIntegrity() || value == null) return;

            MainThreadQueue.InvokeOnMainThread(() => textBox.Font = value);
        }
    }

    public float FontSize
    {
        get
        {
            if (!VerifyIntegrity()) return 0f;

            return MainThreadQueue.InvokeOnMainThread(() => textBox.FontSize);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => textBox.FontSize = value);
        }
    }

    public bool MultiLine
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => textBox.MultiLine);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => textBox.MultiLine = value);
        }
    }

    public void SetText(string text)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.InvokeOnMainThread(() => textBox.SetText(text));
    }
}
