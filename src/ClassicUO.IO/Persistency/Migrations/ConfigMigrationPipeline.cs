namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
///     Combines a <see cref="ConfigMigrationSequence{TDocument}" /> with an
///     <see cref="IMigrationFormat{TDocument}" /> to migrate a config's persisted text.
/// </summary>
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

    /// <summary>
    ///     Migrates persisted text upward, after letting the format repair it. Pure: reads and
    ///     writes nothing outside itself.
    /// </summary>
    /// <returns>
    ///     A result whose <see cref="ConfigMigrationResult.Changed" /> also covers a
    ///     preprocess-only repair, so text already at the latest version still gets its fix persisted.
    /// </returns>
    /// <exception cref="ConfigMigrationException">
    ///     Parsing failed, a migration failed, or the document was written by a newer client.
    /// </exception>
    public ConfigMigrationResult Migrate(string text)
    {
        // Preprocess - usually a no-op but can be used to handle stuff like repairing broken escapes
        (string processedText, bool preprocessModified) = _format.Preprocess(text);

        // Parse into objects
        TDocument document = _format.Parse(processedText);
        int fromVersion = _format.ReadVersion(document);

        if (fromVersion == LatestVersion)
            return new ConfigMigrationResult(preprocessModified, processedText, fromVersion, fromVersion);

        int toVersion = _sequence.Apply(document, fromVersion);

        _format.WriteVersion(document, toVersion);
        string serialized = _format.Serialize(document);

        return new ConfigMigrationResult(true, serialized, fromVersion, toVersion);
    }
}
