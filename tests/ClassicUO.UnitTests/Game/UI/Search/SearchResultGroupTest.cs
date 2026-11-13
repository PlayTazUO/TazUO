using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class SearchResultGroupTest
    {
        private static SearchHit Hit(string label, double score, int order, params string[] segments) =>
            new(new SearchEntry(label, new SearchPath(segments)), score, SearchField.Label, Order: order);

        [Fact]
        public void Nests_By_Path_Segments()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Light level", 1d, 0, "Video", "Lighting"),
                Hit("Dark nights", 0.9d, 1, "Video", "Lighting"),
                Hit("Zoom step", 0.5d, 2, "Video", "Zoom")
            ]);

            roots.Should().HaveCount(1);
            roots[0].DisplayName.Should().Be("Video");
            roots[0].Children.Should().HaveCount(2);
            roots[0].Children[0].DisplayName.Should().Be("Lighting");
            roots[0].Children[0].Hits.Should().HaveCount(2);
        }

        [Fact]
        public void Rolls_Up_Hit_Counts_And_Best_Score()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Light level", 1d, 0, "Video", "Lighting"),
                Hit("Zoom step", 0.5d, 1, "Video", "Zoom")
            ]);

            roots[0].TotalHits.Should().Be(2);
            roots[0].BestScore.Should().Be(1d);
            roots[0].Hits.Should().BeEmpty();
        }

        [Fact]
        public void Collapses_A_Chain_That_Holds_Nothing_Of_Its_Own()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Light overlay rules", 1d, 0, "Video", "Visual Effects", "Overlay Rules")
            ]);

            roots.Should().HaveCount(1);
            roots[0].Segments.Should().Equal("Video", "Visual Effects", "Overlay Rules");
            roots[0].DisplayName.Should().Be(
                $"Video{SearchPath.SEPARATOR}Visual Effects{SearchPath.SEPARATOR}Overlay Rules");
            roots[0].Hits.Should().HaveCount(1);
            roots[0].Children.Should().BeEmpty();
        }

        [Fact]
        public void A_Collapsed_Chain_Keeps_The_Deepest_Path_So_Navigation_Still_Works()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Light overlay rules", 1d, 0, "Video", "Visual Effects", "Overlay Rules")
            ]);

            roots[0].Path.Should().Be(new SearchPath("Video", "Visual Effects", "Overlay Rules"));
        }

        [Fact]
        public void Does_Not_Collapse_A_Node_That_Holds_Its_Own_Hits()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Resolution", 1d, 0, "Video"),
                Hit("Light level", 0.9d, 1, "Video", "Lighting")
            ]);

            roots[0].Segments.Should().Equal("Video");
            roots[0].Hits.Should().HaveCount(1);
            roots[0].Children.Should().HaveCount(1);
        }

        [Fact]
        public void Siblings_Are_Ordered_By_Their_Best_Contained_Score()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Zoom step", 0.4d, 0, "Video", "Zoom"),
                Hit("Light level", 0.95d, 1, "Video", "Lighting")
            ]);

            roots[0].Children[0].DisplayName.Should().Be("Lighting");
            roots[0].Children[1].DisplayName.Should().Be("Zoom");
        }

        [Fact]
        public void Hits_Inside_A_Group_Keep_Index_Order_Rather_Than_Score_Order()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Second", 0.9d, 5, "Video"),
                Hit("First", 1d, 2, "Video")
            ]);

            roots[0].Hits[0].Entry.Label.Should().Be("First");
            roots[0].Hits[1].Entry.Label.Should().Be("Second");
        }

        [Fact]
        public void Pathless_Hits_Land_In_A_Leading_Group_With_No_Header()
        {
            IReadOnlyList<SearchResultGroup> roots = SearchResultGroup.Build([
                Hit("Light level", 0.9d, 1, "Video", "Lighting"),
                Hit("Orphan", 1d, 0)
            ]);

            roots.Should().HaveCount(2);
            roots[0].Segments.Should().BeEmpty();
            roots[0].Hits.Should().HaveCount(1);
            // Video holds nothing itself and leads only to Lighting, so the chain collapses.
            roots[1].DisplayName.Should().Be($"Video{SearchPath.SEPARATOR}Lighting");
        }

        [Fact]
        public void No_Hits_Produces_No_Groups()
        {
            SearchResultGroup.Build([]).Should().BeEmpty();
        }
    }
}
