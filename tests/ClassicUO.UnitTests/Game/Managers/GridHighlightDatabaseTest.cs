using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class GridHighlightDatabaseTest : IDisposable
    {
        private readonly string _tempDir;
        private readonly GridHighlightDatabase _db;

        public GridHighlightDatabaseTest()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "tazuo_gridhl_test_" + Guid.NewGuid().ToString("N"));
            _db = new GridHighlightDatabase(_tempDir);
        }

        private static GridHighlightSetupEntry MakeEntry(string name, ushort hue, params string[] itemNames)
        {
            var entry = new GridHighlightSetupEntry
            {
                Name = name,
                Hue = hue,
                HighlightColor = "#00FF00",
                ItemNames = new List<string>(itemNames),
                Properties = new List<GridHighlightProperty>
                {
                    new() { Name = "Hit Chance Increase", MinValue = 10, IsOptional = false }
                }
            };

            return entry;
        }

        [Fact]
        public void Constructor_WritesDatabaseIntoProfileFolder()
        {
            _db.Save(new List<GridHighlightSetupEntry> { MakeEntry("a", 1) });
            File.Exists(Path.Combine(_tempDir, "gridhighlights.db")).Should().BeTrue();
        }

        [Fact]
        public void Save_Then_Load_RoundTripsEntriesInOrder()
        {
            var entries = new List<GridHighlightSetupEntry>
            {
                MakeEntry("first", 10, "kryss"),
                MakeEntry("second", 20, "war fork", "spear")
            };

            _db.Save(entries);

            List<GridHighlightSetupEntry> loaded = _db.Load();

            loaded.Should().HaveCount(2);
            loaded[0].Name.Should().Be("first");
            loaded[0].Hue.Should().Be(10);
            loaded[0].ItemNames.Should().ContainSingle().Which.Should().Be("kryss");
            loaded[0].Properties.Should().ContainSingle();
            loaded[0].Properties[0].Name.Should().Be("Hit Chance Increase");
            loaded[0].Properties[0].MinValue.Should().Be(10);

            loaded[1].Name.Should().Be("second");
            loaded[1].ItemNames.Should().BeEquivalentTo(new[] { "war fork", "spear" });
        }

        [Fact]
        public void Save_ReplacesPreviousEntries()
        {
            _db.Save(new List<GridHighlightSetupEntry>
            {
                MakeEntry("a", 1),
                MakeEntry("b", 2),
                MakeEntry("c", 3)
            });

            // A shorter set must fully replace the old one, leaving no stale rows.
            _db.Save(new List<GridHighlightSetupEntry> { MakeEntry("only", 9) });

            List<GridHighlightSetupEntry> loaded = _db.Load();
            loaded.Should().ContainSingle().Which.Name.Should().Be("only");
        }

        [Fact]
        public void SeparateProfileFolders_HaveIndependentDatabases()
        {
            string otherDir = Path.Combine(Path.GetTempPath(), "tazuo_gridhl_test_" + Guid.NewGuid().ToString("N"));
            using var other = new GridHighlightDatabase(otherDir);

            try
            {
                _db.Save(new List<GridHighlightSetupEntry> { MakeEntry("mine", 1) });
                other.Save(new List<GridHighlightSetupEntry> { MakeEntry("theirs1", 2), MakeEntry("theirs2", 3) });

                _db.Load().Should().ContainSingle().Which.Name.Should().Be("mine");
                other.Load().Should().HaveCount(2);
            }
            finally
            {
                other.Dispose();
                try { Directory.Delete(otherDir, true); } catch { /* best effort */ }
            }
        }

        [Fact]
        public void Load_EmptyDatabase_ReturnsEmpty()
        {
            _db.Load().Should().BeEmpty();
        }

        [Fact]
        public void Save_EmptyList_ClearsDatabase()
        {
            _db.Save(new List<GridHighlightSetupEntry> { MakeEntry("a", 1) });
            _db.Save(new List<GridHighlightSetupEntry>());

            _db.Load().Should().BeEmpty();
        }

        public void Dispose()
        {
            _db.Dispose();

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
