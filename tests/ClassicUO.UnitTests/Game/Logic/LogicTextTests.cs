using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Logic;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Logic;

/// <summary>
/// The builder matches a combo's reported name back to the enum value it came from, so a name
/// that is missing, blank, or shared by two values makes selections silently fail to take.
/// </summary>
public class LogicTextTests
{
    public static TheoryData<string> DeclaredKeys()
    {
        var keys = new TheoryData<string>();

        foreach (string key in LogicText.DeclaredKeys())
            keys.Add(key);

        return keys;
    }

    /// <summary>
    /// These keys are not attributes, so nothing else can find them - and a key with no entry
    /// behind it renders its English fallback and looks perfectly correct.
    /// </summary>
    [Theory]
    [MemberData(nameof(DeclaredKeys))]
    public void EveryDeclaredKeyHasAnEntry(string key)
    {
        LangIniSerializer.ReadEmbedded().Should().ContainKey(key);
    }

    [Fact]
    public void KeysAreNamespaced()
    {
        LogicText.DeclaredKeys().Should().OnlyContain(key => key.StartsWith("logic_"));
    }

    /// <summary>
    /// Names only have to be unique among the operators offered together, which is per kind -
    /// and they must be, or the combo cannot tell which of two entries was picked.
    /// </summary>
    [Theory]
    [InlineData(LogicValueKind.Text)]
    [InlineData(LogicValueKind.Integer)]
    [InlineData(LogicValueKind.Decimal)]
    [InlineData(LogicValueKind.Boolean)]
    [InlineData(LogicValueKind.Enum)]
    public void OperatorsOfferedTogetherAreNamedUniquely(LogicValueKind kind)
    {
        string[] names = [.. LogicOperators.For(kind).Select(op => LogicText.Name(op, kind))];

        names.Should().OnlyHaveUniqueItems();
        names.Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void ConnectivesAreNamedUniquely()
    {
        string[] connectives = [.. Enum.GetValues<LogicConnective>().Select(LogicText.Name)];

        connectives.Should().OnlyHaveUniqueItems();
        connectives.Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
    }

    /// <summary>A number reads as an ordered set of symbols; the same operator on text reads as
    /// words. Both have to round-trip, which is what the kind argument is for.</summary>
    [Fact]
    public void NumericComparisonsAreNamedAsSymbols()
    {
        LogicText.Name(LogicOperator.Is, LogicValueKind.Integer).Should().Be("==");
        LogicText.Name(LogicOperator.GreaterOrEqual, LogicValueKind.Decimal).Should().Be(">=");
        LogicText.Name(LogicOperator.Is, LogicValueKind.Text).Should().Be("Is");
    }

    /// <summary>The round trip the builder actually performs when a dropdown reports a choice.</summary>
    [Theory]
    [InlineData(LogicValueKind.Text)]
    [InlineData(LogicValueKind.Integer)]
    [InlineData(LogicValueKind.Decimal)]
    [InlineData(LogicValueKind.Boolean)]
    [InlineData(LogicValueKind.Enum)]
    public void ANameResolvesBackToTheOperatorItCameFrom(LogicValueKind kind)
    {
        IReadOnlyList<LogicOperator> operators = LogicOperators.For(kind);

        foreach (LogicOperator op in operators)
            LogicText.ParseOperator(operators, LogicText.Name(op, kind), kind).Should().Be(op);

        LogicText.ParseOperator(operators, "nothing by this name", kind).Should().BeNull();
    }

    /// <summary>The checkboxes on a row follow the operator, and a flag with no reading name
    /// would render as a blank box.</summary>
    [Fact]
    public void EveryApplicableFlagIsNamed()
    {
        foreach (LogicValueKind kind in Enum.GetValues<LogicValueKind>())
        {
            foreach (LogicOperator op in LogicOperators.For(kind))
            {
                foreach (LogicConditionFlags flag in LogicText.ApplicableFlags(op, kind))
                    LogicText.Name(flag).Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    /// <summary>Words split before a capital that follows a lowercase letter or digit, and an
    /// acronym's last capital stays attached to the word after it rather than splitting alone.</summary>
    [Theory]
    [InlineData("VeryAngry", "Very Angry")]
    [InlineData("Murderer", "Murderer")]
    [InlineData("HPRegen", "HP Regen")]
    [InlineData("FastUnmountAndCantRun", "Fast Unmount And Cant Run")]
    public void EnumMemberNamesSplitAtWordBoundaries(string memberName, string expected)
    {
        LogicText.EnumMemberName(memberName).Should().Be(expected);
    }

    /// <summary>Anything the table reports as applicable has to be a flag the operator actually
    /// declares, or the row grows a box that changes nothing.</summary>
    [Fact]
    public void ApplicableFlagsAreASubsetOfWhatTheOperatorDeclares()
    {
        // An operator with no flags at all is the point of the check, so this cannot go through
        // OnlyContain - it treats an empty collection as a failure.
        IEnumerable<string> undeclared = Enum.GetValues<LogicValueKind>()
            .SelectMany(kind => LogicOperators.For(kind).Select(op => (Kind: kind, Operator: op)))
            .SelectMany(
                pair => LogicText.ApplicableFlags(pair.Operator, pair.Kind)
                    .Where(flag => !LogicOperators.FlagsFor(pair.Operator, pair.Kind).HasFlag(flag))
                    .Select(flag => $"{pair.Kind}.{pair.Operator}.{flag}")
            );

        undeclared.Should().BeEmpty();
    }
}
