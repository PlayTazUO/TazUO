#nullable enable

using System;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;

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
    public ITriggerInstance Create(TriggerParameters? parameters)
    {
        if (parameters is not ChatMessageParameters chat)
            throw new ArgumentException($"{nameof(ChatMessageTriggerDefinition)} needs {nameof(ChatMessageParameters)}", nameof(parameters));

        return new ChatMessageTrigger(chat);
    }

    /// <inheritdoc />
    public TriggerParameters? CreateDefaultParameters() => new ChatMessageParameters();
}

/// <summary>
/// Watches incoming messages for one rule's pattern.
/// </summary>
internal sealed class ChatMessageTrigger : IEventTrigger
{
    #region Public events

    /// <inheritdoc />
    public event EventHandler<TriggerFiredArgs>? Fired;

    /// <summary>Never raised - a message has no end to report, and the parameters' duration is what
    /// retires it. Accessors are empty rather than the event being omitted, because the manager
    /// subscribes to every event trigger without asking which shape it is.</summary>
    public event EventHandler? Ended
    {
        add { }
        remove { }
    }

    #endregion

    #region Private members

    private readonly ChatMessageParameters _parameters;

    /// <summary>
    /// Compiled once per rule rather than per message: the pattern is fixed for the instance's
    /// lifetime and this runs on every line the client displays. Null when the mode is not
    /// <see cref="ChatMatchMode.Regex" />, or when the pattern would not compile.
    /// </summary>
    private readonly Regex? _pattern;

    #endregion

    #region Ctor

    public ChatMessageTrigger(ChatMessageParameters parameters)
    {
        _parameters = parameters;
        _pattern = CompilePattern(parameters);
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public void Attach() => EventSink.MessageReceived += OnMessageReceived;

    /// <inheritdoc />
    public void Detach() => EventSink.MessageReceived -= OnMessageReceived;

    /// <inheritdoc />
    public void Dispose() => Detach();

    #endregion

    #region Private methods

    /// <summary>
    /// Builds the regex for a regex-mode rule. A user-authored pattern is untrusted input, so a
    /// broken one disables the rule and says so rather than throwing on every line received.
    /// </summary>
    /// <param name="parameters">The rule's parameters.</param>
    /// <returns>The compiled pattern, or null where none applies.</returns>
    private static Regex? CompilePattern(ChatMessageParameters parameters)
    {
        if (parameters.Mode != ChatMatchMode.Regex || string.IsNullOrEmpty(parameters.Pattern))
            return null;

        try
        {
            return new Regex(parameters.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException e)
        {
            Log.Warn($"Overlay chat trigger pattern '{parameters.Pattern}' is not a valid regex and will never match: {e.Message}");
            return null;
        }
    }

    private void OnMessageReceived(object? sender, MessageEventArgs e)
    {
        if (!Matches(e))
            return;

        var signal = new TriggerSignal { Duration = _parameters.Duration };

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = signal });
    }

    private bool Matches(MessageEventArgs message)
    {
        string? text = message?.Text;

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(_parameters.Pattern))
            return false;

        if (_parameters.FromPlayerOnly && !IsFromPlayer(message!))
            return false;

        return _parameters.Mode switch
        {
            ChatMatchMode.Contains => text.Contains(_parameters.Pattern, StringComparison.OrdinalIgnoreCase),
            ChatMatchMode.Exact => string.Equals(text, _parameters.Pattern, StringComparison.OrdinalIgnoreCase),
            ChatMatchMode.StartsWith => text.StartsWith(_parameters.Pattern, StringComparison.OrdinalIgnoreCase),
            ChatMatchMode.Regex => _pattern?.IsMatch(text) == true,
            _ => false
        };
    }

    /// <summary>Whether the line came from the player's own mobile rather than from anything else on
    /// screen.</summary>
    private static bool IsFromPlayer(MessageEventArgs message) =>
        message.Parent != null && World.Instance?.Player != null && message.Parent.Serial == World.Instance.Player.Serial;

    #endregion
}
