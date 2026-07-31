using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Myra.Utility.Search;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class LevenshteinSearchStrategyTest
    {
        [Fact]
        public void Match_Empty_Query_Always_Matches()
        {
            var strategy = new LevenshteinSearchStrategy();

            strategy.Match("anything", "").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Within_MaxDistance_Matches()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 1 };

            SearchMatch match = strategy.Match("kitten", "kitten");
            match.IsMatch.Should().BeTrue();
            match.Score.Should().Be(1d);
        }

        [Fact]
        public void Match_Beyond_MaxDistance_Returns_None()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 1 };

            strategy.Match("kitten", "sitting").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_Is_Case_Insensitive_By_Default()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 0 };

            strategy.Match("Hello", "hello").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Case_Sensitive_Rejects_Different_Casing()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 0, CaseSensitive = true };

            strategy.Match("Hello", "hello").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_PerTokenBest_Uses_Best_Scoring_Token()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 0, PerTokenBest = true };

            SearchMatch match = strategy.Match("a longsword of vanquishing", "longsword");

            match.IsMatch.Should().BeTrue();
            match.Score.Should().Be(1d);
        }

        [Fact]
        public void Match_Without_PerTokenBest_Requires_Whole_String_Within_Distance()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 100, PerTokenBest = false };

            // "longsword" is close to one token inside the phrase, but comparing it against
            // the WHOLE phrase (no PerTokenBest) is a huge length mismatch. AUTO fuzziness
            // caps the effective distance well below what that would need, so this correctly
            // finds no match instead of degrading into a near-arbitrary match.
            SearchMatch match = strategy.Match("a longsword of vanquishing", "longsword");

            match.IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_Without_PerTokenBest_Scores_Similar_Length_Strings()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 100 };

            SearchMatch match = strategy.Match("longswords", "longsword");

            match.IsMatch.Should().BeTrue();
            match.Score.Should().BeLessThan(1d);
        }

        [Theory]
        // Pins the default fuzziness curve (LevenshteinSearchStrategy.AutoFuzziness): the edit
        // budget by query length is 1 up to 2 chars, 2 up to 5, 3 beyond. Each row is a query
        // matched against a candidate exactly `distance` edits away, at the boundary of its
        // bucket, with MaxDistance and MinScore lifted out of the way so only the curve decides.
        [InlineData("ab", "ab", true)]      // len 2, 0 edits
        [InlineData("ab", "xb", true)]      // len 2, 1 edit  - at the budget
        [InlineData("ab", "xy", false)]     // len 2, 2 edits - over it
        [InlineData("abcde", "xycde", true)]   // len 5, 2 edits - at the budget
        [InlineData("abcde", "xyzde", false)]  // len 5, 3 edits - over it
        [InlineData("abcdef", "xyzdef", true)]  // len 6, 3 edits - at the budget
        [InlineData("abcdef", "xyzwef", false)] // len 6, 4 edits - over it
        public void Match_Default_Fuzziness_Curve_Scales_With_Query_Length(string query, string candidate, bool expected)
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = int.MaxValue, MinScore = 0f };

            strategy.Match(candidate, query).IsMatch.Should().Be(expected);
        }

        [Fact]
        public void Match_Short_Query_Does_Not_Match_Unrelated_Short_Word()
        {
            // Regression: "ad" and "Say" are genuinely only 2 edits apart, so a flat
            // MaxDistance=2 (the default) let a 2-letter query match almost any similarly
            // short, unrelated word. The fuzziness curve caps queries this short to 1 edit.
            var strategy = new LevenshteinSearchStrategy();

            strategy.Match("Say", "ad").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_Short_Query_Still_Matches_Itself_Exactly()
        {
            var strategy = new LevenshteinSearchStrategy();

            strategy.Match("ad", "ad").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Medium_Query_Allows_Small_Edits_But_Not_An_Unrelated_Word()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 5 };

            strategy.Match("catss", "cats").IsMatch.Should().BeTrue();
            strategy.Match("dogs", "cats").IsMatch.Should().BeFalse();
        }

        [Fact]
        public void Match_MaxDistance_Still_Caps_Long_Queries_Below_Auto_Ceiling()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 1 };

            // "elephant"/"elephants" and "elephant"/"elephent" are each 1 edit apart, at a
            // length where AUTO fuzziness would allow 2 - the explicit MaxDistance=1 should
            // still be the binding, lower cap.
            strategy.Match("elephants", "elephant").IsMatch.Should().BeTrue();
            strategy.Match("elephant", "elephent").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Negative_MaxDistance_Still_Matches_Exact_String()
        {
            // Regression: a negative effective distance (from a bad MaxDistance or a custom
            // GetMaxDistanceForQueryLength) must not make even an exact match (dist=0) fail
            // the "dist > effectiveMaxDistance" check.
            var strategy = new LevenshteinSearchStrategy { MaxDistance = -1 };

            strategy.Match("kitten", "kitten").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_Negative_GetMaxDistanceForQueryLength_Still_Matches_Exact_String()
        {
            var strategy = new LevenshteinSearchStrategy
            {
                GetMaxDistanceForQueryLength = _ => -1
            };

            strategy.Match("kitten", "kitten").IsMatch.Should().BeTrue();
        }

        [Fact]
        public void Match_PerTokenBest_Default_Tokenizer_Splits_On_Word_Boundaries()
        {
            var strategy = new LevenshteinSearchStrategy { MaxDistance = 0, PerTokenBest = true };

            SearchMatch match = strategy.Match("All-things,here", "things");

            match.IsMatch.Should().BeTrue();
            match.Score.Should().Be(1d);
        }

        [Fact]
        public void Match_PerTokenBest_Custom_Tokenizer_Is_Used()
        {
            var strategy = new LevenshteinSearchStrategy
            {
                MaxDistance = 0,
                PerTokenBest = true,
                Tokenizer = candidate => candidate.Split(',')
            };

            strategy.Match("longsword,shortsword", "shortsword").IsMatch.Should().BeTrue();
        }
    }
}
