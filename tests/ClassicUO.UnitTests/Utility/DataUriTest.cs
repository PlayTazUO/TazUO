using System.Text;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility
{
    public class DataUriTest
    {
        private const string TinyPng =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0" +
            "DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";

        [Theory]
        [InlineData("data:image/png;base64,AAAA")]
        [InlineData("DATA:text/plain,hello")]
        [InlineData("Data:,")]
        public void IsDataUri_True_For_Data_Scheme(string source)
        {
            DataUri.IsDataUri(source).Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("https://example.com/picture.png")]
        [InlineData("C:\\images\\picture.png")]
        [InlineData("data")]
        public void IsDataUri_False_For_Other_Sources(string source)
        {
            DataUri.IsDataUri(source).Should().BeFalse();
        }

        [Fact]
        public void TryDecode_Base64_Returns_Png_Bytes()
        {
            DataUri.TryDecode(TinyPng, out byte[] data).Should().BeTrue();

            data.Should().NotBeEmpty();
            data[..8].Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
            data.Should().HaveCount(85);
        }

        [Fact]
        public void TryDecode_Percent_Encoded_Returns_Payload()
        {
            DataUri.TryDecode("data:text/plain,hello%20world%21", out byte[] data).Should().BeTrue();

            Encoding.UTF8.GetString(data).Should().Be("hello world!");
        }

        [Fact]
        public void TryDecode_Treats_Plus_As_Literal()
        {
            DataUri.TryDecode("data:text/plain,1%2B1+2", out byte[] data).Should().BeTrue();

            Encoding.UTF8.GetString(data).Should().Be("1+1+2");
        }

        [Fact]
        public void TryDecode_Percent_Encoded_Utf8_Returns_Payload()
        {
            DataUri.TryDecode("data:text/plain,%E2%82%AC", out byte[] data).Should().BeTrue();

            Encoding.UTF8.GetString(data).Should().Be("€");
        }

        [Fact]
        public void TryDecode_Raw_Non_Ascii_Returns_Utf8()
        {
            DataUri.TryDecode("data:text/plain,€", out byte[] data).Should().BeTrue();

            Encoding.UTF8.GetString(data).Should().Be("€");
        }

        [Theory]
        [InlineData("data:image/png;base64,not-valid-base64!")]
        [InlineData("data:image/png;base64")]
        [InlineData("https://example.com/picture.png")]
        public void TryDecode_False_For_Invalid_Sources(string source)
        {
            DataUri.TryDecode(source, out byte[] data).Should().BeFalse();

            data.Should().BeEmpty();
        }
    }
}
