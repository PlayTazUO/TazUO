using FluentAssertions;
using Myra.Utility.Search;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Search
{
    public class SearchScoringTest
    {
        [Fact]
        public void Best_Picks_Highest_Weighted_Score()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch best = SearchScoring.Best(
                strategy,
                "cat",
                ("a cat sat", 0.5),
                ("category", 1.0));

            best.IsMatch.Should().BeTrue();
            best.Score.Should().Be(1.0);
        }

        [Fact]
        public void Best_Ignores_Null_Fields()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch best = SearchScoring.Best(
                strategy,
                "cat",
                ((string)null, 5.0),
                ("category", 1.0));

            best.IsMatch.Should().BeTrue();
            best.Score.Should().Be(1.0);
        }

        [Fact]
        public void Best_Returns_None_When_Nothing_Matches()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch best = SearchScoring.Best(
                strategy,
                "xyz",
                ("cat", 1.0),
                ("dog", 1.0));

            best.Should().Be(SearchMatch.None);
        }

        [Fact]
        public void BestOfMany_Picks_Highest_Scoring_Text()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch best = SearchScoring.BestOfMany(
                strategy,
                "cat",
                new[] { "dog", "category", "concatenate" },
                1.0);

            best.IsMatch.Should().BeTrue();
        }

        [Fact]
        public void BestOfMany_Returns_None_When_Nothing_Matches()
        {
            var strategy = new SubstringSearchStrategy();

            SearchMatch best = SearchScoring.BestOfMany(
                strategy,
                "xyz",
                new[] { "dog", "cat" },
                1.0);

            best.Should().Be(SearchMatch.None);
        }
    }
}
