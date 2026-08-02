using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class AbbreviateToInitials
    {
        [Fact]
        public void AbbreviateToInitials_Capitals_UsesCapitalLetters()
        {
            ClassicUO.Utility.StringHelper.AbbreviateToInitials("Last Object Macro")
                .Should()
                .Be("LOM");
        }

        [Fact]
        public void AbbreviateToInitials_NoCapitals_UsesWordInitials()
        {
            ClassicUO.Utility.StringHelper.AbbreviateToInitials("loot all corpses")
                .Should()
                .Be("LAC");
        }

        [Fact]
        public void AbbreviateToInitials_NoCapitals_HandlesUnderscoreAndDashSeparators()
        {
            ClassicUO.Utility.StringHelper.AbbreviateToInitials("loot-all_corpses")
                .Should()
                .Be("LAC");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void AbbreviateToInitials_NullOrEmpty_ReturnsEmpty(string input)
        {
            ClassicUO.Utility.StringHelper.AbbreviateToInitials(input)
                .Should()
                .BeEmpty();
        }
    }
}
