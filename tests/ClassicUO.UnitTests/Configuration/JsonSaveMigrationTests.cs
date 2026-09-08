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

    /// <summary>A well-formed file at a version no migration in the test's sequence can reach.</summary>
    private const string FromTheFuture = """{"salutation":"from the future","schema_version":9999}""";

    /// <summary>A well-formed file at a version this build migrates from, holding content that makes
    /// the migration throw - valid JSON, bad data.</summary>
    private const string DefeatsTheMigration = $$"""{"greeting":"{{RenameGreetingMigration.FailOn}}"}""";

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
    public void A_File_From_A_Newer_Build_Starts_Clean_Without_Consulting_The_Backups()
    {
        Write(FromTheFuture);
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"salutation":"an older run","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        // Readable though this backup is, it holds a staler copy of settings that equally cannot be
        // saved, so reading it would only hide that the real file is sitting untouched on disk.
        loaded.Salutation.Should().BeNull();
    }

    [Fact]
    public void A_File_From_A_Newer_Build_Is_Left_Exactly_As_It_Is()
    {
        Write(FromTheFuture);

        MigratingSave.LoadFromPath(FilePath);

        // The file is valid, just ahead of this build - resetting it would destroy settings the build
        // that wrote it still reads, so nothing is written, rotated, or copied aside.
        File.ReadAllText(FilePath).Should().Be(FromTheFuture);
        File.Exists(BackupPath(1)).Should().BeFalse();
        Directory.Exists(Path.Combine(_directory, CorruptFileManager.BackupDirectoryName)).Should().BeFalse();

        Reported().Should().ContainSingle()
            .Which.Fallback.Should().Be(CorruptConfigFallback.Preserved);
    }

    [Fact]
    public void A_Document_That_Defeats_A_Migration_Is_Answered_From_The_Backups()
    {
        Write(DefeatsTheMigration);
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"salutation":"an older run","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        // A version this build starts from and a migration that still failed points at the content, not
        // at the build - so the file is damaged like any other, and an older copy is worth trying.
        loaded.Salutation.Should().Be("an older run");
        Reported().Should().ContainSingle()
            .Which.Fallback.Should().Be(CorruptConfigFallback.Backup);
    }

    [Fact]
    public void A_Document_That_Defeats_A_Migration_Does_Not_Suppress_Saves()
    {
        Write(DefeatsTheMigration);

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);
        loaded.Salutation = "set by hand";
        loaded.Save();

        // Suppression is for a file worth keeping. Applied here it would wedge a damaged config at
        // defaults for good, with no way to put it right from the UI.
        MigratingSave.LoadFromPath(FilePath).Salutation.Should().Be("set by hand");
    }

    [Fact]
    public void Saves_Are_Suppressed_For_A_File_From_A_Newer_Build()
    {
        Write(FromTheFuture);

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);
        loaded.Salutation = "this session's defaults";
        loaded.Save();

        // Suppression outlives the load: a save later in the session would destroy the file just as
        // surely as persisting the defaults at load time would have.
        File.ReadAllText(FilePath).Should().Be(FromTheFuture);
        File.Exists(BackupPath(1)).Should().BeFalse();
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

/// <summary>
/// Renames <c>greeting</c> to <c>salutation</c> - enough of a shape change to observe. Throws on the
/// one value <see cref="FailOn"/> names, so a migration defeated by a document's content can be
/// exercised without a second save type.
/// </summary>
internal sealed class RenameGreetingMigration : IConfigMigration<JsonObject>
{
    /// <summary>The greeting this migration cannot handle.</summary>
    public const string FailOn = "boom";

    public int Version => 1;

    public void Up(JsonObject document)
    {
        if (!document.Remove("greeting", out JsonNode value))
            return;

        if (value is JsonValue greeting && greeting.TryGetValue(out string text) && text == FailOn)
            throw new InvalidOperationException($"Cannot migrate the greeting '{FailOn}'.");

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
