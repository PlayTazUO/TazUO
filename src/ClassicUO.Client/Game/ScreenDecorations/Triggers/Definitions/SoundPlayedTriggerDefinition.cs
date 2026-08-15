#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// Any sound the client plays, scaled by how near it was. The shipped earthquake rule is one
/// instance of this with the quake's index filled in, which is all a sound-specific trigger ever
/// was.
/// </summary>
public sealed class SoundPlayedTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "sound_played";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_soundplayed", "Sound played");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => typeof(SoundPlayedParameters);

    /// <summary>A sound starts and nothing announces its end; its parameters say how long the effect
    /// outlives it.</summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not SoundPlayedParameters sound
            ? throw new ArgumentException($@"{nameof(SoundPlayedTriggerDefinition)} needs {nameof(SoundPlayedParameters)}", nameof(parameters))
            : new SoundPlayedTrigger(sound);

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new SoundPlayedParameters();
}
