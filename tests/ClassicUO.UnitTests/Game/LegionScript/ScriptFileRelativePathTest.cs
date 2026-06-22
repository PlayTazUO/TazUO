using ClassicUO.LegionScripting;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript
{
    public class ScriptFileRelativePathTest
    {
        private const string Root = "C:/uo/LegionScripts";

        [Fact]
        public void StripsRoot_ForNormalFile()
        {
            ScriptFile.ToRelativeId(Root, "C:/uo/LegionScripts/group/sub/loot.py")
                .Should().Be("group/sub/loot.py");
        }

        [Fact]
        public void NormalizesBackslashes()
        {
            ScriptFile.ToRelativeId(@"C:\uo\LegionScripts", @"C:\uo\LegionScripts\group\loot.py")
                .Should().Be("group/loot.py");
        }

        [Fact]
        public void KeepsZipEntryForm()
        {
            ScriptFile.ToRelativeId(Root, "C:/uo/LegionScripts/pack.zip::entry/x.py")
                .Should().Be("pack.zip::entry/x.py");
        }

        [Fact]
        public void SameNameDifferentGroups_AreDistinct()
        {
            string a = ScriptFile.ToRelativeId(Root, "C:/uo/LegionScripts/groupA/loot.py");
            string b = ScriptFile.ToRelativeId(Root, "C:/uo/LegionScripts/groupB/loot.py");
            a.Should().NotBe(b);
        }

        [Fact]
        public void EmptyFullPath_ReturnsEmpty()
        {
            ScriptFile.ToRelativeId(Root, "").Should().BeEmpty();
        }

        [Fact]
        public void RootMatchedAsPrefixOnly_NotMidPath()
        {
            // The root appears mid-path, not as a prefix, so nothing should be stripped.
            ScriptFile.ToRelativeId(Root, "D:/elsewhere/C:/uo/LegionScripts/x.py")
                .Should().Be("D:/elsewhere/C:/uo/LegionScripts/x.py");
        }

        [Fact]
        public void SiblingDirSharingRootPrefix_NotStripped()
        {
            // A sibling dir whose name merely shares the root as a string prefix
            // (LegionScripts2) must NOT be stripped — only a real path-segment boundary counts.
            ScriptFile.ToRelativeId(Root, "C:/uo/LegionScripts2/loot.py")
                .Should().Be("C:/uo/LegionScripts2/loot.py");
        }
    }
}
