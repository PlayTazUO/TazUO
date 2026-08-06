// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility
{
    public static class FileSystemHelper
    {
        public static string CreateFolderIfNotExists(string path, params string[] parts)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            char[] invalid = Path.GetInvalidFileNameChars();

            for (int i = 0; i < parts.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    parts[i] = parts[i].Replace(invalid[j].ToString(), "");
                }
            }

            var sb = new StringBuilder();

            foreach (string part in parts)
            {
                sb.Append(Path.Combine(path, part));

                string r = sb.ToString();

                if (!Directory.Exists(r))
                {
                    Directory.CreateDirectory(r);
                }

                path = r;
                sb.Clear();
            }

            return path;
        }

        public static string RemoveInvalidChars(string text)
        {
            char[] invalid = Path.GetInvalidFileNameChars();

            for (int j = 0; j < invalid.Length; j++)
            {
                text = text.Replace(invalid[j].ToString(), "");
            }

            return text;
        }

        public static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Required file not found: {path}", path);
            }
        }


        public static void CopyAllTo(this DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                diSourceSubDir.CopyAllTo(nextTargetSubDir);
            }
        }

        public static void OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    ProcessStartInfo p = new() { FileName = "xdg-open", ArgumentList = { filePath }};
                    Process.Start(p);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    ProcessStartInfo p = new() { FileName = "open", ArgumentList = { filePath }};
                    Process.Start(p);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error opening file: " + ex.Message);
            }
        }

        public static bool OpenLocation(string dirOrFilePath)
        {
            try
            {
                string dir = Path.GetDirectoryName(dirOrFilePath);
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    return false;

                // This may not be 100% water-tight.
                // Think this may work better than relying on ton xdg-open for Linux, though.
                Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true, Verb = "open" });

                // We return a 'true' here to avoid having to wait sync on the UI thread (since async introduces some undue complexity).
                // Suboptimal but good enough for this case. The same issue is already present in `OpenFileWithDefaultApp` equivalent
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error opening directory '{dirOrFilePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely write a file in try/catch.
        /// Writes to a temp file and atomically replaces the target so concurrent readers never see partial content.
        /// Will log the error on failure.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lines"></param>
        /// <returns>true/false</returns>
        public static bool WriteAllLinesSafe(string filePath, List<string> lines)
        {
            string tempPath = $"{filePath}.{Environment.ProcessId}.tmp";

            try
            {
                using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (StreamWriter writer = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    foreach (string line in lines)
                        writer.WriteLine(line);
                }

                File.Move(tempPath, filePath, true);

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());

                try { File.Delete(tempPath); } catch { }

                return false;
            }
        }

        /// <summary>
        /// Reads all lines with a shared handle so multiple clients can access the file concurrently.
        /// </summary>
        public static string[] ReadAllLinesShared(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(fs, new UTF8Encoding(false));
            var lines = new List<string>();

            while (reader.ReadLine() is { } line)
                lines.Add(line);

            return lines.ToArray();
        }

        /// <summary>
        /// Reads all text with a shared handle so multiple clients can access the file concurrently.
        /// </summary>
        public static string ReadAllTextShared(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(fs, new UTF8Encoding(false));

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Safely write a file in try/catch.
        /// Will log the error on failure.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lines"></param>
        /// <returns>true/false</returns>
        public static bool WriteAllTextSafe(string filePath, string text)
        {
            try 
            {
                File.WriteAllText(filePath, text, Encoding.UTF8);
                return true;
            } catch(Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }
        }
    }
}
