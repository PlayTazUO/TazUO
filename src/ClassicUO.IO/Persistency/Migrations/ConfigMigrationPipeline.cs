namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>Combines a <see cref="ConfigMigrationSequence{TDocument}"/> with an
/// <see cref="IMigrationFormat{TDocument}"/> to migrate a config's persisted text.</summary>
/// <typeparam name="TDocument">The mutable document form.</typeparam>
public sealed class ConfigMigrationPipeline<TDocument>
{
    private readonly ConfigMigrationSequence<TDocument> _sequence;
    private readonly IMigrationFormat<TDocument> _format;

    public int LatestVersion => _sequence.LatestVersion;

    public ConfigMigrationPipeline(ConfigMigrationSequence<TDocument> sequence, IMigrationFormat<TDocument> format)
    {
        _sequence = sequence;
        _format = format;
    }

    /// <summary>Migrates persisted text upward. Pure: reads and writes nothing outside itself.</summary>
    /// <exception cref="ConfigMigrationException">
    /// Parsing failed, a migration failed, or the document was written by a newer client.
    /// </exception>
    public ConfigMigrationResult Migrate(string text)
    {
        TDocument document = _format.Parse(text);
        int fromVersion = _format.ReadVersion(document);

        if (fromVersion == LatestVersion)
            return new ConfigMigrationResult(false, text, fromVersion, fromVersion);

        int toVersion = _sequence.Apply(document, fromVersion);

        _format.WriteVersion(document, toVersion);
        string serialized = _format.Serialize(document);

        return new ConfigMigrationResult(true, serialized, fromVersion, toVersion);
    }
}
