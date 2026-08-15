using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Logic;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Logic;

/// <summary>
/// The expression engine. Everything here is written against a stand-in subject rather than
/// against any consumer's type, which is the whole claim the component makes: the tree knows
/// nothing about what it is being asked about.
/// </summary>
public class LogicEvaluatorTests
{
    private enum Mood
    {
        Calm,
        VeryAngry
    }

    private sealed record Subject(string Name, string Data, uint Serial, bool Cursed = false, Mood Mood = Mood.Calm);

    private static readonly LogicSchema<Subject> _schema = new(
        [
            (new LogicField { Key = "name", DisplayName = "Name" }, static (Subject subject) => (object)subject.Name),
            (new LogicField { Key = "data", DisplayName = "Data" }, static (Subject subject) => (object)subject.Data),
            (
                new LogicField { Key = "serial", DisplayName = "Serial", Kind = LogicValueKind.Integer },
                static (Subject subject) => (object)subject.Serial
            ),
            (
                new LogicField { Key = "cursed", DisplayName = "Cursed", Kind = LogicValueKind.Boolean },
                static (Subject subject) => (object)subject.Cursed
            ),
            (
                new LogicField { Key = "mood", DisplayName = "Mood", Kind = LogicValueKind.Enum, EnumType = typeof(Mood) },
                static (Subject subject) => (object)subject.Mood
            )
        ]
    );

    private static readonly Subject _sample = new("Ancient Bone Helm", "Durability 34 / 40", 0x40001234);

    private static bool Match(LogicNode? node, Subject? subject = null) =>
        new LogicEvaluator<Subject>(_schema).Evaluate(node, subject ?? _sample);

    private static LogicCondition Condition(string field, LogicOperator op, string value, LogicConditionFlags flags = LogicConditionFlags.None) =>
        new() { Field = field, Operator = op, Value = value, Flags = flags };

    private static LogicCondition ListCondition(string field, LogicOperator op, params string[] values) =>
        new() { Field = field, Operator = op, Values = [.. values] };

    /// <summary>A bracket whose every join uses the same connective.</summary>
    private static LogicGroup Group(LogicConnective connective, params LogicNode[] children)
    {
        // The first line has nothing above it to join to, so its own connective is never read.
        foreach (LogicNode child in children.Skip(1))
            child.Join = connective;

        return new LogicGroup { Children = [.. children] };
    }

    /// <summary>A bracket built from explicit joins, for the mixed cases a uniform one cannot
    /// express.</summary>
    private static LogicGroup Joined(LogicNode first, params (LogicConnective Join, LogicNode Node)[] rest)
    {
        var children = new List<LogicNode> { first };

        foreach ((LogicConnective join, LogicNode node) in rest)
        {
            node.Join = join;
            children.Add(node);
        }

        return new LogicGroup { Children = children };
    }

    private static LogicCondition Hit() => Condition("name", LogicOperator.Contains, "bone");

    private static LogicCondition Miss() => Condition("name", LogicOperator.Contains, "banana");

    #region Conditions

    [Theory]
    [InlineData(LogicOperator.Contains, "bone", true)]
    [InlineData(LogicOperator.DoesNotContain, "bone", false)]
    [InlineData(LogicOperator.Is, "Ancient Bone Helm", true)]
    [InlineData(LogicOperator.IsNot, "Ancient Bone Helm", false)]
    [InlineData(LogicOperator.StartsWith, "ancient", true)]
    [InlineData(LogicOperator.EndsWith, "helm", true)]
    [InlineData(LogicOperator.MatchesRegex, "^ancient .* helm$", true)]
    public void TextOperatorsCompareAsWritten(LogicOperator op, string value, bool expected)
    {
        Match(Condition("name", op, value)).Should().Be(expected);
    }

    /// <summary>Case is off by default: what the server sends is rarely capitalised the way it
    /// would be typed.</summary>
    [Theory]
    [InlineData(LogicOperator.Contains)]
    [InlineData(LogicOperator.Is)]
    [InlineData(LogicOperator.StartsWith)]
    [InlineData(LogicOperator.MatchesRegex)]
    public void CaseSensitivityReachesEveryTextOperator(LogicOperator op)
    {
        string value = op switch
        {
            LogicOperator.Contains => "bone",
            LogicOperator.Is => "ancient bone helm",
            LogicOperator.StartsWith => "ancient",
            _ => "^ancient"
        };

        Match(Condition("name", op, value)).Should().BeTrue();
        Match(Condition("name", op, value, LogicConditionFlags.CaseSensitive)).Should().BeFalse();
    }

    [Fact]
    public void TrimmingAppliesToBothSidesOfTheComparison()
    {
        var padded = new Subject("  Broadsword  ", string.Empty, 0);

        Match(Condition("name", LogicOperator.Is, " Broadsword "), padded).Should().BeFalse();

        Match(Condition("name", LogicOperator.Is, " Broadsword ", LogicConditionFlags.TrimWhitespace), padded)
            .Should()
            .BeTrue();
    }

    #endregion

    #region Numbers

    [Theory]
    [InlineData(LogicOperator.Is, "0x40001234", true)]
    [InlineData(LogicOperator.Is, "1073746484", true)]
    [InlineData(LogicOperator.IsNot, "0x40001234", false)]
    [InlineData(LogicOperator.GreaterThan, "0x40001233", true)]
    [InlineData(LogicOperator.LessThan, "0x40001233", false)]
    [InlineData(LogicOperator.GreaterOrEqual, "0x40001234", true)]
    [InlineData(LogicOperator.LessOrEqual, "0x40001234", true)]
    public void NumbersAreComparedNumericallyAndAcceptHex(LogicOperator op, string value, bool expected)
    {
        Match(Condition("serial", op, value)).Should().Be(expected);
    }

    /// <summary>A number field written against text is a mis-set condition, not a crash and not a
    /// match.</summary>
    [Fact]
    public void AnOperandThatIsNotANumberNeverMatches()
    {
        Match(Condition("serial", LogicOperator.Is, "banana")).Should().BeFalse();
        Match(Condition("serial", LogicOperator.GreaterThan, "banana")).Should().BeFalse();
    }

    #endregion

    #region Lists

    /// <summary>
    /// The list operand is a list of values, not one delimited string - so a value containing
    /// the delimiter is an ordinary value rather than a trap.
    /// </summary>
    [Fact]
    public void AListOperandHoldsItsEntriesSeparately()
    {
        Match(ListCondition("name", LogicOperator.IsAnyOf, "Broadsword", "Ancient Bone Helm")).Should().BeTrue();
        Match(ListCondition("name", LogicOperator.IsNoneOf, "Broadsword", "Ancient Bone Helm")).Should().BeFalse();
        Match(ListCondition("name", LogicOperator.IsAnyOf, "Broadsword", "Katana")).Should().BeFalse();

        var comma = new Subject("Helm, Ancient", string.Empty, 0);
        Match(ListCondition("name", LogicOperator.IsAnyOf, "Helm, Ancient"), comma).Should().BeTrue();
    }

    [Fact]
    public void ANumericListComparesItsEntriesAsNumbers()
    {
        Match(ListCondition("serial", LogicOperator.IsAnyOf, "0x1", "1073746484")).Should().BeTrue();
        Match(ListCondition("serial", LogicOperator.IsNoneOf, "0x1", "0x2")).Should().BeTrue();
    }

    /// <summary>An empty list is an unfinished condition, and blank rows in one are rows the user
    /// has not filled in yet.</summary>
    [Fact]
    public void AnEmptyOrBlankListNeverMatches()
    {
        Match(ListCondition("name", LogicOperator.IsAnyOf)).Should().BeFalse();
        Match(ListCondition("name", LogicOperator.IsNoneOf)).Should().BeFalse();
        Match(ListCondition("name", LogicOperator.IsAnyOf, string.Empty, string.Empty)).Should().BeFalse();
    }

    #endregion

    #region Booleans

    [Fact]
    public void FlagsCompareAgainstTheWordsTheEditorWrites()
    {
        var cursed = _sample with { Cursed = true };

        Match(Condition("cursed", LogicOperator.Is, "true"), cursed).Should().BeTrue();
        Match(Condition("cursed", LogicOperator.Is, "false"), cursed).Should().BeFalse();
        Match(Condition("cursed", LogicOperator.IsNot, "false"), cursed).Should().BeTrue();
        Match(Condition("cursed", LogicOperator.Is, "false")).Should().BeTrue();
    }

    /// <summary>A hand-edited config can hold anything. An unreadable flag is a mis-set condition,
    /// not a false one.</summary>
    [Fact]
    public void AnUnreadableFlagOperandNeverMatches()
    {
        Match(Condition("cursed", LogicOperator.Is, "yes")).Should().BeFalse();
        Match(Condition("cursed", LogicOperator.IsNot, "yes")).Should().BeFalse();
    }

    #endregion

    #region Enums

    /// <summary>
    /// Stored and compared as the member's declared name - what the editor's dropdown reports
    /// back, and what <see cref="object.ToString" /> gives the resolved field value.
    /// </summary>
    [Fact]
    public void EnumFieldsCompareAgainstTheDeclaredMemberName()
    {
        var angry = _sample with { Mood = Mood.VeryAngry };

        Match(Condition("mood", LogicOperator.Is, "VeryAngry"), angry).Should().BeTrue();
        Match(Condition("mood", LogicOperator.Is, "VeryAngry")).Should().BeFalse();
        Match(Condition("mood", LogicOperator.IsNot, "VeryAngry")).Should().BeTrue();

        // Not case sensitive by default, same as any other text-shaped comparison.
        Match(Condition("mood", LogicOperator.Is, "veryangry"), angry).Should().BeTrue();
    }

    [Fact]
    public void EnumFieldsSupportTheListOperators()
    {
        var angry = _sample with { Mood = Mood.VeryAngry };

        Match(ListCondition("mood", LogicOperator.IsAnyOf, "Calm", "VeryAngry"), angry).Should().BeTrue();
        Match(ListCondition("mood", LogicOperator.IsNoneOf, "Calm", "VeryAngry"), angry).Should().BeFalse();
    }

    #endregion

    #region Malformed input

    /// <summary>
    /// A filter is user input. Every way of getting one wrong has to narrow what matches rather
    /// than throw, because this runs on every packet the owner is watching.
    /// </summary>
    [Fact]
    public void NothingMalformedThrowsAndNothingMalformedMatches()
    {
        Match(Condition("no_such_field", LogicOperator.Contains, "bone")).Should().BeFalse();
        Match(Condition("name", LogicOperator.MatchesRegex, "bone (")).Should().BeFalse();
        Match(Condition("name", LogicOperator.Contains, string.Empty)).Should().BeFalse();
    }

    /// <summary>A half-written condition is false whichever way round it is put, so an unfinished
    /// row can neither fire on its own nor be smuggled past a negation.</summary>
    [Fact]
    public void ABlankOperandIsFalseEvenForANegatingOperator()
    {
        Match(Condition("name", LogicOperator.DoesNotContain, string.Empty)).Should().BeFalse();
        Match(Condition("name", LogicOperator.IsNot, string.Empty)).Should().BeFalse();
    }

    #endregion

    #region Groups

    [Theory]
    [InlineData(LogicConnective.And, false)]
    [InlineData(LogicConnective.Or, true)]
    [InlineData(LogicConnective.Xor, true)]
    [InlineData(LogicConnective.Nand, true)]
    [InlineData(LogicConnective.Nor, false)]
    public void ConnectivesCombineOneHitAndOneMiss(LogicConnective connective, bool expected)
    {
        LogicGroup group = Group(
            connective,
            Condition("name", LogicOperator.Contains, "bone"),
            Condition("name", LogicOperator.Contains, "banana")
        );

        Match(group).Should().Be(expected);
    }

    /// <summary>
    /// Each join sees only the running result and the line below it, so a chain of them is a
    /// fold rather than a statement about the bracket as a whole - three hits joined by XOR is
    /// odd parity, not "exactly one".
    /// </summary>
    [Fact]
    public void ChainedJoinsFoldRatherThanQuantifyOverTheBracket()
    {
        Match(Group(LogicConnective.Xor, Hit(), Hit(), Hit())).Should().BeTrue();
        Match(Group(LogicConnective.Xor, Hit(), Hit(), Hit(), Hit())).Should().BeFalse();
    }

    /// <summary>
    /// Every join is its own choice. Sharing one connective across a bracket meant that editing
    /// any join silently edited the rest of them.
    /// </summary>
    [Fact]
    public void JoinsInOneBracketAreIndependent()
    {
        LogicGroup group = Joined(
            Hit(),
            (LogicConnective.And, Miss()),
            (LogicConnective.Or, Hit())
        );

        // (hit AND miss) OR hit
        Match(group).Should().BeTrue();

        group.Children[2].Join.Should().Be(LogicConnective.Or);
        group.Children[1].Join.Should().Be(LogicConnective.And);

        // Changing one leaves the other exactly as it was.
        group.Children[2].Join = LogicConnective.And;

        group.Children[1].Join.Should().Be(LogicConnective.And);
        Match(group).Should().BeFalse();
    }

    /// <summary>
    /// Read strictly top to bottom, with nothing binding tighter than anything else. Under
    /// boolean precedence <c>miss AND miss OR hit</c> would be <c>miss AND (miss OR hit)</c> and
    /// come out false; left to right it is <c>(miss AND miss) OR hit</c> and holds.
    /// </summary>
    [Fact]
    public void JoinsAreFoldedLeftToRightWithNoPrecedence()
    {
        LogicGroup group = Joined(
            Miss(),
            (LogicConnective.And, Miss()),
            (LogicConnective.Or, Hit())
        );

        Match(group).Should().BeTrue();
    }

    /// <summary>The first line has nothing above it, so whatever connective it carries is never
    /// read - a stored tree from a hand edit cannot change what a bracket means through it.</summary>
    [Fact]
    public void TheFirstLinesOwnJoinIsIgnored()
    {
        LogicCondition first = Hit();
        first.Join = LogicConnective.Nor;

        Match(new LogicGroup { Children = [first] }).Should().BeTrue();
    }

    /// <summary>
    /// The point of the nesting. Precedence is expressed by the brackets and by nothing else, so
    /// this has exactly one reading.
    /// </summary>
    [Fact]
    public void NestedBracketsExpressCompoundConditions()
    {
        LogicGroup filter = Group(
            LogicConnective.And,
            Group(
                LogicConnective.Or,
                Condition("name", LogicOperator.Contains, "bone"),
                Condition("name", LogicOperator.Contains, "banana")
            ),
            Group(
                LogicConnective.And,
                ListCondition("serial", LogicOperator.IsNoneOf, "0x1", "0x2"),
                Group(
                    LogicConnective.Or,
                    Condition("data", LogicOperator.Contains, "Durability"),
                    Condition("data", LogicOperator.Contains, "Resistance")
                )
            )
        );

        Match(filter).Should().BeTrue();

        // One leaf flipped, and the whole thing has to fall over.
        Match(filter, _sample with { Data = "Weight 5 stones" }).Should().BeFalse();
    }

    /// <summary>
    /// An empty filter narrows nothing rather than contradicting itself. The other reading makes
    /// a newly added filter silently dead, which is far harder to discover than one that fires
    /// too often.
    /// </summary>
    [Fact]
    public void AnEmptyTreeMatchesEverything()
    {
        Match(null).Should().BeTrue();
        Match(new LogicGroup()).Should().BeTrue();
        Match(Group(LogicConnective.Or)).Should().BeTrue();
        Match(Group(LogicConnective.Nor)).Should().BeTrue();
    }

    #endregion

    #region Model

    [Fact]
    public void CloningATreeCopiesEveryLevelOfIt()
    {
        LogicGroup original = Group(
            LogicConnective.Or,
            Condition("name", LogicOperator.Contains, "bone", LogicConditionFlags.CaseSensitive),
            Group(LogicConnective.And, ListCondition("serial", LogicOperator.IsAnyOf, "0x1", "0x2"))
        );

        var copy = (LogicGroup)original.Clone();

        copy.Should().BeEquivalentTo(original);
        copy.Children[0].Should().NotBeSameAs(original.Children[0]);
        copy.Children[1].Should().NotBeSameAs(original.Children[1]);

        ((LogicCondition)copy.Children[0]).Value = "changed";
        ((LogicCondition)original.Children[0]).Value.Should().Be("bone");

        // The list operand is its own collection, or two conditions share one and editing either
        // edits both.
        var copiedList = (LogicCondition)((LogicGroup)copy.Children[1]).Children[0];
        var originalList = (LogicCondition)((LogicGroup)original.Children[1]).Children[0];

        copiedList.Values.Should().NotBeSameAs(originalList.Values);
        copiedList.Values.Add("0x3");
        originalList.Values.Should().HaveCount(2);
    }

    [Fact]
    public void ASchemaRefusesTwoFieldsSharingAKey()
    {
        Action build = () => _ = new LogicSchema<Subject>(
            [
                (new LogicField { Key = "name", DisplayName = "One" }, static (Subject _) => (object?)null),
                (new LogicField { Key = "NAME", DisplayName = "Two" }, static (Subject _) => (object?)null)
            ]
        );

        build.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Operator applicability

    /// <summary>
    /// The editor offers operators from this table, so a pairing it allows that the evaluator
    /// cannot honour is an offer that silently does nothing.
    /// </summary>
    [Theory]
    [InlineData(LogicValueKind.Integer)]
    [InlineData(LogicValueKind.Decimal)]
    public void NumberFieldsAreNotOfferedTheSubstringOperators(LogicValueKind kind)
    {
        LogicOperators.For(kind).Should().NotContain(LogicOperator.Contains);
        LogicOperators.For(kind).Should().NotContain(LogicOperator.MatchesRegex);
        LogicOperators.For(kind).Should().Contain(LogicOperator.GreaterOrEqual);
        LogicOperators.For(LogicValueKind.Text).Should().Contain(LogicOperator.Contains);
    }

    /// <summary>Two values, so there is nothing between equality and its negation to offer.</summary>
    [Fact]
    public void FlagFieldsAreOfferedOnlyEqualityAndItsNegation()
    {
        LogicOperators.For(LogicValueKind.Boolean).Should().BeEquivalentTo([LogicOperator.Is, LogicOperator.IsNot]);
    }

    /// <summary>A closed set of named values has nothing for the substring operators to search,
    /// and no ordering for the comparison ones - only equality and the list forms of it.</summary>
    [Fact]
    public void EnumFieldsAreOfferedOnlyEqualityAndListMembership()
    {
        LogicOperators.For(LogicValueKind.Enum).Should().BeEquivalentTo(
            [LogicOperator.Is, LogicOperator.IsNot, LogicOperator.IsAnyOf, LogicOperator.IsNoneOf]
        );
    }

    /// <summary>Changing a row's field must leave it with an operator that field accepts.</summary>
    [Fact]
    public void AnOperatorTheNewFieldRejectsIsCoercedToOneItAccepts()
    {
        LogicOperators.Coerce(LogicOperator.Contains, LogicValueKind.Integer)
            .Should()
            .Be(LogicOperators.For(LogicValueKind.Integer)[0]);

        LogicOperators.Coerce(LogicOperator.Is, LogicValueKind.Integer).Should().Be(LogicOperator.Is);
        LogicOperators.Coerce(LogicOperator.GreaterThan, LogicValueKind.Boolean).Should().Be(LogicOperator.Is);
    }

    /// <summary>
    /// Case and whitespace have nothing to say about a number or a flag, so the editor must not
    /// grow a box for them - even for an operator that carries them on a text field.
    /// </summary>
    [Fact]
    public void OnlyTextComparisonsCarryFlags()
    {
        LogicOperators.FlagsFor(LogicOperator.Contains, LogicValueKind.Text)
            .Should()
            .HaveFlag(LogicConditionFlags.CaseSensitive);

        LogicOperators.FlagsFor(LogicOperator.GreaterThan, LogicValueKind.Integer).Should().Be(LogicConditionFlags.None);
        LogicOperators.FlagsFor(LogicOperator.Is, LogicValueKind.Integer).Should().Be(LogicConditionFlags.None);
        LogicOperators.FlagsFor(LogicOperator.Is, LogicValueKind.Boolean).Should().Be(LogicConditionFlags.None);
    }

    #endregion
}
