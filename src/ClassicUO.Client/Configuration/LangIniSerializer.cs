using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ClassicUO.Configuration
{
    internal static class LangIniSerializer
    {
        private const string EMBEDDED_RESOURCE = "ClassicUO.Configuration.language.EN.ini";

        public static Dictionary<string, string> Parse(string text)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var trimmed = line.TrimStart();

                if (trimmed.Length == 0 || trimmed[0] == ';')
                    continue;

                int eq = line.IndexOf('=');
                if (eq < 1)
                    continue;

                var key = line[..eq].Trim();
                if (key.Length == 0)
                    continue;

                var value = line[(eq + 1)..];
                dict[key] = Unescape(value);
            }

            return dict;
        }

        public static Dictionary<string, string> ReadEmbedded()
        {
            var assembly = typeof(TazLang).Assembly;
            using var stream = assembly.GetManifestResourceStream(EMBEDDED_RESOURCE);

            if (stream == null)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return Parse(reader.ReadToEnd());
        }

        public static void ExtractEmbedded(string destPath)
        {
            var assembly = typeof(TazLang).Assembly;
            using var stream = assembly.GetManifestResourceStream(EMBEDDED_RESOURCE);

            if (stream == null)
                return;

            using var dest = File.Create(destPath);
            stream.CopyTo(dest);
        }

        // Compares _version in user dict vs embedded EN.
        // If embedded is newer: appends missing keys to userDict and rewrites the file.
        // Returns true if the file was modified.
        public static bool MergeIfStale(string userFilePath, Dictionary<string, string> userDict)
        {
            var embedded = ReadEmbedded();

            int embeddedVersion = ParseVersion(embedded);
            int userVersion = ParseVersion(userDict);

            if (userVersion >= embeddedVersion)
                return false;

            bool anyAdded = false;
            foreach (var kv in embedded)
            {
                if (kv.Key == "_version")
                    continue;

                if (!userDict.ContainsKey(kv.Key))
                {
                    userDict[kv.Key] = kv.Value;
                    anyAdded = true;
                }
            }

            userDict["_version"] = embeddedVersion.ToString();

            // Rewrite the file, preserving leading comment lines
            var lines = new List<string>();
            if (File.Exists(userFilePath))
            {
                foreach (var rawLine in File.ReadAllLines(userFilePath))
                {
                    var trimmed = rawLine.TrimStart();
                    if (trimmed.Length == 0 || trimmed[0] == ';')
                        lines.Add(rawLine);
                    else
                        break;
                }
            }

            lines.Add($"_version={embeddedVersion}");
            lines.Add("");

            foreach (var kv in userDict)
            {
                if (kv.Key == "_version")
                    continue;
                lines.Add($"{kv.Key}={Escape(kv.Value)}");
            }

            File.WriteAllLines(userFilePath, lines, Encoding.UTF8);
            return anyAdded;
        }

        private static int ParseVersion(Dictionary<string, string> dict)
        {
            if (dict.TryGetValue("_version", out var v) && int.TryParse(v, out int n))
                return n;
            return 0;
        }

        private static string Unescape(string value)
        {
            if (!value.Contains('\\'))
                return value;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\\' && i + 1 < value.Length)
                {
                    switch (value[i + 1])
                    {
                        case 'n': sb.Append('\n'); i++; break;
                        case '\\': sb.Append('\\'); i++; break;
                        default: sb.Append(value[i]); break;
                    }
                }
                else
                {
                    sb.Append(value[i]);
                }
            }
            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (!value.Contains('\\') && !value.Contains('\n'))
                return value;

            return value.Replace("\\", "\\\\").Replace("\n", "\\n");
        }
    }
}
