#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>When a <see cref="BuffChangedTrigger" /> answers to its buff.</summary>
public enum BuffTriggerMode
{
    /// <summary>
    ///     The moment the buff is added. Carries a duration - the add is an instant, so how
    ///     long the effect runs is a decision for whoever wired the rule.
    /// </summary>
    Added,

    /// <summary>
    ///     The moment the buff is removed. Carries a duration for the same reason
    ///     <see cref="Added" /> does.
    /// </summary>
    Removed,

    /// <summary>
    ///     The whole span the buff is up. No duration: the buff already brackets its own
    ///     lifetime, and a configured one would either cut the effect short or hold it up after the
    ///     buff is gone.
    /// </summary>
    Active
}

/// <summary>
///     Fires on one buff being added, removed, or simply up, on the player: which buff, which of those
///     moments, and how long a momentary one runs are all decisions for whoever wires the rule.
///     <para>
///         Lives beside the trigger that reads it rather than with the config types, because nothing else can
///         interpret it: the fields mean whatever <see cref="BuffChangedTrigger" /> does with them.
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
    ///     Which moment of the buff's life raises the rule.
    ///     <para>
    ///         No grid row: <see cref="BuffTriggerPicker" /> edits it above the grid, where it can also hide
    ///         the duration field for <see cref="BuffTriggerMode.Active" />, which gives it no meaning.
    ///     </para>
    /// </summary>
    [Browsable(false)]
    [BuffTriggerEditor(nameof(BuffType), nameof(DurationSeconds))]
    [LocalizedDisplayName("overlaytrigger_buff_mode", "When")]
    [LocalizedDescription(
        "overlaytrigger_buff_mode_tooltip",
        "Which moment of the buff's life sets this effect off."
    )]
    public BuffTriggerMode Mode { get; set; } = BuffTriggerMode.Added;

    /// <summary>
    ///     The buff to watch for, by its numeric type.
    ///     <para>
    ///         A number rather than only the enum: a shard can send an id this client's
    ///         <see cref="ClassicUO.Game.Data.BuffIconType" /> has no member for, and the editor takes one
    ///         outright for those the same way it offers every name it does know.
    ///     </para>
    /// </summary>
    [Browsable(false)]
    [LocalizedDisplayName("overlaytrigger_buff_type", "Watch buff")]
    [LocalizedDescription(
        "overlaytrigger_buff_type_tooltip",
        "The buff or debuff that sets this effect off.\n"
        + "Pick one by name, or type its number if it has none."
    )]
    public short BuffType { get; set; }

    /// <summary>
    ///     How long one occurrence runs, in seconds. Ignored under
    ///     <see cref="BuffTriggerMode.Active" />, where the buff's own span decides that instead. Stored
    ///     as a number rather than a <see cref="TimeSpan" /> so the persisted form stays readable and
    ///     hand-editable.
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
    ///     The configured duration as a span, floored at zero. Hidden from the editor: it is a reading of
    ///     <see cref="DurationSeconds" />, and a property grid would otherwise offer every member of a
    ///     <see cref="TimeSpan" /> as though each were separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new BuffChangedParameters { Mode = Mode, BuffType = BuffType, DurationSeconds = DurationSeconds };

    #endregion
}
