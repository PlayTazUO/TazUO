#nullable enable

using System;
using System.Text.RegularExpressions;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

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

    #region Internal methods

    /// <summary>
    /// Whether a line satisfies the rule's pattern. The decision alone, with no message object and
    /// no world behind it, so the match semantics can be exercised directly.
    /// </summary>
    /// <param name="parameters">The rule's parameters.</param>
    /// <param name="pattern">The compiled regex, where the mode uses one.</param>
    /// <param name="text">The line received.</param>
    /// <returns>Whether it matches.</returns>
    internal static bool MatchesText(ChatMessageParameters parameters, Regex? pattern, string? text)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(parameters.Pattern))
            return false;

        // The regex carries its own case handling, baked in when it was compiled; the plain modes
        // take it from the same flag through the comparison.
        StringComparison comparison = parameters.Comparison;

        return parameters.Mode switch
        {
            ChatMatchMode.Contains => text.Contains(parameters.Pattern, comparison),
            ChatMatchMode.Exact => string.Equals(text, parameters.Pattern, comparison),
            ChatMatchMode.StartsWith => text.StartsWith(parameters.Pattern, comparison),
            ChatMatchMode.Regex => pattern?.IsMatch(text) == true,
            _ => false
        };
    }

    /// <summary>
    /// Builds the regex for a regex-mode rule. A user-authored pattern is untrusted input, so a
    /// broken one disables the rule and says so rather than throwing on every line received.
    /// </summary>
    /// <param name="parameters">The rule's parameters.</param>
    /// <returns>The compiled pattern, or null where none applies.</returns>
    internal static Regex? CompilePattern(ChatMessageParameters parameters)
    {
        if (parameters.Mode != ChatMatchMode.Regex || string.IsNullOrEmpty(parameters.Pattern))
            return null;

        RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        if (!parameters.CaseSensitive)
            options |= RegexOptions.IgnoreCase;

        try
        {
            return new Regex(parameters.Pattern, options);
        }
        catch (ArgumentException e)
        {
            Log.Warn($"Overlay chat trigger pattern '{parameters.Pattern}' is not a valid regex and will never match: {e.Message}");
            return null;
        }
    }

    #endregion

    #region Private methods

    private void OnMessageReceived(object? sender, MessageEventArgs e)
    {
        if (!Matches(e))
            return;

        var signal = new TriggerSignal { Duration = _parameters.Duration };

        Fired?.Invoke(this, new TriggerFiredArgs { Signal = signal });
    }

    private bool Matches(MessageEventArgs? message)
    {
        if (message == null)
            return false;

        // Cheapest test first, and the only one needing the message itself rather than its text.
        if (_parameters.FromPlayerOnly && !IsFromPlayer(message))
            return false;

        return MatchesText(_parameters, _pattern, message.Text);
    }

    /// <summary>Whether the line came from the player's own mobile rather than from anything else on
    /// screen.</summary>
    private static bool IsFromPlayer(MessageEventArgs message) =>
        message.Parent != null && World.Instance?.Player != null && message.Parent.Serial == World.Instance.Player.Serial;

    #endregion
}
