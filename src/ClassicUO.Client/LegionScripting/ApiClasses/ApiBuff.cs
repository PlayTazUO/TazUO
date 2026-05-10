using ClassicUO.Game.Data;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiBuff(BuffIcon icon)
{
    public readonly ushort Graphic = icon.Graphic;

    public readonly string Text = icon.Text;

    public readonly long Timer = icon.Timer;

    public readonly BuffIconType Type = icon.Type;

    public readonly string Title = icon.Title;
}
