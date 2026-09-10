#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

public sealed class ObjectUsedTriggerDefinition : ITriggerDefinition
{
    public string Id => "object_used";
    public string DisplayName => TazLang.Get("overlaytrigger_objectused", "Object used");
    public TriggerKind Kind => TriggerKind.Event;
    public Type? ParameterType => typeof(ObjectUsedParameters);
    public bool IsStateful => false;

    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not ObjectUsedParameters objectUsed
            ? throw new ArgumentException($@"{nameof(ObjectUsedTriggerDefinition)} needs {nameof(ObjectUsedParameters)}", nameof(parameters))
            : new ObjectUsedTrigger(objectUsed);

    public TriggerParameters? CreateDefaultParameters() => new ObjectUsedParameters();
}
