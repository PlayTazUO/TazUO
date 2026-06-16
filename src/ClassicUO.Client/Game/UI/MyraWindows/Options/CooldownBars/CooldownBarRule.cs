using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

namespace ClassicUO.Game.UI.MyraWindows.Options.CooldownBars;

public enum CooldownTriggerMessageType
{
    All = 0,
    Self = 1,
    Other = 2
}

public class CooldownBarRule : IRule, INotifyPropertyChanged
{
    public uint Order { get; set => SetField(ref field, value); }
    public bool Enabled { get; set => SetField(ref field, value); } = true;
    public bool CanEdit { get; set => SetField(ref field, value); } = true;
    public bool CanDelete { get; set => SetField(ref field, value); } = true;

    public string Name { get; set => SetField(ref field, value); } = string.Empty;
    public uint Cooldown { get; set => SetField(ref field, value); }
    public ushort Hue { get; set => SetField(ref field, value); }
    public string TriggerMessage { get; set => SetField(ref field, value); } = string.Empty;
    public CooldownTriggerMessageType TriggerMessageType { get; set => SetField(ref field, value); } = CooldownTriggerMessageType.All;

    public static CooldownBarRule FromLegacyCondition(uint order, CoolDownBar.CoolDownConditionData data)
    {
        CooldownTriggerMessageType trigger = data.message_type switch
        {
            0 => CooldownTriggerMessageType.All,
            1 => CooldownTriggerMessageType.Self,
            2 => CooldownTriggerMessageType.Other,
            _ => CooldownTriggerMessageType.All
        };

        return new CooldownBarRule
        {
            Order = order,
            Name = data.label,
            TriggerMessage = data.trigger,
            TriggerMessageType = trigger,
            Hue = data.hue,
            Cooldown = (uint)data.cooldown
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}
