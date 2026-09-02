#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.IO;
using ClassicUO.IO.Persistency;
using ClassicUO.IO.Persistency.Migrations;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

/// <summary>Loads a versioned JSON config file, migrating its persisted shape upward before it is
/// bound to a typed instance.</summary>
public static class VersionedJsonConfig
{
    #region Private members

    /// <summary>Holds the pre-migration file. Separate reason from the corrupt-file backups a failed
    /// load takes, so the two do not evict each other.</summary>
    private static readonly ConfigBackupStore _migrationBackups = new("premigration");

    #endregion

    #region Public methods

    /// <summary>Migrates a versioned JSON config upward, then binds it. The migrated form is written
    /// back only once it has been shown to bind.</summary>
    /// <param name="path">Path to the config file.</param>
    /// <param name="typeInfo">Source-generated type metadata for <typeparamref name="T"/>.</param>
    /// <param name="pipeline">The config's migration pipeline.</param>
    /// <param name="accept">Gates the write-back on the bound instance. Null accepts any bind.</param>
    /// <returns>The bound instance, or null when the file does not exist.</returns>
    /// <exception cref="ConfigMigrationException">
    /// The file could not be brought to the current shape - unreadable, migrated by a newer client, a
    /// migration threw, or the migrated text does not bind. Nothing was written.
    /// </exception>
    /// <remarks>A failed write-back is logged, not raised: the caller's instance is already good.</remarks>
    public static T? Load<T>(
        string path,
        JsonTypeInfo<T> typeInfo,
        ConfigMigrationPipeline<JsonObject> pipeline,
        Func<T, bool>? accept = null
    ) where T : class
    {
        if (!File.Exists(path))
            return null;

        ConfigMigrationResult result = pipeline.Migrate(File.ReadAllText(path));

        T? instance = Bind(result, typeInfo);

        if (instance != null && result.Changed && (accept == null || accept(instance)))
            WriteBack(path, result.Text);

        return instance;
    }

    #endregion

    #region Private methods

    /// <summary>Binds the migrated text, restating any failure as the one exception this load path
    /// raises - a source-generated context throws several unrelated types over bad metadata, and the
    /// caller has one question to ask.</summary>
    /// <exception cref="ConfigMigrationException">The migrated text does not bind.</exception>
    private static T? Bind<T>(ConfigMigrationResult result, JsonTypeInfo<T> typeInfo) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize(result.Text, typeInfo);
        }
        catch (Exception e)
        {
            throw new ConfigMigrationException(
                $"Config at version {result.ToVersion} does not bind to {typeof(T).Name}: {e.Message}",
                e
            );
        }
    }

    /// <summary>Persists the migrated text, first copying the pre-migration file aside. Failure leaves
    /// the file unmigrated for the next load to retry.</summary>
    private static void WriteBack(string path, string text)
    {
        _migrationBackups.TryBackup(path, out Exception? backupError);

        if (backupError != null)
        {
            // No backup, no write: a wrong migration would have nothing to restore from.
            Log.Error($"Failed to back up '{path}' before migration, leaving it unmigrated - {backupError}");

            return;
        }

        try
        {
            AtomicFile.Write(path, text);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to write migrated config '{path}' - {e}");
        }
    }

    #endregion
}
