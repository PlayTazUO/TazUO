#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// Orders and runs a config's <see cref="IConfigMigration{TDocument}"/> steps. Transport-agnostic:
/// knows nothing of JSON, XML or files, only the document type it is given.
/// </summary>
/// <typeparam name="TDocument">The mutable document form the migrations operate on.</typeparam>
public sealed class ConfigMigrationSequence<TDocument>
{
    private readonly IReadOnlyList<IConfigMigration<TDocument>> _migrations;

    /// <summary>Highest version this build can produce. Zero when no migration is registered.</summary>
    public int LatestVersion { get; }

    /// <param name="migrations">Every migration this config has, in strictly ascending version
    /// order. Order is the contract, and is validated here rather than assumed.</param>
    /// <exception cref="ArgumentException">
    /// A version below 1, a duplicate version, or a version out of ascending order.
    /// </exception>
    public ConfigMigrationSequence(IReadOnlyList<IConfigMigration<TDocument>> migrations)
    {
        int previous = 0;

        foreach (IConfigMigration<TDocument> migration in migrations)
        {
            if (migration.Version < 1)
                throw new ArgumentException($"Migration version must be >= 1, got {migration.Version}.", nameof(migrations));

            if (migration.Version <= previous)
                throw new ArgumentException($"Migration versions must be strictly ascending and unique; {migration.Version} follows {previous}.", nameof(migrations));

            previous = migration.Version;
        }

        _migrations = migrations;
        LatestVersion = migrations.Count == 0 ? 0 : migrations[^1].Version;
    }

    /// <summary>Runs every migration above <paramref name="fromVersion"/>, in order, mutating
    /// <paramref name="document"/> in place. A failure leaves it half-migrated: all-or-nothing is the
    /// caller's, bought by parsing a throwaway document first.</summary>
    /// <returns>The version the document now sits at.</returns>
    /// <exception cref="ConfigMigrationException">
    /// A migration failed, or <paramref name="fromVersion"/> exceeds <see cref="LatestVersion"/>.
    /// </exception>
    public int Apply(TDocument document, int fromVersion)
    {
        if (fromVersion > LatestVersion)
            throw new ConfigMigrationException($"Document is at version {fromVersion}, ahead of this build's latest known version {LatestVersion}.");

        if (fromVersion == LatestVersion)
            return fromVersion;

        foreach (IConfigMigration<TDocument> migration in _migrations.Where(m => m.Version > fromVersion))
        {
            try
            {
                migration.Up(document);
            }
            catch (Exception e)
            {
                throw new ConfigMigrationException($"Migration to version {migration.Version} ({migration.GetType()}) failed.", migration.Version, e);
            }
        }

        return LatestVersion;
    }
}
