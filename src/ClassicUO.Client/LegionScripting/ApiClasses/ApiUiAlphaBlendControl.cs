using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiAlphaBlendControl(AlphaBlendControl control) : ApiUiBaseControl(control)
{
    public ushort Hue
    {
        get => GetProp(() => control.Hue);
        set => SetProp(() => control.Hue = value);
    }

    public float Alpha
    {
        get => GetProp(() => control.Alpha);
        set => SetProp(() => control.Alpha = value);
    }

    public byte BaseColorR => GetProp(() => control.BaseColor.R);

    public byte BaseColorG => GetProp(() => control.BaseColor.G);

    public byte BaseColorB => GetProp(() => control.BaseColor.B);

    public byte BaseColorA => GetProp(() => control.BaseColor.A);

    /// <summary>
    /// Sets the base color of the alpha blend control using RGBA values (0-255)
    /// </summary>
    /// <param name="r">Red component (0-255)</param>
    /// <param name="g">Green component (0-255)</param>
    /// <param name="b">Blue component (0-255)</param>
    /// <param name="a">Alpha component (0-255), defaults to 255 if not specified</param>
    public void SetBaseColor(byte r, byte g, byte b, byte a = 255) => SetProp(() => control.BaseColor = new Color(r, g, b, a));
}
