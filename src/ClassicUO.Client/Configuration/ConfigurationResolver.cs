#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ClassicUO.IO;
using ClassicUO.IO.Persistency;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

internal static partial class ConfigurationResolver
{
    /// <summary>
    ///     Holds the file as found, so a corrupt config the client overwrites with defaults is
    ///     still recoverable by hand.
    /// </summary>
    private static readonly ConfigBackupStore _corruptBackups = new("corrupt");

    /// <summary>
    ///     Copies a config file that could not be loaded aside and records it for the in-world notice.
    ///     Call before falling back to defaults, which overwrite the file.
    /// </summary>
    /// <param name="file">The file that could not be loaded.</param>
    /// <returns>Where the copy was written, or null if none could be taken.</returns>
    public static string? BackupAndReportCorruptFile(string file)
    {
        string? backupPath = _corruptBackups.TryBackup(file, out Exception? backupError);

        if (backupError != null)
            Log.Error($"Failed to back up corrupt configuration file '{file}' - {backupError}");
        else if (backupPath != null)
            Log.Warn($"Corrupt configuration file backed up to '{backupPath}'.");

        CorruptConfigReporter.Report(file, backupPath);

        return backupPath;
    }

    /// <summary>Un-escapes the backslash-escaping legacy config writers applied before saving.</summary>
    internal static string NormalizeText(string text) => EscapeNormalizeRegex().Replace(text, @"\\");

    // Matches a lone backslash - not part of an already-escaped \\ pair.
    [GeneratedRegex(@"(?<!\\)\\(?!\\)")]
    private static partial Regex EscapeNormalizeRegex();

    public static T? Load<T>(string file, JsonTypeInfo<T> ctx) where T : class
    {
        if (!File.Exists(file))
        {
            Log.Warn(file + " not found.");
            return null;
        }

        string text = NormalizeText(File.ReadAllText(file));

        try
        {
            return JsonSerializer.Deserialize(text, ctx);
        }
        catch (JsonException e)
        {
            // The configuration file is corrupt or malformed (e.g. truncated write,
            // manual edit, disk corruption). Rather than crashing the client at boot,
            // back up the bad file so it isn't silently overwritten and return null so
            // the caller can fall back to sane defaults.
            Log.Error($"Failed to load configuration file '{file}' - {e}");

            BackupAndReportCorruptFile(file);

            return null;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load configuration file '{file}' - {e}");
            throw;
        }
    }

    public static void Save<T>(T obj, string file, JsonTypeInfo<T> ctx) where T : class
    {
        // this try catch is necessary when multiples cuo instances points to this file.
        try
        {
            string json = JsonSerializer.Serialize(obj, ctx);
            AtomicFile.Write(file, json);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }
}
