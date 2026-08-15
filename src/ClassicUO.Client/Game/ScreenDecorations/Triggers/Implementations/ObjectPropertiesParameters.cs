#nullable enable

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// Fires when an item's property list arrives and satisfies an expression written over its serial,
/// its name and its properties text.
/// <para>
/// The filter is a tree rather than a row of fields because the useful questions are compound - a
/// name that says one of two things, on anything but a known set of serials - and a fixed set of
/// boxes can only ever offer one shape of them.
/// </para>
/// </summary>
public sealed class ObjectPropertiesParameters : TriggerParameters, ILogicFilterParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "object_properties";

    #endregion

    #region Private members

    private const float DEFAULT_DURATION_SECONDS = 3f;

    #endregion

    #region Public accessors

    /// <summary>
    /// The expression an incoming property list has to satisfy. An empty tree matches everything,
    /// which is what a newly bound rule starts as - and why the duration below is the first thing
    /// worth setting.
    /// </summary>
    [Browsable(false)]
    public LogicGroup Filter { get; set; } = new();

    /// <summary>
    /// How long one match runs for, in seconds. A property list arrives and is gone, so as with the
    /// chat trigger this is the only thing that decides when the effect ends.
    /// </summary>
    [LocalizedDisplayName("overlaytrigger_opl_duration", "Duration (s)")]
    [LocalizedDescription(
        "overlaytrigger_opl_duration_tooltip",
        "How long the effect runs for after a match,\n"
        + "in seconds. A property list has no length of its own,\n"
        + "so this is the only thing that decides when the effect ends."
    )]
    public float DurationSeconds { get; set; } = DEFAULT_DURATION_SECONDS;

    /// <summary>
    /// The configured duration as a span, floored at zero. Hidden from the editor for the same
    /// reason the chat trigger's is: it is a reading of <see cref="DurationSeconds" />, and a grid
    /// would otherwise offer every member of a <see cref="TimeSpan" /> as separately settable.
    /// </summary>
    [JsonIgnore]
    [Browsable(false)]
    public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(DurationSeconds, 0f));

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public ILogicSchema FilterSchema => ObjectPropertiesLogic.Schema;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new ObjectPropertiesParameters
        {
            Filter = (LogicGroup)Filter.Clone(),
            DurationSeconds = DurationSeconds
        };

    #endregion
}
