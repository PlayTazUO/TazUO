using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Covers the rotating backups <see cref="JsonSave{T}"/> keeps: how far down a version travels before
/// it is dropped, and how far the load walks to answer for a file it cannot read.
/// </summary>
[Collection(CorruptFileReportCollection.Name)]
public class JsonSaveBackupTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"json-save-backup-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Each_Save_Pushes_The_Previous_Version_One_Slot_Down()
    {
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);

        save.Salutation = "first";
        save.Save();

        save.Salutation = "second";
        save.Save();

        File.ReadAllText(FilePath).Should().Contain("second");
        File.ReadAllText(BackupPath(1)).Should().Contain("first");
    }

    [Fact]
    public void The_Oldest_Version_Falls_Off_Once_Every_Slot_Is_Taken()
    {
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);

        // One more save than there are slots, so the first value written has nowhere left to go.
        for (int version = 1; version <= 7; version++)
        {
            save.Salutation = $"v{version}";
            save.Save();
        }

        File.ReadAllText(FilePath).Should().Contain("v7");

        // Newest first: slot 1 holds the version displaced by the last save.
        File.ReadAllText(BackupPath(1)).Should().Contain("v6");

        List<string> retained = Directory.GetFiles(Path.Combine(_directory, Constants.BACKUP_FOLDER))
            .Select(File.ReadAllText)
            .ToList();

        retained.Should().HaveCount(5);
        retained.Should().NotContain(text => text.Contains("v1"));
    }

    [Fact]
    public void A_Load_Walks_Past_A_Backup_It_Cannot_Read_To_One_It_Can()
    {
        Write("{ not json at all");
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), "{ truncated too");
        File.WriteAllText(BackupPath(2), """{"salutation":"two runs ago","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        loaded.Salutation.Should().Be("two runs ago");

        Reported().Should().ContainSingle()
            .Which.Fallback.Should().Be(CorruptConfigFallback.Backup);
    }

    [Fact]
    public void A_Recovered_File_Is_Left_Alone_Until_It_Is_Saved_Over()
    {
        const string corrupt = "{ not json at all";
        Write(corrupt);
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath(1))!);
        File.WriteAllText(BackupPath(1), """{"salutation":"an older run","schema_version":1}""");

        MigratingSave loaded = MigratingSave.LoadFromPath(FilePath);

        // The recovery is in memory: the unreadable file stays for the corrupt-file copy to point at.
        File.ReadAllText(FilePath).Should().Be(corrupt);

        loaded.Save();

        File.ReadAllText(FilePath).Should().Contain("an older run");
        File.ReadAllText(BackupPath(1)).Should().Be(corrupt);
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
