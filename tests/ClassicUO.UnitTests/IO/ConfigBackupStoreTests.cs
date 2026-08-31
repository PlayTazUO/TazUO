#nullable enable

using System;
using System.IO;
using System.Linq;
using ClassicUO.IO.Persistency;
using Xunit;

namespace ClassicUO.UnitTests.IO;

public class ConfigBackupStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"config-backup-store-tests-{Guid.NewGuid():N}");

    private string BackupDirectory => Path.Combine(_directory, ConfigBackupStore.DirectoryName);

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

    [Fact]
    public void Backup_Copies_Into_A_Subdirectory_Not_Beside_The_File()
    {
        string path = WriteConfig("first");
        var store = new ConfigBackupStore("corrupt");

        string? backupPath = store.TryBackup(path, out Exception? error);

        Assert.Null(error);
        Assert.NotNull(backupPath);
        Assert.Equal(BackupDirectory, Path.GetDirectoryName(backupPath));

        // Nothing lands next to the config itself - a sweep of the config directory must not see it.
        Assert.Equal(path, Assert.Single(Directory.GetFiles(_directory)));
    }

    [Fact]
    public void Backup_Of_A_Missing_File_Does_Nothing()
    {
        Directory.CreateDirectory(_directory);
        var store = new ConfigBackupStore("corrupt");

        string? backupPath = store.TryBackup(Path.Combine(_directory, "absent.json"), out Exception? error);

        Assert.Null(backupPath);
        Assert.Null(error);
        Assert.False(Directory.Exists(BackupDirectory));
    }

    [Fact]
    public void Retention_Keeps_The_Newest_And_Drops_The_Oldest()
    {
        string path = WriteConfig("v1");
        var store = new ConfigBackupStore("premigration", retained: 3);

        foreach (string contents in new[] { "v1", "v2", "v3", "v4", "v5" })
        {
            File.WriteAllText(path, contents);
            store.TryBackup(path, out _);
        }

        string[] backups = Backups();

        Assert.Equal(3, backups.Length);
        Assert.Equal(new[] { "v3", "v4", "v5" }, backups.Select(File.ReadAllText).ToArray());
    }

    [Fact]
    public void An_Unchanged_File_Is_Not_Backed_Up_Twice()
    {
        string path = WriteConfig("same");
        var store = new ConfigBackupStore("corrupt", retained: 3);

        string? first = store.TryBackup(path, out _);
        string? second = store.TryBackup(path, out _);

        // A fault that repeats every launch must not rotate the useful history out.
        Assert.Equal(first, second);
        Assert.Single(Backups());
    }

    [Fact]
    public void Reasons_Retain_Independently()
    {
        string path = WriteConfig("v1");
        var migration = new ConfigBackupStore("premigration", retained: 1);
        var corrupt = new ConfigBackupStore("corrupt", retained: 1);

        migration.TryBackup(path, out _);
        File.WriteAllText(path, "v2");
        corrupt.TryBackup(path, out _);

        Assert.Equal(2, Backups().Length);
    }

    [Fact]
    public void A_Blank_Reason_Or_Zero_Retention_Is_Rejected()
    {
        Assert.Throws<ArgumentException>(() => new ConfigBackupStore("  "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfigBackupStore("corrupt", retained: 0));
    }

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
}
