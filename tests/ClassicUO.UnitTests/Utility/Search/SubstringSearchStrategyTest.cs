using FluentAssertions;
using Myra.Utility.Search;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Search
{
    public class SubstringSearchStrategyTest
    {
        [Fact]
        public void Match_Empty_Query_Always_Matches_With_Score_One()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch match = strategy.Match("anything", "");

            match.IsMatch.Should().BeTrue();
            match.Score.Should().Be(1d);
        }

        [Fact]
        public void Match_Is_Case_Insensitive_By_Default()
        {
            var strategy = new SubstringSearchStrategy();

            strategy.Match("Hello World", "world").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Case_Sensitive_Rejects_Different_Casing()
        {
            var strategy = new SubstringSearchStrategy { CaseSensitive = true };

            strategy.Match("Hello World", "world").IsMatch.Should().BeFalse();
            strategy.Match("Hello World", "World").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_No_Substring_Returns_None()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch match = strategy.Match("Hello World", "xyz");

            match.Should().Be(SearchMatch.None);
        }

        [Fact]
        public void Match_Fills_Span_Of_The_Hit()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch match = strategy.Match("Hello World", "World");

            match.Spans.Should().ContainSingle();
            match.Spans![0].Should().Be((6, 5));
        }
    }
}
