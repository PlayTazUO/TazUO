using Xunit;

namespace ClassicUO.UnitTests.Fixtures;

/// <summary>
/// Serializes the tests that read or drain
/// <see cref="ClassicUO.Configuration.CorruptFileManager.Files" />, which is a process-wide queue
/// xUnit would otherwise let two test classes drain out from under each other. No fixture: the
/// collection exists only for the ordering.
/// </summary>
[CollectionDefinition(Name)]
public class CorruptFileReportCollection
{
    public const string Name = "CorruptFileReport collection";
}
