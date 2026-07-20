using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiLabel(Label label) : ApiUiBaseControl(label)
{
    public string Text
    {
        get => GetProp(() => label.Text, string.Empty);
        set { if (value != null) SetProp(() => label.Text = value); }
    }

    public ushort Hue
    {
        get => GetProp(() => label.Hue);
        set => SetProp(() => label.Hue = value);
    }
}
