using System;
using System.Collections.Generic;
using ClassicUO.IO.Persistency.Migrations;
using Xunit;

namespace ClassicUO.UnitTests.IO.Persistency.Migrations;

public class ConfigMigrationSequenceTests
{
    [Fact]
    public void Ctor_Throws_For_Duplicate_Versions()
    {
        var migrations = new List<IConfigMigration<TestDocument>>
        {
            new RecordingMigration(1),
            new RecordingMigration(1)
        };

        Assert.Throws<ArgumentException>(() => new ConfigMigrationSequence<TestDocument>(migrations));
    }

    [Fact]
    public void Ctor_Throws_For_Descending_Order()
    {
        var migrations = new List<IConfigMigration<TestDocument>>
        {
            new RecordingMigration(2),
            new RecordingMigration(1)
        };

        Assert.Throws<ArgumentException>(() => new ConfigMigrationSequence<TestDocument>(migrations));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_Throws_For_Zero_Or_Negative_Version(int version)
    {
        var migrations = new List<IConfigMigration<TestDocument>> { new RecordingMigration(version) };

        Assert.Throws<ArgumentException>(() => new ConfigMigrationSequence<TestDocument>(migrations));
    }

    [Fact]
    public void Empty_Set_Has_Zero_LatestVersion_And_Apply_Is_NoOp()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([]);
        var document = new TestDocument();

        Assert.Equal(0, sequence.LatestVersion);

        int result = sequence.Apply(document, 0);

        Assert.Equal(0, result);
        Assert.Empty(document.Applied);
    }

    [Fact]
    public void Apply_From_Zero_Runs_Whole_Chain_In_Order_Seeing_Prior_Output()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([
            new RecordingMigration(1),
            new RecordingMigration(2),
            new RecordingMigration(3)
        ]);
        var document = new TestDocument();

        int result = sequence.Apply(document, 0);

        Assert.Equal(3, result);
        Assert.Equal(["v1", "v2", "v3"], document.Applied);
    }

    [Fact]
    public void Apply_Already_At_Latest_Runs_Nothing()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([
            new RecordingMigration(1),
            new RecordingMigration(2)
        ]);
        var document = new TestDocument();

        int result = sequence.Apply(document, 2);

        Assert.Equal(2, result);
        Assert.Empty(document.Applied);
    }

    [Fact]
    public void Apply_Above_Latest_Throws()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([new RecordingMigration(1)]);
        var document = new TestDocument();

        Assert.Throws<ConfigMigrationException>(() => sequence.Apply(document, 5));
    }

    [Fact]
    public void Apply_NonContiguous_Set_Applies_All_And_Reports_Highest()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([
            new RecordingMigration(1),
            new RecordingMigration(2),
            new RecordingMigration(5)
        ]);
        var document = new TestDocument();

        int result = sequence.Apply(document, 0);

        Assert.Equal(5, result);
        Assert.Equal(["v1", "v2", "v5"], document.Applied);
    }

    [Fact]
    public void Apply_Throwing_Migration_Surfaces_ConfigMigrationException_Naming_It()
    {
        var sequence = new ConfigMigrationSequence<TestDocument>([
            new RecordingMigration(1),
            new ThrowingMigration(2),
            new RecordingMigration(3)
        ]);
        var document = new TestDocument();

        ConfigMigrationException ex = Assert.Throws<ConfigMigrationException>(() => sequence.Apply(document, 0));

        Assert.Equal(2, ex.FailedAtVersion);
        Assert.Equal(["v1"], document.Applied);
    }

    private sealed class TestDocument
    {
        public List<string> Applied { get; } = [];
    }

    private sealed class RecordingMigration(int version) : IConfigMigration<TestDocument>
    {
        public int Version { get; } = version;

        public void Up(TestDocument document) => document.Applied.Add($"v{Version}");
    }

    private sealed class ThrowingMigration(int version) : IConfigMigration<TestDocument>
    {
        public int Version { get; } = version;

        public void Up(TestDocument document) => throw new InvalidOperationException("boom");
    }
}
