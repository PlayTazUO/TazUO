using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyLabel(Label label) : PyBaseControl(label)
{
    public string Text
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => label.Text);
        }
        set
        {
            if (!VerifyIntegrity() || value == null) return;

            MainThreadQueue.InvokeOnMainThread(() => label.Text = value);
        }
    }

    public ushort Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => label.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => label.Hue = value);
        }
    }
}
