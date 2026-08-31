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

    /// <summary>Holds the pre-migration file, so a migration that turns out to be wrong is not the
    /// end of the user's settings. Distinct from the corrupt-file backups a failed load takes.</summary>
    private static readonly ConfigBackupStore _migrationBackups = new("premigration");

    #endregion

    #region Public methods

    /// <summary>Migrates a versioned JSON config upward, then binds it - writing the migrated form
    /// back only once it has been shown to bind.</summary>
    /// <param name="path">Path to the config file.</param>
    /// <param name="typeInfo">Source-generated type metadata for <typeparamref name="T"/>.</param>
    /// <param name="pipeline">The config's migration pipeline.</param>
    /// <param name="accept">Gates the write-back on the bound instance. Null accepts any bind.</param>
    /// <returns>The bound instance, or null when the file does not exist.</returns>
    /// <exception cref="ConfigMigrationException">
    /// The file could not be brought to the current shape - unreadable, migrated by a newer client, a
    /// migration threw, or the migrated text does not bind. <see cref="Exception.InnerException"/>
    /// carries what actually failed (a <see cref="JsonException"/> for the last two). Nothing was
    /// written; the file on disk is untouched.
    /// </exception>
    public static T? Load<T>(
        string path,
        JsonTypeInfo<T> typeInfo,
        ConfigMigrationPipeline<JsonObject> pipeline,
        Func<T, bool>? accept = null
    ) where T : class
    {
        if (!File.Exists(path))
            return null;

        string text = ConfigurationResolver.NormalizeText(File.ReadAllText(path));

        ConfigMigrationResult result = pipeline.Migrate(text);

        T? instance = Bind(result, typeInfo);

        if (instance != null && result.Changed && (accept == null || accept(instance)))
        {
            BackupOriginal(path);
            AtomicFile.Write(path, result.Text);
        }

        return instance;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Binds the migrated text, restating a bind failure as the one exception this load path raises.
    /// A caller that has to tell "this file is beyond us" from "everything is fine" should not have to
    /// catch two unrelated types to do it, and the migrated shape failing to bind is a fault of the
    /// same kind as the migration itself failing.
    /// </summary>
    /// <exception cref="ConfigMigrationException">The migrated text does not bind.</exception>
    private static T? Bind<T>(ConfigMigrationResult result, JsonTypeInfo<T> typeInfo) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize(result.Text, typeInfo);
        }
        catch (JsonException e)
        {
            throw new ConfigMigrationException(
                $"Config at version {result.ToVersion} does not bind to {typeof(T).Name}: {e.Message}",
                e
            );
        }
    }

    /// <summary>Copies the pre-migration file aside, keeping a bounded history per file.</summary>
    private static void BackupOriginal(string path)
    {
        _migrationBackups.TryBackup(path, out Exception? error);

        if (error != null)
            Log.Error($"Failed to back up '{path}' before migration - {error}");
    }

    #endregion
}
