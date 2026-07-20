using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Configuration;
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
        public void AllFields_RoundTripThroughColumns()
        {
            var entry = new GridHighlightSetupEntry
            {
                Enabled = false,
                Name = "everything",
                Hue = 1234,
                HighlightColor = "#123456",
                AcceptExtraProperties = false,
                Overweight = true,
                MinimumWeight = 5,
                MaximumWeight = 50,
                MinimumProperty = 2,
                MaximumProperty = 8,
                MinimumMatchingProperty = 3,
                MaximumMatchingProperty = 7,
                LootOnMatch = true,
                DestinationContainer = 0xDEADBEEF, // exercises the full uint range
                IsHighlightProperties = false,
                ItemNames = new List<string> { "kryss", "war fork" },
                ExcludeNegatives = new List<string> { "cursed", "brittle" },
                RequiredRarities = new List<string> { "Legendary Artifact" },
                Properties = new List<GridHighlightProperty>
                {
                    new() { Name = "Hit Chance Increase", MinValue = 15, IsOptional = false },
                    new() { Name = "Damage Increase", MinValue = -1, IsOptional = true }
                },
                GridHighlightSlot = new GridHighlightSlot
                {
                    Talisman = false,
                    Ring = false,
                    Other = true
                    // remaining slots keep their (true) defaults
                }
            };

            _db.Save(new List<GridHighlightSetupEntry> { entry });

            GridHighlightSetupEntry loaded = _db.Load().Should().ContainSingle().Subject;

            loaded.Should().BeEquivalentTo(entry, options => options
                .Excluding(e => e.HighlightColor)); // stored/compared below case-insensitively

            loaded.HighlightColor.Should().BeEquivalentTo("#123456");
            loaded.DestinationContainer.Should().Be(0xDEADBEEF);
            loaded.GridHighlightSlot.Talisman.Should().BeFalse();
            loaded.GridHighlightSlot.Ring.Should().BeFalse();
            loaded.GridHighlightSlot.Other.Should().BeTrue();
            loaded.GridHighlightSlot.Head.Should().BeTrue();
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

        [Fact]
        public void LoadForProfile_KeepsSeededRules_WhenDatabaseAndLegacyEmpty()
        {
            // Simulates a profile seeded from a default template: no rows yet in its own DB, no legacy JSON.
            var profile = new Profile { GridHighlightSetup = new List<GridHighlightSetupEntry> { MakeEntry("from-default", 7) } };

            bool migrated = _db.LoadForProfile(profile);

            migrated.Should().BeTrue();
            profile.GridHighlightSetup.Should().ContainSingle().Which.Name.Should().Be("from-default");
            _db.Load().Should().ContainSingle().Which.Name.Should().Be("from-default");
        }

        [Fact]
        public void LoadForProfile_PrefersStoredRulesOverSeeded()
        {
            _db.Save(new List<GridHighlightSetupEntry> { MakeEntry("stored", 1) });

            var profile = new Profile { GridHighlightSetup = new List<GridHighlightSetupEntry> { MakeEntry("seeded", 2) } };

            bool migrated = _db.LoadForProfile(profile);

            migrated.Should().BeFalse();
            profile.GridHighlightSetup.Should().ContainSingle().Which.Name.Should().Be("stored");
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
