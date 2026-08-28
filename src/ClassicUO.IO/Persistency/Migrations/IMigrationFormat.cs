namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// Moves one config between its persisted text and a mutable document, and carries the version
/// marker that says which shape the text is in.
/// </summary>
/// <typeparam name="TDocument">The mutable document form.</typeparam>
public interface IMigrationFormat<TDocument>
{
    /// <exception cref="ConfigMigrationException">The text is not a document of this format.</exception>
    TDocument Parse(string text);

    string Serialize(TDocument document);

    /// <returns>The recorded version, or 0 where the marker is absent.</returns>
    int ReadVersion(TDocument document);

    void WriteVersion(TDocument document, int version);
}
