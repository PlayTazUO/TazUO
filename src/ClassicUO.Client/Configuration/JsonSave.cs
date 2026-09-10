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

namespace ClassicUO.Configuration;

/// <summary>
///     Base class for JSON save files that live in a scoped location on disk (see
///     <see cref="SettingsScope" /> and <see cref="JsonSaveLocationHelper" />).
///     Provides:
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="Save" /> - writes the file atomically and keeps up to
///                 <see cref="MAX_BACKUPS" /> rotating backups in a <c>backups</c> sub-folder.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Load" /> - loads the file, falling back through the backups on failure and
///                 finally to defaults, persisted unless <see cref="PersistFreshDefaults" /> says otherwise. An
///                 unreadable main file is copied aside and reported through <see cref="CorruptFileManager" />. The one
///                 exception is a file
///                 written by a newer build, which is left alone and never written to by the instance standing in for
///                 it.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="MigrationPipeline" /> - optional. Brings an older persisted shape up to
///                 the current one before it binds, and writes the result back.
///             </description>
///         </item>
///     </list>
///     Uses the curiously-recurring-template pattern so <see cref="Load" /> can return the concrete type.
///     Derived types are their own serializable data container and must supply a source-generated
///     <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}" /> via <see cref="TypeInfo" />.
/// </summary>
/// <typeparam name="T">The concrete derived save type.</typeparam>
public abstract class JsonSave<T> where T : JsonSave<T>, INotifyPropertyChanged, new()
{
    /// <summary>Raised when a property set through <see cref="SetProperty{TFieldType}" /> changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>How many rotating copies of the file are kept beside it.</summary>
    private const int MAX_BACKUPS = 5;

    /// <summary>The scope that determines which folder this file is saved in.</summary>
    protected abstract SettingsScope Scope { get; }

    /// <summary>The file name including extension, e.g. <c>"friends.json"</c>.</summary>
    protected abstract string FileName { get; }

    /// <summary>Source-generated JSON metadata used to (de)serialize this save.</summary>
    protected abstract JsonTypeInfo<T> TypeInfo { get; }

    /// <summary>
    ///     Brings this save's persisted shape up to the one <typeparamref name="T" /> binds, run on the raw
    ///     text before it is deserialized. Null - the default - for a save whose shape has never changed.
    ///     <para>
    ///         A save that declares one must carry its version in the document (see
    ///         <see cref="JsonMigrationFormat" />); the migrated text is written back once it has been shown to
    ///         bind, so the migration is paid for once rather than on every load.
    ///     </para>
    /// </summary>
    protected virtual ConfigMigrationPipeline<JsonObject>? MigrationPipeline => null;

    /// <summary>
    ///     Whether a load that found nothing on disk writes its defaults out. False for an opt-in config
    ///     that should stay absent until configured. A file that existed and failed is replaced either way.
    /// </summary>
    /// <remarks>Opt-in system, off by default - an untouched profile keeps no file.</remarks>
    protected virtual bool PersistFreshDefaults => true;

    /// <summary>The directory this file is saved in, resolved from <see cref="Scope" />.</summary>
    [JsonIgnore]
    public string SaveDirectory => JsonSaveLocationHelper.GetScopeDirectory(Scope);

    /// <summary>The full path to the save file.</summary>
    [JsonIgnore]
    public string FilePath => Path.Combine(SaveDirectory, FileName);

    /// <summary>The directory that holds the rotating backups for this file.</summary>
    [JsonIgnore]
    public string BackupDirectory => Path.Combine(SaveDirectory, Constants.BACKUP_FOLDER);

    /// <summary>
    ///     The file this instance was loaded from, when <see cref="LoadFrom" /> named one. Null for an
    ///     instance built in memory or loaded through <see cref="Scope" /> - those keep following the
    ///     scope, which is the point of loading through it.
    /// </summary>
    protected string? SourcePath { get; private set; }

    /// <summary>
    ///     Set on an instance that stands in for a file it must not write over - see
    ///     <see cref="NewLeavingFileIntact" />. Holds for the instance's whole life: the file stays
    ///     unreadable to this build no matter how much later the save comes.
    /// </summary>
    private bool _savesSuppressed;

    /// <summary>
    ///     Loads the save for type <typeparamref name="T" /> from <see cref="FilePath" />. If the main file is
    ///     missing or unreadable the backups are tried in order; if they all fail a fresh instance is created,
    ///     and written to disk unless <see cref="PersistFreshDefaults" /> says otherwise. A file written by a
    ///     newer build is the exception - see <see cref="MigrationPipeline" />.
    /// </summary>
    public static T Load() => LoadInternal(new T().FilePath, false);

    /// <summary>
    ///     Like <see cref="Load" />, but reads from an explicit path rather than <see cref="FilePath" />.
    ///     The instance is bound to that path, so a later <see cref="Save" /> writes back to the file it
    ///     came from rather than to wherever <see cref="Scope" /> resolves to by then.
    /// </summary>
    protected static T LoadFrom(string filePath) => LoadInternal(filePath, true);

    /// <summary>
    ///     Saves this instance atomically, rotating the previous version into the backups folder. Writes
    ///     to <see cref="SourcePath" /> where one was bound, and to <see cref="FilePath" /> otherwise.
    ///     <para>
    ///         A no-op on an instance that stands in for a file written by a newer build: those defaults are
    ///         not what the file holds, and writing them would destroy it.
    ///     </para>
    /// </summary>
    public virtual void Save() => SaveTo(SourcePath ?? FilePath);

    /// <summary>
    ///     Like <see cref="Save" />, but writes to an explicit path rather than <see cref="FilePath" />.
    /// </summary>
    protected void SaveTo(string filePath)
    {
        using (AcquireLock(filePath))
            SaveCore(filePath);
    }

    /// <summary>
    ///     Reads <paramref name="filePath" /> into a fresh instance, optionally binding the instance to
    ///     that path for its later saves.
    /// </summary>
    /// <param name="filePath">The main file to load.</param>
    /// <param name="pinSource">Whether the instance should remember where it came from.</param>
    /// <returns>The loaded instance, or a new one when nothing on disk could be used.</returns>
    private static T LoadInternal(string filePath, bool pinSource)
    {
        var instance = new T();
        T result;

        using (instance.AcquireLock(filePath))
            result = instance.LoadCore(filePath);

        if (pinSource)
            result.SourcePath = filePath;

        return result;
    }

    /// <summary>
    ///     Produces an instance from <paramref name="filePath" />, falling back through its backups and
    ///     finally to defaults - see <see cref="PersistFreshDefaults" /> for whether those are written.
    ///     Assumes the caller already holds the file lock.
    /// </summary>
    /// <param name="filePath">The main file to load.</param>
    /// <returns>The loaded instance, or a new one when nothing on disk could be used.</returns>
    private T LoadCore(string filePath)
    {
        // Try the main file first. Only it gets the migrated text written back: a backup is read to
        // recover from, not to become the new main file.
        LoadOutcome outcome = TryLoad(filePath, true, out T? loaded);

        if (loaded != null)
            return loaded;

        // The one outcome that says the file is intact rather than damaged, and so the one worth
        // more than anything that could replace it. A backup would be a staler copy of the same
        // settings that equally cannot be saved, so nothing is gained by reading one.
        if (outcome == LoadOutcome.AheadOfThisBuild)
            return NewLeavingFileIntact(filePath);

        // Copied now, before the fallbacks write over it, but reported only once the outcome is
        // known: what the user needs to hear differs between recovered settings and fresh defaults.
        bool hadMainFile = File.Exists(filePath);
        string? corruptCopy = hadMainFile ? CorruptFileManager.Backup(filePath) : null;

        // Fall back through the rotating backups, newest first.
        for (int i = 1; i <= MAX_BACKUPS; i++)
        {
            TryLoad(GetBackupPath(filePath, i), false, out loaded);

            if (loaded == null)
                continue;

            Log.Warn($"Recovered JSON save '{filePath}' from backup {i}.");

            if (hadMainFile)
                CorruptFileManager.Report(filePath, corruptCopy, CorruptConfigFallback.Backup);

            return loaded;
        }

        // Nothing usable on disk - start fresh and persist it (already holding the lock).
        if (hadMainFile)
        {
            Log.Error($"Failed to load JSON save '{filePath}'; creating a fresh copy.");
            CorruptFileManager.Report(filePath, corruptCopy);
        }

        var fresh = new T();

        // A file that was there and failed is replaced regardless - leaving it re-reports every launch.
        if (hadMainFile || PersistFreshDefaults)
            fresh.SaveCore(filePath);

        return fresh;
    }

    /// <summary>
    ///     Answers a file written by a newer build with defaults that will never be written back over it.
    ///     <para>
    ///         Nothing is copied aside: the file is undamaged, and it is the only copy of settings the build
    ///         that wrote it still reads. Persisting defaults here would destroy them, and so would any save
    ///         later in the session - hence the suppression rather than a one-off skip.
    ///     </para>
    /// </summary>
    /// <param name="filePath">The file being left as it is.</param>
    /// <returns>A default instance whose saves are suppressed.</returns>
    private static T NewLeavingFileIntact(string filePath)
    {
        Log.Error($"JSON save '{filePath}' was written by a newer build; leaving it as it is and running on defaults.");
        CorruptFileManager.Report(filePath, null, CorruptConfigFallback.Preserved);

        var fresh = new T { _savesSuppressed = true };
        return fresh;
    }

    /// <summary>
    ///     Serializes and writes this instance, swallowing any failure. Assumes the caller already holds
    ///     the file lock.
    /// </summary>
    /// <param name="filePath">The file to write.</param>
    private void SaveCore(string filePath)
    {
        if (_savesSuppressed)
        {
            Log.Trace($"Skipping save of '{filePath}': the file on disk is newer than this build can read.");
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize((T)this, TypeInfo);
            ConfigMigrationPipeline<JsonObject>? pipeline = MigrationPipeline;

            // Stamped rather than serialized off a property, which could drift from the migration list.
            WriteJson(filePath, pipeline == null ? json : pipeline.Stamp(json));
        }
        catch (Exception e)
        {
            // Mirrors the existing resolver behaviour: never let a save failure crash the client,
            // e.g. when multiple instances point at the same file.
            Log.Error($"Failed to save JSON '{filePath}': {e}");
        }
    }

    /// <summary>
    ///     Rotates the current file into the backups and publishes <paramref name="json" /> in its place.
    ///     <para>
    ///         The new content is staged first, so a write that fails - a full disk, a revoked permission -
    ///         leaves the file already on disk exactly as it was. Only once the bytes are down does the
    ///         current version rotate out, and the destination it frees is filled by a rename. The main file
    ///         is therefore absent for one rename rather than for a whole write, and a rename that fails puts
    ///         the rotated version back rather than leaving the file to be recovered from a backup.
    ///     </para>
    /// </summary>
    /// <param name="filePath">The file to publish to. Its directory is created if missing.</param>
    /// <param name="json">The text to write.</param>
    /// <exception cref="IOException">The write, the rotation, or the publish failed.</exception>
    private static void WriteJson(string filePath, string json)
    {
        string stagedPath = AtomicFile.Stage(filePath, json);
        bool rotated = false;

        try
        {
            rotated = RotateBackups(filePath);
            AtomicFile.Publish(stagedPath, filePath);
        }
        catch
        {
            AtomicFile.Delete(stagedPath);

            if (rotated)
                RestoreRotated(filePath);

            throw;
        }
    }

    /// <summary>
    ///     Moves the version <see cref="RotateBackups" /> put in slot 1 back over the main file, after a
    ///     publish that failed to fill the place it left. Best effort: the caller is already raising the
    ///     failure that led here. The deeper slots stay shifted, which costs a duplicate rather than a
    ///     version.
    /// </summary>
    /// <param name="filePath">The main file to put back.</param>
    private static void RestoreRotated(string filePath)
    {
        try
        {
            string firstBackup = GetBackupPath(filePath, 1);

            if (File.Exists(firstBackup) && !File.Exists(filePath))
                File.Move(firstBackup, filePath);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to restore '{filePath}' after a failed save: {e}");
        }
    }

    /// <summary>
    ///     Reads one candidate file, migrating its shape where <see cref="MigrationPipeline" /> says to.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="persistMigration">
    ///     Whether a migration that changed the text should be written back
    ///     to <paramref name="path" />. False when reading a backup, which stays as it was found.
    /// </param>
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
            catch (ConfigVersionAheadException e)
            {
                Log.Error($"JSON save '{path}' was written by a newer build - {e.Message}");
                return LoadOutcome.AheadOfThisBuild;
            }
            catch (ConfigMigrationException e)
            {
                // The document parsed and its version was one this build starts from, so a migration
                // failing over it points at the content, not at the build. Damaged like any other
                // unreadable file, and answered the same way.
                Log.Error($"Cannot migrate JSON save '{path}' to the current shape - {e}");
                return LoadOutcome.Unreadable;
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
    ///     Writes migrated text back over the file it came from, rotating the pre-migration version into
    ///     the backups. Logged rather than raised on failure: the caller's instance is already good, and
    ///     the next load simply migrates again.
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
    ///     Shifts the rotating backups up one slot, dropping the oldest, then moves the current file into
    ///     slot 1. The main file is left absent, for the caller to write in its place.
    /// </summary>
    /// <param name="filePath">The main file being rotated out.</param>
    /// <returns>
    ///     <c>true</c> when the main file was moved into slot 1 and the caller now owes it a
    ///     replacement, <c>false</c> when there was no main file to rotate.
    /// </returns>
    /// <exception cref="IOException">A slot could not be deleted or moved.</exception>
    private static bool RotateBackups(string filePath)
    {
        string backupDir = GetBackupDirectory(filePath);
        Directory.CreateDirectory(backupDir);

        // Shifted up one by overwriting moves (4 -> 5, then 3 -> 4, ...). The oldest slot is replaced
        // rather than emptied first, so a move that fails has not already discarded a version.
        for (int i = MAX_BACKUPS - 1; i > 0; i--)
        {
            string current = GetBackupPath(filePath, i);

            if (File.Exists(current))
                File.Move(current, GetBackupPath(filePath, i + 1), overwrite: true);
        }

        // Move the current main file into backup slot 1.
        if (!File.Exists(filePath))
            return false;

        File.Move(filePath, GetBackupPath(filePath, 1), overwrite: true);
        return true;
    }

    private static string GetBackupDirectory(string filePath) => Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Constants.BACKUP_FOLDER);

    private static string GetBackupPath(string filePath, int index) => Path.Combine(GetBackupDirectory(filePath), $"{Path.GetFileName(filePath)}.{index}");

    /// <summary>
    ///     Updates the given property with the given value if it is different from the current one.
    ///     Raises the <see cref="PropertyChanged" /> event, if a change has occurred
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
    ///     Raises the <see cref="PropertyChanged" /> event with the specified property name
    /// </summary>
    /// <param name="propertyName">The property that was updated. Passed by the compiler.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///     Acquires a cross-process lock guarding this file. A save can be touched by multiple client
    ///     instances at once regardless of scope - Global files share the <c>Data</c> folder, but Server/
    ///     Account/Char files can also collide when the same server/account/character is logged in from more
    ///     than one client - so every scope is protected with a named mutex keyed on the file path.
    /// </summary>
    private CrossProcessLock AcquireLock(string? filePath = null) => new CrossProcessLock(filePath ?? FilePath);

    /// <summary>
    ///     Why one candidate file did not yield an instance, which decides whether another is worth trying.
    /// </summary>
    private enum LoadOutcome
    {
        /// <summary>An instance was produced.</summary>
        Loaded,

        /// <summary>
        ///     Missing text, unreadable text, text that defeated a migration, or text that did not bind.
        ///     Damaged either way, so another copy may still be good and this one may be replaced.
        /// </summary>
        Unreadable,

        /// <summary>
        ///     Intact, but at a version above the highest this build knows. Not damaged and not to be
        ///     replaced - see <see cref="NewLeavingFileIntact" />.
        /// </summary>
        AheadOfThisBuild
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
                try { _mutex.ReleaseMutex(); }
                catch
                {
                    /* best effort */
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
