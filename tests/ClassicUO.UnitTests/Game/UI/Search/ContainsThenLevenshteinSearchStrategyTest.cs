using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class ContainsThenLevenshteinSearchStrategyTest
    {
        [Fact]
        public void Match_Short_Circuits_On_Substring_Hit()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy();

            strategy.Match("All things", "ing").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Falls_Back_To_Levenshtein_Per_Token_When_No_Substring_Hit()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy();

            strategy.Match("All things", "thngs").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Is_Case_Insensitive_By_Default()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy();

            strategy.Match("APPLE", "apple").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void CaseSensitive_Applies_To_Both_Inner_Strategies()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy { CaseSensitive = true };

            strategy.Match("APPLE", "apple").IsMatch.Should().BeFalse();
        }
    }
}
