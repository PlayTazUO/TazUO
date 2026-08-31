using System;
using System.IO;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.IO.Persistency;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>Exercises <see cref="ScreenDecorations.LoadForProfile"/>, the versioned-JSON load path
/// wired up in front of the config.</summary>
public class ScreenDecorationsPersistenceTests : IDisposable
{
    private readonly string _profileDirectory = Path.Combine(Path.GetTempPath(), $"screen-decorations-tests-{Guid.NewGuid():N}");

    [Fact]
    public void LoadForProfile_Reads_A_PreMigration_File_With_No_SchemaVersion()
    {
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.FileName);
        File.WriteAllText(path, """{"enabled": true}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeTrue();
    }

    [Fact]
    public void LoadForProfile_Reads_A_File_That_Already_Carries_SchemaVersion()
    {
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.FileName);
        File.WriteAllText(path, $$"""{"enabled": true, "schema_version": {{ScreenDecorationsMigrations.LatestVersion}}}""");

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
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.FileName);
        const string fromTheFuture = """{"enabled": true, "schema_version": 9999}""";
        File.WriteAllText(path, fromTheFuture);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        // This build cannot run those settings, so it starts clean - but the next Save() overwrites
        // the file, so the user's real settings have to survive somewhere.
        loaded.Enabled.Should().BeFalse();

        string[] backups = Directory.GetFiles(Path.Combine(_profileDirectory, ConfigBackupStore.DirectoryName));
        File.ReadAllText(backups.Should().ContainSingle().Subject).Should().Be(fromTheFuture);
    }

    [Fact]
    public void LoadForProfile_Of_A_File_That_Cannot_Bind_Backs_It_Up_And_Starts_Clean()
    {
        Directory.CreateDirectory(_profileDirectory);
        string path = Path.Combine(_profileDirectory, ScreenDecorations.FileName);

        // Valid JSON, valid object - but no trigger kind this build knows, so only the typed bind
        // can reject it. This is the case that used to escape as an unhandled JsonException.
        const string unbindable =
            """{"enabled": true, "overlays": {"rules": [{"trigger": {"parameters": {"kind": "from_a_newer_client"}}}]}}""";
        File.WriteAllText(path, unbindable);

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeFalse();

        string[] backups = Directory.GetFiles(Path.Combine(_profileDirectory, ConfigBackupStore.DirectoryName));
        File.ReadAllText(backups.Should().ContainSingle().Subject).Should().Be(unbindable);
    }

    public void Dispose()
    {
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
