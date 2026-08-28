using System;
using System.IO;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
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
        File.WriteAllText(path, """{"enabled": true, "schema_version": 0}""");

        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeTrue();
        loaded.SchemaVersion.Should().Be(0);
    }

    [Fact]
    public void LoadForProfile_With_No_File_Returns_Defaults_At_Latest_SchemaVersion()
    {
        ScreenDecorations loaded = ScreenDecorations.LoadForProfile(_profileDirectory);

        loaded.Enabled.Should().BeFalse();
        loaded.SchemaVersion.Should().Be(ScreenDecorationsMigrations.LatestVersion);
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
