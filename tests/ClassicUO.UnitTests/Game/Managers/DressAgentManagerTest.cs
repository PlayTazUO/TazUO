using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class DressAgentManagerTest
    {
        #region DressConfig Tests

        [Fact]
        public void DressConfig_DefaultValues_ShouldBeEmpty()
        {
            // Arrange & Act
            var config = new DressConfig();

            // Assert
            config.Name.Should().BeEmpty();
            config.CharacterName.Should().BeEmpty();
            config.UndressBagSerial.Should().Be(0u);
            config.Items.Should().NotBeNull();
            config.Items.Should().BeEmpty();
            config.UseKREquipPacket.Should().BeFalse();
        }

        [Fact]
        public void DressConfig_Contains_WithExistingSerial_ShouldReturnTrue()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 123u, Name = "Test Item", Layer = 1 },
                    new DressItem { Serial = 456u, Name = "Another Item", Layer = 2 }
                }
            };

            // Act
            bool result = config.Contains(123u);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void DressConfig_Contains_WithNonExistingSerial_ShouldReturnFalse()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 123u, Name = "Test Item", Layer = 1 }
                }
            };

            // Act
            bool result = config.Contains(999u);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void DressConfig_Contains_WithEmptyItems_ShouldReturnFalse()
        {
            // Arrange
            var config = new DressConfig();

            // Act
            bool result = config.Contains(123u);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void DressConfig_SetProperties_ShouldPersist()
        {
            // Arrange
            var config = new DressConfig();

            // Act
            config.Name = "My Config";
            config.CharacterName = "TestChar";
            config.UndressBagSerial = 789u;
            config.UseKREquipPacket = true;

            // Assert
            config.Name.Should().Be("My Config");
            config.CharacterName.Should().Be("TestChar");
            config.UndressBagSerial.Should().Be(789u);
            config.UseKREquipPacket.Should().BeTrue();
        }

        #endregion

        #region DressItem Tests

        [Fact]
        public void DressItem_DefaultValues_ShouldBeEmpty()
        {
            // Arrange & Act
            var item = new DressItem();

            // Assert
            item.Serial.Should().Be(0u);
            item.Name.Should().BeEmpty();
            item.Layer.Should().Be(0);
        }

        [Fact]
        public void DressItem_SetProperties_ShouldPersist()
        {
            // Arrange
            var item = new DressItem();

            // Act
            item.Serial = 12345u;
            item.Name = "Platemail Chest";
            item.Layer = 13;

            // Assert
            item.Serial.Should().Be(12345u);
            item.Name.Should().Be("Platemail Chest");
            item.Layer.Should().Be(13);
        }

        #endregion

        #region Config Collection Tests

        [Fact]
        public void DressConfig_AddMultipleItems_ShouldMaintainOrder()
        {
            // Arrange
            var config = new DressConfig();
            var item1 = new DressItem { Serial = 100u, Name = "Item 1", Layer = 1 };
            var item2 = new DressItem { Serial = 200u, Name = "Item 2", Layer = 2 };
            var item3 = new DressItem { Serial = 300u, Name = "Item 3", Layer = 3 };

            // Act
            config.Items.Add(item1);
            config.Items.Add(item2);
            config.Items.Add(item3);

            // Assert
            config.Items.Should().HaveCount(3);
            config.Items[0].Should().Be(item1);
            config.Items[1].Should().Be(item2);
            config.Items[2].Should().Be(item3);
        }

        [Fact]
        public void DressConfig_RemoveItem_ShouldWork()
        {
            // Arrange
            var item1 = new DressItem { Serial = 100u, Name = "Item 1", Layer = 1 };
            var item2 = new DressItem { Serial = 200u, Name = "Item 2", Layer = 2 };
            var config = new DressConfig
            {
                Items = new List<DressItem> { item1, item2 }
            };

            // Act
            config.Items.Remove(item1);

            // Assert
            config.Items.Should().HaveCount(1);
            config.Items[0].Should().Be(item2);
            config.Contains(100u).Should().BeFalse();
            config.Contains(200u).Should().BeTrue();
        }

        [Fact]
        public void DressConfig_ClearItems_ShouldWork()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 100u, Name = "Item 1", Layer = 1 },
                    new DressItem { Serial = 200u, Name = "Item 2", Layer = 2 }
                }
            };

            // Act
            config.Items.Clear();

            // Assert
            config.Items.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases and Validation

        [Fact]
        public void DressConfig_Contains_WithMultipleSameSerials_ShouldReturnTrue()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 123u, Name = "Item 1", Layer = 1 },
                    new DressItem { Serial = 123u, Name = "Item 2", Layer = 2 } // Duplicate serial
                }
            };

            // Act
            bool result = config.Contains(123u);

            // Assert
            result.Should().BeTrue();
            config.Items.Count(i => i.Serial == 123u).Should().Be(2);
        }

        [Fact]
        public void DressConfig_WithSpecialCharactersInName_ShouldWork()
        {
            // Arrange & Act
            var config = new DressConfig
            {
                Name = "Config with \"quotes\" and 'apostrophes'",
                CharacterName = "Char-Name_123"
            };

            // Assert
            config.Name.Should().Be("Config with \"quotes\" and 'apostrophes'");
            config.CharacterName.Should().Be("Char-Name_123");
        }

        [Fact]
        public void DressItem_WithLongName_ShouldWork()
        {
            // Arrange & Act
            var item = new DressItem
            {
                Serial = 999u,
                Name = "This is a very long item name that might be used for testing purposes and should not cause any issues",
                Layer = 5
            };

            // Assert
            item.Name.Length.Should().BeGreaterThan(50);
            item.Serial.Should().Be(999u);
        }

        [Fact]
        public void DressConfig_WithMaxUIntSerial_ShouldWork()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = uint.MaxValue, Name = "Max Serial", Layer = 1 }
                }
            };

            // Act
            bool result = config.Contains(uint.MaxValue);

            // Assert
            result.Should().BeTrue();
            config.UndressBagSerial = uint.MaxValue;
            config.UndressBagSerial.Should().Be(uint.MaxValue);
        }

        [Fact]
        public void DressConfig_WithMaxByteLayer_ShouldWork()
        {
            // Arrange & Act
            var item = new DressItem
            {
                Serial = 123u,
                Name = "Test",
                Layer = byte.MaxValue
            };

            // Assert
            item.Layer.Should().Be(byte.MaxValue);
        }

        #endregion

        #region Bulk Operations

        [Fact]
        public void DressConfig_AddManyItems_ShouldHandleLargeCollections()
        {
            // Arrange
            var config = new DressConfig();
            const int itemCount = 100;

            // Act
            for (uint i = 0; i < itemCount; i++)
            {
                config.Items.Add(new DressItem
                {
                    Serial = i,
                    Name = $"Item {i}",
                    Layer = (byte)(i % byte.MaxValue)
                });
            }

            // Assert
            config.Items.Should().HaveCount(itemCount);
            for (uint i = 0; i < itemCount; i++)
            {
                config.Contains(i).Should().BeTrue();
            }
        }

        [Fact]
        public void DressConfig_FindItemsByLayer_ShouldWork()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 100u, Name = "Item 1", Layer = 5 },
                    new DressItem { Serial = 200u, Name = "Item 2", Layer = 5 },
                    new DressItem { Serial = 300u, Name = "Item 3", Layer = 10 }
                }
            };

            // Act
            var layer5Items = config.Items.Where(i => i.Layer == 5).ToList();

            // Assert
            layer5Items.Should().HaveCount(2);
            layer5Items.Should().Contain(i => i.Serial == 100u);
            layer5Items.Should().Contain(i => i.Serial == 200u);
        }

        #endregion

        #region Configuration Naming

        [Fact]
        public void DressConfig_EmptyName_ShouldBeAllowed()
        {
            // Arrange & Act
            var config = new DressConfig { Name = "" };

            // Assert
            config.Name.Should().BeEmpty();
        }

        [Fact]
        public void DressConfig_WhitespaceName_ShouldBeAllowed()
        {
            // Arrange & Act
            var config = new DressConfig { Name = "   " };

            // Assert
            config.Name.Should().Be("   ");
        }

        [Fact]
        public void DressConfig_UnicodeCharactersInName_ShouldWork()
        {
            // Arrange & Act
            var config = new DressConfig
            {
                Name = "装备配置 🛡️⚔️",
                CharacterName = "角色名"
            };

            // Assert
            config.Name.Should().Be("装备配置 🛡️⚔️");
            config.CharacterName.Should().Be("角色名");
        }

        #endregion

        #region Item Lookup Performance

        [Fact]
        public void DressConfig_Contains_WithZeroSerial_ShouldWork()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 0u, Name = "Zero Serial", Layer = 1 }
                }
            };

            // Act
            bool result = config.Contains(0u);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void DressConfig_MultipleContainsChecks_ShouldBeConsistent()
        {
            // Arrange
            var config = new DressConfig
            {
                Items = new List<DressItem>
                {
                    new DressItem { Serial = 123u, Name = "Test", Layer = 1 }
                }
            };

            // Act & Assert
            config.Contains(123u).Should().BeTrue();
            config.Contains(123u).Should().BeTrue(); // Second call should be same
            config.Contains(456u).Should().BeFalse();
            config.Contains(456u).Should().BeFalse(); // Second call should be same
        }

        #endregion
    }
}
