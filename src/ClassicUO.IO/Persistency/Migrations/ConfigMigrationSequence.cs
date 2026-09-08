#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.IO.Persistency.Migrations;

/// <summary>
/// Orders and runs a config's <see cref="IConfigMigration{TDocument}"/> steps. Transport-agnostic:
/// knows nothing of JSON, XML, or files, only the document type it is given.
/// </summary>
/// <typeparam name="TDocument">The mutable document form the migrations operate on.</typeparam>
public sealed class ConfigMigrationSequence<TDocument>
{
    private readonly IReadOnlyList<IConfigMigration<TDocument>> _migrations;

    /// <summary>The highest version this build can produce. Zero when no migration is registered.</summary>
    public int LatestVersion { get; }

    /// <param name="migrations">Every migration this config has, in strictly ascending version
    /// order. Order is the contract and is validated here rather than assumed.</param>
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

        // Copied: the ordering above is the contract Apply relies on, and a caller holding the original
        // list could otherwise reorder it afterwards.
        _migrations = migrations.ToArray();
        LatestVersion = _migrations.Count == 0 ? 0 : _migrations[^1].Version;
    }

    /// <summary>Runs every migration above <paramref name="fromVersion"/>, in order, mutating
    /// <paramref name="document"/> in place. A failure leaves it half-migrated: all-or-nothing is the
    /// caller's, bought by parsing a throwaway document first.</summary>
    /// <returns>The version the document now sits at.</returns>
    /// <exception cref="ConfigDocumentMalformedException">
    /// <paramref name="fromVersion"/> is negative, which no writer produces.
    /// </exception>
    /// <exception cref="ConfigVersionAheadException">
    /// <paramref name="fromVersion"/> exceeds <see cref="LatestVersion"/>.
    /// </exception>
    /// <exception cref="ConfigMigrationException">A migration failed.</exception>
    public int Apply(TDocument document, int fromVersion)
    {
        // Malformed rather than unmigratable: nothing writes a negative version, so the marker is
        // damaged rather than describing a shape this build is too old for - and a damaged marker
        // says nothing about the other copies of the file, which are still worth reading.
        if (fromVersion < 0)
            throw new ConfigDocumentMalformedException($"Document version {fromVersion} is not a valid version.");

        if (fromVersion > LatestVersion)
            throw new ConfigVersionAheadException(fromVersion, LatestVersion);

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
