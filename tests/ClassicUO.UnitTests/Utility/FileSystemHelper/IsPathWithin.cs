using System.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.FileSystemHelper
{
    public class IsPathWithin
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "tazuo-pathwithin");

        [Fact]
        public void SameDirectory_ReturnsTrue()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin(Root, Root).Should().BeTrue();
        }

        [Fact]
        public void NestedDirectory_ReturnsTrue()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin(Path.Combine(Root, "uo"), Root).Should().BeTrue();
        }

        [Fact]
        public void RelativeParentSegmentEscaping_ReturnsFalse()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin(Path.Combine(Root, "..", "uo"), Root).Should().BeFalse();
        }

        [Fact]
        public void DotSegment_ReturnsTrue()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin(Path.Combine(Root, "."), Root).Should().BeTrue();
        }

        [Fact]
        public void SiblingWithSharedPrefix_ReturnsFalse()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin(Root + "2", Root).Should().BeFalse();
        }

        [Fact]
        public void EmptyPath_ReturnsFalse()
        {
            ClassicUO.Utility.FileSystemHelper.IsPathWithin("", Root).Should().BeFalse();
        }
    }
}
