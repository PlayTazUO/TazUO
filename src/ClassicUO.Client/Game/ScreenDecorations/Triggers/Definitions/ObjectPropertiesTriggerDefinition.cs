#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// An item's property list arriving and satisfying a filter. The general-purpose one: what it
/// watches is common enough that almost anything the server says about an item passes through it,
/// so the interesting part is entirely in the expression the rule carries.
/// </summary>
public sealed class ObjectPropertiesTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "object_properties";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_objectproperties", "Item properties");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => typeof(ObjectPropertiesParameters);

    /// <summary>A property list arrives and is gone; its parameters say how long the effect
    /// outlives it.</summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not ObjectPropertiesParameters properties
            ? throw new ArgumentException(
                $@"{nameof(ObjectPropertiesTriggerDefinition)} needs {nameof(ObjectPropertiesParameters)}",
                nameof(parameters)
            )
            : new ObjectPropertiesTrigger(properties);

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new ObjectPropertiesParameters();
}
