using System;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class DecodeName
    {
        [Fact]
        public void EmptyInput_ReturnsEmptyString()
        {
            ClassicUO.Utility.StringHelper.DecodeName(ReadOnlySpan<byte>.Empty).Should().BeEmpty();
        }

        [Fact]
        public void AsciiName_IsUnchanged()
        {
            byte[] bytes = "Test Name"u8.ToArray();

            ClassicUO.Utility.StringHelper.DecodeName(bytes).Should().Be("Test Name");
        }

        [Theory]
        [InlineData(new byte[] { 0x4A, 0x6F, 0x73, 0xE9 }, "José")]
        [InlineData(new byte[] { 0x4D, 0xFC, 0x6C, 0x6C, 0x65, 0x72 }, "Müller")]
        [InlineData(new byte[] { 0xC9, 0x6C, 0x6F, 0x64, 0x69, 0x65 }, "Élodie")]
        public void Cp1252Name_IsDecodedAsCp1252(byte[] bytes, string expected)
        {
            ClassicUO.Utility.StringHelper.DecodeName(bytes).Should().Be(expected);
        }

        [Theory]
        [InlineData(new byte[] { 0xC8, 0xE2, 0xE0, 0xED }, "Иван")]
        [InlineData(new byte[] { 0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2 }, "Привет")]
        [InlineData(new byte[] { 0xA8, 0xE6, 0xE8, 0xEA }, "Ёжик")]
        [InlineData(new byte[] { 0xAF, 0xE6, 0xE0, 0xF7, 0xEE, 0xEA }, "Їжачок")]
        public void Cp1251Name_IsDecodedAsCp1251(byte[] bytes, string expected)
        {
            ClassicUO.Utility.StringHelper.DecodeName(bytes).Should().Be(expected);
        }
    }
}
