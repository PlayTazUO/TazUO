using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Search
{
    public class SearchIndexTest
    {
        private static SearchEntry Entry(
            string label,
            SearchPath path = default,
            string matchText = null,
            IReadOnlyList<string> keywords = null
        ) => new(label, path, matchText, keywords);

        [Fact]
        public void An_Empty_Query_Matches_Nothing()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light level"));

            index.Search(null).Should().BeEmpty();
            index.Search(string.Empty).Should().BeEmpty();
            index.Search("   ").Should().BeEmpty();
        }

        [Fact]
        public void Matches_On_The_Label()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light level"));
            index.Add(Entry("Sound volume"));

            IReadOnlyList<SearchHit> hits = index.Search("light");

            hits.Should().HaveCount(1);
            hits[0].Entry.Label.Should().Be("Light level");
            hits[0].Field.Should().Be(SearchField.Label);
        }

        [Fact]
        public void A_Keyword_Match_Reports_The_Term_So_The_Row_Can_Explain_Itself()
        {
            var index = new SearchIndex();
            index.Add(Entry("Speech hue", keywords: ["speech", "talk"]));

            IReadOnlyList<SearchHit> hits = index.Search("talk");

            hits.Should().HaveCount(1);
            hits[0].Field.Should().Be(SearchField.Keyword);
            hits[0].MatchedTerm.Should().Be("talk");
        }

        [Fact]
        public void A_Path_Match_Surfaces_Everything_Underneath_It()
        {
            var index = new SearchIndex();
            // Labels deliberately unrelated to the query, so only the path can match them.
            index.Add(Entry("Gamma", new SearchPath("Video", "Lighting")));
            index.Add(Entry("Contrast", new SearchPath("Video", "Lighting")));
            index.Add(Entry("Volume", new SearchPath("Sounds")));

            IReadOnlyList<SearchHit> hits = index.Search("lighting");

            hits.Should().HaveCount(2);
            hits.Should().OnlyContain(hit => hit.Field == SearchField.Path);
            hits.Should().OnlyContain(hit => hit.MatchedTerm == "Lighting");
        }

        [Fact]
        public void A_Fuzzy_Label_Hit_Outranks_An_Exact_Path_Hit()
        {
            var index = new SearchIndex();
            index.Add(Entry("Gamma", new SearchPath("Video", "Lighting")));
            index.Add(Entry("Light level", new SearchPath("Video", "Lighting")));

            IReadOnlyList<SearchHit> hits = index.Search("lighting");

            // The label carries full weight, so even an approximate hit on it beats the path's
            // exact hit at half weight - what the user is reading wins.
            hits[0].Entry.Label.Should().Be("Light level");
            hits[0].Field.Should().Be(SearchField.Label);
        }

        [Fact]
        public void A_Label_Hit_Outranks_A_Path_Hit()
        {
            var index = new SearchIndex();
            index.Add(Entry("Shadows", new SearchPath("Video", "Lighting")));
            index.Add(Entry("Light level", new SearchPath("Video", "Lighting")));

            IReadOnlyList<SearchHit> hits = index.Search("light");

            hits[0].Entry.Label.Should().Be("Light level");
            hits[0].Field.Should().Be(SearchField.Label);
        }

        [Fact]
        public void Match_Text_Lets_A_Group_Be_Found_By_Any_Child_Label()
        {
            var index = new SearchIndex();
            index.Add(Entry("Highlight settings", matchText: "Highlight settings Border width Show count"));

            IReadOnlyList<SearchHit> hits = index.Search("border width");

            hits.Should().HaveCount(1);
            hits[0].Field.Should().Be(SearchField.MatchText);
        }

        [Fact]
        public void Ties_Fall_Back_To_Index_Order()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light A"));
            index.Add(Entry("Light B"));
            index.Add(Entry("Light C"));

            IReadOnlyList<SearchHit> hits = index.Search("light");

            hits.Should().HaveCount(3);
            hits[0].Entry.Label.Should().Be("Light A");
            hits[2].Entry.Label.Should().Be("Light C");
        }

        [Fact]
        public void Hits_Carry_Their_Index_Position()
        {
            var index = new SearchIndex();
            index.Add(Entry("Sound volume"));
            index.Add(Entry("Light level"));

            index.Search("light")[0].Order.Should().Be(1);
        }

        [Fact]
        public void Repeating_A_Query_Returns_The_Memoised_Result()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light level"));

            index.Search("light").Should().BeSameAs(index.Search("light"));
        }

        [Fact]
        public void Adding_An_Entry_Invalidates_The_Memoised_Result()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light level"));

            index.Search("light").Should().HaveCount(1);

            index.Add(Entry("Light filter"));

            index.Search("light").Should().HaveCount(2);
        }

        [Fact]
        public void Clear_Empties_The_Index()
        {
            var index = new SearchIndex();
            index.Add(Entry("Light level"));
            index.Clear();

            index.Count.Should().Be(0);
            index.Search("light").Should().BeEmpty();
        }

        [Fact]
        public void A_Blank_Entry_Is_Never_Produced_For_A_Query_Matching_Its_Section()
        {
            // Guards the shape of the bug: a section-level query must not return rows that have
            // nothing of their own to show.
            var index = new SearchIndex();
            index.Add(Entry("Sound volume", new SearchPath("Sounds"), keywords: ["audio"]));

            foreach (SearchHit hit in index.Search("audio"))
                hit.Entry.Label.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void A_Fuzzy_Query_Still_Finds_A_Misspelled_Label()
        {
            var index = new SearchIndex();
            index.Add(Entry("Lighting"));

            index.Search("lightnig").Should().HaveCount(1);
        }
    }
}
