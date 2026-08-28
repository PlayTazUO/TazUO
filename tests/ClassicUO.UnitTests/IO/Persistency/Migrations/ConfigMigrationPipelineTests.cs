using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.IO.Persistency.Migrations;
using Xunit;

namespace ClassicUO.UnitTests.IO.Persistency.Migrations;

public class ConfigMigrationPipelineTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    [Fact]
    public void Migrate_Already_At_Latest_Returns_Original_Text_Byte_Identical()
    {
        var pipeline = new ConfigMigrationPipeline<JsonObject>(
            new ConfigMigrationSequence<JsonObject>([new AddPropertyMigration(1)]),
            new JsonMigrationFormat(Options)
        );

        const string text = """{"schema_version":1,"foo":"bar"}""";

        ConfigMigrationResult result = pipeline.Migrate(text);

        Assert.False(result.Changed);
        Assert.Same(text, result.Text);
        Assert.Equal(1, result.FromVersion);
        Assert.Equal(1, result.ToVersion);
    }

    [Theory]
    [InlineData("""{"foo":"bar"}""")]
    [InlineData("""{"schema_version":null,"foo":"bar"}""")]
    [InlineData("""{"schema_version":"nope","foo":"bar"}""")]
    public void ReadVersion_Treats_Absent_Null_And_NonNumeric_As_Zero(string text)
    {
        var pipeline = new ConfigMigrationPipeline<JsonObject>(
            new ConfigMigrationSequence<JsonObject>([new AddPropertyMigration(1)]),
            new JsonMigrationFormat(Options)
        );

        ConfigMigrationResult result = pipeline.Migrate(text);

        Assert.Equal(0, result.FromVersion);
        Assert.True(result.Changed);
    }

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("[1,2,3]")]
    public void Migrate_Malformed_Or_NonObject_Throws(string text)
    {
        var pipeline = new ConfigMigrationPipeline<JsonObject>(
            new ConfigMigrationSequence<JsonObject>([]),
            new JsonMigrationFormat(Options)
        );

        Assert.Throws<ConfigMigrationException>(() => pipeline.Migrate(text));
    }

    [Fact]
    public void Migrate_Runs_Migration_Writes_Version_And_Serializes()
    {
        var pipeline = new ConfigMigrationPipeline<JsonObject>(
            new ConfigMigrationSequence<JsonObject>([new AddPropertyMigration(1)]),
            new JsonMigrationFormat(Options)
        );

        ConfigMigrationResult result = pipeline.Migrate("""{"foo":"bar"}""");

        Assert.True(result.Changed);
        Assert.Equal(0, result.FromVersion);
        Assert.Equal(1, result.ToVersion);

        JsonObject reparsed = (JsonObject)JsonNode.Parse(result.Text)!;
        Assert.Equal(1, (int)reparsed["schema_version"]!);
        Assert.Equal("added", (string)reparsed["migrated"]!);
    }

    private sealed class AddPropertyMigration(int version) : IConfigMigration<JsonObject>
    {
        public int Version { get; } = version;

        public void Up(JsonObject document) => document["migrated"] = "added";
    }
}
