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
    }
}
