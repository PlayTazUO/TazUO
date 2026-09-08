using Xunit;

namespace ClassicUO.UnitTests.Fixtures;

/// <summary>
/// Serializes the tests that plant <see cref="ClassicUO.Configuration.ProfileManager.CurrentProfile" />,
/// which is process-wide static state xUnit would otherwise let two test classes fight over. No
/// fixture: the collection exists only for the ordering.
/// </summary>
[CollectionDefinition(Name)]
public class CurrentProfileCollection
{
    public const string Name = "CurrentProfile collection";
}
