using System;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>Exercises <see cref="ScreenDecorations.LoadForProfile"/>, the versioned-JSON load path
/// <see cref="ClassicUO.Configuration.JsonSave{T}"/> runs in front of the config.</summary>
public class ScreenDecorationsPersistenceTests : IDisposable
{
    private readonly string _profileDirectory = Path.Combine(Path.GetTempPath(), $"screen-decorations-tests-{Guid.NewGuid():N}");

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
        WriteConfig($$"""{"enabled": true, "schema_version": {{ScreenDecorationsMigrations.LatestVersion}}}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeTrue();
        loaded.SchemaVersion.Should().Be(ScreenDecorationsMigrations.LatestVersion);
    }

    [Fact]
    public void LoadForProfile_With_No_File_Returns_Defaults_At_Latest_SchemaVersion()
    {
        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeFalse();
        loaded.SchemaVersion.Should().Be(ScreenDecorationsMigrations.LatestVersion);
    }

    [Fact]
    public void LoadForProfile_Of_A_File_From_A_Newer_Client_Backs_It_Up_And_Starts_Clean()
    {
        const string fromTheFuture = """{"enabled": true, "schema_version": 9999}""";
        string path = WriteConfig(fromTheFuture);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        // Starting clean is fine; the fresh copy overwrites the file, so the original has to survive.
        loaded.Enabled.Should().BeFalse();
        File.ReadAllText(path + ".corrupt").Should().Be(fromTheFuture);
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
        File.ReadAllText(path + ".corrupt").Should().Be(unbindable);
    }

    [Fact]
    public void LoadForProfile_Reports_A_File_It_Could_Not_Use_So_The_User_Is_Told()
    {
        string path = WriteConfig("""{"enabled": true, "schema_version": 9999}""");

        ScreenDecorations.LoadForProfile(_profileDirectory);

        CorruptConfigReporter.Files.TryDequeue(out CorruptConfigFile reported).Should().BeTrue();
        reported.Path.Should().Be(path);
        reported.BackupPath.Should().Be(path + ".corrupt");
    }

    [Fact]
    public void LoadForProfile_Recovers_A_Corrupt_File_From_Its_Backup()
    {
        string path = WriteConfig("{ this is not json");
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"enabled": true}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        // Unreadable text says nothing about the shape, so an older copy is still worth trying.
        loaded.Enabled.Should().BeTrue();
        File.Exists(path + ".corrupt").Should().BeTrue();
    }

    private string WriteConfig(string json)
    {
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.ConfigFileName);
        File.WriteAllText(path, json);

        return path;
    }

    private string BackupPath(int index) =>
        Path.Combine(_profileDirectory, Constants.BACKUP_FOLDER, $"{ScreenDecorations.ConfigFileName}.{index}");

    /// <summary>The report queue is process-wide; a leftover entry would be read as this test's.</summary>
    private static void DrainCorruptReports()
    {
        while (CorruptConfigReporter.Files.TryDequeue(out _))
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
