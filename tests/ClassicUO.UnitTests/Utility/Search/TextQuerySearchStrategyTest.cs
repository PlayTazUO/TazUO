using FluentAssertions;
using Myra.Utility.Search;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Search
{
    public class TextQuerySearchStrategyTest
    {
        [Fact]
        public void Match_Empty_Query_Always_Matches()
        {
            var strategy = new TextQuerySearchStrategy();

            strategy.Match("anything", "").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Literal_Is_Case_Insensitive_By_Default()
        {
            var strategy = new TextQuerySearchStrategy();

            strategy.Match("Hello World", "hello").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Case_Sensitive_Rejects_Different_Casing()
        {
            var strategy = new TextQuerySearchStrategy { CaseSensitive = true };

            strategy.Match("Hello World", "hello").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_WholeWord_Requires_Word_Boundary()
        {
            var strategy = new TextQuerySearchStrategy { WholeWord = true };

            strategy.Match("catalog", "cat").IsMatch.Should().BeFalse();
            strategy.Match("the cat sat", "cat").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_UseRegex_Matches_Pattern()
        {
            var strategy = new TextQuerySearchStrategy { UseRegex = true };

            strategy.Match("value-123", @"\d+").IsMatch.Should().BeTrue();
            strategy.Match("no digits here", @"\d+").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void IsQueryValid_Invalid_Regex_Returns_False_And_Does_Not_Throw()
        {
            var strategy = new TextQuerySearchStrategy { UseRegex = true };

            bool valid = strategy.IsQueryValid("(unclosed");

            valid.Should().BeFalse();
        }

        [Fact]
        public void Match_Invalid_Regex_Returns_None_And_Does_Not_Throw()
        {
            var strategy = new TextQuerySearchStrategy { UseRegex = true };

            SearchMatch match = strategy.Match("anything", "(unclosed");

            match.IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Regex_Cache_Invalidates_When_Flag_Changes()
        {
            var strategy = new TextQuerySearchStrategy { UseRegex = false };

            strategy.Match("value-123", @"\d+").IsMatch.Should().BeFalse();

            strategy.UseRegex = true;

            strategy.Match("value-123", @"\d+").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Regex_Cache_Invalidates_When_Query_Changes()
        {
            var strategy = new TextQuerySearchStrategy { UseRegex = true };

            strategy.Match("value-123", @"\d+").IsMatch.Should().BeTrue();
            strategy.Match("no digits here", @"[a-z]+").IsMatch.Should().BeTrue();
        }
    }
}
