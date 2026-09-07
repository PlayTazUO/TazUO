#nullable enable

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

internal static partial class ConfigurationResolver
{
    /// <summary>
    ///     Doubles every lone backslash, so text a legacy writer left with raw path separators parses as
    ///     JSON. An already-escaped pair is left alone.
    /// </summary>
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

            CorruptFileManager.BackupAndReport(file);

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
