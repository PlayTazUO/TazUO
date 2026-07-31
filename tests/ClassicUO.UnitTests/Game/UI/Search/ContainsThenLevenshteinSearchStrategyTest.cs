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

        [Fact]
        public void MaxDistance_And_MinScore_Forward_To_The_Levenshtein_Half()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy { MaxDistance = 0 };

            // The substring half still hits outright...
            strategy.Match("All things", "ing").IsMatch.Should().BeTrue();
            // ...but the fuzzy fallback is now pinned to exact matches only.
            strategy.Match("All things", "thngs").IsMatch.Should().BeFalse();

            strategy.MaxDistance = 2;
            strategy.MinScore = 0.99f;
            strategy.Match("All things", "thngs").IsMatch.Should().BeFalse();

            strategy.MinScore = 0.6f;
            strategy.Match("All things", "thngs").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Clone_Keeps_The_Concrete_Type_So_Its_Knobs_Stay_Reachable()
        {
            // SearchableComboBox<T>.CopyFrom assigns Strategy = other.Strategy.Clone(); a base
            // CompositeSearchStrategy coming back would strip CaseSensitive/MaxDistance/MinScore
            // off whatever the widget then searches with.
            var strategy = new ContainsThenLevenshteinSearchStrategy { MaxDistance = 1, CaseSensitive = true };

            var clone = strategy.Clone().Should().BeOfType<ContainsThenLevenshteinSearchStrategy>().Subject;

            clone.MaxDistance.Should().Be(1);
            clone.CaseSensitive.Should().BeTrue();
        }

        [Fact]
        public void Clone_Does_Not_Share_Inner_State_With_The_Original()
        {
            var strategy = new ContainsThenLevenshteinSearchStrategy();
            var clone = (ContainsThenLevenshteinSearchStrategy)strategy.Clone();

            clone.CaseSensitive = true;

            strategy.CaseSensitive.Should().BeFalse();
            strategy.Match("APPLE", "apple").IsMatch.Should().BeTrue();
            clone.Match("APPLE", "apple").IsMatch.Should().BeFalse();
        }
    }
}
