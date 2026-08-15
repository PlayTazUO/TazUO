using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.Logic;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The shipped rules are built in code and carry their own parameters, so nothing about them is
/// checked by deserialization. A rule naming a trigger this build has withdrawn, or handing one the
/// wrong parameter type, is logged and skipped at runtime - the effect simply never fires.
/// </summary>
public class BuiltInRuleWiringTests
{
    private static OverlayRule Rule(System.Guid id) =>
        BuiltInRules.Create().Single(rule => rule.Id == id);

    [Fact]
    public void EveryShippedRuleNamesALiveTrigger()
    {
        foreach (OverlayRule rule in BuiltInRules.Create())
            TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId).Should().NotBeNull(rule.Name);
    }

    /// <summary>
    /// Definitions throw on a parameter type they cannot read, so building each rule's trigger is
    /// what proves the pairing rather than a type check that would have to be kept in step.
    /// </summary>
    [Fact]
    public void EveryShippedRuleCarriesParametersItsTriggerAccepts()
    {
        foreach (OverlayRule rule in BuiltInRules.Create())
        {
            ITriggerDefinition definition = TriggerCatalog.Instance.Find(rule.Trigger.DefinitionId)!;

            rule.Trigger.Parameters.Should().NotBeNull(rule.Name);
            definition.Invoking(target => target.Create(rule.Trigger.Parameters)).Should().NotThrow(rule.Name);
        }
    }

    [Fact]
    public void TheEarthquakeRuleListensForTheQuakeSound()
    {
        OverlayRule rule = Rule(BuiltInRules.Ids.Earthquake);

        var parameters = rule.Trigger.Parameters.Should().BeOfType<SoundPlayedParameters>().Subject;

        parameters.SoundIndex.Should().Be(755);

        // Left at the defaults on purpose: they are the falloff the dedicated earthquake trigger had.
        parameters.MinDistance.Should().Be(0);
        parameters.MaxDistance.Should().Be(0);
        parameters.Curve.Should().Be(FalloffCurve.Quadratic);
    }

    public static TheoryData<System.Guid, string> FlagRules() =>
        new()
        {
            { BuiltInRules.Ids.PlayerPoisoned, "ispoisoned" },
            { BuiltInRules.Ids.PlayerDead, "isdead" }
        };

    /// <summary>
    /// The field is named by a string the schema has to know. A key it does not recognise makes the
    /// condition false rather than an error, so the rule would silently never fire.
    /// </summary>
    [Theory]
    [MemberData(nameof(FlagRules))]
    public void EveryFlagRuleTestsAFieldTheSchemaKnows(System.Guid id, string expectedField)
    {
        OverlayRule rule = Rule(id);

        var parameters = rule.Trigger.Parameters.Should().BeOfType<PlayerAttributeParameters>().Subject;
        var condition = parameters.Filter.Children.Single().Should().BeOfType<LogicCondition>().Subject;

        condition.Field.Should().Be(expectedField);
        condition.Operator.Should().Be(LogicOperator.Is);
        condition.Value.Should().Be(bool.TrueString);

        parameters.FilterSchema.Fields
            .Select(field => field.Key)
            .Should()
            .Contain(condition.Field);
    }

    [Fact]
    public void TheDeathRuleRaisesTheDeathLook()
    {
        Rule(BuiltInRules.Ids.PlayerDead).ProfileId.Should().Be(BuiltInProfiles.Ids.Death);
    }
}
