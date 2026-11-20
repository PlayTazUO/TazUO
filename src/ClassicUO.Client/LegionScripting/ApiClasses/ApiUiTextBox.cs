using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiTextBox(TextBox textBox) : ApiUiBaseControl(textBox)
{
    /// <summary>
    /// The rendered size of the text in pixels, as measured by the renderer. Read <c>MeasuredSize.X</c>
    /// for the text width and <c>MeasuredSize.Y</c> for the text height.
    /// </summary>
    public Point MeasuredSize => GetProp(() => textBox.MeasuredSize);

    public string Text
    {
        get => GetProp(() => textBox.Text, string.Empty);
        set { if (value != null) SetProp(() => textBox.Text = value); }
    }

    public int Hue
    {
        get => GetProp(() => textBox.Hue);
        set => SetProp(() => textBox.Hue = value);
    }

    public string Font
    {
        get => GetProp(() => textBox.Font, string.Empty);
        set { if (value != null) SetProp(() => textBox.Font = value); }
    }

    public float FontSize
    {
        get => GetProp(() => textBox.FontSize);
        set => SetProp(() => textBox.FontSize = value);
    }

    public bool MultiLine
    {
        get => GetProp(() => textBox.MultiLine);
        set => SetProp(() => textBox.MultiLine = value);
    }

    public void SetText(string text) => SetProp(() => textBox.SetText(text));
}
