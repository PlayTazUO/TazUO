using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiNiceButton(NiceButton button) : ApiUiBaseControl(button)
{
    public int ButtonParameter
    {
        get => GetProp(() => button.ButtonParameter);
        set => SetProp(() => button.ButtonParameter = value);
    }

    public bool IsSelectable
    {
        get => GetProp(() => button.IsSelectable);
        set => SetProp(() => button.IsSelectable = value);
    }

    public bool IsSelected
    {
        get => GetProp(() => button.IsSelected);
        set => SetProp(() => button.IsSelected = value);
    }

    public bool DisplayBorder
    {
        get => GetProp(() => button.DisplayBorder);
        set => SetProp(() => button.DisplayBorder = value);
    }

    public bool AlwaysShowBackground
    {
        get => GetProp(() => button.AlwaysShowBackground);
        set => SetProp(() => button.AlwaysShowBackground = value);
    }

    public string Text
    {
        get => GetProp(() => button.TextLabel.Text, string.Empty);
        set { if (value != null) SetProp(() => button.SetText(value)); }
    }

    public void SetText(string text) => Text = text;

    public ushort TextHue
    {
        get => GetProp(() => button.TextLabel.Hue);
        set => SetProp(() => button.TextLabel.Hue = value);
    }

    public ushort BackgroundHue
    {
        get => GetProp(() => button.Hue);
        set => SetProp(() => button.SetBackgroundHue(value));
    }

    public void SetBackgroundHue(ushort hue) => BackgroundHue = hue;

    /// <summary>
    /// Sets the background color of the button. Pass null to clear.
    /// </summary>
    public void SetBackgroundColor(int? r, int? g, int? b, int? a = 255) => SetProp(() =>
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

    /// <summary>
    /// Clears the background color of the button.
    /// </summary>
    public void ClearBackgroundColor() => SetProp(() => button.BackgroundColor = null);
}
