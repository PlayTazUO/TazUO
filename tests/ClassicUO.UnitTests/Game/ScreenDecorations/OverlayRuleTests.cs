using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Rules are wiring: a trigger, a look, and where the row sits. The shipped ones are resolved
/// from code every session so that they stay correct and cannot be lost, with only the parts the
/// user actually changed stored against them.
/// </summary>
public class OverlayRuleTests
{
    private static DecorationSettings RoundTrip(DecorationSettings config)
    {
        return JsonSerializer.Deserialize(
            JsonSerializer.Serialize(config, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations),
            ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations
        );
    }

    private static OverlayRule UserRule(string name, uint order)
    {
        return new OverlayRule
        {
            Name = name,
            Order = order,
            ProfileId = BuiltInProfiles.Ids.Fog,
            Trigger = new TriggerBinding { DefinitionId = "player_attribute" }
        };
    }

    [Fact]
    public void ShippedRulesAreAlwaysPresent()
    {
        var config = new DecorationSettings();

        List<OverlayRule> resolved = config.Overlays.ResolveRules();

        resolved.Should().Contain(rule => rule.Id == BuiltInRules.Ids.PlayerPoisoned);
        resolved.Should().Contain(rule => rule.Id == BuiltInRules.Ids.Earthquake);
        resolved.Where(rule => rule.IsBuiltIn).Should().OnlyContain(rule => !rule.CanEdit && !rule.CanDelete);
    }

    /// <summary>
    /// Every shipped rule has to name a trigger this build knows and a look it can find, or it is
    /// a row that silently does nothing.
    /// </summary>
    [Fact]
    public void ShippedRulesResolveTheirTriggerAndProfile()
    {
        var config = new DecorationSettings();

        foreach (OverlayRule rule in config.Overlays.ResolveRules().Where(rule => rule.IsBuiltIn))
        {
            TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId).Should().NotBeNull();
            config.Overlays.FindProfile(rule.ProfileId).Should().NotBeNull();
        }
    }

    /// <summary>
    /// A shipped rule that has never been touched stores nothing at all; switching one off stores
    /// only that. Deleting is not offered, which is what makes the built-ins dependable.
    /// </summary>
    [Fact]
    public void OnlyTheUsersOwnChangesToAShippedRuleAreStored()
    {
        var config = new DecorationSettings();

        config.Overlays.BuiltInRuleStates.Should().BeEmpty();

        OverlayRule quake = config.Overlays.ResolveRules().Single(rule => rule.Id == BuiltInRules.Ids.Earthquake);
        quake.Enabled = true;
        quake.Order = 7;
        config.Overlays.TrackRuleState(quake);

        DecorationSettings loaded = RoundTrip(config);
        OverlayRule reloaded = loaded.Overlays.ResolveRules().Single(rule => rule.Id == BuiltInRules.Ids.Earthquake);

        loaded.Overlays.BuiltInRuleStates.Should().ContainSingle();
        reloaded.Enabled.Should().BeTrue();
        reloaded.Order.Should().Be(7);

        // Everything else still comes from code.
        reloaded.IsBuiltIn.Should().BeTrue();
        reloaded.ProfileId.Should().Be(BuiltInProfiles.Ids.EarthquakeRumble);
    }

    [Fact]
    public void UserRulesSurviveARoundTrip()
    {
        var config = new DecorationSettings();
        OverlayRule authored = UserRule("Mine", 5);
        config.Overlays.Rules.Add(authored);

        OverlayRule loaded = RoundTrip(config).Overlays.Rules.Single();

        loaded.Id.Should().Be(authored.Id);
        loaded.Name.Should().Be("Mine");
        loaded.Order.Should().Be(5);
        loaded.ProfileId.Should().Be(BuiltInProfiles.Ids.Fog);
        loaded.Trigger.DefinitionId.Should().Be("player_attribute");
        loaded.IsBuiltIn.Should().BeFalse();
    }

    /// <summary>
    /// The parameterized shape has to survive as its concrete type, or a trigger's knobs come
    /// back as an empty base object and the rule stops matching anything.
    /// </summary>
    [Fact]
    public void TriggerParametersSurviveARoundTripAsTheirOwnType()
    {
        var config = new DecorationSettings();

        config.Overlays.Rules.Add(
            new OverlayRule
            {
                Name = "Chat",
                ProfileId = BuiltInProfiles.Ids.Fog,
                Trigger = new TriggerBinding
                {
                    DefinitionId = "chat_message",
                    Parameters = new ChatMessageParameters
                    {
                        Mode = ChatMatchMode.Regex,
                        Pattern = "you feel .*",
                        CaseSensitive = true,
                        DurationSeconds = 4.5f,
                        FromPlayerOnly = true
                    }
                }
            }
        );

        OverlayRule loaded = RoundTrip(config).Overlays.Rules.Single();

        loaded.Trigger.Parameters.Should().BeOfType<ChatMessageParameters>();

        var parameters = (ChatMessageParameters)loaded.Trigger.Parameters;
        parameters.Mode.Should().Be(ChatMatchMode.Regex);
        parameters.Pattern.Should().Be("you feel .*");
        parameters.CaseSensitive.Should().BeTrue();
        parameters.DurationSeconds.Should().Be(4.5f);
        parameters.FromPlayerOnly.Should().BeTrue();
        parameters.Duration.Should().Be(TimeSpan.FromSeconds(4.5));
    }

    /// <summary>
    /// The rulebase is lowest-first, the compositor takes higher as stronger. Inverting at read
    /// time is what lets dragging a rule up make it win, with no second field to keep in step.
    /// </summary>
    [Fact]
    public void TablePositionIsCompositePrecedenceInverted()
    {
        UserRule("first", 0).Priority.Should().BeGreaterThan(UserRule("second", 1).Priority);
        UserRule("second", 1).Priority.Should().BeGreaterThan(UserRule("last", 9).Priority);
    }

    [Fact]
    public void ResolvedRulesComeBackInTableOrder()
    {
        var config = new DecorationSettings();
        config.Overlays.Rules.Add(UserRule("mine", 0));

        OverlayRule quake = config.Overlays.ResolveRules().Single(rule => rule.Id == BuiltInRules.Ids.Earthquake);
        quake.Order = 99;
        config.Overlays.TrackRuleState(quake);

        List<OverlayRule> resolved = config.Overlays.ResolveRules();

        resolved.Select(rule => rule.Order).Should().BeInAscendingOrder();
        resolved[^1].Id.Should().Be(BuiltInRules.Ids.Earthquake);
    }

    /// <summary>
    /// Copying is the only way to customise a shipped rule, so the copy must be the user's
    /// outright - a new identity, editable, deletable.
    /// </summary>
    [Fact]
    public void CopyingAShippedRuleProducesAnOrdinaryOne()
    {
        OverlayRule shipped = new DecorationSettings().Overlays
                                                    .ResolveRules()
                                                    .Single(rule => rule.Id == BuiltInRules.Ids.PlayerPoisoned);

        OverlayRule copy = shipped.Clone("Mine");

        copy.Id.Should().NotBe(shipped.Id);
        copy.IsBuiltIn.Should().BeFalse();
        copy.CanEdit.Should().BeTrue();
        copy.CanDelete.Should().BeTrue();
        copy.ProfileId.Should().Be(shipped.ProfileId);
        copy.Trigger.DefinitionId.Should().Be(shipped.Trigger.DefinitionId);
        copy.Trigger.Should().NotBeSameAs(shipped.Trigger);
    }

    /// <summary>Rules persist the definition id, not the display name, so a translated or
    /// renamed trigger cannot orphan them.</summary>
    [Fact]
    public void EveryShippedTriggerHasAStableId()
    {
        TriggerCatalog.Instance.All.Should().NotBeEmpty();
        TriggerCatalog.Instance.All.Select(definition => definition.Id).Should().OnlyHaveUniqueItems();
        TriggerCatalog.Instance.All.Should().OnlyContain(definition => !string.IsNullOrWhiteSpace(definition.Id));

        TriggerCatalog.Instance.Find("nothing_by_this_name").Should().BeNull();
        TriggerCatalog.Instance.Find(null).Should().BeNull();
    }

    /// <summary>A definition that declares a parameter type has to be able to produce one, or a
    /// rule newly bound to it can never be built.</summary>
    [Fact]
    public void ParameterizedTriggersSupplyTheirOwnDefaults()
    {
        foreach (ITriggerDefinition definition in TriggerCatalog.Instance.All)
        {
            TriggerParameters parameters = definition.CreateDefaultParameters();

            if (definition.ParameterType == null)
            {
                parameters.Should().BeNull();
                continue;
            }

            parameters.Should().BeOfType(definition.ParameterType);
            definition.Create(parameters).Should().NotBeNull();
        }
    }

    /// <summary>
    /// An id is the rule's identity, so two entries carrying the same one are one rule stored
    /// twice. Configs written while the rulebase announced a creation twice carry exactly that,
    /// and would otherwise show the rule again on every restart.
    /// </summary>
    [Fact]
    public void ARuleStoredTwiceResolvesOnce()
    {
        var config = new DecorationSettings();
        OverlayRule rule = UserRule("Doubled", 5);

        config.Overlays.Rules.Add(rule);
        config.Overlays.Rules.Add(rule);

        config.Overlays.ResolveRules().Count(entry => entry.Id == rule.Id).Should().Be(1);
    }

    /// <summary>Resolving has to clear the duplicate out of the stored list too, or the next save
    /// writes it straight back.</summary>
    [Fact]
    public void ResolvingClearsStoredDuplicates()
    {
        var config = new DecorationSettings();
        OverlayRule kept = UserRule("Kept", 0);

        config.Overlays.Rules.Add(kept);
        config.Overlays.Rules.Add(kept);
        config.Overlays.Rules.Add(UserRule("Other", 1));

        config.Overlays.ResolveRules();

        config.Overlays.Rules.Should().HaveCount(2);
        config.Overlays.Rules.Select(rule => rule.Id).Should().OnlyHaveUniqueItems();
    }

    /// <summary>Two rules that merely look alike are still two rules; only a shared id makes them
    /// one.</summary>
    [Fact]
    public void RulesThatOnlyLookAlikeBothSurvive()
    {
        var config = new DecorationSettings();

        config.Overlays.Rules.Add(UserRule("Same name", 0));
        config.Overlays.Rules.Add(UserRule("Same name", 1));

        config.Overlays.ResolveRules();

        config.Overlays.Rules.Should().HaveCount(2);
    }
}
