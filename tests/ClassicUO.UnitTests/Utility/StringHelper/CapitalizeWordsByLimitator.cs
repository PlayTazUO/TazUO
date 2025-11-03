using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class CapitalizeWordsByLimitator
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CapitalizeWordsByLimitator_Empty_Input_Should_Return_Empty_String(string input) =>
            ClassicUO.Utility.StringHelper.CapitalizeWordsByLimitator(input)
                .Should()
                .Be(string.Empty);

        [Theory]
        [InlineData(".")]
        [InlineData(",")]
        [InlineData(";")]
        [InlineData("!")]
        public void CapitalizeWordsByLimitator_Allowed_Deliminators_Should_Work(string deliminator)
        {
            string input = $"hello{deliminator} fans of ultima online. time to play";

            string expectedResult = $"Hello{deliminator} Fans of ultima online. Time to play";

            string result = ClassicUO.Utility.StringHelper.CapitalizeWordsByLimitator(input);

            result
                .Should()
                .BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("y")]
        [InlineData("?")]
        [InlineData("/")]
        [InlineData("\\")]
        public void CapitalizeWordsByLimitator_Illegal_Deliminators_Should_NotWork(string deliminator)
        {
            string input = $"hello{deliminator} fans of ultima online. time to play";

            string expectedResult = $"Hello{deliminator} fans of ultima online. Time to play";

            string result = ClassicUO.Utility.StringHelper.CapitalizeWordsByLimitator(input);

            result
                .Should()
                .BeEquivalentTo(expectedResult);
        }
    }
}