using System;
using System.IO;
using ClassicUO.IO;
using Xunit;

namespace ClassicUO.UnitTests.IO;

public class AtomicFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"atomic-file-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Write_Creates_Temp_In_Targets_Directory_Not_System_Temp()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "config.json");

        AtomicFile.Write(path, "hello");

        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(Path.GetTempPath(), "config.json.*.tmp"));
        Assert.DoesNotContain(Directory.GetFiles(_directory), f => f != path);
    }

    [Fact]
    public void Write_Replaces_Existing_Target_Content_Complete()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "old content, longer than the new one");

        AtomicFile.Write(path, "new");

        Assert.Equal("new", File.ReadAllText(path));
    }

    [Fact]
    public void Write_Creates_Missing_Target_Directory()
    {
        string path = Path.Combine(_directory, "nested", "config.json");

        AtomicFile.Write(path, "hello");

        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path));
    }

    [Fact]
    public void Write_Failure_Leaves_Original_Intact_And_No_Stray_Temp()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "original");

        // A directory at the write target makes the final File.Move fail after the temp file has
        // already been written, exercising the cleanup path.
        string blockedPath = Path.Combine(_directory, "blocked");
        Directory.CreateDirectory(blockedPath);

        Assert.ThrowsAny<IOException>(() => AtomicFile.Write(blockedPath, "new"));

        Assert.Equal("original", File.ReadAllText(path));
        Assert.DoesNotContain(Directory.GetFiles(_directory), f => f.Contains(".tmp"));
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
