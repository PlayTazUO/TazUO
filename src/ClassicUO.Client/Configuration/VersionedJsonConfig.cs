#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.IO;
using ClassicUO.IO.Persistency.Migrations;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

/// <summary>Loads a versioned JSON config file, migrating its persisted shape upward before it is
/// bound to a typed instance.</summary>
public static class VersionedJsonConfig
{
    /// <summary>
    /// Loads a versioned JSON config, migrating it upward first and writing the migrated form back
    /// only once it has been shown to bind.
    /// </summary>
    /// <param name="path">Path to the config file.</param>
    /// <param name="typeInfo">Source-generated type metadata for <typeparamref name="T"/>.</param>
    /// <param name="pipeline">The config's migration pipeline.</param>
    /// <param name="accept">Ignored when null. Otherwise the migrated text is written back only if
    /// this returns true - where a caller binds the result, that is the check.</param>
    /// <returns>The bound instance, or null when the file does not exist.</returns>
    /// <exception cref="ConfigMigrationException">The file could not be brought to the current shape.
    /// Nothing was written; the file on disk is untouched.</exception>
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

        T? instance = JsonSerializer.Deserialize(result.Text, typeInfo);

        if (instance != null && result.Changed && (accept == null || accept(instance)))
        {
            BackupOriginal(path, result.FromVersion);
            AtomicFile.Write(path, result.Text);
        }

        return instance;
    }

    /// <summary>
    /// Copies the pre-migration file aside so a repeated failure across launches never overwrites
    /// the true original.
    /// </summary>
    private static void BackupOriginal(string path, int fromVersion)
    {
        string backupPath = $"{path}.v{fromVersion}.bak";

        if (File.Exists(backupPath))
            return;

        try
        {
            File.Copy(path, backupPath);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to back up '{path}' before migration - {e}");
        }
    }
}
