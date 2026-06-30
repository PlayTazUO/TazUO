using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;

/// <summary>Controls which incoming messages trigger a cooldown bar</summary>
public enum CooldownTriggerMessageType
{
    /// <summary>Trigger on messages from any source</summary>
    All = 0,
    /// <summary>Trigger only on messages originating from the player character</summary>
    Self = 1,
    /// <summary>Trigger only on messages originating from other entities</summary>
    Other = 2
}

/// <summary>
/// A single cooldown-bar rule: defines the display name, duration, color, and trigger conditions
/// for a bar shown when a matching message is received
/// </summary>
public class CooldownBarRule : IRule, INotifyPropertyChanged
{
    /// <inheritdoc/>
    public uint Order { get; set => SetField(ref field, value); }

    /// <inheritdoc/>
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    /// <inheritdoc/>
    public bool CanEdit { get; set => SetField(ref field, value); } = true;

    /// <inheritdoc/>
    public bool CanDelete { get; set => SetField(ref field, value); } = true;

    /// <summary>Display name shown on the cooldown bar</summary>
    public string Name { get; set => SetField(ref field, value); } = string.Empty;

    /// <summary>Duration of the cooldown bar in seconds</summary>
    public uint Cooldown { get; set => SetField(ref field, value); }

    /// <summary>The cooldown bar's hue (color)</summary>
    public ushort Hue { get; set => SetField(ref field, value); }

    /// <summary>The message substring that triggers this cooldown bar when received</summary>
    public string TriggerMessage { get; set => SetField(ref field, value); } = string.Empty;

    /// <summary>Determines which message sources can trigger this rule</summary>
    public CooldownTriggerMessageType TriggerMessageType { get; set => SetField(ref field, value); } = CooldownTriggerMessageType.All;

    /// <summary>
    /// When <see langword="true"/>, triggering this rule restarts an already-running bar of the same name
    /// </summary>
    public bool ReplaceExisting { get; set => SetField(ref field, value); } = true;

    /// <summary>
    /// Converts a legacy <see cref="CoolDownBar.CoolDownConditionData"/> record into a
    /// <see cref="CooldownBarRule"/> for use in the rulebase editor
    /// </summary>
    /// <param name="order">The sort position to assign to the new rule</param>
    /// <param name="data">The legacy condition data to convert</param>
    /// <returns>A new <see cref="CooldownBarRule"/> populated from <paramref name="data"/></returns>
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
            Cooldown = (uint)data.cooldown,
            ReplaceExisting = data.replace_if_exists
        };
    }

    /// <inheritdoc/>
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
