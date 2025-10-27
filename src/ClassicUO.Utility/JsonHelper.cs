using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility
{
    public static class JsonHelper
    {
        /// <summary>
        /// Deserialize a json file into an object.
        /// Returns false on errors or file not found. Otherwise returns true if deserialize was successfull.
        /// </summary>
        /// <typeparam name="T">Class type to deserialize into</typeparam>
        /// <param name="path">Path to the file</param>
        /// <param name="obj">Output object</param>
        /// <returns></returns>
        [Obsolete("Use Load instead")]
        public static bool LoadJsonFile<T>(string path, out T obj)
        {
            if (File.Exists(path))
            {
                try
                {
                    obj = JsonSerializer.Deserialize<T>(File.ReadAllText(path));
                    return true;
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }

            obj = default(T);
            return false;
        }

        /// <summary>
        /// Save an object to a json file at the specified path.
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">The object to be serialized into json</param>
        /// <param name="path">The path to the save file including file name and extension</param>
        /// <param name="prettified">Should the output file be indented for readability</param>
        /// <returns></returns>
        [Obsolete("Use SaveAndBackup instead")]
        public static bool SaveJsonFile<T>(T obj, string path, bool prettified = true)
        {
            try
            {
                string output = JsonSerializer.Serialize(obj, new JsonSerializerOptions() { WriteIndented = prettified });
                File.WriteAllText(path, output);
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }

            return false;
        }

        public static bool SaveAndBackup<T>(T obj, string pathAndFile, JsonTypeInfo jsonTypeInfo)
        {
            string tempPath = null;
            try
            {
                string output = JsonSerializer.Serialize(obj, jsonTypeInfo);

                tempPath = Path.GetTempFileName();
                File.WriteAllText(tempPath, output);

                // Rotate backups: backup2 -> backup3, backup1 -> backup2, main -> backup1
                string backup3Path = GetBackupSavePath(pathAndFile, 3);
                string backup2Path = GetBackupSavePath(pathAndFile, 2);
                string backup1Path = GetBackupSavePath(pathAndFile, 1);

                // Remove oldest backup
                if (File.Exists(backup3Path))
                    File.Delete(backup3Path);

                // Rotate existing backups
                if (File.Exists(backup2Path))
                    File.Move(backup2Path, backup3Path);

                if (File.Exists(backup1Path))
                    File.Move(backup1Path, backup2Path);

                // Move current main file to backup1
                if (File.Exists(pathAndFile))
                    File.Move(pathAndFile, backup1Path);

                // Move temp file to main
                File.Move(tempPath, pathAndFile);
                tempPath = null;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            // Clean up temp file if it still exists
            if (tempPath != null && File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { }
            }

            return true;
        }

        /// <summary>
        /// Will try to load from backups if main file fails
        /// </summary>
        public static bool Load<T>(string path, JsonTypeInfo jsonTypeInfo, out T obj)
        {
            string[] filesToTry = new[] { path, GetBackupSavePath(path, 1), GetBackupSavePath(path,2), GetBackupSavePath(path, 3) };

            foreach (string filePath in filesToTry)
            {
                try
                {
                    if (!File.Exists(filePath))
                        continue;

                    string json = File.ReadAllText(filePath);
                    obj = (T)JsonSerializer.Deserialize(json, jsonTypeInfo);

                    return true;
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to load from {filePath}: {e.Message}");
                }
            }

            // If we get here, all files failed to load
            Log.Error("Failed to load from main file and all backups");
            obj = default(T);
            return false;
        }

        private static string GetBackupSavePath(string path, ushort index) => path + ".backup" + index;
    }
}
