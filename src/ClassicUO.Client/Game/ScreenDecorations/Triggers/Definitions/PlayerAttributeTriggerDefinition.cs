#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// A rule built from an expression over the player's own state - hit points, buffs and flags,
/// resistances, whatever <see cref="PlayerAttributeLogic.Schema" /> exposes - rather than one fixed
/// condition. The shipped poison rule is one instance of this, testing a single flag.
/// </summary>
public sealed class PlayerAttributeTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "player_attribute";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_playerattribute", "Player attribute");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Poll;

    /// <inheritdoc />
    public Type? ParameterType => typeof(PlayerAttributeParameters);

    /// <summary>A polled state ends when the poll stops matching, so nothing supplies a duration.</summary>
    public bool IsStateful => true;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not PlayerAttributeParameters attributes
            ? throw new ArgumentException(
                $@"{nameof(PlayerAttributeTriggerDefinition)} needs {nameof(PlayerAttributeParameters)}",
                nameof(parameters)
            )
            : new PlayerAttributeTrigger(attributes);

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new PlayerAttributeParameters();
}
