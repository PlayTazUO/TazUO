#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>When a <see cref="BuffChangedTrigger" /> answers to its buff.</summary>
public enum BuffTriggerMode
{
    /// <summary>The moment the buff is added. An instant, so it carries a duration.</summary>
    Added,

    /// <summary>The moment the buff is removed. Carries a duration, as <see cref="Added" /> does.</summary>
    Removed,

    /// <summary>
    ///     The whole span the buff is up. One buff, and no duration - the buff brackets its own lifetime.
    /// </summary>
    Active
}

/// <summary>
///     Fires on a buff being added to, removed from, or simply up on the player.
///     <para>
///         Lives beside the trigger that reads it rather than with the config types: the fields mean
///         whatever <see cref="BuffChangedTrigger" /> does with them.
///     </para>
/// </summary>
public sealed class BuffChangedParameters : TriggerParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "buff_changed";

    #endregion

    #region Private members

    private const float DEFAULT_DURATION_SECONDS = 3f;

    #endregion

    #region Public accessors

    /// <summary>
    ///     Which moment of the buff's life raises the rule. No grid row:
    ///     <see cref="BuffTriggerPicker" /> edits it above the grid, along with the fields it governs.
    /// </summary>
    [Browsable(false)]
    [BuffTriggerEditor(nameof(BuffTypes), nameof(DurationSeconds))]
    [LocalizedDisplayName("overlaytrigger_buff_mode", "When")]
    [LocalizedDescription(
        "overlaytrigger_buff_mode_tooltip",
        "Which moment of the buff's life sets this effect off."
    )]
    public BuffTriggerMode Mode { get; set; } = BuffTriggerMode.Added;

    /// <summary>
    ///     The buffs to watch for, by numeric type. Any one fires the rule, except under
    ///     <see cref="BuffTriggerMode.Active" />, which honours the first alone.
    ///     <para>
    ///         Numbers rather than the enum: a shard can send an ID
    ///         <see cref="ClassicUO.Game.Data.BuffIconType" /> has no member for.
    ///     </para>
    /// </summary>
    [Browsable(false)]
    [LocalizedDisplayName("overlaytrigger_buff_type", "Watch buffs")]
    [LocalizedDescription(
        "overlaytrigger_buff_type_tooltip",
        "The buffs or debuffs that set this effect off - any one of them.\n"
        + "Pick by name, or type a number if it has none.\n"
        + "Watching for a buff being active takes one buff, whose own span the effect follows."
    )]

    // Null-normalizing: an explicit null in the persisted file would otherwise reach the trigger,
    // which enumerates this without a guard.
    public List<short> BuffTypes { get; set => field = value ?? []; } = [];

    /// <summary>
    ///     How long one occurrence runs, in seconds. Ignored under
    ///     <see cref="BuffTriggerMode.Active" />. A number rather than a <see cref="TimeSpan" /> to keep the
    ///     persisted form hand-editable.
    /// </summary>
    [Browsable(false)]
    [LocalizedDisplayName("overlaytrigger_buff_duration", "Lasts for (seconds)")]
    [LocalizedDescription(
        "overlaytrigger_buff_duration_tooltip",
        "How long the effect runs for after the buff is added or removed.\n"
        + "Not used while watching for the buff being active - its own span decides that."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    ///     The configured duration as a span, floored at zero. Hidden from the editor, which would
    ///     otherwise offer every <see cref="TimeSpan" /> member as separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new BuffChangedParameters { Mode = Mode, BuffTypes = [..BuffTypes], DurationSeconds = DurationSeconds };

    #endregion
}
