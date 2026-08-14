#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Definitions;

/// <summary>
/// A line of text matching a pattern. The parameterized shape: the pattern and how long a match
/// should hold the effect up are decisions for whoever wires the rule, so they live on the binding
/// rather than in code.
/// </summary>
public sealed class ChatMessageTriggerDefinition : ITriggerDefinition
{
    /// <inheritdoc />
    public string Id => "chat_message";

    /// <inheritdoc />
    public string DisplayName => TazLang.Get("overlaytrigger_chatmessage", "Chat message");

    /// <inheritdoc />
    public TriggerKind Kind => TriggerKind.Event;

    /// <inheritdoc />
    public Type? ParameterType => typeof(ChatMessageParameters);

    /// <summary>A message arrives and is gone; its parameters say how long the effect outlives it.</summary>
    public bool IsStateful => false;

    /// <inheritdoc />
    public ITriggerInstance Create(TriggerParameters? parameters) =>
        parameters is not ChatMessageParameters chat
            ? throw new ArgumentException($@"{nameof(ChatMessageTriggerDefinition)} needs {nameof(ChatMessageParameters)}", nameof(parameters))
            : new ChatMessageTrigger(chat);

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new ChatMessageParameters();
}
