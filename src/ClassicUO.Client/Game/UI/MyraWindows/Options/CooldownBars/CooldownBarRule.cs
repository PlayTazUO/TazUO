using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.CooldownBars;

public enum CooldownTriggerMessageType
{
    All,
    Self,
    Other
}

public class CooldownBarRule : IRule
{
    public uint Order { get; } = 0;
    public bool Enabled { get; set; } = true;
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = true;

    public string Name { get; set; } = string.Empty;
    public uint Cooldown { get; set; }
    public ushort Hue { get; set; }
    public CooldownTriggerMessageType TriggerMessageType { get; set; } = CooldownTriggerMessageType.All;

    public Widget DisplayComponent => OptionTabCommons.StyledStackPanel(
        Orientation.Horizontal,
        new MyraLabel(Order.ToString(), MyraLabel.TextStyle.P),
        new MyraLabel(Name, MyraLabel.TextStyle.P),
        new MyraLabel(Cooldown.ToString(), MyraLabel.TextStyle.P),
        new MyraLabel(Hue.ToString(), MyraLabel.TextStyle.P),
        new MyraLabel(TriggerMessageType.ToString(), MyraLabel.TextStyle.P)
    );

    public static CooldownBarRule FromLegacyCondition(CoolDownBar.CoolDownConditionData data)
    {
        CooldownTriggerMessageType trigger = data.trigger?.ToLower() switch
        {
            "all" => CooldownTriggerMessageType.All,
            "self" => CooldownTriggerMessageType.Self,
            "other" => CooldownTriggerMessageType.Other,
            _ => CooldownTriggerMessageType.All
        };


        return new CooldownBarRule { Name = data.label, TriggerMessageType = trigger, Hue = data.hue, Cooldown = (uint)data.cooldown };
    }
}
