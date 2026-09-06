#nullable enable

using System;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Covers the copy-aside half of <see cref="CorruptFileManager" />: where copies land, what is kept,
/// and what a caller is told about them.
/// </summary>
[Collection(CorruptFileReportCollection.Name)]
public class CorruptFileManagerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"corrupt-file-manager-tests-{Guid.NewGuid():N}");

    public CorruptFileManagerTests() => DrainReports();

    [Fact]
    public void Backup_Copies_Into_A_Subdirectory_Not_Beside_The_File()
    {
        string path = WriteConfig("first");

        string? backupPath = CorruptFileManager.Backup(path);

        backupPath.Should().NotBeNull();
        Path.GetDirectoryName(backupPath).Should().Be(BackupDirectory);

        // Nothing lands next to the config itself - a sweep of the config directory must not see it.
        Directory.GetFiles(_directory).Should().ContainSingle().Which.Should().Be(path);
    }

    [Fact]
    public void Backup_Of_A_Missing_File_Does_Nothing()
    {
        Directory.CreateDirectory(_directory);

        CorruptFileManager.Backup(Path.Combine(_directory, "absent.json")).Should().BeNull();
        Directory.Exists(BackupDirectory).Should().BeFalse();
    }

    [Fact]
    public void An_Unchanged_File_Is_Not_Backed_Up_Twice()
    {
        string path = WriteConfig("same");

        string? first = CorruptFileManager.Backup(path);
        string? second = CorruptFileManager.Backup(path);

        // A fault that repeats every launch must not rotate the useful history out - and the second
        // caller is still handed a copy that really does hold what it just read.
        second.Should().Be(first);
        Backups().Should().ContainSingle();
    }

    [Fact]
    public void Every_Distinct_Version_Is_Kept_Up_To_The_Retention_Limit()
    {
        string path = WriteConfig("v0");

        string[] written = Enumerable.Range(0, 12).Select(version => $"v{version}").ToArray();

        foreach (string contents in written)
        {
            File.WriteAllText(path, contents);
            CorruptFileManager.Backup(path);
        }

        string[] backups = Backups();

        // Oldest dropped, newest kept: what the user needs is the versions nearest the failure.
        backups.Should().HaveCount(10);
        backups.Select(File.ReadAllText).Should().Equal(written.TakeLast(10));
    }

    [Fact]
    public void BackupAndReport_Records_The_Copy_It_Took()
    {
        string path = WriteConfig("broken");

        string? backupPath = CorruptFileManager.BackupAndReport(path);

        CorruptConfigFile reported = Reported().Should().ContainSingle().Subject;

        reported.Path.Should().Be(path);
        reported.BackupPath.Should().Be(backupPath);
        reported.Fallback.Should().Be(CorruptConfigFallback.Defaults);
    }

    private string BackupDirectory => Path.Combine(_directory, CorruptFileManager.BackupDirectoryName);

    private string WriteConfig(string contents)
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, contents);

        return path;
    }

    private string[] Backups() =>
        Directory.Exists(BackupDirectory)
            ? Directory.GetFiles(BackupDirectory).OrderBy(file => file, StringComparer.Ordinal).ToArray()
            : [];

    /// <summary>The reports for this test's own directory - the queue is process-wide and shared.</summary>
    private CorruptConfigFile[] Reported() =>
        CorruptFileManager.Files.Where(file => Path.GetDirectoryName(file.Path) == _directory).ToArray();

    private static void DrainReports()
    {
        while (CorruptFileManager.Files.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
        DrainReports();

        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test isolation.
        }

        GC.SuppressFinalize(this);
    }
}
