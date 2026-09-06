namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
///     Outcome of running a <see cref="ConfigMigrationPipeline{TDocument}" /> over persisted
///     text. Non-generic on purpose - it carries text, not documents.
/// </summary>
/// <param name="Changed">Specifies whether the migration resulted in configuration changes that must be stored</param>
/// <param name="Text">A textual representation of the migrated entity, after conversion</param>
/// <param name="FromVersion">The entity's schema version, pre-migration</param>
/// <param name="ToVersion">The entity's schema version, post-migration</param>
public readonly record struct ConfigMigrationResult(
    bool Changed,
    string Text,
    int FromVersion,
    int ToVersion
);
