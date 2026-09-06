#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

/// <summary>
///     Owns what happens to a config file that could not be loaded: a bounded set of copies kept in a
///     subdirectory beside it, and the record the UI reads to tell the user about it once they are
///     in-world - detection happens at boot, long before the viewport exists.
///     <para>
///         Every storage facade goes through this rather than keeping its own copy-aside, so neither the
///         notice nor the on-disk layout depends on which of them happened to load the file.
///         <see cref="Files" /> is drained by the reader; a file is reported once per load.
///     </para>
/// </summary>
public static class CorruptFileManager
{
    #region Public constants

    /// <summary>Subdirectory copies are written into, relative to the file being backed up.</summary>
    public const string BackupDirectoryName = "corrupt-backups";

    #endregion

    #region Public accessors

    /// <summary>The files reported so far, oldest first. Drain with <c>TryDequeue</c>.</summary>
    public static ConcurrentQueue<CorruptConfigFile> Files { get; } = new();

    #endregion

    #region Private members

    /// <summary>
    ///     How many copies of any one file are kept. Generous on purpose: a config that would not load is
    ///     the only evidence of whatever produced it, and these are small text files.
    /// </summary>
    private const int RETAINED = 10;

    /// <summary>
    ///     Sorts lexicographically in time order, so pruning needs no filesystem timestamps - which a
    ///     copy, sync tool, or restore can rewrite.
    /// </summary>
    private const string TIMESTAMP_FORMAT = "yyyyMMdd-HHmmssfff";

    /// <summary>Ceiling on same-millisecond copies before naming gives up.</summary>
    private const int MAX_SEQUENCE = 1000;

    private const int SEQUENCE_DIGITS = 3;

    private const string BACKUP_EXTENSION = ".bak";

    /// <summary>Part of every copy's name, so a hand-browsing user can see why it was taken.</summary>
    private const string REASON = "corrupt";

    #endregion

    #region Public methods

    /// <summary>
    ///     Copies a file that could not be loaded aside and records it for the in-world notice. Call
    ///     before falling back to defaults, which overwrite the file.
    /// </summary>
    /// <param name="path">The file that could not be loaded.</param>
    /// <returns>Where the copy was written, or null if none could be taken.</returns>
    public static string? BackupAndReport(string path)
    {
        string? backupPath = Backup(path);

        Report(path, backupPath);

        return backupPath;
    }

    /// <summary>
    ///     Copies a file that could not be loaded aside, without recording it. For a caller that only
    ///     learns afterwards whether the fallback recovered the settings or reset them - it reports the
    ///     copy itself through <see cref="Report" />.
    ///     <para>
    ///         Logs rather than throws: a failed copy must not stop the load it was protecting. A copy
    ///         identical to the newest held is skipped, so a fault repeating every launch cannot rotate the
    ///         useful history out.
    ///     </para>
    /// </summary>
    /// <param name="path">The file to copy. Nothing happens if it does not exist.</param>
    /// <returns>
    ///     The copy's full path; the newest existing copy's path where an identical one was already held;
    ///     null where nothing was written.
    /// </returns>
    public static string? Backup(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string directory = BackupDirectoryFor(path);
            Directory.CreateDirectory(directory);

            List<string> existing = ExistingBackups(directory, path);

            if (existing.Count > 0 && IsSameContent(path, existing[^1]))
                return existing[^1];

            string backupPath = NextBackupPath(directory, path, existing);
            File.Copy(path, backupPath);

            Log.Warn($"Corrupt configuration file '{path}' backed up to '{backupPath}'.");

            // Only once the copy is safely taken: losing a prune costs disk, losing the copy costs the
            // one record of what the file held.
            Prune(existing);

            return backupPath;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to back up corrupt configuration file '{path}' - {e}");

            return null;
        }
    }

    /// <summary>Records a file that could not be loaded, and what answered in its place.</summary>
    /// <param name="path">The file that failed to load.</param>
    /// <param name="backupPath">Where its contents were copied, or null if no copy could be taken.</param>
    /// <param name="fallback">
    ///     What the client fell back to. <see cref="CorruptConfigFallback.Backup" /> is still worth
    ///     reporting: the settings that loaded may be behind what the user last saved.
    /// </param>
    public static void Report(
        string path,
        string? backupPath,
        CorruptConfigFallback fallback = CorruptConfigFallback.Defaults
    ) => Files.Enqueue(new CorruptConfigFile(path, backupPath, fallback));

    #endregion

    #region Private methods

    private static string BackupDirectoryFor(string path) =>
        Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, BackupDirectoryName);

    /// <summary>Every copy held of one file, oldest first.</summary>
    private static List<string> ExistingBackups(string directory, string path) =>
        // Ordinal: a culture-aware compare is free to disagree about digits.
        Directory.EnumerateFiles(directory, $"{Path.GetFileName(path)}.{REASON}.*{BACKUP_EXTENSION}")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    ///     Names the next copy. Sequence is always present and zero-padded, so names within a millisecond
    ///     still sort in order - what <see cref="Prune" /> evicts by.
    /// </summary>
    /// <exception cref="IOException">A thousand copies already share this millisecond.</exception>
    private static string NextBackupPath(string directory, string path, List<string> existing)
    {
        string timestamp = DateTime.UtcNow.ToString(TIMESTAMP_FORMAT, CultureInfo.InvariantCulture);
        string prefix = $"{Path.GetFileName(path)}.{REASON}.{timestamp}-";

        int sequence = NextSequence(existing, prefix);

        if (sequence >= MAX_SEQUENCE)
            throw new IOException($"Cannot name a backup of '{path}': {MAX_SEQUENCE} already share this millisecond.");

        return Path.Combine(directory, $"{prefix}{sequence:D3}{BACKUP_EXTENSION}");
    }

    /// <summary>
    ///     One past the highest sequence this millisecond holds. Highest in use rather than lowest free:
    ///     pruning frees low sequences, and reusing one would sort the new copy ahead of older ones.
    /// </summary>
    private static int NextSequence(List<string> existing, string prefix)
    {
        int highest = -1;

        foreach (string file in existing)
        {
            string name = Path.GetFileName(file);

            // Length-checked too: the directory is user-visible, so a hand-named file can match the
            // prefix without carrying a sequence.
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length < prefix.Length + SEQUENCE_DIGITS)
                continue;

            if (int.TryParse(name.AsSpan(prefix.Length, SEQUENCE_DIGITS), out int sequence) && sequence > highest)
                highest = sequence;
        }

        return highest + 1;
    }

    /// <summary>Drops oldest-first until one slot is free for the copy just taken.</summary>
    private static void Prune(List<string> existing)
    {
        int excess = existing.Count - (RETAINED - 1);

        for (int i = 0; i < excess; i++)
        {
            // Best effort, each on its own: an undeletable old copy costs disk, and must not be
            // reported as a failure to back up.
            try
            {
                File.Delete(existing[i]);
            }
            catch (Exception e)
            {
                Log.Warn($"Could not prune corrupt-config backup '{existing[i]}': {e.Message}");
            }
        }
    }

    private static bool IsSameContent(string fileA, string fileB)
    {
        var infoA = new FileInfo(fileA);
        var infoB = new FileInfo(fileB);

        // Short-circuit on different length, otherwise byte-by-byte comparison
        return infoA.Length == infoB.Length && File.ReadAllBytes(fileA).AsSpan().SequenceEqual(File.ReadAllBytes(fileB));
    }

    #endregion
}
