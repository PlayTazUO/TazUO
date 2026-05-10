using System;
using System.IO;
using ClassicUO.IO.Persistency;
using Xunit;

namespace ClassicUO.UnitTests.IO;

public class FilePersistenceManagerTests : IDisposable
{
    private readonly string _persistencyDirName = $"file-persistence-tests-{Guid.NewGuid():N}";

    [Fact]
    public void Ctor_Throws_For_Null_Or_Whitespace_Directory_Name()
    {
        Assert.Throws<ArgumentException>(() => new FilePersistenceManager<TestEntryType>(null));
        Assert.Throws<ArgumentException>(() => new FilePersistenceManager<TestEntryType>(""));
        Assert.Throws<ArgumentException>(() => new FilePersistenceManager<TestEntryType>("   "));
    }

    [Fact]
    public void Ctor_Throws_For_Invalid_Directory_Name_Characters()
    {
        string invalidName = $"bad{Path.GetInvalidFileNameChars()[0]}name";

        Assert.Throws<ArgumentException>(() => new FilePersistenceManager<TestEntryType>(invalidName));
    }

    [Fact]
    public void Get_Returns_New_Instance_When_File_Does_Not_Exist()
    {
        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();

        TestValue value = manager.Get(definition);

        Assert.NotNull(value);
        Assert.Equal(0, value.Counter);
        Assert.Equal(string.Empty, value.Name);
    }

    [Fact]
    public void Set_Returns_False_When_Data_Is_Null()
    {
        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();

        bool result = manager.Set(definition, null);

        Assert.False(result);
    }

    [Fact]
    public void Set_Persists_File_And_Get_Returns_Cached_Value()
    {
        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();
        var value = new TestValue
        {
            Counter = 42,
            Name = "Hello World"
        };

        bool saved = manager.Set(definition, value);

        Assert.True(saved);

        string filePath = GetFilePath();
        Assert.True(File.Exists(filePath));

        string json = File.ReadAllText(filePath);
        Assert.Contains("\"counter\": 42", json);
        Assert.Contains("\"name\": \"Hello World\"", json);

        TestValue loaded = manager.Get(definition);

        Assert.Same(value, loaded);
        Assert.Equal(42, loaded.Counter);
        Assert.Equal("Hello World", loaded.Name);
    }

    [Fact]
    public void Get_Loads_From_Existing_File()
    {
        string filePath = GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, """
        {
          "counter": 7,
          "name": "FromDisk"
        }
        """);

        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();

        TestValue loaded = manager.Get(definition);

        Assert.NotNull(loaded);
        Assert.Equal(7, loaded.Counter);
        Assert.Equal("FromDisk", loaded.Name);
    }

    [Fact]
    public void Get_Returns_New_Instance_When_File_Contains_Invalid_Json()
    {
        string filePath = GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "{ not valid json");

        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();

        TestValue loaded = manager.Get(definition);

        Assert.NotNull(loaded);
        Assert.Equal(0, loaded.Counter);
        Assert.Equal(string.Empty, loaded.Name);
    }

    [Fact]
    public void Clear_Removes_Backup_File()
    {
        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);
        var definition = new TestDefinition();
        manager.Set(definition, new TestValue { Counter = 1, Name = "Cached" });

        string filePath = GetFilePath();
        Assert.True(File.Exists(filePath));

        manager.Clear(TestEntryType.Primary);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void ClearAll_Removes_All_Backup_Files()
    {
        var manager = new FilePersistenceManager<TestEntryType>(_persistencyDirName);

        manager.Set(new TestDefinition(TestEntryType.Primary), new TestValue { Counter = 1, Name = "One" });
        manager.Set(new TestDefinition(TestEntryType.Secondary), new TestValue { Counter = 2, Name = "Two" });

        Assert.True(File.Exists(GetFilePath(TestEntryType.Primary)));
        Assert.True(File.Exists(GetFilePath(TestEntryType.Secondary)));

        manager.ClearAll();

        Assert.False(File.Exists(GetFilePath(TestEntryType.Primary)));
        Assert.False(File.Exists(GetFilePath(TestEntryType.Secondary)));
    }

    private string GetFilePath(TestEntryType entryType = TestEntryType.Primary)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _persistencyDirName,
            $"{entryType.ToString().ToLowerInvariant()}.json");
    }

    public void Dispose()
    {
        try
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _persistencyDirName);
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test isolation.
        }
    }

    private enum TestEntryType
    {
        Primary,
        Secondary
    }

    private sealed class TestDefinition(TestEntryType key = TestEntryType.Primary) : PersistentItemDefinition<TestEntryType, TestValue>
    {
        public override TestEntryType Key => key;
    }

    private sealed class TestValue
    {
        public int Counter { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
