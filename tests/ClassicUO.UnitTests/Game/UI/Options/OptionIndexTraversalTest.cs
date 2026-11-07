using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.UI.MyraWindows.Options;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using FluentAssertions;
using Myra.Graphics2D.UI;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI.Options
{
    /// <summary>
    /// Covers the flattening pass that turns the options tree into search entries. Render factories
    /// are never invoked here, so they can return null - only the metadata and paths matter.
    /// </summary>
    public class OptionIndexTraversalTest
    {
        private static OptionEntry Leaf(string label) => new(() => null, new SearchMetadata(label));

        private static List<SearchEntry> Index(IOptionSource source, params string[] rootSegments) =>
            source.Index(new OptionIndexContext(new SearchPath(rootSegments))).ToList();

        [Fact]
        public void A_Leaf_Takes_The_Accumulated_Path_And_Its_Own_Label()
        {
            List<SearchEntry> entries = Index(Leaf("Light level"), "Video", "Lighting");

            entries.Should().HaveCount(1);
            entries[0].Label.Should().Be("Light level");
            entries[0].Path.Should().Be(new SearchPath("Video", "Lighting"));
            entries[0].FullPath.Text.Should().Be(
                $"Video{SearchPath.SEPARATOR}Lighting{SearchPath.SEPARATOR}Light level");
        }

        [Fact]
        public void A_Leaf_With_No_Label_Is_Skipped()
        {
            // How spacers and decorative content stay out of the index.
            var unlabelled = new OptionEntry(() => null);

            Index(unlabelled, "Video").Should().BeEmpty();
        }

        [Fact]
        public void A_Leaf_With_No_Label_Stays_Skipped_Under_Labelled_Metadata()
        {
            // The case that matters in practice: spacers sit inside a fragment carrying its
            // section's metadata. Inheriting that label would index every spacer under the
            // section's name and surface them as blank result rows.
            var spacer = new OptionEntry(() => null);
            var section = new OptionFragment(() => null, [spacer, Leaf("Sound volume")])
                .WithSearch(new SearchMetadata("Sounds", Keywords: ["audio", "music"]));

            List<SearchEntry> entries = Index(section, "Sounds");

            entries.Should().ContainSingle();
            entries[0].Label.Should().Be("Sound volume");
        }

        [Fact]
        public void Match_Text_And_Keywords_Are_Still_Inherited()
        {
            // Only the label is withheld; the rest is what makes a section findable by its own name.
            var section = new OptionFragment(() => null, [Leaf("Sound volume")])
                .WithSearch(new SearchMetadata("Sounds", Keywords: ["audio"]));

            SearchEntry entry = Index(section, "Sounds")[0];

            entry.Keywords.Should().Contain("audio");
            entry.MatchText.Should().Be("Sound volume");
        }

        [Fact]
        public void A_Leaf_Defaults_To_Inline_And_Custom_Content_Does_Not()
        {
            Index(Leaf("Light level"), "Video")[0].Presentation.Should().Be(SearchPresentation.Inline);

            OptionEntry custom = Option.Custom(() => null, new SearchMetadata("Overlay rules"));

            Index(custom, "Video")[0].Presentation.Should().Be(SearchPresentation.Navigate);
        }

        [Fact]
        public void AsLinkOnly_Overrides_The_Default()
        {
            Index(Leaf("Light level").AsLinkOnly(), "Video")[0]
                .Presentation.Should().Be(SearchPresentation.Navigate);
        }

        [Fact]
        public void Tab_Labels_Become_Path_Segments()
        {
            var group = new OptionTabGroup()
                .AddTab("Lighting", () => Leaf("Light level"))
                .AddTab("Zoom", () => Leaf("Zoom step"));

            List<SearchEntry> entries = Index(group, "Video");

            entries.Select(entry => entry.Path.Text).Should().Equal(
                $"Video{SearchPath.SEPARATOR}Lighting",
                $"Video{SearchPath.SEPARATOR}Zoom");
        }

        [Fact]
        public void One_Content_Factory_Reached_Through_Two_Tabs_Gets_Two_Distinct_Paths()
        {
            // The reason the path is accumulated on the way down rather than walked back up from a
            // parent pointer: a shared factory is one node reachable by several routes, and each
            // occurrence needs its own breadcrumb.
            IOptionSource Shared() => Leaf("Shared setting");

            var group = new OptionTabGroup()
                .AddTab("Alpha", Shared)
                .AddTab("Beta", Shared);

            List<SearchEntry> entries = Index(group, "Root");

            entries.Should().HaveCount(2);
            entries.Select(entry => entry.Path.Text).Should().Equal(
                $"Root{SearchPath.SEPARATOR}Alpha",
                $"Root{SearchPath.SEPARATOR}Beta");
        }

        [Fact]
        public void Nested_Tab_Groups_Accumulate_Every_Segment()
        {
            var inner = new OptionTabGroup().AddTab("Overlay Rules", () => Leaf("Light overlay rules"));
            var outer = new OptionTabGroup().AddTab("Visual Effects", () => inner);

            Index(outer, "Video")[0].Path.Text.Should().Be(
                $"Video{SearchPath.SEPARATOR}Visual Effects{SearchPath.SEPARATOR}Overlay Rules");
        }

        [Fact]
        public void An_Unlabelled_Fragment_Is_Transparent()
        {
            var fragment = new OptionFragment(() => null, [Leaf("Light level")]);

            Index(fragment, "Video")[0].Path.Should().Be(new SearchPath("Video"));
        }

        [Fact]
        public void A_Labelled_Fragment_Contributes_A_Segment()
        {
            // What a visual container's caption does, so breadcrumbs read like the page does.
            var fragment = new OptionFragment(() => null, [Leaf("Light level")]).WithLabel("Light Sources");

            Index(fragment, "Video", "Lighting")[0].Path.Text.Should().Be(
                $"Video{SearchPath.SEPARATOR}Lighting{SearchPath.SEPARATOR}Light Sources");
        }

        [Fact]
        public void Page_Labels_Become_Path_Segments_And_Unlabelled_Pages_Are_Transparent()
        {
            var group = new OptionPageGroup()
                .AddPage(() => Leaf("Named page setting"), "Experimental")
                .AddPage(() => Leaf("Anonymous page setting"));

            List<SearchEntry> entries = Index(group, "Misc");

            entries[0].Path.Text.Should().Be($"Misc{SearchPath.SEPARATOR}Experimental");
            entries[1].Path.Should().Be(new SearchPath("Misc"));
        }

        [Fact]
        public void A_Search_Group_Yields_One_Entry_Labelled_By_Itself()
        {
            var group = new OptionFragment(() => null, [Leaf("Border width"), Leaf("Show count")])
                .AsSearchGroup()
                .WithLabel("Grid highlighting");

            List<SearchEntry> entries = Index(group, "Interface");

            entries.Should().HaveCount(1);
            entries[0].Label.Should().Be("Grid highlighting");
            entries[0].Presentation.Should().Be(SearchPresentation.Navigate);
        }

        [Fact]
        public void A_Search_Group_Stays_Findable_By_Any_Child_Label()
        {
            var group = new OptionFragment(() => null, [Leaf("Border width"), Leaf("Show count")])
                .AsSearchGroup()
                .WithLabel("Grid highlighting");

            SearchEntry entry = Index(group, "Interface")[0];

            entry.MatchText.Should().Contain("Border width").And.Contain("Show count");
        }

        [Fact]
        public void A_Search_Group_With_No_Name_Borrows_Its_First_Child_Label()
        {
            var group = new OptionFragment(() => null, [Leaf("Border width"), Leaf("Show count")])
                .AsSearchGroup();

            Index(group, "Interface")[0].Label.Should().Be("Border width");
        }

        [Fact]
        public void Keywords_Are_Inherited_And_Split_On_Commas()
        {
            var leaf = new OptionEntry(() => null, new SearchMetadata("Speech hue", Keywords: ["talk, chatter"]));
            var fragment = new OptionFragment(() => null, [leaf])
                .WithSearch(new SearchMetadata("Speech", Keywords: ["voice"]));

            SearchEntry entry = Index(fragment, "Chat")[0];

            entry.Keywords.Should().Contain("talk").And.Contain("chatter").And.Contain("voice");
        }

        [Fact]
        public void A_Whitespace_Only_Label_Is_Skipped()
        {
            // A localization that blanks a key would otherwise index a row with nothing to read.
            var blank = new OptionEntry(() => null, new SearchMetadata("   "));

            Index(blank, "Video").Should().BeEmpty();
        }

        [Fact]
        public void A_Search_Group_Named_Only_By_Whitespace_Is_Skipped()
        {
            var group = new OptionFragment(() => null, [Leaf("Border width")])
                .AsSearchGroup()
                .WithLabel("   ");

            Index(group, "Interface").Should().BeEmpty();
        }

        [Fact]
        public void A_Leaf_Matches_On_Tags_As_Well_As_Keywords()
        {
            var leaf = new OptionEntry(() => null, new SearchMetadata("Speech hue", Tags: ["chat"], Keywords: ["talk"]));

            Index(leaf, "Chat").Single().Keywords.Should().Contain("chat").And.Contain("talk");
        }

        [Fact]
        public void A_Search_Group_Matches_On_Tags_As_Well_As_Keywords()
        {
            // The group path used to read keywords alone, so the same metadata found a plain entry
            // but not a grouped one.
            var group = new OptionFragment(() => null, [Leaf("Border width")])
                .AsSearchGroup()
                .WithLabel("Grid highlighting")
                .WithSearch(new SearchMetadata("Grid highlighting", Tags: ["grid"], Keywords: ["outline"]));

            SearchEntry entry = Index(group, "Interface").Single();

            entry.Keywords.Should().Contain("grid").And.Contain("outline");
        }

        [Fact]
        public void A_Nested_Search_Group_Keeps_Its_Own_Name_In_The_Enclosing_Match_Text()
        {
            // A nested group carries its children's labels in its match text but not its own, so
            // taking only the match text left the outer group unable to answer a query naming it.
            var inner = new OptionFragment(() => null, [Leaf("Hide if full health")])
                .AsSearchGroup()
                .WithLabel("Nameplate health bar");

            var outer = new OptionFragment(() => null, [inner])
                .AsSearchGroup()
                .WithLabel("Nameplates");

            SearchEntry entry = Index(outer, "Interface").Single();

            entry.Label.Should().Be("Nameplates");
            entry.MatchText.Should().Contain("Nameplate health bar").And.Contain("Hide if full health");
        }

        [Fact]
        public void Category_Synonyms_Reach_A_Leaf_Without_Naming_It()
        {
            // Mirrors how the window seeds a category: metadata carrying synonyms and no label, so
            // one blob covers every setting underneath without retitling any of them.
            var tabs = new OptionTabGroup().AddTab("Lighting", () => Leaf("Light level"));
            var category = new OptionIndexContext(new SearchPath("Video"), new SearchMetadata(Keywords: ["graphics,gfx"]));

            SearchEntry entry = tabs.Index(category).Single();

            entry.Label.Should().Be("Light level");
            entry.Keywords.Should().Contain("graphics").And.Contain("gfx");
        }

        [Fact]
        public void A_Node_That_Does_Not_Inherit_Ignores_Ancestor_Metadata()
        {
            var leaf = new OptionEntry(() => null, new SearchMetadata("Light level")) { InheritsSearch = false };
            var fragment = new OptionFragment(() => null, [leaf])
                .WithSearch(new SearchMetadata("Lighting", Keywords: ["brightness"]));

            SearchEntry entry = Index(fragment, "Video")[0];

            entry.Label.Should().Be("Light level");
            entry.Keywords.Should().BeNull();
        }
    }
}
