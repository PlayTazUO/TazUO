#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Configuration;
using ClassicUO.IO.Persistency;
using ClassicUO.IO.Persistency.Migrations;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>Covers the migrating overload of <see cref="ConfigurationResolver" />: the loader raises,
/// the resolver answers with the same null-and-back-up contract as the plain load.</summary>
public class ConfigurationResolverMigrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"resolver-migration-tests-{Guid.NewGuid():N}");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    [Fact]
    public void Load_Missing_File_Returns_Null()
    {
        ConfigMigrationPipeline<JsonObject> pipeline = MakePipeline(new NoOpMigration());

        TestConfig? result = ConfigurationResolver.Load(
            Path.Combine(_directory, "missing.json"),
            TestConfigJsonContext.Default.TestConfig,
            pipeline
        );

        Assert.Null(result);
    }

    [Fact]
    public void Load_Migrates_And_Binds()
    {
        string path = WriteConfig("config.json", """{"Name":"original"}""");

        TestConfig? result = ConfigurationResolver.Load(
            path,
            TestConfigJsonContext.Default.TestConfig,
            MakePipeline(new NoOpMigration())
        );

        Assert.Equal("original", result?.Name);
        Assert.Equal(1, (int)((JsonObject)JsonNode.Parse(File.ReadAllText(path))!)["schema_version"]!);
    }

    [Fact]
    public void Load_Where_Migration_Cannot_Bind_Returns_Null_And_Reports_The_File()
    {
        const string original = """{"name":"ok"}""";
        string path = WriteConfig("unbindable.json", original);

        Unbindable? result = ConfigurationResolver.Load(
            path,
            ThrowingJsonContext.Default.Unbindable,
            MakePipeline(new AddUnboundPropertyMigration())
        );

        Assert.Null(result);

        // Reported for the in-world notice, and left on disk untouched for the caller's defaults to
        // overwrite - having been copied aside first.
        Assert.Contains(ConfigurationResolver.CorruptFiles, corrupt => corrupt.Path == path);
        Assert.Equal(original, File.ReadAllText(path));

        string[] backups = Directory.GetFiles(Path.Combine(_directory, ConfigBackupStore.DirectoryName));
        Assert.Contains(original, backups.Select(File.ReadAllText));
    }

    private string WriteConfig(string name, string json)
    {
        Directory.CreateDirectory(_directory);

        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, json);

        return path;
    }

    private static ConfigMigrationPipeline<JsonObject> MakePipeline(IConfigMigration<JsonObject> migration) =>
        new(new ConfigMigrationSequence<JsonObject>([migration]), new JsonMigrationFormat(Options));

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
    private sealed class NoOpMigration : IConfigMigration<JsonObject>
    {
        public int Version => 1;

        public void Up(JsonObject document)
        {
        }
    }

    private sealed class AddUnboundPropertyMigration : IConfigMigration<JsonObject>
    {
        public int Version => 1;

        public void Up(JsonObject document) => document["RequiredNumber"] = "boom";
    }
}
