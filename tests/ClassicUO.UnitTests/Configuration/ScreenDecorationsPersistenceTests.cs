using System;
using System.IO;
using System.Text.Json.Nodes;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.Game;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>Exercises <see cref="ScreenDecorations.LoadForProfile"/>, the versioned-JSON load path
/// <see cref="ClassicUO.Configuration.JsonSave{T}"/> runs in front of the config.</summary>
[Collection(CorruptFileReportCollection.Name)]
public class ScreenDecorationsPersistenceTests : IDisposable
{
    private readonly string _profileDirectory = Path.Combine(Path.GetTempPath(), $"screen-decorations-tests-{Guid.NewGuid():N}");

    /// <summary>A well-formed config at a version this build has no migration path to.</summary>
    private const string FromTheFuture = """{"enabled": true, "schema_version": 9999}""";

    public ScreenDecorationsPersistenceTests() => DrainCorruptReports();

    [Fact]
    public void LoadForProfile_Reads_A_PreMigration_File_With_No_SchemaVersion()
    {
        WriteConfig("""{"enabled": true}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeTrue();
    }

    [Fact]
    public void LoadForProfile_Persists_The_Migrated_Shape_And_Keeps_The_Original_As_A_Backup()
    {
        const string preMigration = """{"enabled": true}""";
        string path = WriteConfig(preMigration);

        ScreenDecorations.LoadForProfile(_profileDirectory);

        // Written back so the migration is paid for once, with the pre-migration file rotated aside.
        File.ReadAllText(path).Should().Contain("schema_version");
        File.ReadAllText(BackupPath(1)).Should().Be(preMigration);
    }

    [Fact]
    public void LoadForProfile_Reads_A_File_That_Already_Carries_SchemaVersion()
    {
        string path = WriteConfig($$"""{"enabled": true, "schema_version": {{ScreenDecorationsMigrations.LatestVersion}}}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeTrue();

        // Already current, so nothing is rewritten.
        File.Exists(BackupPath(1)).Should().BeFalse();
        VersionOf(path).Should().Be(ScreenDecorationsMigrations.LatestVersion);
    }

    [Fact]
    public void Save_Stamps_The_Current_SchemaVersion_Without_The_Model_Holding_One()
    {
        string path = WriteConfig("""{"enabled": false}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);
        loaded.Enabled = true;
        loaded.Save();

        VersionOf(path).Should().Be(ScreenDecorationsMigrations.LatestVersion);
    }

    [Fact]
    public void LoadForProfile_With_No_File_Returns_Defaults_And_Writes_Nothing()
    {
        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeFalse();
        File.Exists(Path.Combine(_profileDirectory, ScreenDecorations.ConfigFileName)).Should().BeFalse();
    }

    [Fact]
    public void Saving_Settings_That_Had_No_File_Creates_One()
    {
        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);
        loaded.Enabled = true;
        loaded.Save();

        string path = Path.Combine(_profileDirectory, ScreenDecorations.ConfigFileName);

        File.Exists(path).Should().BeTrue();
        VersionOf(path).Should().Be(ScreenDecorationsMigrations.LatestVersion);
    }

    [Fact]
    public void LoadForProfile_Of_A_File_From_A_Newer_Client_Starts_Clean_And_Leaves_It_Alone()
    {
        string path = WriteConfig(FromTheFuture);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        // The newer client's settings are intact and still its own; this one just runs on defaults.
        loaded.Enabled.Should().BeFalse();
        File.ReadAllText(path).Should().Be(FromTheFuture);
        File.Exists(BackupPath(1)).Should().BeFalse();
    }

    [Fact]
    public void Settings_Standing_In_For_A_Newer_Client_File_Never_Save_Over_It()
    {
        string path = WriteConfig(FromTheFuture);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);
        loaded.Enabled = true;
        loaded.Save();

        File.ReadAllText(path).Should().Be(FromTheFuture);
    }

    [Fact]
    public void LoadForProfile_Of_A_File_That_Cannot_Bind_Backs_It_Up_And_Starts_Clean()
    {
        // Valid JSON, valid object, but no trigger kind this build knows - only the typed bind can
        // reject it.
        const string unbindable =
            """{"enabled": true, "overlays": {"rules": [{"trigger": {"parameters": {"kind": "from_a_newer_client"}}}]}}""";
        string path = WriteConfig(unbindable);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeFalse();
        SoleCorruptBackup().Should().Be(unbindable);
    }

    [Fact]
    public void LoadForProfile_Reports_A_File_It_Could_Not_Use_So_The_User_Is_Told()
    {
        const string unreadable = "{ this is not json";
        string path = WriteConfig(unreadable);

        ScreenDecorations.LoadForProfile(_profileDirectory);

        CorruptFileManager.Files.TryDequeue(out CorruptConfigFile reported).Should().BeTrue();
        reported.Path.Should().Be(path);
        reported.Fallback.Should().Be(CorruptConfigFallback.Defaults);
        File.ReadAllText(reported.BackupPath!).Should().Be(unreadable);
    }

    [Fact]
    public void LoadForProfile_Reports_A_Newer_Client_File_As_Left_Alone()
    {
        string path = WriteConfig(FromTheFuture);

        ScreenDecorations.LoadForProfile(_profileDirectory);

        // No copy is named, because none was taken - the file the notice points at is still the file.
        CorruptFileManager.Files.TryDequeue(out CorruptConfigFile reported).Should().BeTrue();
        reported.Path.Should().Be(path);
        reported.Fallback.Should().Be(CorruptConfigFallback.Preserved);
        reported.BackupPath.Should().BeNull();
    }

    [Fact]
    public void LoadForProfile_Recovers_A_Corrupt_File_From_Its_Backup()
    {
        const string unreadable = "{ this is not json";
        WriteConfig(unreadable);
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"enabled": true}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        // Unreadable text says nothing about the shape, so an older copy is still worth trying.
        loaded.Enabled.Should().BeTrue();
        SoleCorruptBackup().Should().Be(unreadable);
    }

    /// <summary>
    /// Saving settings loaded from an explicit path must write back to that same path, whatever
    /// <see cref="SettingsScope.Char"/> resolves to - here a temp directory no profile ever names.
    /// </summary>
    [Fact]
    public void Settings_Loaded_From_A_Path_Save_Back_To_It()
    {
        string path = WriteConfig("""{"enabled": false}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);
        loaded.Enabled = true;
        loaded.Save();

        ScreenDecorations.LoadForProfile(_profileDirectory).Enabled.Should().BeTrue();
        File.ReadAllText(path).Should().Contain("true");
    }

    /// <summary>The single copy the corrupt-file backups hold for this test's config.</summary>
    private string SoleCorruptBackup()
    {
        string directory = Path.Combine(_profileDirectory, CorruptFileManager.BackupDirectoryName);

        return File.ReadAllText(Directory.GetFiles(directory).Should().ContainSingle().Subject);
    }

    private string WriteConfig(string json)
    {
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.ConfigFileName);
        File.WriteAllText(path, json);

        return path;
    }

    private static int VersionOf(string path) =>
        JsonNode.Parse(File.ReadAllText(path))!["schema_version"]!.GetValue<int>();

    private string BackupPath(int index) =>
        Path.Combine(_profileDirectory, Constants.BACKUP_FOLDER, $"{ScreenDecorations.ConfigFileName}.{index}");

    /// <summary>The report queue is process-wide; a leftover entry would be read as this test's.</summary>
    private static void DrainCorruptReports()
    {
        while (CorruptFileManager.Files.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
        DrainCorruptReports();

        try
        {
            if (Directory.Exists(_profileDirectory))
                Directory.Delete(_profileDirectory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test isolation.
        }
    }
}
