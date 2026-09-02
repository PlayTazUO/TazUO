#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ClassicUO.IO.Persistency;

/// <summary>
///     Keeps a bounded set of copies of a config file, in a subdirectory beside it.
///     <para>
///         Oldest copy past <see cref="Retained" /> is dropped. A copy identical to the newest held is
///         skipped, so a fault repeating every launch cannot rotate the useful history out.
///     </para>
/// </summary>
public sealed class ConfigBackupStore
{
    #region Public constants

    /// <summary>Subdirectory copies are written into, relative to the file being backed up.</summary>
    public const string DirectoryName = "config-backups";

    #endregion

    #region Public accessors

    /// <summary>How many copies of any one file are kept.</summary>
    public int Retained { get; }

    #endregion

    #region Private members

    private const int DEFAULT_RETAINED = 3;

    /// <summary>Sorts lexicographically in time order, so pruning needs no filesystem timestamps -
    /// which a copy, sync tool or restore can rewrite.</summary>
    private const string TIMESTAMP_FORMAT = "yyyyMMdd-HHmmssfff";

    /// <summary>Ceiling on same-millisecond copies before naming gives up.</summary>
    private const int MAX_SEQUENCE = 1000;

    private const int SEQUENCE_DIGITS = 3;

    private const string BACKUP_EXTENSION = ".bak";

    private readonly string _reason;

    #endregion

    #region Ctor

    /// <param name="reason">
    ///     Why these copies are taken - "corrupt", "premigration". Part of the file name, and separates
    ///     retention: one reason's copies never evict another's.
    /// </param>
    /// <param name="retained">How many copies of any one file to keep.</param>
    /// <exception cref="ArgumentException"><paramref name="reason" /> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="retained" /> is below 1.</exception>
    public ConfigBackupStore(string reason, int retained = DEFAULT_RETAINED)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A backup reason is required.", nameof(reason));

        ArgumentOutOfRangeException.ThrowIfLessThan(retained, 1);

        _reason = reason;
        Retained = retained;
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Copies <paramref name="path" /> aside, then prunes past <see cref="Retained" />. Reports rather
    ///     than throws: a failed backup must not stop the load or save it was protecting.
    /// </summary>
    /// <param name="path">The file to copy. Nothing happens if it does not exist.</param>
    /// <param name="error">The failure, when one stopped a copy being taken.</param>
    /// <returns>
    ///     The copy's full path; the newest existing copy's path where an identical one was already held;
    ///     null where nothing was written.
    /// </returns>
    public string? TryBackup(string path, out Exception? error)
    {
        error = null;

        try
        {
            if (!File.Exists(path))
                return null;

            string directory = BackupDirectory(path);

            Directory.CreateDirectory(directory);

            List<string> existing = ExistingBackups(directory, path);

            if (existing.Count > 0 && SameContent(path, existing[^1]))
                return existing[^1];

            string backupPath = NextBackupPath(directory, path, existing);

            File.Copy(path, backupPath);

            Prune(existing);

            return backupPath;
        }
        catch (Exception e)
        {
            error = e;

            return null;
        }
    }

    #endregion

    #region Private methods

    private static string BackupDirectory(string path) =>
        Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, DirectoryName);

    /// <summary>
    ///     Names the next copy. Sequence is always present and zero-padded, so names within a millisecond
    ///     still sort in order - what <see cref="Prune" /> evicts by.
    /// </summary>
    /// <exception cref="IOException">A thousand copies already share this millisecond.</exception>
    private string NextBackupPath(string directory, string path, List<string> existing)
    {
        string timestamp = DateTime.UtcNow.ToString(TIMESTAMP_FORMAT, CultureInfo.InvariantCulture);
        string prefix = $"{Path.GetFileName(path)}.{_reason}.{timestamp}-";

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

    /// <summary>Every copy this store holds of one file, oldest first.</summary>
    private List<string> ExistingBackups(string directory, string path) =>
        // Ordinal: a culture-aware compare is free to disagree about digits.
        Directory.EnumerateFiles(directory, $"{Path.GetFileName(path)}.{_reason}.*{BACKUP_EXTENSION}")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

    /// <summary>Drops oldest-first until one slot is free for the copy about to be taken.</summary>
    private void Prune(List<string> existing)
    {
        int excess = existing.Count - (Retained - 1);

        for (int i = 0; i < excess; i++)
            File.Delete(existing[i]);
    }

    private static bool SameContent(string left, string right)
    {
        var leftInfo = new FileInfo(left);
        var rightInfo = new FileInfo(right);

        if (leftInfo.Length != rightInfo.Length)
            return false;

        return File.ReadAllBytes(left).AsSpan().SequenceEqual(File.ReadAllBytes(right));
    }

    #endregion
}
