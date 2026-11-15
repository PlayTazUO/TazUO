using System.Text.RegularExpressions;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility
{
    public class RegexHelperTests
    {
        [Fact]
        public void GetRegex_ValidPattern_Matches()
        {
            Regex regex = RegexHelper.GetRegex("quiver of blight", RegexOptions.IgnoreCase);

            regex.IsMatch("Quiver Of Blight").Should().BeTrue();
        }

        [Fact]
        public void GetRegex_ValidPattern_ReturnsCachedInstance()
        {
            Regex first = RegexHelper.GetRegex("cached-pattern");
            Regex second = RegexHelper.GetRegex("cached-pattern");

            second.Should().BeSameAs(first);
        }

        [Fact]
        public void GetRegex_CachesPerOptions()
        {
            Regex caseSensitive = RegexHelper.GetRegex("options-pattern", RegexOptions.None);
            Regex caseInsensitive = RegexHelper.GetRegex("options-pattern", RegexOptions.IgnoreCase);

            caseSensitive.IsMatch("OPTIONS-PATTERN").Should().BeFalse();
            caseInsensitive.IsMatch("OPTIONS-PATTERN").Should().BeTrue();
        }

        [Fact]
        public void GetRegex_InvalidPattern_DoesNotThrowAndNeverMatches()
        {
            // The stacked quantifier in the middle is what crashed AutoLootManager.
            const string pattern = ".*Pads Of The Cu Sidhe.*|.*Dungeon Plant.**Quiver Of Blight.*";

            Regex regex = RegexHelper.GetRegex(pattern, RegexOptions.Multiline);

            regex.IsMatch("Quiver Of Blight").Should().BeFalse();
            regex.IsMatch("Pads Of The Cu Sidhe").Should().BeFalse();
        }

        [Fact]
        public void GetRegex_InvalidPattern_ReturnsCachedNeverMatch()
        {
            Regex first = RegexHelper.GetRegex("invalid-pattern(**");
            Regex second = RegexHelper.GetRegex("invalid-pattern(**");

            second.Should().BeSameAs(first);
        }

        [Fact]
        public void IsValid_CompilingPattern_ReturnsTrue()
        {
            RegexHelper.IsValid(".*Quiver Of Blight.*").Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsValid_EmptyPattern_ReturnsTrue(string pattern)
        {
            RegexHelper.IsValid(pattern).Should().BeTrue();
        }

        [Fact]
        public void IsValid_UncompilablePattern_ReturnsFalse()
        {
            RegexHelper.IsValid(".*Dungeon Plant.**Quiver Of Blight.*").Should().BeFalse();
        }
    }
}
