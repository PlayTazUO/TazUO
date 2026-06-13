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
            
            return string.Format(v, replace);
        }

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
            if (!langCode.Equals("EN", StringComparison.OrdinalIgnoreCase) && !File.Exists(target))
            {
                Log.Warn($"TazLang: language file not found: '{target}', falling back to EN");
                target = enPath;
            }

            Dictionary<string, string> dict = LangIniSerializer.Parse(File.ReadAllText(target));
            LangIniSerializer.MergeIfStale(target, dict);

            dict.Remove("_version");
            _strings = dict;
        }

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
