using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Definitions;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The general-purpose trigger: an item's property list, filtered by an expression. Everything
/// interesting about it is the schema it exposes and the fact that its tree survives being
/// written to disk, since the matching itself belongs to the evaluator.
/// </summary>
public class ObjectPropertiesTriggerTests
{
    private static readonly OPLEventArgs _sample = new(
        0x40001234,
        "Ancient Bone Helm",
        "Durability 34 / 40\nPhysical Resist 15%"
    );

    private static bool Match(LogicGroup filter)
    {
        return new LogicEvaluator<OPLEventArgs>(ObjectPropertiesLogic.Schema).Evaluate(filter, _sample);
    }

    private static LogicCondition Condition(string field, LogicOperator op, string value)
    {
        return new LogicCondition { Field = field, Operator = op, Value = value };
    }

    [Fact]
    public void EveryPieceOfThePacketIsReachable()
    {
        Match(new LogicGroup { Children = [Condition(ObjectPropertiesLogic.SerialKey, LogicOperator.Is, "0x40001234")] })
            .Should()
            .BeTrue();

        Match(new LogicGroup { Children = [Condition(ObjectPropertiesLogic.NameKey, LogicOperator.Contains, "bone")] })
            .Should()
            .BeTrue();

        Match(new LogicGroup { Children = [Condition(ObjectPropertiesLogic.DataKey, LogicOperator.Contains, "Durability")] })
            .Should()
            .BeTrue();
    }

    /// <summary>The serial is the one field worth comparing as a number, and it is written in hex
    /// everywhere else in the client.</summary>
    [Fact]
    public void TheSerialIsANumberAndTheRestIsText()
    {
        ObjectPropertiesLogic.Schema.Find(ObjectPropertiesLogic.SerialKey)!.Kind.Should().Be(LogicValueKind.Integer);
        ObjectPropertiesLogic.Schema.Find(ObjectPropertiesLogic.NameKey)!.Kind.Should().Be(LogicValueKind.Text);
        ObjectPropertiesLogic.Schema.Find(ObjectPropertiesLogic.DataKey)!.Kind.Should().Be(LogicValueKind.Text);
    }

    /// <summary>Every field the editor offers has to have a name and an explanation behind it, or
    /// the dropdown is a list of identifiers.</summary>
    [Fact]
    public void EveryFieldIsNamedAndExplained()
    {
        ObjectPropertiesLogic.Schema.Fields.Should().NotBeEmpty();

        ObjectPropertiesLogic.Schema.Fields.Should()
            .OnlyContain(field => !string.IsNullOrWhiteSpace(field.DisplayName) && !string.IsNullOrWhiteSpace(field.Description));
    }

    [Fact]
    public void TheTriggerIsRegisteredAndBuildable()
    {
        ITriggerDefinition? definition = TriggerCatalog.Instance.Find("object_properties");

        definition.Should().NotBeNull();
        definition!.Kind.Should().Be(TriggerKind.Event);
        definition.ParameterType.Should().Be<ObjectPropertiesParameters>();
        definition.Create(definition.CreateDefaultParameters()).Should().NotBeNull();
    }

    /// <summary>A newly bound rule has an empty tree, which matches everything - so the trigger
    /// works out of the box and is narrowed rather than assembled.</summary>
    [Fact]
    public void ANewlyBoundRuleStartsUnfiltered()
    {
        var parameters = (ObjectPropertiesParameters)new ObjectPropertiesTriggerDefinition().CreateDefaultParameters();

        parameters.Filter.IsEmpty.Should().BeTrue();
        Match(parameters.Filter).Should().BeTrue();
    }

    /// <summary>
    /// The tree is nested and polymorphic, which is exactly the shape a serializer is most likely
    /// to flatten. Losing it turns a narrow rule into one that fires on every item the client
    /// ever asks about.
    /// </summary>
    [Fact]
    public void TheFilterTreeSurvivesARoundTripWithItsNestingIntact()
    {
        var config = new DecorationSettings();

        var authored = new ObjectPropertiesParameters
        {
            DurationSeconds = 4.5f,
            Filter = new LogicGroup
            {
                Children =
                [
                    new LogicGroup
                    {
                        Children =
                        [
                            Condition(ObjectPropertiesLogic.NameKey, LogicOperator.Contains, "bone"),
                            new LogicCondition
                            {
                                Join = LogicConnective.Or,
                                Field = ObjectPropertiesLogic.NameKey,
                                Operator = LogicOperator.Contains,
                                Value = "banana"
                            }
                        ]
                    },
                    new LogicCondition
                    {
                        Join = LogicConnective.And,
                        Field = ObjectPropertiesLogic.SerialKey,
                        Operator = LogicOperator.IsNoneOf,
                        Values = ["0x1", "0x2"],
                        Flags = LogicConditionFlags.CaseSensitive
                    }
                ]
            }
        };

        config.Overlays.Rules.Add(
            new OverlayRule
            {
                Name = "Properties",
                ProfileId = BuiltInProfiles.Ids.Fog,
                Trigger = new TriggerBinding { DefinitionId = "object_properties", Parameters = authored }
            }
        );

        DecorationSettings loaded = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(config, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations),
            ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations
        );

        TriggerParameters restored = loaded.Overlays.Rules.Single().Trigger.Parameters;

        restored.Should().BeOfType<ObjectPropertiesParameters>();

        var properties = (ObjectPropertiesParameters)restored;
        properties.DurationSeconds.Should().Be(4.5f);
        properties.Filter.Children.Should().HaveCount(2);

        properties.Filter.Children[0].Should().BeOfType<LogicGroup>();

        var nested = (LogicGroup)properties.Filter.Children[0];
        nested.Children.Should().HaveCount(2);

        // Each join is stored on the line it leads into, so they have to survive independently.
        nested.Children[1].Join.Should().Be(LogicConnective.Or);
        properties.Filter.Children[1].Join.Should().Be(LogicConnective.And);

        properties.Filter.Children[1].Should().BeOfType<LogicCondition>();

        var condition = (LogicCondition)properties.Filter.Children[1];
        condition.Operator.Should().Be(LogicOperator.IsNoneOf);
        condition.Values.Should().Equal("0x1", "0x2");
        condition.Flags.Should().Be(LogicConditionFlags.CaseSensitive);
    }

    /// <summary>Editing a rule works on a copy, so the copy must not share the tree it was made
    /// from - the whole point of the draft is that cancelling leaves no trace.</summary>
    [Fact]
    public void CloningDetachesTheTree()
    {
        var original = new ObjectPropertiesParameters
        {
            Filter = new LogicGroup { Children = [Condition(ObjectPropertiesLogic.NameKey, LogicOperator.Contains, "bone")] }
        };

        var copy = (ObjectPropertiesParameters)original.Clone();

        copy.Filter.Should().NotBeSameAs(original.Filter);
        ((LogicCondition)copy.Filter.Children[0]).Value = "changed";
        ((LogicCondition)original.Filter.Children[0]).Value.Should().Be("bone");
    }
}
