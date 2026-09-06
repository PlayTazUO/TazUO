#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
///     <see cref="IMigrationFormat{TDocument}" /> over a JSON object, versioned by a top-level property
/// </summary>
public class JsonMigrationFormat : IMigrationFormat<JsonObject>
{
    private readonly JsonSerializerOptions _options;
    private readonly string _versionPropertyName;

    /// <param name="options">
    ///     The config's own serializer options, so migrated text round-trips
    ///     through the naming policy and converters the typed bind uses.
    /// </param>
    /// <param name="versionPropertyName">
    ///     Defaults to <c>schema_version</c> - what snake-case renders
    ///     a <c>SchemaVersion</c> property to.
    /// </param>
    public JsonMigrationFormat(JsonSerializerOptions options, string versionPropertyName = "schema_version")
    {
        _options = options;
        _versionPropertyName = versionPropertyName;
    }

    /// <inheritdoc cref="IMigrationFormat{TDocument}.Preprocess" />
    public virtual (string Text, bool Changed) Preprocess(string text) => (text, false);

    /// <inheritdoc cref="IMigrationFormat{TDocument}.Parse" />
    /// <exception cref="ConfigDocumentMalformedException">The text is not valid JSON, or not a JSON object.</exception>
    public JsonObject Parse(string text)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException e)
        {
            throw new ConfigDocumentMalformedException("Config text is not valid JSON.", e);
        }

        if (node is not JsonObject document)
            throw new ConfigDocumentMalformedException("Config text does not parse to a JSON object.");

        return document;
    }

    /// <inheritdoc cref="IMigrationFormat{TDocument}.Serialize" />
    public string Serialize(JsonObject document) => document.ToJsonString(_options);

    /// <inheritdoc cref="IMigrationFormat{TDocument}.ReadVersion" />
    public int ReadVersion(JsonObject document)
    {
        if (!document.TryGetPropertyValue(_versionPropertyName, out JsonNode? node) || node is not JsonValue value)
            return 0;

        return value.TryGetValue(out int version) ? version : 0;
    }

    /// <inheritdoc cref="IMigrationFormat{TDocument}.WriteVersion" />
    public void WriteVersion(JsonObject document, int version) => document[_versionPropertyName] = version;
}
