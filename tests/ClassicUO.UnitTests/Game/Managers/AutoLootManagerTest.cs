using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class AutoLootManagerTest
    {
        #region AutoLootConfigEntry - Default Values Tests

        [Fact]
        public void AutoLootConfigEntry_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry();

            // Assert
            entry.Name.Should().BeEmpty();
            entry.Graphic.Should().Be(0);
            entry.Hue.Should().Be(ushort.MaxValue);
            entry.RegexSearch.Should().BeEmpty();
            entry.DestinationContainer.Should().Be(0u);
            entry.Uid.Should().NotBeEmpty();
        }

        [Fact]
        public void AutoLootConfigEntry_Uid_ShouldBeUnique()
        {
            // Arrange & Act
            var entry1 = new AutoLootManager.AutoLootConfigEntry();
            var entry2 = new AutoLootManager.AutoLootConfigEntry();

            // Assert
            entry1.Uid.Should().NotBe(entry2.Uid);
        }

        [Fact]
        public void AutoLootConfigEntry_Uid_ShouldBeValidGuid()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry();

            // Assert
            Guid.TryParse(entry.Uid, out _).Should().BeTrue();
        }

        #endregion

        #region AutoLootConfigEntry - Property Setting Tests

        [Fact]
        public void AutoLootConfigEntry_SetProperties_ShouldPersist()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry();
            string testUid = Guid.NewGuid().ToString();

            // Act
            entry.Name = "Gold Coin";
            entry.Graphic = 3821;
            entry.Hue = 1153;
            entry.RegexSearch = ".*gold.*";
            entry.DestinationContainer = 12345u;
            entry.Uid = testUid;

            // Assert
            entry.Name.Should().Be("Gold Coin");
            entry.Graphic.Should().Be(3821);
            entry.Hue.Should().Be(1153);
            entry.RegexSearch.Should().Be(".*gold.*");
            entry.DestinationContainer.Should().Be(12345u);
            entry.Uid.Should().Be(testUid);
        }

        [Fact]
        public void AutoLootConfigEntry_SetNegativeGraphic_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = -1
            };

            // Assert
            entry.Graphic.Should().Be(-1);
        }

        [Fact]
        public void AutoLootConfigEntry_WithMaxUIntDestinationContainer_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                DestinationContainer = uint.MaxValue
            };

            // Assert
            entry.DestinationContainer.Should().Be(uint.MaxValue);
        }

        #endregion

        #region AutoLootConfigEntry - Equals Tests

        [Fact]
        public void AutoLootConfigEntry_Equals_WithSameProperties_ShouldReturnTrue()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ".*gold.*"
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ".*gold.*"
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_WithDifferentGraphic_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ""
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3822,
                Hue = 1153,
                RegexSearch = ""
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_WithDifferentHue_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ""
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1154,
                RegexSearch = ""
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_WithDifferentRegexSearch_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ".*gold.*"
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = ".*silver.*"
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_IgnoresNameAndDestination()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = "",
                Name = "Entry 1",
                DestinationContainer = 100u
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 3821,
                Hue = 1153,
                RegexSearch = "",
                Name = "Entry 2",
                DestinationContainer = 200u
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_WithEmptyRegexSearch_ShouldReturnTrue()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 0,
                RegexSearch = ""
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 0,
                RegexSearch = string.Empty
            };

            // Act
            bool result = entry1.Equals(entry2);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region AutoLootConfigEntry - String Handling Tests

        [Fact]
        public void AutoLootConfigEntry_WithSpecialCharactersInName_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = "Item with \"quotes\" and 'apostrophes'"
            };

            // Assert
            entry.Name.Should().Be("Item with \"quotes\" and 'apostrophes'");
        }

        [Fact]
        public void AutoLootConfigEntry_WithUnicodeCharactersInName_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = "黄金 💰 Gold"
            };

            // Assert
            entry.Name.Should().Be("黄金 💰 Gold");
        }

        [Fact]
        public void AutoLootConfigEntry_WithLongName_ShouldWork()
        {
            // Arrange
            string longName = new('A', 1000);

            // Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = longName
            };

            // Assert
            entry.Name.Should().Be(longName);
            entry.Name.Length.Should().Be(1000);
        }

        [Fact]
        public void AutoLootConfigEntry_WithComplexRegexPattern_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                RegexSearch = @"^(?=.*\bvanq\b)(?=.*\bkatana\b).*$"
            };

            // Assert
            entry.RegexSearch.Should().Be(@"^(?=.*\bvanq\b)(?=.*\bkatana\b).*$");
        }

        #endregion

        #region AutoLootConfigEntry - Edge Cases

        [Fact]
        public void AutoLootConfigEntry_WithZeroGraphic_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 0
            };

            // Assert
            entry.Graphic.Should().Be(0);
        }

        [Fact]
        public void AutoLootConfigEntry_WithZeroHue_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Hue = 0
            };

            // Assert
            entry.Hue.Should().Be(0);
        }

        [Fact]
        public void AutoLootConfigEntry_WithMaxHue_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Hue = ushort.MaxValue
            };

            // Assert
            entry.Hue.Should().Be(ushort.MaxValue);
        }

        [Fact]
        public void AutoLootConfigEntry_WithNullName_ShouldNotThrow()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry();

            // Act
            Action act = () => entry.Name = null;

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AutoLootConfigEntry_WithNullRegexSearch_ShouldNotThrow()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry();

            // Act
            Action act = () => entry.RegexSearch = null;

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public void AutoLootConfigEntry_Serialization_ShouldPreserveAllProperties()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = "Test Item",
                Graphic = 1234,
                Hue = 5678,
                RegexSearch = ".*test.*",
                DestinationContainer = 99999u,
                Uid = "test-uid-123"
            };

            // Act
            string json = JsonSerializer.Serialize(entry, AutoLootJsonContext.Default.AutoLootConfigEntry);
            AutoLootManager.AutoLootConfigEntry deserialized = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootConfigEntry);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().Be(entry.Name);
            deserialized.Graphic.Should().Be(entry.Graphic);
            deserialized.Hue.Should().Be(entry.Hue);
            deserialized.RegexSearch.Should().Be(entry.RegexSearch);
            deserialized.DestinationContainer.Should().Be(entry.DestinationContainer);
            deserialized.Uid.Should().Be(entry.Uid);
        }

        [Fact]
        public void AutoLootConfigEntry_ListSerialization_ShouldWork()
        {
            // Arrange
            var list = new List<AutoLootManager.AutoLootConfigEntry>
            {
                new()
                {
                    Name = "Item 1",
                    Graphic = 100,
                    Hue = 200
                },
                new()
                {
                    Name = "Item 2",
                    Graphic = 300,
                    Hue = 400
                }
            };

            // Act
            string json = JsonSerializer.Serialize(list, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
            List<AutoLootManager.AutoLootConfigEntry> deserialized = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.ListAutoLootConfigEntry);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().HaveCount(2);
            deserialized[0].Name.Should().Be("Item 1");
            deserialized[0].Graphic.Should().Be(100);
            deserialized[1].Name.Should().Be("Item 2");
            deserialized[1].Graphic.Should().Be(300);
        }

        [Fact]
        public void AutoLootConfigEntry_Serialization_WithSpecialCharacters_ShouldWork()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = "Item with \"quotes\" and \nnewlines\t and tabs",
                RegexSearch = @"^\s*test\s*$"
            };

            // Act
            string json = JsonSerializer.Serialize(entry, AutoLootJsonContext.Default.AutoLootConfigEntry);
            AutoLootManager.AutoLootConfigEntry deserialized = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootConfigEntry);

            // Assert
            deserialized.Name.Should().Be(entry.Name);
            deserialized.RegexSearch.Should().Be(entry.RegexSearch);
        }

        [Fact]
        public void AutoLootConfigEntry_Serialization_WithDefaultValues_ShouldWork()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry();

            // Act
            string json = JsonSerializer.Serialize(entry, AutoLootJsonContext.Default.AutoLootConfigEntry);
            AutoLootManager.AutoLootConfigEntry deserialized = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootConfigEntry);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().BeEmpty();
            deserialized.Graphic.Should().Be(0);
            deserialized.Hue.Should().Be(ushort.MaxValue);
            deserialized.RegexSearch.Should().BeEmpty();
            deserialized.DestinationContainer.Should().Be(0);
        }

        #endregion

        #region Collection Tests

        [Fact]
        public void AutoLootConfigEntry_InList_ShouldSupportMultipleEntries()
        {
            // Arrange
            var list = new List<AutoLootManager.AutoLootConfigEntry>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                list.Add(new AutoLootManager.AutoLootConfigEntry
                {
                    Name = $"Item {i}",
                    Graphic = i,
                    Hue = (ushort)i
                });
            }

            // Assert
            list.Should().HaveCount(100);
            list[50].Name.Should().Be("Item 50");
            list[99].Graphic.Should().Be(99);
        }

        [Fact]
        public void AutoLootConfigEntry_FindInList_UsingEquals_ShouldWork()
        {
            // Arrange
            var target = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 500,
                Hue = 600,
                RegexSearch = ".*test.*"
            };
            var list = new List<AutoLootManager.AutoLootConfigEntry>
            {
                new() { Graphic = 100, Hue = 200, RegexSearch = "" },
                new() { Graphic = 500, Hue = 600, RegexSearch = ".*test.*" },
                new() { Graphic = 700, Hue = 800, RegexSearch = "" }
            };

            // Act
            bool found = false;
            foreach (AutoLootManager.AutoLootConfigEntry entry in list)
            {
                if (entry.Equals(target))
                {
                    found = true;
                    break;
                }
            }

            // Assert
            found.Should().BeTrue();
        }

        [Fact]
        public void AutoLootConfigEntry_RemoveFromList_ShouldWork()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry { Name = "Item 1", Graphic = 100 };
            var entry2 = new AutoLootManager.AutoLootConfigEntry { Name = "Item 2", Graphic = 200 };
            var list = new List<AutoLootManager.AutoLootConfigEntry> { entry1, entry2 };

            // Act
            list.Remove(entry1);

            // Assert
            list.Should().HaveCount(1);
            list[0].Should().Be(entry2);
        }

        #endregion

        #region Boundary Value Tests

        [Fact]
        public void AutoLootConfigEntry_WithMaxInt32Graphic_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = int.MaxValue
            };

            // Assert
            entry.Graphic.Should().Be(int.MaxValue);
        }

        [Fact]
        public void AutoLootConfigEntry_WithMinInt32Graphic_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = int.MinValue
            };

            // Assert
            entry.Graphic.Should().Be(int.MinValue);
        }

        [Fact]
        public void AutoLootConfigEntry_WithZeroDestinationContainer_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                DestinationContainer = 0u
            };

            // Assert
            entry.DestinationContainer.Should().Be(0u);
        }

        #endregion

        #region Equals Consistency Tests

        [Fact]
        public void AutoLootConfigEntry_Equals_ShouldBeReflexive()
        {
            // Arrange
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };

            // Act & Assert
            entry.Equals(entry).Should().BeTrue();
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_ShouldBeSymmetric()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };

            // Act & Assert
            entry1.Equals(entry2).Should().Be(entry2.Equals(entry1));
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_ShouldBeTransitive()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };
            var entry3 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };

            // Act & Assert
            if (entry1.Equals(entry2) && entry2.Equals(entry3))
            {
                entry1.Equals(entry3).Should().BeTrue();
            }
        }

        [Fact]
        public void AutoLootConfigEntry_Equals_MultipleCallsShouldBeConsistent()
        {
            // Arrange
            var entry1 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };
            var entry2 = new AutoLootManager.AutoLootConfigEntry
            {
                Graphic = 100,
                Hue = 200,
                RegexSearch = "test"
            };

            // Act
            bool result1 = entry1.Equals(entry2);
            bool result2 = entry1.Equals(entry2);
            bool result3 = entry1.Equals(entry2);

            // Assert
            result1.Should().Be(result2);
            result2.Should().Be(result3);
        }

        #endregion

        #region Empty and Null String Tests

        [Fact]
        public void AutoLootConfigEntry_WithEmptyStrings_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = string.Empty,
                RegexSearch = string.Empty
            };

            // Assert
            entry.Name.Should().BeEmpty();
            entry.RegexSearch.Should().BeEmpty();
        }

        [Fact]
        public void AutoLootConfigEntry_WithWhitespaceStrings_ShouldWork()
        {
            // Arrange & Act
            var entry = new AutoLootManager.AutoLootConfigEntry
            {
                Name = "   ",
                RegexSearch = "\t\n\r"
            };

            // Assert
            entry.Name.Should().Be("   ");
            entry.RegexSearch.Should().Be("\t\n\r");
        }

        #endregion
    }
}
