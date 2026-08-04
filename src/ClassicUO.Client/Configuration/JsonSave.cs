using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Base class for JSON save files that live in a scoped location on disk (see
    /// <see cref="SettingsScope"/> and <see cref="JsonSaveLocationHelper"/>).
    ///
    /// Provides:
    /// <list type="bullet">
    /// <item><description><see cref="Save"/> - writes the file atomically and keeps up to
    /// <see cref="MAX_BACKUPS"/> rotating backups in a <c>backups</c> sub-folder.</description></item>
    /// <item><description><see cref="Load"/> - loads the file, falling back through the backups on failure and
    /// finally creating (and persisting) a fresh copy if nothing can be read. An unreadable main file is
    /// preserved once as <c>&lt;file&gt;.corrupt</c> for later inspection.</description></item>
    /// </list>
    ///
    /// Uses the curiously-recurring-template pattern so <see cref="Load"/> can return the concrete type.
    /// Derived types are their own serializable data container and must supply a source-generated
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> via <see cref="TypeInfo"/>.
    /// </summary>
    /// <typeparam name="T">The concrete derived save type.</typeparam>
    public abstract class JsonSave<T> where T : JsonSave<T>, new()
    {
        private const int MAX_BACKUPS = 3;

        /// <summary>The scope that determines which folder this file is saved in.</summary>
        protected abstract SettingsScope Scope { get; }

        /// <summary>The file name including extension, e.g. <c>"friends.json"</c>.</summary>
        protected abstract string FileName { get; }

        /// <summary>Source-generated JSON metadata used to (de)serialize this save.</summary>
        protected abstract JsonTypeInfo<T> TypeInfo { get; }

        /// <summary>The directory this file is saved in, resolved from <see cref="Scope"/>.</summary>
        [JsonIgnore] public string SaveDirectory => JsonSaveLocationHelper.GetScopeDirectory(Scope);

        /// <summary>The full path to the save file.</summary>
        [JsonIgnore] public string FilePath => Path.Combine(SaveDirectory, FileName);

        /// <summary>The directory that holds the rotating backups for this file.</summary>
        [JsonIgnore] public string BackupDirectory => Path.Combine(SaveDirectory, Constants.BACKUP_FOLDER);

        /// <summary>
        /// Loads the save for type <typeparamref name="T"/> from <see cref="FilePath"/>. If the main file is
        /// missing or unreadable the backups are tried in order; if they all fail a fresh instance is created
        /// and written to disk so a valid file always exists afterwards.
        /// </summary>
        public static T Load() => LoadFrom(new T().FilePath);

        /// <summary>
        /// Like <see cref="Load"/>, but reads from an explicit path rather than <see cref="FilePath"/>.
        /// </summary>
        protected static T LoadFrom(string filePath)
        {
            T instance = new T();

            using (instance.AcquireLock(filePath))
                return instance.LoadCore(filePath);
        }

        /// <summary>
        /// Saves this instance to <see cref="FilePath"/> atomically, rotating the previous version into the
        /// backups folder.
        /// </summary>
        public void Save() => SaveTo(FilePath);

        /// <summary>
        /// Like <see cref="Save"/>, but writes to an explicit path rather than <see cref="FilePath"/>.
        /// </summary>
        protected void SaveTo(string filePath)
        {
            using (AcquireLock(filePath))
                SaveCore(filePath);
        }

        private T LoadCore(string filePath)
        {
            // Try the main file first.
            if (TryDeserialize(filePath, out T loaded))
                return loaded;

            // The main file exists but couldn't be parsed - preserve a single copy for inspection.
            if (File.Exists(filePath))
                BackupCorruptFile(filePath);

            // Fall back through the rotating backups, newest first.
            for (int i = 1; i <= MAX_BACKUPS; i++)
            {
                if (TryDeserialize(GetBackupPath(filePath, i), out loaded))
                {
                    Log.Warn($"Recovered JSON save '{filePath}' from backup {i}.");
                    return loaded;
                }
            }

            // Nothing valid on disk - start fresh and persist it (already holding the lock).
            if (File.Exists(filePath))
                Log.Error($"Failed to load JSON save '{filePath}' and all backups; creating a fresh copy.");

            T fresh = new T();
            fresh.SaveCore(filePath);
            return fresh;
        }

        private void SaveCore(string filePath)
        {
            string tempPath = filePath + ".tmp";
            string directory = Path.GetDirectoryName(filePath);

            try
            {
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize((T)this, TypeInfo);
                File.WriteAllText(tempPath, json);

                RotateBackups(filePath);

                // RotateBackups moved the old main file into backup 1, so the destination is free.
                File.Move(tempPath, filePath);
                tempPath = null;
            }
            catch (Exception e)
            {
                // Mirrors the existing resolver behaviour: never let a save failure crash the client,
                // e.g. when multiple instances point at the same file.
                Log.Error($"Failed to save JSON '{filePath}': {e}");
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* best effort */ }
                }
            }
        }

        private bool TryDeserialize(string path, out T result)
        {
            result = null;

            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path);
                result = JsonSerializer.Deserialize(json, TypeInfo);
                return result != null;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load JSON save '{path}': {e.Message}");
                return false;
            }
        }

        private void RotateBackups(string filePath)
        {
            string backupDir = GetBackupDirectory(filePath);
            Directory.CreateDirectory(backupDir);

            // Rotate existing backups: oldest deleted, each other shifted up one (2 -> 3, 1 -> 2).
            for (int i = MAX_BACKUPS; i > 0; i--)
            {
                string current = GetBackupPath(filePath, i);

                if (i == MAX_BACKUPS)
                {
                    if (File.Exists(current))
                        File.Delete(current);
                }
                else
                {
                    string next = GetBackupPath(filePath, i + 1);

                    if (File.Exists(current))
                    {
                        if (File.Exists(next))
                            File.Delete(next);

                        File.Move(current, next);
                    }
                }
            }

            // Move the current main file into backup slot 1.
            string firstBackup = GetBackupPath(filePath, 1);

            if (File.Exists(filePath))
            {
                if (File.Exists(firstBackup))
                    File.Delete(firstBackup);

                File.Move(filePath, firstBackup);
            }
        }

        private void BackupCorruptFile(string filePath)
        {
            try
            {
                string corruptPath = filePath + ".corrupt";

                // Keep at most one corrupt copy - don't overwrite an earlier failure.
                if (File.Exists(corruptPath))
                    return;

                File.Copy(filePath, corruptPath);
                Log.Warn($"Backed up corrupt JSON save '{filePath}' to '{corruptPath}'.");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to back up corrupt JSON save '{filePath}': {e}");
            }
        }

        private static string GetBackupDirectory(string filePath) => Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Constants.BACKUP_FOLDER);

        private static string GetBackupPath(string filePath, int index) => Path.Combine(GetBackupDirectory(filePath), $"{Path.GetFileName(filePath)}.{index}");

        /// <summary>
        /// Acquires a cross-process lock guarding this file. A save can be touched by multiple client
        /// instances at once regardless of scope - Global files share the <c>Data</c> folder, but Server/
        /// Account/Char files can also collide when the same server/account/character is logged in from more
        /// than one client - so every scope is protected with a named mutex keyed on the file path.
        /// </summary>
        private IDisposable AcquireLock(string filePath = null) => new CrossProcessLock(filePath ?? FilePath);

        private sealed class CrossProcessLock : IDisposable
        {
            private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

            private readonly Mutex _mutex;
            private readonly bool _acquired;

            public CrossProcessLock(string key)
            {
                _mutex = new Mutex(false, BuildMutexName(key));

                try
                {
                    _acquired = _mutex.WaitOne(Timeout);

                    if (!_acquired)
                        Log.Warn($"Timed out acquiring cross-process lock for '{key}'; proceeding anyway.");
                }
                catch (AbandonedMutexException)
                {
                    // A previous owner crashed without releasing; we now own the mutex.
                    _acquired = true;
                }
            }

            public void Dispose()
            {
                if (_acquired)
                {
                    try { _mutex.ReleaseMutex(); }
                    catch { /* best effort */ }
                }

                _mutex.Dispose();
            }

            private static string BuildMutexName(string key)
            {
                // Named mutexes can't contain path separators, so hash the path into a stable, valid name.
                string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key)));

                // The Global\ prefix makes the mutex machine-wide on Windows; it isn't used on Unix.
                string prefix = CUOEnviroment.IsUnix ? string.Empty : "Global\\";

                return $"{prefix}TazUO_JsonSave_{hash}";
            }
        }
    }
}
