using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyAlphaBlendControl(AlphaBlendControl control) : PyBaseControl(control)
{
    public ushort Hue
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => control.Hue);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => control.Hue = value);
        }
    }

    public float Alpha
    {
        get
        {
            if (!VerifyIntegrity()) return 0f;

            return MainThreadQueue.InvokeOnMainThread(() => control.Alpha);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.InvokeOnMainThread(() => control.Alpha = value);
        }
    }

    public byte BaseColorR
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => control.BaseColor.R);
        }
    }

    public byte BaseColorG
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => control.BaseColor.G);
        }
    }

    public byte BaseColorB
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => control.BaseColor.B);
        }
    }

    public byte BaseColorA
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => control.BaseColor.A);
        }
    }

    /// <summary>
    /// Sets the base color of the alpha blend control using RGBA values (0-255)
    /// </summary>
    /// <param name="r">Red component (0-255)</param>
    /// <param name="g">Green component (0-255)</param>
    /// <param name="b">Blue component (0-255)</param>
    /// <param name="a">Alpha component (0-255), defaults to 255 if not specified</param>
    public void SetBaseColor(byte r, byte g, byte b, byte a = 255)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.InvokeOnMainThread(() => control.BaseColor = new Color(r, g, b, a));
    }
}
