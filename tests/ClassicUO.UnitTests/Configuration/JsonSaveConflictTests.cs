using System;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
/// Covers the external-change guard on <see cref="JsonSave{T}"/>: a file another client rewrote
/// while this instance held it loaded must be asked about, not silently overwritten.
/// </summary>
[Collection(CorruptFileReportCollection.Name)]
public class JsonSaveConflictTests : IDisposable
{
    private const string Current = """{"salutation":"hi","schema_version":1}""";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"json-save-conflict-tests-{Guid.NewGuid():N}");

    [Fact]
    public void A_Save_With_No_External_Change_Writes_Without_Prompting()
    {
        LoadCurrent();
        bool prompted = false;
        JsonSaveConflictHandler.Prompt = _ => prompted = true;

        MigratingSave save = MigratingSave.LoadFromPath(FilePath);
        save.Salutation = "mine";
        save.Save();

        prompted.Should().BeFalse();
        File.ReadAllText(FilePath).Should().Contain("mine");
    }

    [Fact]
    public void A_Touched_But_Identical_File_Does_Not_Prompt()
    {
        LoadCurrent();
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);
        bool prompted = false;
        JsonSaveConflictHandler.Prompt = _ => prompted = true;

        // Same bytes, newer timestamp: another client re-saved what it found without changing it.
        File.SetLastWriteTimeUtc(FilePath, DateTime.UtcNow.AddMinutes(1));

        save.Salutation = "mine";
        save.Save();

        prompted.Should().BeFalse();
        File.ReadAllText(FilePath).Should().Contain("mine");
    }

    [Fact]
    public void A_Changed_File_Is_Kept_When_No_Prompt_Is_Available()
    {
        LoadCurrent();
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);
        JsonSaveConflictHandler.Prompt = null;

        RewriteExternally("theirs");
        save.Salutation = "mine";
        save.Save();

        File.ReadAllText(FilePath).Should().Contain("theirs");
    }

    [Fact]
    public void A_Changed_File_Asks_And_Overwrites_Only_When_Resolved_To()
    {
        LoadCurrent();
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);
        JsonSaveConflict raised = null;
        JsonSaveConflictHandler.Prompt = conflict => raised = conflict;

        RewriteExternally("theirs");
        save.Salutation = "mine";
        save.Save();

        // Withheld while the question is open, so the other client's version survives a crash here.
        raised.Should().NotBeNull();
        File.ReadAllText(FilePath).Should().Contain("theirs");

        raised.Resolve(true);

        File.ReadAllText(FilePath).Should().Contain("mine");
    }

    [Fact]
    public void A_Changed_File_Stays_As_It_Is_When_The_Disk_Version_Is_Kept()
    {
        LoadCurrent();
        MigratingSave save = MigratingSave.LoadFromPath(FilePath);
        JsonSaveConflict raised = null;
        JsonSaveConflictHandler.Prompt = conflict => raised = conflict;

        RewriteExternally("theirs");
        save.Salutation = "mine";
        save.Save();
        raised.Resolve(false);

        File.ReadAllText(FilePath).Should().Contain("theirs");
    }

    private string FilePath => Path.Combine(_directory, MigratingSave.TestFileName);

    private void LoadCurrent()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, Current);
    }

    private void RewriteExternally(string salutation)
    {
        File.WriteAllText(FilePath, $$"""{"salutation":"{{salutation}}","schema_version":1}""");

        // Guarantee a fingerprint mismatch even where timestamps are coarse.
        File.SetLastWriteTimeUtc(FilePath, DateTime.UtcNow.AddMinutes(1));
    }

    public void Dispose()
    {
        JsonSaveConflictHandler.Prompt = null;

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
