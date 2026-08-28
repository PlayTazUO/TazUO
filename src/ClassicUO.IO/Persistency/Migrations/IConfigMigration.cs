namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// One upward step in a config's persisted shape. Operates on the document type and string
/// literals only, never on live C# model types - a migration referencing a model class breaks the
/// day a later change renames or removes it. Migrations are frozen against the shape as of their
/// own version.
/// </summary>
/// <typeparam name="TDocument">The mutable document form: JsonObject, XElement, or a typed graph.</typeparam>
public interface IConfigMigration<in TDocument>
{
    /// <summary>Version this migration produces. It applies to any document below it.</summary>
    int Version { get; }

    /// <summary>Transforms <paramref name="document"/> in place.</summary>
    void Up(TDocument document);
}
