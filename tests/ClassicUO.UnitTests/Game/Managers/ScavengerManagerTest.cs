using System;
using System.Collections.Generic;
using System.Text.Json;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class ScavengerManagerTest
    {
        #region ScavengerEntry - Default Values Tests

        [Fact]
        public void ScavengerEntry_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var entry = new ScavengerManager.ScavengerEntry();

            // Assert
            entry.Name.Should().BeEmpty();
            entry.Graphic.Should().Be(0);
            entry.Hue.Should().Be(ushort.MaxValue);
            entry.RegexSearch.Should().BeEmpty();
            entry.DestinationContainer.Should().Be(0u);
            entry.MaxAmount.Should().Be(0);
            entry.Uid.Should().NotBeEmpty();
        }

        [Fact]
        public void ScavengerEntry_Uid_ShouldBeUniqueAndValidGuid()
        {
            // Arrange & Act
            var entry1 = new ScavengerManager.ScavengerEntry();
            var entry2 = new ScavengerManager.ScavengerEntry();

            // Assert
            entry1.Uid.Should().NotBe(entry2.Uid);
            Guid.TryParse(entry1.Uid, out _).Should().BeTrue();
        }

        #endregion

        #region ScavengerEntry - Property Setting Tests

        [Fact]
        public void ScavengerEntry_SetProperties_ShouldPersist()
        {
            // Arrange
            var entry = new ScavengerManager.ScavengerEntry();
            string testUid = Guid.NewGuid().ToString();

            // Act
            entry.Name = "Gold Coin";
            entry.Graphic = 3821;
            entry.Hue = 1153;
            entry.RegexSearch = ".*gold.*";
            entry.DestinationContainer = 12345u;
            entry.MaxAmount = 100;
            entry.Priority = ScavengerManager.ScavengerPriority.High;
            entry.Uid = testUid;

            // Assert
            entry.Name.Should().Be("Gold Coin");
            entry.Graphic.Should().Be(3821);
            entry.Hue.Should().Be(1153);
            entry.RegexSearch.Should().Be(".*gold.*");
            entry.DestinationContainer.Should().Be(12345u);
            entry.MaxAmount.Should().Be(100);
            entry.Priority.Should().Be(ScavengerManager.ScavengerPriority.High);
            entry.Uid.Should().Be(testUid);
        }

        #endregion

        #region ScavengerEntry - Equals Tests

        [Fact]
        public void ScavengerEntry_Equals_WithSameProperties_ShouldReturnTrue()
        {
            // Arrange
            var entry1 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = ".*gold.*" };
            var entry2 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = ".*gold.*" };

            // Act & Assert
            entry1.Equals(entry2).Should().BeTrue();
        }

        [Fact]
        public void ScavengerEntry_Equals_WithDifferentGraphic_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = "" };
            var entry2 = new ScavengerManager.ScavengerEntry { Graphic = 3822, Hue = 1153, RegexSearch = "" };

            // Act & Assert
            entry1.Equals(entry2).Should().BeFalse();
        }

        [Fact]
        public void ScavengerEntry_Equals_WithDifferentHue_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = "" };
            var entry2 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1154, RegexSearch = "" };

            // Act & Assert
            entry1.Equals(entry2).Should().BeFalse();
        }

        [Fact]
        public void ScavengerEntry_Equals_WithDifferentRegexSearch_ShouldReturnFalse()
        {
            // Arrange
            var entry1 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = ".*gold.*" };
            var entry2 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = ".*silver.*" };

            // Act & Assert
            entry1.Equals(entry2).Should().BeFalse();
        }

        [Fact]
        public void ScavengerEntry_Equals_IgnoresNameDestinationAndPriority()
        {
            // Arrange
            var entry1 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = "", Name = "A", DestinationContainer = 100u, Priority = ScavengerManager.ScavengerPriority.Low };
            var entry2 = new ScavengerManager.ScavengerEntry { Graphic = 3821, Hue = 1153, RegexSearch = "", Name = "B", DestinationContainer = 200u, Priority = ScavengerManager.ScavengerPriority.High };

            // Act & Assert
            entry1.Equals(entry2).Should().BeTrue();
        }

        #endregion

        #region ScavengerEntry - JSON Serialization Tests

        [Fact]
        public void ScavengerEntry_Serialization_ShouldPreserveAllProperties()
        {
            // Arrange
            var entry = new ScavengerManager.ScavengerEntry
            {
                Name = "Test Item",
                Graphic = 1234,
                Hue = 5678,
                RegexSearch = ".*test.*",
                DestinationContainer = 99999u,
                MaxAmount = 250,
                Priority = ScavengerManager.ScavengerPriority.High,
                Uid = "test-uid-123"
            };

            // Act
            string json = JsonSerializer.Serialize(entry, ScavengerJsonContext.Default.ScavengerEntry);
            ScavengerManager.ScavengerEntry deserialized = JsonSerializer.Deserialize(json, ScavengerJsonContext.Default.ScavengerEntry);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().Be(entry.Name);
            deserialized.Graphic.Should().Be(entry.Graphic);
            deserialized.Hue.Should().Be(entry.Hue);
            deserialized.RegexSearch.Should().Be(entry.RegexSearch);
            deserialized.DestinationContainer.Should().Be(entry.DestinationContainer);
            deserialized.MaxAmount.Should().Be(entry.MaxAmount);
            deserialized.Priority.Should().Be(entry.Priority);
            deserialized.Uid.Should().Be(entry.Uid);
        }

        [Fact]
        public void ScavengerEntry_ListSerialization_ShouldWork()
        {
            // Arrange
            var list = new List<ScavengerManager.ScavengerEntry>
            {
                new() { Name = "Item 1", Graphic = 100, Hue = 200 },
                new() { Name = "Item 2", Graphic = 300, Hue = 400 }
            };

            // Act
            string json = JsonSerializer.Serialize(list, ScavengerJsonContext.Default.ListScavengerEntry);
            List<ScavengerManager.ScavengerEntry> deserialized = JsonSerializer.Deserialize(json, ScavengerJsonContext.Default.ListScavengerEntry);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().HaveCount(2);
            deserialized[0].Name.Should().Be("Item 1");
            deserialized[1].Name.Should().Be("Item 2");
            deserialized[1].Graphic.Should().Be(300);
        }

        #endregion

        #region ScavengerList / ScavengerData Tests

        [Fact]
        public void ScavengerList_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var list = new ScavengerManager.ScavengerList();

            // Assert
            list.Name.Should().BeEmpty();
            list.Entries.Should().NotBeNull();
            list.Entries.Should().BeEmpty();
            list.Uid.Should().NotBeEmpty();
            Guid.TryParse(list.Uid, out _).Should().BeTrue();
        }

        [Fact]
        public void ScavengerData_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var data = new ScavengerManager.ScavengerData();

            // Assert
            data.Lists.Should().NotBeNull();
            data.Lists.Should().BeEmpty();
        }

        [Fact]
        public void ScavengerData_Serialization_ShouldPreserveLists()
        {
            // Arrange
            var data = new ScavengerManager.ScavengerData();
            data.Lists.Add(new ScavengerManager.ScavengerList
            {
                Name = "Default",
                Uid = "list-1",
                Entries =
                {
                    new ScavengerManager.ScavengerEntry { Name = "Gold", Graphic = 3821, Hue = 0 }
                }
            });

            // Act
            string json = JsonSerializer.Serialize(data, ScavengerJsonContext.Default.ScavengerData);
            ScavengerManager.ScavengerData deserialized = JsonSerializer.Deserialize(json, ScavengerJsonContext.Default.ScavengerData);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Lists.Should().HaveCount(1);
            deserialized.Lists[0].Name.Should().Be("Default");
            deserialized.Lists[0].Uid.Should().Be("list-1");
            deserialized.Lists[0].Entries.Should().HaveCount(1);
            deserialized.Lists[0].Entries[0].Name.Should().Be("Gold");
            deserialized.Lists[0].Entries[0].Graphic.Should().Be(3821);
        }

        #endregion
    }
}
