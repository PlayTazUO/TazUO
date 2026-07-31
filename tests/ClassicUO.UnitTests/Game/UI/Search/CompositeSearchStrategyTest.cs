using System;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Myra.Utility.Search;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class CompositeSearchStrategyTest
    {
        [Fact]
        public void Match_Short_Circuits_On_First_Matching_Strategy()
        {
            var composite = new CompositeSearchStrategy(new SubstringSearchStrategy(), new LevenshteinSearchStrategy { MaxDistance = 0 });

            SearchMatch match = composite.Match("All things", "ing");

            match.IsMatch.Should().BeTrue();
            match.Score.Should().Be(1d);
        }

        [Fact]
        public void Match_Falls_Back_To_Next_Strategy_When_First_Does_Not_Match()
        {
            var composite = new CompositeSearchStrategy(new SubstringSearchStrategy(), new LevenshteinSearchStrategy { MaxDistance = 2 });

            composite.Match("kitten", "kittne").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Returns_None_When_No_Strategy_Matches()
        {
            var composite = new CompositeSearchStrategy(new SubstringSearchStrategy(), new LevenshteinSearchStrategy { MaxDistance = 0 });

            composite.Match("kitten", "xyzxyz").Should().Be(SearchMatch.None);
        }

        [Fact]
        public void Constructor_Throws_When_No_Strategies_Given()
        {
            Action act = () => new CompositeSearchStrategy();

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IsQueryValid_Requires_Every_Strategy_To_Accept_The_Query()
        {
            // Levenshtein takes anything; the regex strategy can't compile this one. Reporting
            // "valid" off the permissive half would turn the caller's invalid-query affordance
            // off and leave the user with a silently empty list.
            var composite = new CompositeSearchStrategy(
                new LevenshteinSearchStrategy(),
                new TextQuerySearchStrategy { UseRegex = true });

            composite.IsQueryValid("(unclosed").Should().BeFalse();
            composite.IsQueryValid("fine").Should().BeTrue();
        }

        [Fact]
        public void Clone_Deep_Copies_The_Inner_Strategies()
        {
            var inner = new LevenshteinSearchStrategy { MaxDistance = 0 };
            var composite = new CompositeSearchStrategy(inner);

            var clone = (CompositeSearchStrategy)composite.Clone();
            inner.MaxDistance = 5;

            // The clone kept its own copy, so retuning the original leaves it alone.
            clone.Match("kitten", "kittn").IsMatch.Should().BeFalse();
            composite.Match("kitten", "kittn").IsMatch.Should().BeTrue();
        }
    }
}
