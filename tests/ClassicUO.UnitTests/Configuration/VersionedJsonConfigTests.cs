#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.IO.Persistency;
using ClassicUO.IO.Persistency.Migrations;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

public class VersionedJsonConfigTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"versioned-json-config-tests-{Guid.NewGuid():N}");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private string PathFor(string name) => Path.Combine(_directory, name);

    [Fact]
    public void Load_Missing_File_Returns_Null_And_Creates_Nothing()
    {
        string path = PathFor("missing.json");
        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline([new NoOpMigration(1)]);

        TestConfig? result = VersionedJsonConfig.Load(path, TestConfigJsonContext.Default.TestConfig, pipeline);

        Assert.Null(result);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void Load_Where_Bind_Fails_Throws_Migration_Exception_Carrying_The_Json_Error()
    {
        Directory.CreateDirectory(_directory);
        string path = PathFor("unbindable.json");
        const string original = """{"name":"ok"}""";
        File.WriteAllText(path, original);

        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline([new AddUnboundPropertyMigration(1)]);

        ConfigMigrationException thrown = Assert.Throws<ConfigMigrationException>(
            () => VersionedJsonConfig.Load(path, ThrowingJsonContext.Default.Unbindable, pipeline)
        );

        // The bind failure is restated, not swallowed.
        Assert.IsType<JsonException>(thrown.InnerException);
        Assert.Contains(nameof(Unbindable), thrown.Message);
    }

    [Fact]
    public void Load_Where_Bind_Fails_Leaves_File_Byte_Identical()
    {
        Directory.CreateDirectory(_directory);
        string path = PathFor("unbindable.json");
        const string original = """{"name":"ok"}""";
        File.WriteAllText(path, original);

        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline([new AddUnboundPropertyMigration(1)]);

        Assert.Throws<ConfigMigrationException>(() => VersionedJsonConfig.Load(path, ThrowingJsonContext.Default.Unbindable, pipeline));

        Assert.Equal(original, File.ReadAllText(path));
    }

    [Fact]
    public void Load_Backs_The_PreMigration_File_Up_Into_The_Backup_Directory()
    {
        Directory.CreateDirectory(_directory);
        string path = PathFor("config.json");
        const string original = """{"Name":"original"}""";
        File.WriteAllText(path, original);

        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline([new NoOpMigration(1)]);

        TestConfig? result = VersionedJsonConfig.Load(path, TestConfigJsonContext.Default.TestConfig, pipeline);

        Assert.NotNull(result);

        string[] backups = Directory.GetFiles(Path.Combine(_directory, ConfigBackupStore.DirectoryName));

        // The pre-migration text, not the migrated one that replaced it on disk.
        Assert.Equal(original, File.ReadAllText(Assert.Single(backups)));
    }

    [Fact]
    public void Load_Migrates_And_Rewrites_File_When_Bind_Succeeds()
    {
        Directory.CreateDirectory(_directory);
        string path = PathFor("config.json");
        File.WriteAllText(path, """{"Name":"original"}""");

        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline([new NoOpMigration(1)]);

        TestConfig? result = VersionedJsonConfig.Load(path, TestConfigJsonContext.Default.TestConfig, pipeline);

        Assert.NotNull(result);
        Assert.Equal("original", result!.Name);

        JsonObject onDisk = (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(1, (int)onDisk["schema_version"]!);
    }

    private static ConfigMigrationPipeline<JsonObject> MakePipeline(IConfigMigration<JsonObject>[] migrations) =>
        new(new ConfigMigrationSequence<JsonObject>(migrations), new JsonMigrationFormat(Options));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test isolation.
        }
    }

    /// <summary>Changes nothing - present only to give the document a version to be carried to.</summary>
    private sealed class NoOpMigration(int version) : IConfigMigration<JsonObject>
    {
        public int Version { get; } = version;

        public void Up(JsonObject document)
        {
        }
    }

    private sealed class AddUnboundPropertyMigration(int version) : IConfigMigration<JsonObject>
    {
        public int Version { get; } = version;

        public void Up(JsonObject document) => document["RequiredNumber"] = "boom";
    }
}

public class TestConfig
{
    public string Name { get; set; } = string.Empty;
}

[JsonSerializable(typeof(TestConfig))]
internal sealed partial class TestConfigJsonContext : JsonSerializerContext
{
}

public class Unbindable
{
    public int RequiredNumber { get; set; }
}

[JsonSerializable(typeof(Unbindable))]
internal sealed partial class ThrowingJsonContext : JsonSerializerContext
{
}
