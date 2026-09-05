#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.IO.Persistency.Migrations;
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
    /// preserved once as <c>&lt;file&gt;.corrupt</c> for later inspection and reported through
    /// <see cref="CorruptConfigReporter"/>.</description></item>
    /// <item><description><see cref="MigrationPipeline"/> - optional. Brings an older persisted shape up to
    /// the current one before it binds, and writes the result back.</description></item>
    /// </list>
    ///
    /// Uses the curiously-recurring-template pattern so <see cref="Load"/> can return the concrete type.
    /// Derived types are their own serializable data container and must supply a source-generated
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> via <see cref="TypeInfo"/>.
    /// </summary>
    /// <typeparam name="T">The concrete derived save type.</typeparam>
    public abstract class JsonSave<T> where T : JsonSave<T>, INotifyPropertyChanged, new()
    {
        /// <summary>Raised when a property set through <see cref="SetProperty{TFieldType}"/> changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        private const int MAX_BACKUPS = 3;

        /// <summary>The scope that determines which folder this file is saved in.</summary>
        protected abstract SettingsScope Scope { get; }

        /// <summary>The file name including extension, e.g. <c>"friends.json"</c>.</summary>
        protected abstract string FileName { get; }

        /// <summary>Source-generated JSON metadata used to (de)serialize this save.</summary>
        protected abstract JsonTypeInfo<T> TypeInfo { get; }

        /// <summary>
        /// Brings this save's persisted shape up to the one <typeparamref name="T"/> binds, run on the raw
        /// text before it is deserialized. Null - the default - for a save whose shape has never changed.
        /// <para>
        /// A save that declares one must carry its version in the document (see
        /// <see cref="JsonMigrationFormat"/>); the migrated text is written back once it has been shown to
        /// bind, so the migration is paid for once rather than on every load.
        /// </para>
        /// </summary>
        protected virtual ConfigMigrationPipeline<JsonObject>? MigrationPipeline => null;

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
            var instance = new T();

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

        /// <summary>
        /// Produces an instance from <paramref name="filePath"/>, falling back through its backups and
        /// finally to a freshly persisted default. Assumes the caller already holds the file lock.
        /// </summary>
        /// <param name="filePath">The main file to load.</param>
        /// <returns>The loaded instance, or a new one when nothing on disk could be used.</returns>
        private T LoadCore(string filePath)
        {
            // Try the main file first. Only it gets the migrated text written back: a backup is read to
            // recover from, not to become the new main file.
            LoadOutcome outcome = TryLoad(filePath, persistMigration: true, out T? loaded);

            if (loaded != null)
                return loaded;

            // Copied now, before the fallbacks write over it, but reported only once the outcome is
            // known: what the user needs to hear differs between recovered settings and fresh defaults.
            bool hadMainFile = File.Exists(filePath);
            string? corruptCopy = hadMainFile ? BackupCorruptFile(filePath) : null;

            // A shape this build cannot migrate is not worth chasing through the backups: they hold
            // older shapes of the same file, so none of them can answer what the newest one could not.
            if (outcome != LoadOutcome.Unmigratable)
            {
                // Fall back through the rotating backups, newest first.
                for (int i = 1; i <= MAX_BACKUPS; i++)
                {
                    TryLoad(GetBackupPath(filePath, i), persistMigration: false, out loaded);

                    if (loaded == null)
                        continue;

                    Log.Warn($"Recovered JSON save '{filePath}' from backup {i}.");

                    if (hadMainFile)
                        CorruptConfigReporter.ReportRecovered(filePath, corruptCopy);

                    return loaded;
                }
            }

            // Nothing usable on disk - start fresh and persist it (already holding the lock).
            if (hadMainFile)
            {
                Log.Error($"Failed to load JSON save '{filePath}'; creating a fresh copy.");
                CorruptConfigReporter.Report(filePath, corruptCopy);
            }

            var fresh = new T();
            fresh.SaveCore(filePath);
            return fresh;
        }

        /// <summary>
        /// Serializes and writes this instance, swallowing any failure. Assumes the caller already holds
        /// the file lock.
        /// </summary>
        /// <param name="filePath">The file to write.</param>
        private void SaveCore(string filePath)
        {
            try
            {
                WriteJson(filePath, JsonSerializer.Serialize((T)this, TypeInfo));
            }
            catch (Exception e)
            {
                // Mirrors the existing resolver behaviour: never let a save failure crash the client,
                // e.g. when multiple instances point at the same file.
                Log.Error($"Failed to save JSON '{filePath}': {e}");
            }
        }

        /// <summary>
        /// Rotates the current file into the backups, then publishes <paramref name="json"/> in its place.
        /// <para>
        /// Rotation first, so what is on disk is preserved before anything overwrites it. That leaves a
        /// window where the main file is absent and backup 1 holds its content - which is what
        /// <see cref="LoadCore"/> recovers from - rather than one where a half-written file has replaced
        /// the only copy.
        /// </para>
        /// </summary>
        /// <param name="filePath">The file to publish to. Its directory is created if missing.</param>
        /// <param name="json">The text to write.</param>
        /// <exception cref="IOException">The rotation or the write failed.</exception>
        private static void WriteJson(string filePath, string json)
        {
            string? directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            RotateBackups(filePath);
            AtomicFile.Write(filePath, json);
        }

        /// <summary>
        /// Reads one candidate file, migrating its shape where <see cref="MigrationPipeline"/> says to.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <param name="persistMigration">Whether a migration that changed the text should be written back
        /// to <paramref name="path"/>. False when reading a backup, which stays as it was found.</param>
        /// <param name="result">The instance, when one was produced.</param>
        /// <returns>What became of the attempt.</returns>
        private LoadOutcome TryLoad(string path, bool persistMigration, out T? result)
        {
            result = null;

            if (!File.Exists(path))
                return LoadOutcome.Unreadable;

            string jsonText;

            try
            {
                jsonText = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to read JSON save '{path}': {e.Message}");
                return LoadOutcome.Unreadable;
            }

            ConfigMigrationPipeline<JsonObject>? pipeline = MigrationPipeline;
            bool shapeChanged = false;

            if (pipeline != null)
            {
                ConfigMigrationResult migration;

                try
                {
                    migration = pipeline.Migrate(jsonText);
                }
                catch (ConfigDocumentMalformedException e)
                {
                    // Not a document at all, so nothing was established about its shape - a backup of the
                    // same file may well still be readable.
                    Log.Warn($"Failed to parse JSON save '{path}': {e.Message}");
                    return LoadOutcome.Unreadable;
                }
                catch (ConfigMigrationException e)
                {
                    Log.Error($"Cannot migrate JSON save '{path}' to the current shape - {e}");
                    return LoadOutcome.Unmigratable;
                }

                jsonText = migration.Text;
                shapeChanged = migration.Changed;
            }

            if (!TryBind(path, jsonText, out result))
                return LoadOutcome.Unreadable;

            // Written back only now, with the bind standing as proof the migrated text is usable.
            if (shapeChanged && persistMigration)
                TryPersistFile(path, jsonText);

            return LoadOutcome.Loaded;
        }

        /// <summary>Deserializes prepared text, reporting failure rather than raising it.</summary>
        /// <param name="path">The file the text came from, for logging only.</param>
        /// <param name="json">Text already brought to the current shape.</param>
        /// <param name="result">The bound instance, or null when the text did not bind.</param>
        /// <returns><c>true</c> when an instance was produced, <c>false</c> otherwise.</returns>
        private bool TryBind(string path, string json, out T? result)
        {
            result = null;

            try
            {
                result = JsonSerializer.Deserialize(json, TypeInfo);
                return result != null;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load JSON save '{path}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Writes migrated text back over the file it came from, rotating the pre-migration version into
        /// the backups. Logged rather than raised on failure: the caller's instance is already good, and
        /// the next load simply migrates again.
        /// </summary>
        /// <param name="filePath">The file the migrated text came from.</param>
        /// <param name="content">The migrated text, already shown to bind.</param>
        private static void TryPersistFile(string filePath, string content)
        {
            try
            {
                WriteJson(filePath, content);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to write migrated JSON save '{filePath}': {e}");
            }
        }

        /// <summary>
        /// Shifts the rotating backups up one slot, dropping the oldest, then moves the current file into
        /// slot 1. The main file is left absent, for the caller to write in its place.
        /// </summary>
        /// <param name="filePath">The main file being rotated out.</param>
        /// <exception cref="IOException">A slot could not be deleted or moved.</exception>
        private static void RotateBackups(string filePath)
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

        /// <summary>
        /// Copies a file that could not be used aside, so it outlives whatever is written in its place.
        /// Call before falling back, which overwrites what is on disk. Telling the user is the caller's:
        /// only it knows whether the fallback recovered the settings or reset them.
        /// </summary>
        /// <param name="filePath">The file that could not be used. Must exist.</param>
        /// <returns>Where the copy was written, or null if none could be taken.</returns>
        private static string? BackupCorruptFile(string filePath)
        {
            string? corruptPath = filePath + ".corrupt";

            try
            {
                // Keep at most one corrupt copy - don't overwrite an earlier failure. Still returned:
                // the file is being answered now, whatever happened on an earlier run.
                if (!File.Exists(corruptPath))
                {
                    File.Copy(filePath, corruptPath);
                    Log.Warn($"Backed up corrupt JSON save '{filePath}' to '{corruptPath}'.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to back up corrupt JSON save '{filePath}': {e}");
                corruptPath = null;
            }

            return corruptPath;
        }

        private static string GetBackupDirectory(string filePath) => Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Constants.BACKUP_FOLDER);

        private static string GetBackupPath(string filePath, int index) => Path.Combine(GetBackupDirectory(filePath), $"{Path.GetFileName(filePath)}.{index}");

        /// <summary>
        /// Updates the given property with the given value if it is different from the current one.
        /// Raises the <see cref="PropertyChanged" /> event, if a change has occurred
        /// </summary>
        /// <param name="storage">The field to update</param>
        /// <param name="value">The value to set</param>
        /// <param name="propertyName">The name of the property being updated</param>
        /// <typeparam name="TFieldType">The type of property being updated</typeparam>
        /// <returns><c>true</c> if a change has occurred, <c>false</c> otherwise</returns>
        protected bool SetProperty<TFieldType>(ref TFieldType storage, TFieldType value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<TFieldType>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event with the specified property name
        /// </summary>
        /// <param name="propertyName">The property that was updated. Passed by the compiler.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Acquires a cross-process lock guarding this file. A save can be touched by multiple client
        /// instances at once regardless of scope - Global files share the <c>Data</c> folder, but Server/
        /// Account/Char files can also collide when the same server/account/character is logged in from more
        /// than one client - so every scope is protected with a named mutex keyed on the file path.
        /// </summary>
        private IDisposable AcquireLock(string? filePath = null) => new CrossProcessLock(filePath ?? FilePath);

        /// <summary>
        /// Why one candidate file did not yield an instance, which decides whether another is worth trying.
        /// </summary>
        private enum LoadOutcome
        {
            /// <summary>An instance was produced.</summary>
            Loaded,

            /// <summary>Missing, unreadable, or not bindable text. Another copy may still be good.</summary>
            Unreadable,

            /// <summary>Readable, but its shape cannot be brought to the current one. No older copy helps.</summary>
            Unmigratable
        }

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
