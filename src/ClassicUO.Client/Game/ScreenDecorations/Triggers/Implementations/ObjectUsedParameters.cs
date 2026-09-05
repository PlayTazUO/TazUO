#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Fires when the player double-clicks one of several watched objects.
/// <para>
/// Lives beside the trigger that reads it rather than with the config types: the fields mean whatever
/// <see cref="ObjectUsedTrigger" /> does with them.
/// </para>
/// </summary>
public sealed class ObjectUsedParameters : TriggerParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "object_used";

    #endregion

    #region Private members

    private const float DEFAULT_DURATION_SECONDS = 2f;

    #endregion

    #region Public accessors

    /// <summary>The objects to watch, by serial. Any one of them fires the rule.</summary>
    [Browsable(false)]
    [SerialListEditor]
    [LocalizedDisplayName("overlaytrigger_objectused_serials", "Watch objects")]
    [LocalizedDescription(
        "overlaytrigger_objectused_serials_tooltip",
        "The objects that set this effect off when double-clicked - any one of them.\n"
        + "Type a serial (decimal or hex), or target the object in the world."
    )]

    // Null-normalizing: an explicit null in the persisted file would otherwise reach the trigger,
    // which enumerates this without a guard.
    public List<uint> Serials { get; set => field = value ?? []; } = [];

    /// <summary>How long one occurrence runs, in seconds.</summary>
    [LocalizedDisplayName("overlaytrigger_objectused_duration", "Lasts for (seconds)")]
    [LocalizedDescription(
        "overlaytrigger_objectused_duration_tooltip",
        "How long the effect stays on screen after the object is used.\n"
        + "Using it again restarts the clock."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    /// <see cref="DurationSeconds" /> as a span, floored at zero. Hidden from the editor, which would
    /// otherwise offer every <see cref="TimeSpan" /> member as separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new ObjectUsedParameters { Serials = [..Serials], DurationSeconds = DurationSeconds };

    #endregion
}
