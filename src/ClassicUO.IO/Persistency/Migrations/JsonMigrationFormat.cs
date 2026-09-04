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

    /// <exception cref="ConfigMigrationException">The text is not a JSON object.</exception>
    public JsonObject Parse(string text)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException e)
        {
            throw new ConfigMigrationException("Config text is not valid JSON.", e);
        }

        if (node is not JsonObject document)
            throw new ConfigMigrationException("Config text does not parse to a JSON object.");

        return document;
    }

    public string Serialize(JsonObject document) => document.ToJsonString(_options);

    public int ReadVersion(JsonObject document)
    {
        if (!document.TryGetPropertyValue(_versionPropertyName, out JsonNode? node) || node is not JsonValue value)
            return 0;

        return value.TryGetValue(out int version) ? version : 0;
    }

    public void WriteVersion(JsonObject document, int version) => document[_versionPropertyName] = version;
}
