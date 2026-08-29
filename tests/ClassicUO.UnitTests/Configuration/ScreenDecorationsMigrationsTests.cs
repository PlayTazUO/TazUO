using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Exercises <see cref="MultiValueTriggerSelectorsMigration" />, which carries a rule authored before
/// <c>sound_played</c>/<c>buff_changed</c> went multi-select forward onto their list-valued fields.
/// </summary>
public class ScreenDecorationsMigrationsTests
{
    [Fact]
    public void SoundIndex_Becomes_A_OneElement_SoundIndexes_List()
    {
        var document = (JsonObject)JsonNode.Parse(
            """{"overlays":{"rules":[{"trigger":{"parameters":{"kind":"sound_played","sound_index":755}}}]}}"""
        )!;

        new MultiValueTriggerSelectorsMigration().Up(document);

        JsonObject parameters = (JsonObject)document["overlays"]!["rules"]![0]!["trigger"]!["parameters"]!;

        parameters["sound_index"].Should().BeNull();
        ((JsonArray)parameters["sound_indexes"]!).Select(node => (int)node!).Should().Equal(755);
    }

    [Fact]
    public void BuffType_Becomes_A_OneElement_BuffTypes_List()
    {
        var document = (JsonObject)JsonNode.Parse(
            """{"overlays":{"rules":[{"trigger":{"parameters":{"kind":"buff_changed","buff_type":5}}}]}}"""
        )!;

        new MultiValueTriggerSelectorsMigration().Up(document);

        JsonObject parameters = (JsonObject)document["overlays"]!["rules"]![0]!["trigger"]!["parameters"]!;

        parameters["buff_type"].Should().BeNull();
        ((JsonArray)parameters["buff_types"]!).Select(node => (int)node!).Should().Equal(5);
    }

    [Fact]
    public void A_Rule_Of_A_Kind_The_Migration_Does_Not_Know_Is_Left_Alone()
    {
        var document = (JsonObject)JsonNode.Parse(
            """{"overlays":{"rules":[{"trigger":{"parameters":{"kind":"chat_message","pattern":"hi"}}}]}}"""
        )!;

        new MultiValueTriggerSelectorsMigration().Up(document);

        JsonObject parameters = (JsonObject)document["overlays"]!["rules"]![0]!["trigger"]!["parameters"]!;

        parameters["pattern"]!.GetValue<string>().Should().Be("hi");
    }

    [Fact]
    public void A_Document_With_No_Overlays_Rules_Is_Left_Alone()
    {
        var document = (JsonObject)JsonNode.Parse("""{"enabled":true}""")!;

        var act = () => new MultiValueTriggerSelectorsMigration().Up(document);

        act.Should().NotThrow();
        document["enabled"]!.GetValue<bool>().Should().BeTrue();
    }

    /// <summary>End-to-end through <see cref="ScreenDecorations.LoadForProfile" />: a file predating
    /// the multi-select rename loads with the old value carried into the new list.</summary>
    [Fact]
    public void LoadForProfile_Migrates_A_PreMultiSelect_SoundPlayedRule()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"screen-decorations-migration-tests-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, ScreenDecorations.FileName);
            File.WriteAllText(
                path,
                """{"enabled":true,"overlays":{"rules":[{"trigger":{"parameters":{"kind":"sound_played","sound_index":755}}}]}}"""
            );

            ScreenDecorations loaded = ScreenDecorations.LoadForProfile(directory);

            loaded.SchemaVersion.Should().Be(ScreenDecorationsMigrations.LatestVersion);

            var parameters = loaded.Overlays.Rules[0].Trigger.Parameters
                .Should().BeOfType<SoundPlayedParameters>().Subject;

            parameters.SoundIndexes.Should().Equal(755);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
