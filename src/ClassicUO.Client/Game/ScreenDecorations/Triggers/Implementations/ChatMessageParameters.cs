#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>How a chat trigger compares an incoming line against its pattern.</summary>
public enum ChatMatchMode
{
    /// <summary>The line contains the pattern anywhere in it.</summary>
    Contains,

    /// <summary>The line is exactly the pattern.</summary>
    Exact,

    /// <summary>The line begins with the pattern.</summary>
    StartsWith,

    /// <summary>The pattern is a .NET regular expression.</summary>
    Regex
}

/// <summary>
/// Fires on a chat line matching a pattern. Carries a duration because a message has no natural
/// length - the line arrives and is gone, so how long the effect it raises should run is a decision
/// for whoever wired the rule.
/// <para>
/// Lives beside the trigger that reads it rather than with the config types, because nothing else
/// can interpret it: the fields mean whatever <see cref="ChatMessageTrigger" /> does with them.
/// </para>
/// </summary>
public sealed class ChatMessageParameters : TriggerParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "chat_message";

    #endregion

    #region Private members

    private const float DEFAULT_DURATION_SECONDS = 3f;

    #endregion

    #region Public accessors

    /// <summary>How <see cref="Pattern" /> is compared against the line.</summary>
    [LocalizedDisplayName("overlaytrigger_chat_mode", "Match")]
    [LocalizedDescription(
        "overlaytrigger_chat_mode_tooltip",
        "How the pattern is compared against the line.\n"
        + "Regex takes a .NET regular expression;\n"
        + "the rest are plain text."
    )]
    public ChatMatchMode Mode { get; set; } = ChatMatchMode.Contains;

    /// <summary>What to look for.</summary>
    [LocalizedDisplayName("overlaytrigger_chat_pattern", "Pattern")]
    [LocalizedDescription(
        "overlaytrigger_chat_pattern_tooltip",
        "The text, or regular expression, to look for in each line."
    )]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether the comparison respects capitalisation. Applies to every mode, the regex included -
    /// case is a property of the comparison rather than of any one way of making it.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_chat_casesensitive", "Case sensitive")]
    [LocalizedDescription(
        "overlaytrigger_chat_casesensitive_tooltip",
        "Match capitalisation exactly. Applies to every match mode,\n"
        + "regular expressions included. Off by default:\n"
        + "what the server sends is rarely capitalised the way\n"
        + "you would type it."
    )]
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// How long one match runs for, in seconds. Stored as a number rather than a
    /// <see cref="TimeSpan" /> so the persisted form stays readable and hand-editable - and so the
    /// editor offers one field rather than every member of a span.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_chat_duration", "Duration (s)")]
    [LocalizedDescription(
        "overlaytrigger_chat_duration_tooltip",
        "How long the effect runs for after a match,\n"
        + "in seconds. A message has no length of its own,\n"
        + "so this is the only thing that decides when the effect ends."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>Only messages the player themselves spoke, rather than any line on screen.</summary>
    [LocalizedDisplayName("overlaytrigger_chat_fromplayeronly", "From the player only")]
    [LocalizedDescription(
        "overlaytrigger_chat_fromplayeronly_tooltip",
        "Match only lines the player character spoke,\n"
        + "not every line on screen."
    )]
    public bool FromPlayerOnly { get; set; }

    /// <summary>
    /// The configured duration as a span, floored at zero. Hidden from the editor: it is a reading
    /// of <see cref="DurationSeconds" />, and a property grid would otherwise offer every member of
    /// a <see cref="TimeSpan" /> as though each were separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    /// <summary>How string comparisons should be made, given <see cref="CaseSensitive" />.</summary>
    [JsonIgnore]
    [Browsable(false)]
    public StringComparison Comparison =>
        CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new ChatMessageParameters
        {
            Mode = Mode,
            Pattern = Pattern,
            CaseSensitive = CaseSensitive,
            DurationSeconds = DurationSeconds,
            FromPlayerOnly = FromPlayerOnly
        };

    #endregion
}
