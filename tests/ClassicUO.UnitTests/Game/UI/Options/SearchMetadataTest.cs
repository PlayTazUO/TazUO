using ClassicUO.Game.UI.MyraWindows.Options;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Options
{
    /// <summary>
    /// Covers the match-fuel accessors. Tags and keywords are both plain synonyms, so anything built
    /// from one has to match the queries the other would.
    /// </summary>
    public class SearchMetadataTest
    {
        [Fact]
        public void MatchTerms_Folds_Keywords_And_Tags_Together()
        {
            var metadata = new SearchMetadata("Visual effects", Tags: ["video"], Keywords: ["vfx"]);

            metadata.MatchTerms.Should().BeEquivalentTo(["vfx", "video"]);
        }

        [Fact]
        public void MatchTerms_Splits_Both_Halves_On_Commas()
        {
            var metadata = new SearchMetadata("Visual effects", Tags: ["video, display"], Keywords: ["vfx,fx"]);

            metadata.MatchTerms.Should().BeEquivalentTo(["vfx", "fx", "video", "display"]);
        }

        [Fact]
        public void MatchTerms_Survives_Either_Half_Being_Absent()
        {
            new SearchMetadata("x", Keywords: ["only"]).MatchTerms.Should().BeEquivalentTo(["only"]);
            new SearchMetadata("x", Tags: ["only"]).MatchTerms.Should().BeEquivalentTo(["only"]);
        }

        [Fact]
        public void MatchTerms_Is_Null_When_There_Are_No_Terms()
        {
            // Null rather than empty so a caller can leave the field off the entry entirely.
            new SearchMetadata("Light level").MatchTerms.Should().BeNull();
            new SearchMetadata("Light level", Tags: ["  "], Keywords: [","]).MatchTerms.Should().BeNull();
        }

        [Fact]
        public void Label_Defaults_To_The_Search_Text()
        {
            new SearchMetadata("Light level").Label.Should().Be("Light level");
        }

        [Fact]
        public void Merge_Keeps_The_Inner_Label_And_Unions_The_Match_Fuel()
        {
            var inner = new SearchMetadata("Light level", Keywords: ["brightness"]);
            var outer = new SearchMetadata("Lighting", Tags: ["video"], Keywords: ["dark"]);

            SearchMetadata merged = SearchMetadata.Merge(inner, outer);

            merged.Label.Should().Be("Light level");
            merged.SearchText.Should().Be("Light level");
            merged.MatchTerms.Should().BeEquivalentTo(["brightness", "dark", "video"]);
        }

        [Fact]
        public void Merge_Does_Not_Inherit_A_Label()
        {
            // What keeps spacers and decorative widgets out of the index: they pass no metadata of
            // their own, so an inherited label would name them after their section.
            SearchMetadata.Merge(null, new SearchMetadata("Lighting", Keywords: ["dark"])).Label.Should().BeNull();
        }
    }
}
