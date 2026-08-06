#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

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

/// <summary>Reports the poison flag for as long as it is set.</summary>
internal sealed class PlayerPoisonedTrigger : IPollingTrigger
{
    /// <summary>Nothing to hook: the state is read where it lives.</summary>
    public void Attach()
    {
    }

    /// <inheritdoc />
    public void Detach()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public TriggerSignal? Sample() =>
        World.Instance?.Player?.IsPoisoned == true ? TriggerSignal.Default : null;
}
