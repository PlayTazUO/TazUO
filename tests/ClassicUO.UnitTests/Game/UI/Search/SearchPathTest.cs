using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class SearchPathTest
    {
        [Fact]
        public void Ctor_Drops_Blank_Segments()
        {
            var path = new SearchPath("Video", null, "   ", "Lighting");

            path.Length.Should().Be(2);
            path.Text.Should().Be($"Video{SearchPath.SEPARATOR}Lighting");
        }

        [Fact]
        public void Empty_Has_No_Segments_And_No_Leaf()
        {
            SearchPath.Empty.IsEmpty.Should().BeTrue();
            SearchPath.Empty.Length.Should().Be(0);
            SearchPath.Empty.Leaf.Should().BeNull();
            SearchPath.Empty.Text.Should().BeEmpty();
        }

        [Fact]
        public void Default_Instance_Behaves_As_Empty()
        {
            // The struct's default must be usable - it is what an uninitialised field holds.
            SearchPath path = default;

            path.IsEmpty.Should().BeTrue();
            path.Text.Should().BeEmpty();
            path.Push("Video").Text.Should().Be("Video");
        }

        [Fact]
        public void Push_Appends_And_Leaves_The_Original_Alone()
        {
            var original = new SearchPath("Video");
            SearchPath extended = original.Push("Lighting");

            original.Length.Should().Be(1);
            extended.Length.Should().Be(2);
            extended.Leaf.Should().Be("Lighting");
        }

        [Fact]
        public void Push_Ignores_Blank_Segments()
        {
            var path = new SearchPath("Video");

            path.Push(null).Should().Be(path);
            path.Push("  ").Should().Be(path);
        }

        [Fact]
        public void Take_Returns_Leading_Segments_And_Clamps()
        {
            var path = new SearchPath("Video", "Visual Effects", "Overlay Rules");

            path.Take(2).Text.Should().Be($"Video{SearchPath.SEPARATOR}Visual Effects");
            path.Take(0).Should().Be(SearchPath.Empty);
            path.Take(99).Should().Be(path);
        }

        [Fact]
        public void Equality_Compares_Segments()
        {
            new SearchPath("Video", "Lighting").Should().Be(new SearchPath("Video", "Lighting"));
            new SearchPath("Video", "Lighting").Should().NotBe(new SearchPath("Video"));
            (new SearchPath("Video") == new SearchPath("Video")).Should().BeTrue();
            (new SearchPath("Video") != new SearchPath("Sounds")).Should().BeTrue();
        }

        [Fact]
        public void Equality_Is_Case_Sensitive()
        {
            new SearchPath("Video").Should().NotBe(new SearchPath("video"));
        }

        [Fact]
        public void GetHashCode_Agrees_With_Equality()
        {
            new SearchPath("Video", "Lighting").GetHashCode()
                .Should().Be(new SearchPath("Video", "Lighting").GetHashCode());
        }

        [Fact]
        public void StartsWith_Treats_Empty_As_A_Prefix_Of_Everything()
        {
            var path = new SearchPath("Video", "Lighting");

            path.StartsWith(SearchPath.Empty).Should().BeTrue();
            path.StartsWith(new SearchPath("Video")).Should().BeTrue();
            path.StartsWith(new SearchPath("Sounds")).Should().BeFalse();
            new SearchPath("Video").StartsWith(path).Should().BeFalse();
        }

        [Fact]
        public void CommonPrefixLength_Counts_Shared_Leading_Segments()
        {
            var left = new SearchPath("Video", "Visual Effects", "Overlay Rules");
            var right = new SearchPath("Video", "Visual Effects", "Profiles");

            left.CommonPrefixLength(right).Should().Be(2);
            left.CommonPrefixLength(new SearchPath("Sounds")).Should().Be(0);
            left.CommonPrefixLength(SearchPath.Empty).Should().Be(0);
        }
    }
}
