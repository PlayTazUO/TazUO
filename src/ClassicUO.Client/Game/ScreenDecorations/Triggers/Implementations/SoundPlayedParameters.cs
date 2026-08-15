#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Fires on one sound being played near the player: which sound, how near it has to be, and how its
/// nearness turns into strength are all decisions for whoever wires the rule.
/// <para>
/// Lives beside the trigger that reads it rather than with the config types, because nothing else can
/// interpret it: the fields mean whatever <see cref="SoundPlayedTrigger" /> does with them.
/// </para>
/// </summary>
public sealed class SoundPlayedParameters : TriggerParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "sound_played";

    #endregion

    #region Private members

    private const float DEFAULT_DURATION_SECONDS = 3f;
    private const float DEFAULT_MIN_INTENSITY = 0.25f;
    private const float DEFAULT_CURVE_EXPONENT = 2f;

    #endregion

    #region Public accessors

    /// <summary>
    /// The sound to watch for, by its index in the client's sound data.
    /// <para>
    /// A number rather than an enum: what any given index is depends on the shard's data files, so
    /// there is no set of names this client could name in code and still be right about. The editor
    /// reads the names out of the loaded data instead, and takes a raw number for anything it cannot
    /// find one for.
    /// </para>
    /// </summary>
    [Browsable(false)]
    [SoundIndexEditor]
    [LocalizedDisplayName("overlaytrigger_sound_index", "Play on sound")]
    [LocalizedDescription(
        "overlaytrigger_sound_index_tooltip",
        "The sound that sets this effect off.\n"
        + "Pick one by name, or type its number if it has none.\n"
        + "Press Play to hear the current choice."
    )]
    public int SoundIndex { get; set; }

    /// <summary>
    /// Nearest the sound may be and still count, in tiles. Above zero for an effect that should only
    /// answer to something happening at a distance.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_sound_mindistance", "Ignore closer than (tiles)")]
    [LocalizedDescription(
        "overlaytrigger_sound_mindistance_tooltip",
        "Sounds nearer than this are ignored.\n"
        + "Use it for an effect that should only answer to distant events.\n"
        + "Leave at 0 to include sounds right beside you."
    )]
    public int MinDistance { get; set; }

    /// <summary>
    /// Furthest the sound may be and still count, in tiles. Zero means the client's own audible
    /// range, which is what the player can actually hear - a fixed number here would either cut the
    /// effect off before the sound stops or keep it going after.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_sound_maxdistance", "Ignore further than (tiles)")]
    [LocalizedDescription(
        "overlaytrigger_sound_maxdistance_tooltip",
        "Sounds beyond this are ignored.\n"
        + "Leave at 0 to match how far you can hear."
    )]
    public int MaxDistance { get; set; }

    /// <summary>
    /// How nearness within the band becomes strength.
    /// <para>
    /// No grid row: <see cref="FalloffPicker" /> edits it above the grid, where there is room for the
    /// line of text explaining the chosen curve and for the power that one of them needs.
    /// </para>
    /// </summary>
    [Browsable(false)]
    [FalloffEditor(
        nameof(CurveExponent),
        nameof(MaxIntensity),
        nameof(MinIntensity)
    )]
    [LocalizedDisplayName("overlaytrigger_sound_curve", "Fade with distance")]
    [LocalizedDescription(
        "overlaytrigger_sound_curve_tooltip",
        "How much weaker the effect gets as the sound gets further away."
    )]
    public FalloffCurve Curve { get; set; } = FalloffCurve.Quadratic;

    /// <summary>
    /// The power nearness is raised to under <see cref="FalloffCurve.Custom" />, for a curve none of
    /// the named ones give. Ignored by every other curve.
    /// <para>
    /// No row of its own: it is meaningless for every curve but one, and a knob that does nothing
    /// five times out of six reads as a broken one. <see cref="FalloffPicker" /> shows it under the
    /// curve, and only where it applies.
    /// </para>
    /// </summary>
    [Browsable(false)]
    [LocalizedDisplayName("falloff_power", "Power")]
    [LocalizedDescription(
        "falloff_power_tooltip",
        "The exponent nearness is raised to.\n"
        + "Above 1 concentrates strength near the player,\n"
        + "below 1 spreads it towards the far edge."
    )]
    public float CurveExponent { get; set; } = DEFAULT_CURVE_EXPONENT;

    /// <summary>Strength at the near edge of the band.</summary>
    [LocalizedDisplayName("overlaytrigger_sound_maxintensity", "Strength when closest")]
    [Browsable(false)]
    [LocalizedDescription(
        "overlaytrigger_sound_maxintensity_tooltip",
        "Strength for a sound as close as it can get, from 0 to 1.\n"
        + "Set it below the furthest strength to invert the effect."
    )]
    public float MaxIntensity { get; set; } = 1f;

    /// <summary>
    /// Strength at the far edge of the band. Rarely zero: a sound the player can still hear is worth
    /// showing, it just should not compete with one right beside them.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_sound_minintensity", "Strength when furthest")]
    [Browsable(false)]
    [LocalizedDescription(
        "overlaytrigger_sound_minintensity_tooltip",
        "Strength for a sound at the edge of the range, from 0 to 1.\n"
        + "Scales the effect's own settings rather than replacing them."
    )]
    public float MinIntensity { get; set; } = DEFAULT_MIN_INTENSITY;

    /// <summary>
    /// How long one occurrence runs, in seconds. Stored as a number rather than a
    /// <see cref="TimeSpan" /> so the persisted form stays readable and hand-editable - and so the
    /// editor offers one field rather than every member of a span.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_sound_duration", "Lasts for (seconds)")]
    [LocalizedDescription(
        "overlaytrigger_sound_duration_tooltip",
        "How long the effect stays on screen after the sound plays.\n"
        + "Hearing the sound again restarts the clock."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    /// The configured duration as a span, floored at zero. Hidden from the editor: it is a reading of
    /// <see cref="DurationSeconds" />, and a property grid would otherwise offer every member of a
    /// <see cref="TimeSpan" /> as though each were separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new SoundPlayedParameters
        {
            SoundIndex = SoundIndex,
            MinDistance = MinDistance,
            MaxDistance = MaxDistance,
            Curve = Curve,
            CurveExponent = CurveExponent,
            MinIntensity = MinIntensity,
            MaxIntensity = MaxIntensity,
            DurationSeconds = DurationSeconds
        };

    #endregion
}
