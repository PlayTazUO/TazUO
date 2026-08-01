using System;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility
{
    public class LevenshteinTest
    {
        private static int NaiveDistance(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("", "abc")]
        [InlineData("abc", "")]
        [InlineData("abc", "abc")]
        [InlineData("a", "b")]
        [InlineData("kitten", "sitting")]
        [InlineData("flaw", "lawn")]
        [InlineData("intention", "execution")]
        public void Distance_Unbounded_Matches_Naive(string a, string b)
        {
            int expected = NaiveDistance(a, b);

            Levenshtein.Distance(a, b).Should().Be(expected);
            Levenshtein.Distance(b, a).Should().Be(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(100)]
        public void Distance_Bounded_Matches_Naive_Or_Cutoff(int maxDistance)
        {
            const string a = "intention";
            const string b = "execution";
            int naive = NaiveDistance(a, b);

            int result = Levenshtein.Distance(a, b, maxDistance);

            if (naive <= maxDistance)
            {
                result.Should().Be(naive);
            }
            else
            {
                result.Should().Be(maxDistance + 1);
            }
        }

        [Fact]
        public void Distance_Random_Inputs_Match_Naive_Across_MaxDistance_Cutoffs()
        {
            var rng = new Random(1234);
            const string alphabet = "abcde";

            for (int iter = 0; iter < 200; iter++)
            {
                string a = RandomString(rng, alphabet, rng.Next(0, 12));
                string b = RandomString(rng, alphabet, rng.Next(0, 12));

                int naive = NaiveDistance(a, b);

                foreach (int maxDistance in new[] { 0, 1, 2, 3, 5 })
                {
                    int result = Levenshtein.Distance(a, b, maxDistance);
                    int expected = naive <= maxDistance ? naive : maxDistance + 1;

                    result.Should().Be(expected, $"a='{a}' b='{b}' maxDistance={maxDistance}");
                }
            }
        }

        private static string RandomString(Random rng, string alphabet, int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[rng.Next(alphabet.Length)];
            }

            return new string(chars);
        }

        [Fact]
        public void Distance_Same_String_Is_Zero()
        {
            Levenshtein.Distance("hello world", "hello world").Should().Be(0);
        }

        [Fact]
        public void Distance_One_Char_Difference_Is_One()
        {
            Levenshtein.Distance("cat", "cut").Should().Be(1);
        }

        [Fact]
        public void Distance_Empty_Vs_Empty_Is_Zero()
        {
            Levenshtein.Distance("", "").Should().Be(0);
        }
    }
}
