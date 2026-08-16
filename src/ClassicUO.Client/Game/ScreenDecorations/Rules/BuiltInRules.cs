#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Game.ScreenDecorations.Rules;

/// <summary>
/// The rules shipped with the client.
/// <para>
/// Resolved from code every session rather than seeded into config, which is what makes them
/// durable: they are always present and always wired the way this build intends. Only their enabled
/// state and table position belong to the user, and those are stored as overrides. A user who wants
/// one wired differently copies it, which produces an ordinary rule.
/// </para>
/// </summary>
public static class BuiltInRules
{
    #region Public accessors

    /// <summary>
    /// Stable identities. Persisted by the overrides that switch these off or move them, so never
    /// reuse or renumber them.
    /// </summary>
    public static class Ids
    {
        public static readonly Guid PlayerPoisoned = new("3f2a1d64-7c93-4f0e-9b7a-1d6c2e58a410");
        public static readonly Guid Earthquake = new("8c5b0f21-4ae6-4d38-8f14-6b90c7d2e553");
        public static readonly Guid PlayerDead = new("a91e7d05-2b64-4c8f-9e13-5f7a0c6d84b2");
    }

    #endregion

    #region Private members

    /// <summary>The client's earthquake sound, in its sound data.</summary>
    private const int EARTHQUAKE_SOUND_INDEX = 755;

    /// <summary><see cref="MobileAttributeLogic" />'s poison flag, by its persisted key.</summary>
    private const string POISONED_FIELD = "ispoisoned";

    /// <summary><see cref="MobileAttributeLogic" />'s death flag, by its persisted key.</summary>
    private const string DEAD_FIELD = "isdead";

    #endregion

    #region Public methods

    /// <summary>
    /// Fresh instances of every shipped rule, in their default order. New objects each call: the
    /// caller stamps the user's overrides onto them, and those must not accumulate across passes.
    /// </summary>
    /// <returns>The rules.</returns>
    public static IReadOnlyList<OverlayRule> Create() =>
    [
        Rule(
            Ids.PlayerPoisoned,
            TazLang.Get("overlayrule_playerpoisoned", "Player poisoned"),
            PlayerAttributeParameters.Discriminator,
            PlayerFlag(POISONED_FIELD),
            BuiltInProfiles.Ids.Poison,
            order: 0
        ),
        Rule(
            Ids.PlayerDead,
            TazLang.Get("overlayrule_playerdead", "Player dead"),
            PlayerAttributeParameters.Discriminator,
            PlayerFlag(DEAD_FIELD),
            BuiltInProfiles.Ids.Death,
            order: 1
        ),
        Rule(
            Ids.Earthquake,
            TazLang.Get("overlayrule_earthquake", "Earthquake"),
            SoundPlayedParameters.Discriminator,
            EarthquakeSound(),
            BuiltInProfiles.Ids.EarthquakeRumble,
            order: 2
        )
    ];

    #endregion

    #region Private methods

    /// <summary>
    /// The client's earthquake sound. Everything else is left at its default, which is what the
    /// dedicated earthquake trigger did before this became one instance of the generic one: the band
    /// is the client's own audible range and strength falls off squarely across it.
    /// </summary>
    /// <returns>The parameters.</returns>
    private static SoundPlayedParameters EarthquakeSound() =>
        new() { SoundIndex = EARTHQUAKE_SOUND_INDEX };

    /// <summary>
    /// One boolean field of the player's state, as the single-condition expression that tests it.
    /// The shape every shipped state rule takes: the generic trigger's expressiveness is for the
    /// user, and a rule that ships wants the plainest tree that says what it means.
    /// </summary>
    /// <param name="field">The schema field to test, by its persisted key.</param>
    /// <returns>The parameters.</returns>
    private static PlayerAttributeParameters PlayerFlag(string field) =>
        new()
        {
            Filter = new LogicGroup
            {
                Children =
                {
                    new LogicCondition
                    {
                        Field = field,
                        Operator = LogicOperator.Is,
                        Value = bool.TrueString
                    }
                }
            }
        };

    private static OverlayRule Rule(
        Guid id,
        string name,
        string definitionId,
        TriggerParameters parameters,
        Guid profileId,
        uint order
    ) =>
        new()
        {
            Id = id,
            Name = name,
            ProfileId = profileId,
            Trigger = new TriggerBinding { DefinitionId = definitionId, Parameters = parameters },
            Order = order,

            // Opt-in like every other part of this system: these effects obscure and displace the
            // world, so a clean profile must not start distorting anyone's screen on its own.
            Enabled = false,
            IsBuiltIn = true,
            CanEdit = false,
            CanDelete = false
        };

    #endregion
}
