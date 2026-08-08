#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// The ground moving, taken from the earthquake sound the server plays.
/// </summary>
public sealed class EarthquakeTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "earthquake";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_earthquake", "Earthquake sound");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => null;

    /// <summary>Nothing announces the end of a quake; the signal's own duration retires it.</summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) => new EarthquakeTrigger();

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => null;
}
