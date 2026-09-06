using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.IO.Persistency.Migrations;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Covers the migration hook on <see cref="JsonSave{T}"/> itself, on a save built for the test rather
/// than through any real config: the behaviour here is inherited by every save that declares a
/// pipeline.
/// </summary>
[Collection(CorruptFileReportCollection.Name)]
public class JsonSaveMigrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"json-save-migration-tests-{Guid.NewGuid():N}");

    [Fact]
    public void An_Unversioned_File_Is_Migrated_Before_It_Binds()
    {
        Write("""{"greeting":"hi"}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        loaded.Salutation.Should().Be("hi");
        loaded.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void A_Migrated_File_Is_Written_Back_With_The_Original_Rotated_Into_The_Backups()
    {
        const string original = """{"greeting":"hi"}""";
        Write(original);

        MigratingSave.LoadFromPath(FilePath);

        File.ReadAllText(FilePath).Should().Contain("salutation");
        File.ReadAllText(BackupPath(1)).Should().Be(original);
    }

    [Fact]
    public void A_File_Already_At_The_Latest_Version_Is_Not_Rewritten()
    {
        const string current = """{"salutation":"hi","schema_version":1}""";
        Write(current);

        MigratingSave.LoadFromPath(FilePath);

        File.ReadAllText(FilePath).Should().Be(current);
        File.Exists(BackupPath(1)).Should().BeFalse();
    }

    [Fact]
    public void A_Backup_Read_As_A_Fallback_Is_Migrated_Too_But_Left_On_Disk_As_Found()
    {
        const string unversionedBackup = """{"greeting":"hi"}""";
        Write("{ not json at all");
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), unversionedBackup);

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        // Migrated in memory, so the recovered instance binds the current shape...
        loaded.Salutation.Should().Be("hi");

        // ...but the backup itself is a record of what was there, not a file to rewrite.
        File.ReadAllText(BackupPath(1)).Should().Be(unversionedBackup);
    }

    [Fact]
    public void A_Shape_This_Build_Cannot_Migrate_Starts_Clean_Without_Consulting_The_Backups()
    {
        Write("""{"salutation":"from the future","schema_version":9999}""");
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"salutation":"an older run","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        // The backups hold older shapes of the same file, so none can answer what the newest could not.
        loaded.Salutation.Should().BeNull();
        SoleCorruptBackup().Should().Contain("from the future");
    }

    [Fact]
    public void A_Save_Loaded_From_An_Explicit_Path_Writes_Back_To_It()
    {
        Write("""{"salutation":"hi","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);
        loaded.Salutation = "changed";
        loaded.Save();

        // Not to the Global scope directory FilePath would otherwise resolve to.
        File.ReadAllText(FilePath).Should().Contain("changed");
        MigratingSave.LoadFromPath(FilePath).Salutation.Should().Be("changed");
    }

    [Fact]
    public void A_File_Answered_From_Its_Backups_Is_Reported_As_Recovered()
    {
        Write("{ not json at all");
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"salutation":"an older run","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        loaded.Salutation.Should().Be("an older run");

        // Reported, because the settings may be behind what was last saved - but not as a reset, which
        // is what the notice would otherwise tell the user.
        Reported().Should().ContainSingle()
            .Which.Fallback.Should().Be(CorruptConfigFallback.Backup);
    }

    [Fact]
    public void A_File_That_Was_Never_There_Is_Not_Reported()
    {
        MigratingSave.LoadFromPath(FilePath);

        // A first run has nothing to warn about.
        Reported().Should().BeEmpty();
    }

    [Fact]
    public void A_File_Nothing_Could_Answer_For_Is_Reported_As_Reset()
    {
        Write("{ not json at all");

        MigratingSave.LoadFromPath(FilePath);

        Reported().Should().ContainSingle()
            .Which.Fallback.Should().Be(CorruptConfigFallback.Defaults);
    }

    /// <summary>The reports for this test's own file - the queue is process-wide and shared.</summary>
    private List<CorruptConfigFile> Reported() =>
        CorruptFileManager.Files.Where(file => file.Path == FilePath).ToList();

    /// <summary>The single copy the corrupt-file backups hold for this test's file.</summary>
    private string SoleCorruptBackup()
    {
        string directory = Path.Combine(_directory, CorruptFileManager.BackupDirectoryName);

        return File.ReadAllText(Directory.GetFiles(directory).Should().ContainSingle().Subject);
    }

    private string FilePath => Path.Combine(_directory, MigratingSave.TestFileName);

    private string BackupPath(int index) =>
        Path.Combine(_directory, Constants.BACKUP_FOLDER, $"{MigratingSave.TestFileName}.{index}");

    private void Write(string json)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, json);
    }

    public void Dispose()
    {
        while (CorruptFileManager.Files.TryDequeue(out _))
        {
        }

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
}

/// <summary>Renames <c>greeting</c> to <c>salutation</c> - enough of a shape change to observe.</summary>
internal sealed class RenameGreetingMigration : IConfigMigration<JsonObject>
{
    public int Version => 1;

    public void Up(JsonObject document)
    {
        if (!document.Remove("greeting", out JsonNode value))
            return;

        document["salutation"] = value;
    }
}

/// <summary>A minimal <see cref="JsonSave{T}"/> declaring a pipeline, so the hook can be exercised
/// without dragging a real config's shape into the test.</summary>
internal sealed class MigratingSave : JsonSave<MigratingSave>, INotifyPropertyChanged
{
    public const string TestFileName = "migrating_save.json";

    private static readonly ConfigMigrationPipeline<JsonObject> _pipeline = new(
        new ConfigMigrationSequence<JsonObject>(new List<IConfigMigration<JsonObject>> { new RenameGreetingMigration() }),
        new JsonMigrationFormat(MigratingSaveJsonContext.SerializerOptions)
    );

    public int SchemaVersion { get; set; } = 1;

    public string Salutation { get; set; }

    protected override SettingsScope Scope => SettingsScope.Global;

    protected override string FileName => TestFileName;

    protected override JsonTypeInfo<MigratingSave> TypeInfo => MigratingSaveJsonContext.Default.MigratingSave;

    protected override ConfigMigrationPipeline<JsonObject> MigrationPipeline => _pipeline;

    /// <summary>Reaches the protected path-taking load, so the test never touches the real scope
    /// directories.</summary>
    public static MigratingSave LoadFromPath(string filePath) => LoadFrom(filePath);
}

[JsonSerializable(typeof(MigratingSave))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal sealed partial class MigratingSaveJsonContext : JsonSerializerContext
{
    /// <summary>Not named <c>Options</c>: that would hide <see cref="JsonSerializerContext.Options"/>,
    /// which the generated metadata reads.</summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
