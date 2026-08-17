#nullable enable

using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;

namespace ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

/// <summary>
/// The expression a poll of the player's state has to satisfy, written over whatever
/// <see cref="PlayerAttributeLogic.Schema" /> exposes.
/// <para>
/// No duration, unlike the event triggers' parameters: this is sampled every reconcile pass, so the
/// occurrence's own lifetime already tracks exactly how long the expression holds. Adding one would
/// only let an effect outlive the state that started it.
/// </para>
/// </summary>
public sealed class PlayerAttributeParameters : TriggerParameters, ILogicFilterParameters
{
    #region Public constants

    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "player_attribute";

    #endregion

    #region Public accessors

    /// <summary>
    /// The expression the player's state has to satisfy. An empty tree matches everything, same as
    /// every other logic-filtered trigger - a newly bound rule fires immediately until its condition
    /// is filled in.
    /// </summary>
    [Browsable(false)]
    public LogicGroup Filter { get; set; } = new();

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public ILogicSchema FilterSchema => PlayerAttributeLogic.Schema;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public override TriggerParameters Clone() =>
        new PlayerAttributeParameters { Filter = (LogicGroup)Filter.Clone() };

    #endregion
}
