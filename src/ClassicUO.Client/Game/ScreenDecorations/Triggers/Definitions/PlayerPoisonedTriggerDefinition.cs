#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// The player's poisoned flag. An ambient state rather than an occurrence, so it is polled: there is
/// no event for it, and the flag is a field read on an object the client already holds.
/// </summary>
public sealed class PlayerPoisonedTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "player_poisoned";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_playerpoisoned", "Player poisoned");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Poll;

    /// <inheritdoc />
    public Type? ParameterType => null;

    /// <summary>A polled state ends when the poll stops matching, so nothing supplies a duration.</summary>
    public bool IsStateful => true;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) => new PlayerPoisonedTrigger();

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => null;
}
