using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    internal static class LangIniSerializer
    {
        private const string EMBEDDED_RESOURCE = "ClassicUO.Configuration.language.ini";

        public static Dictionary<string, string> Parse(string text)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                string trimmed = line.TrimStart();

                if (trimmed.Length == 0 || trimmed[0] == ';')
                    continue;

                int eq = line.IndexOf('=');
                if (eq < 1)
                    continue;

                string key = line[..eq].Trim();
                if (key.Length == 0)
                    continue;

                string value = line[(eq + 1)..];
                dict[key] = Unescape(value);
            }

            return dict;
        }

        public static Dictionary<string, string> ReadEmbedded()
        {
            Assembly assembly = typeof(TazLang).Assembly;
            using Stream stream = assembly.GetManifestResourceStream(EMBEDDED_RESOURCE);

            if (stream == null)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return Parse(reader.ReadToEnd());
        }

        public static void ExtractEmbedded(string destPath)
        {
            Assembly assembly = typeof(TazLang).Assembly;
            foreach(string s in assembly.GetManifestResourceNames())
                Console.WriteLine(s);
                
            using Stream stream = assembly.GetManifestResourceStream(EMBEDDED_RESOURCE);

            if (stream == null)
            {
                throw new Exception("Failed to find language file.");
            }

            using FileStream dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            stream.CopyTo(dest);
        }

        // Compares _version in user dict vs embedded EN.
        // If embedded is newer: appends missing keys to userDict and rewrites the file.
        // Returns true if the file was modified.
        public static bool MergeIfStale(string userFilePath, Dictionary<string, string> userDict)
        {
            Dictionary<string, string> embedded = ReadEmbedded();

            foreach (KeyValuePair<string, string> kv in embedded)
            {
                if (kv.Key == "_version")
                    continue;

                if (!userDict.ContainsKey(kv.Key))
                    userDict[kv.Key] = kv.Value;
            }

            // Remove user keys that don't exist in embedded version
            List<string> removal = new();

            foreach (KeyValuePair<string, string> kv in userDict)
                if (!embedded.ContainsKey(kv.Key))
                    removal.Add(kv.Key);
            
            foreach (string k in removal)
                userDict.Remove(k);

            // Rewrite the file, preserving leading comment lines
            var lines = new List<string>();
            if (File.Exists(userFilePath))
            {
                foreach (string rawLine in FileSystemHelper.ReadAllLinesShared(userFilePath))
                {
                    string trimmed = rawLine.TrimStart();
                    if (trimmed.Length == 0 || trimmed[0] == ';')
                        lines.Add(rawLine);
                    else
                        break;
                }
            }

            lines.Add("");

            foreach (KeyValuePair<string, string> kv in userDict)
            {
                if (kv.Key == "_version")
                    continue;
                lines.Add($"{kv.Key}={Escape(kv.Value)}");
            }

            FileSystemHelper.WriteAllLinesSafe(userFilePath, lines);

            return true;
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
