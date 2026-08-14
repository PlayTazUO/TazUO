#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;

namespace ClassicUO.Game.ScreenDecorations.Triggers;

/// <summary>
/// A trigger the client knows how to build: code, not config. The catalogue of these is what the
/// rule editor offers in its trigger dropdown.
/// </summary>
public interface ITriggerDefinition
{
    /// <summary>Stable across releases - rules persist this, not the display name.</summary>
    string Id { get; }

    /// <summary>Name shown in the rule editor.</summary>
    string DisplayName { get; }

    /// <summary>Whether the manager has to sample this or wait to be told.</summary>
    TriggerKind Kind { get; }

    /// <summary>
    /// The parameter type this accepts, or null if it takes none. Drives both deserialization and
    /// the editor, which property-grids the concrete type and so shows exactly its knobs.
    /// </summary>
    Type? ParameterType { get; }

    /// <summary>
    /// Whether an occurrence of this trigger ends itself. A stateful trigger raises
    /// <see cref="IEventTrigger.Ended" />; anything else supplies a duration, either inherently or
    /// through its parameters, and the editor greys out the duration field for the former.
    /// </summary>
    bool IsStateful { get; }

    /// <summary>
    /// Builds a live instance for one rule. Two rules on the same definition get two instances,
    /// since each carries its own parameters.
    /// </summary>
    /// <param name="parameters">The rule's values for this definition, or null where it takes
    /// none.</param>
    /// <returns>The instance, watching nothing until <see cref="ITriggerInstance.Attach" />.</returns>
    /// <exception cref="ArgumentException">The parameters are not of
    /// <see cref="ParameterType" />.</exception>
    ITriggerInstance Create(TriggerParameters? parameters);

    /// <summary>
    /// A fresh parameter object for a rule newly bound to this definition, or null where it takes
    /// none. Lets the editor offer a filled-in default rather than an empty grid.
    /// </summary>
    /// <returns>The parameters, or null.</returns>
    TriggerParameters? CreateDefaultParameters();
}
