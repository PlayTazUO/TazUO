using System.Text.Json;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers;

public class GridContainerSaveDataTests
{
    [Fact]
    public void HighlightsDisabledSurvivesSerializationRoundTrip()
    {
        var entry = new GridContainerEntry
        {
            Serial = 0x40000001,
            HighlightsDisabled = true
        };

        string json = JsonSerializer.Serialize(entry, GridContainerSerializerContext.Default.GridContainerEntry);
        GridContainerEntry restored = JsonSerializer.Deserialize(json, GridContainerSerializerContext.Default.GridContainerEntry);

        restored.HighlightsDisabled.Should().BeTrue();
    }

    [Fact]
    public void HighlightsDisabledIsScopedToOneContainer()
    {
        GridContainerEntry[] entries =
        [
            new GridContainerEntry { Serial = 0x40000001, HighlightsDisabled = true },
            new GridContainerEntry { Serial = 0x40000002 }
        ];

        string json = JsonSerializer.Serialize(entries, GridContainerSerializerContext.Default.GridContainerEntryArray);
        GridContainerEntry[] restored = JsonSerializer.Deserialize(json, GridContainerSerializerContext.Default.GridContainerEntryArray);

        restored.Should().ContainSingle(entry => entry.Serial == 0x40000001 && entry.HighlightsDisabled);
        restored.Should().ContainSingle(entry => entry.Serial == 0x40000002 && !entry.HighlightsDisabled);
    }

    [Fact]
    public void ExistingSaveWithoutHighlightOverrideKeepsHighlightsEnabled()
    {
        GridContainerEntry restored = JsonSerializer.Deserialize(
            "{\"s\":1073741825}",
            GridContainerSerializerContext.Default.GridContainerEntry
        );

        restored.HighlightsDisabled.Should().BeFalse();
    }
}
