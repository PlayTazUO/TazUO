using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    public static class TazLang
    {
        private static Dictionary<string, string> _strings = new(StringComparer.Ordinal);

        /// <summary>
        /// Returns the localized string for <paramref name="key"/>, or
        /// <paramref name="fallback"/> if the key is not found.
        /// </summary>
        public static string Get(string key, string fallback = "")
            => _strings.TryGetValue(key, out string v) ? v : fallback;

        /// <summary>
        /// Returns the localized string for <paramref name="key"/> with formatted values.
        /// </summary>
        public static string Get(string key, string[] replace)
        {
            if(!_strings.TryGetValue(key, out string v))
                return string.Empty;

            try
            {
                return string.Format(v, replace);
            }
            catch (FormatException ex)
            {
                // A malformed localization template (stray braces or a placeholder
                // index beyond the supplied args) must never crash the client.
                Log.Warn($"TazLang: invalid format string for key '{key}': {ex.Message}");
                return v;
            }
        }

        /// <summary>
        /// Loads language strings from <c>Data/language.{langCode}.ini</c> into <see cref="Get"/>.
        /// Falls back to <c>language.EN.ini</c> if the requested file does not exist.
        /// Creates <c>Data/language.EN.ini</c> from the embedded resource on first run.
        /// Missing keys are auto-appended from the embedded EN resource when the version is stale.
        /// </summary>
        /// <param name="langCode">Language code matching a <c>language.{code}.ini</c> filename (e.g. <c>"EN"</c>, <c>"DE"</c>). Defaults to <c>"EN"</c>.</param>
        public static void Load(string langCode = "EN")
        {
            if (string.IsNullOrWhiteSpace(langCode))
                langCode = "EN";

            string dataDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            Directory.CreateDirectory(dataDir);

            string enPath = Path.Combine(dataDir, "language.EN.ini");
            if (!File.Exists(enPath))
                LangIniSerializer.ExtractEmbedded(enPath);

            if (!File.Exists(enPath))
            {
                Log.Error("Failed to load language file");
                return;
            }

            string target = Path.Combine(dataDir, $"language.{langCode}.ini");
            if (!File.Exists(target))
            {
                if (!langCode.Equals("EN", StringComparison.OrdinalIgnoreCase))
                    Log.Warn($"TazLang: language file not found: '{target}', falling back to EN");
                target = enPath;
            }

            Dictionary<string, string> dict = LangIniSerializer.Parse(File.ReadAllText(target));
            LangIniSerializer.MergeIfStale(target, dict);

            dict.Remove("_version");
            _strings = dict;
        }

        /// <summary>
        /// Returns the language codes available in the <c>Data/</c> directory,
        /// derived from files matching <c>language.*.ini</c>.
        /// </summary>
        /// <returns>Sorted array of language codes (e.g. <c>["DE", "EN", "FR"]</c>). Returns <c>["EN"]</c> if no files are found.</returns>
        public static string[] GetAvailableLanguages()
        {
            string dataDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            if (!Directory.Exists(dataDir))
                return new[] { "EN" };

            string[] codes = Directory.GetFiles(dataDir, "language.*.ini")
                .Select(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    string[] parts = name.Split('.');
                    return parts.Length == 2 ? parts[1] : null;
                })
                .Where(c => c != null)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToArray();

            return codes.Length > 0 ? codes : new[] { "EN" };
        }
    }
}
