namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>Outcome of running a <see cref="ConfigMigrationPipeline{TDocument}"/> over persisted
/// text. Non-generic on purpose - it carries text, not documents.</summary>
public readonly record struct ConfigMigrationResult(
    bool Changed,
    string Text,
    int FromVersion,
    int ToVersion
);
