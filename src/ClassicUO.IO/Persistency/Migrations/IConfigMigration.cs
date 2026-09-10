namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// One upward step in a config's persisted shape, frozen against the shape as of its own version.
/// Document and string literals only: naming a live model type breaks when that type is renamed.
/// </summary>
/// <typeparam name="TDocument">The mutable document form: JsonObject, XElement, or a typed graph.</typeparam>
public interface IConfigMigration<in TDocument>
{
    /// <summary>Version this migration produces. It applies to any document below it.</summary>
    int Version { get; }

    /// <summary>Transforms <paramref name="document"/> in place.</summary>
    void Up(TDocument document);
}
