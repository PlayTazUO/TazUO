#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// One buff or debuff being added to, or removed from, the player.
/// </summary>
public sealed class BuffChangedTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "buff_changed";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_buffchanged", "Buff changed");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => typeof(BuffChangedParameters);

    /// <summary>
    /// Mixed rather than one answer: <see cref="BuffTriggerMode.Active" /> is stateful, but
    /// <see cref="BuffTriggerMode.Added" /> and <see cref="BuffTriggerMode.Removed" /> are momentary
    /// and carry their own duration. False, since two of the three modes need it -
    /// <see cref="ClassicUO.Game.UI.MyraWindows.Widgets.BuffTriggerPicker" /> hides the duration field itself for the one that
    /// does not, rather than relying on this flag.
    /// </summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not BuffChangedParameters buff
            ? throw new ArgumentException($@"{nameof(BuffChangedTriggerDefinition)} needs {nameof(BuffChangedParameters)}", nameof(parameters))
            : new BuffChangedTrigger(buff);

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new BuffChangedParameters();
}
