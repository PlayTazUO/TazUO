#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Overlays;

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
    }

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
            "player_poisoned",
            BuiltInProfiles.Ids.Poison,
            order: 0
        ),
        Rule(
            Ids.Earthquake,
            TazLang.Get("overlayrule_earthquake", "Earthquake"),
            "earthquake",
            BuiltInProfiles.Ids.EarthquakeRumble,
            order: 1
        )
    ];

    #endregion

    #region Private methods

    private static OverlayRule Rule(Guid id, string name, string definitionId, Guid profileId, uint order) =>
        new()
        {
            Id = id,
            Name = name,
            ProfileId = profileId,
            Trigger = new TriggerBinding { DefinitionId = definitionId },
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
