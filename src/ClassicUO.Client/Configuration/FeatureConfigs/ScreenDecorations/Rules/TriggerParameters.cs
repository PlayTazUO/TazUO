#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// Values for one parameterizable trigger. One subtype per definition that needs them, so a
/// definition's knobs are narrowly typed the same way a layer's are - a bag of strings would push
/// every type error to runtime, and the generated serializer contexts this project requires cannot
/// describe one.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChatMessageParameters), ChatMessageParameters.Discriminator)]
public abstract class TriggerParameters
{
    /// <summary>Copy, so editing a rule's parameters cannot write into another rule's.</summary>
    /// <returns>An independent copy.</returns>
    public abstract TriggerParameters Clone();
}

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
/// </summary>
public sealed class ChatMessageParameters : TriggerParameters
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "chat_message";

    private const float DEFAULT_DURATION_SECONDS = 3f;

    /// <summary>How <see cref="Pattern" /> is compared against the line.</summary>
    [Description(
        "How the pattern is compared against the line. Regex takes a\n"
        + ".NET regular expression; the rest are plain text. All four\n"
        + "ignore case."
    )]
    public ChatMatchMode Mode { get; set; } = ChatMatchMode.Contains;

    /// <summary>What to look for. Case-insensitive in every mode.</summary>
    [Description("The text, or regular expression, to look for in each line.")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// How long one match runs for, in seconds. Stored as a number rather than a
    /// <see cref="TimeSpan" /> so the persisted form stays readable and hand-editable - and so the
    /// editor offers one field rather than every member of a span.
    /// </summary>
    [Description(
        "How long the effect runs for after a match, in seconds. A\n"
        + "message has no length of its own, so this is the only thing\n"
        + "that decides when the effect ends."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>Only messages the player themselves spoke, rather than any line on screen.</summary>
    [Description("Match only lines the player character spoke, not every line on screen.")]
    public bool FromPlayerOnly { get; set; }

    /// <summary>
    /// The configured duration as a span, floored at zero. Hidden from the editor: it is a reading
    /// of <see cref="DurationSeconds" />, and a property grid would otherwise offer every member of
    /// a <see cref="TimeSpan" /> as though each were separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new ChatMessageParameters
        {
            Mode = Mode,
            Pattern = Pattern,
            DurationSeconds = DurationSeconds,
            FromPlayerOnly = FromPlayerOnly
        };
}
