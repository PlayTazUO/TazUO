#nullable enable

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
///     Moves one config between its persisted text and a mutable document, and carries the version
///     marker that says which shape the text is in.
/// </summary>
/// <typeparam name="TDocument">The mutable document form.</typeparam>
public interface IMigrationFormat<TDocument>
{
    /// <summary>
    ///     Parses the given string into its document form
    /// </summary>
    /// <param name="text">The text to parse</param>
    /// <returns>The parsed document</returns>
    /// <exception cref="ConfigDocumentMalformedException">The text is not a document of this format</exception>
    TDocument Parse(string text);

    /// <summary>
    ///     Serializes the given document to a <see langword="string" /> representation
    /// </summary>
    /// <param name="document">The document to serialize</param>
    /// <returns>The serialized document</returns>
    string Serialize(TDocument document);

    /// <summary>
    ///     Reads the schema version off of the given document
    /// </summary>
    /// <param name="document">The document to get the version of</param>
    /// <returns>The document's schema version or 0, if unversioned</returns>
    int ReadVersion(TDocument document);

    /// <summary>
    ///     Writes the schema version on to the given document
    /// </summary>
    /// <param name="document">The document to update</param>
    /// <param name="version">The schema version to set</param>
    void WriteVersion(TDocument document, int version);

    /// <summary>
    ///     Repairs text a legacy writer left unparsable, before <see cref="Parse" /> sees it.
    ///     Defaults to parsing as read.
    /// </summary>
    /// <returns>
    ///     The text to parse, and whether it differs from what was read - a <see langword="true" /> carries into
    ///     <see cref="ConfigMigrationResult.Changed" />, persisting the repair.
    /// </returns>
    (string Text, bool Changed) Preprocess(string text) => (text, false);
}
