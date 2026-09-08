#nullable enable

using System;
using System.IO;
using System.Text;

namespace ClassicUO.IO;

/// <summary>
/// Writes a file through a same-directory temp plus a rename, so a reader sees fully the old content
/// or fully the new one, never a truncated mix.
/// </summary>
public static class AtomicFile
{
    /// <summary>
    /// Replaces <paramref name="path"/> with <paramref name="contents"/>, creating the directory if it
    /// is missing. The temp file is cleaned up on failure.
    /// </summary>
    /// <param name="path">File to replace.</param>
    /// <param name="contents">Text to write.</param>
    /// <param name="flushToDisk">
    /// Forces the content onto the device before the rename publishes it. Without it, a power loss can
    /// commit the rename with the data still in the page cache, and the file reads as zeroes. Costs a
    /// device flush per write, so a hot caller writing recreatable data can turn it off.
    /// </param>
    /// <exception cref="IOException">The write or the rename failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The path is not writable.</exception>
    public static void Write(string path, string contents, bool flushToDisk = true)
    {
        string stagedPath = Stage(path, contents, flushToDisk);

        try
        {
            Publish(stagedPath, path);
        }
        catch
        {
            Delete(stagedPath);
            throw;
        }
    }

    /// <summary>
    /// Writes <paramref name="contents"/> to a temp file beside <paramref name="path"/> without
    /// touching <paramref name="path"/> itself, for a caller with work to do between the write and the
    /// rename that publishes it - rotating the current version out, say. Pair with <see cref="Publish"/>,
    /// and <see cref="Delete"/> the staged file on any path that abandons it.
    /// </summary>
    /// <param name="path">The file that will eventually be replaced. Its directory is created if missing.</param>
    /// <param name="contents">Text to write.</param>
    /// <param name="flushToDisk">As <see cref="Write"/>.</param>
    /// <returns>The staged file's path.</returns>
    /// <exception cref="IOException">The write failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The path is not writable.</exception>
    public static string Stage(string path, string contents, bool flushToDisk = true)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Unique so concurrent writers can't clobber each other's bytes. Costs a leaked temp file if the
        // process dies before Publish; nothing sweeps them.
        string stagedPath = Path.Combine(directory ?? string.Empty, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            // Writing is a bit non-standard here, to avoid flushing concerns. More on that in WriteTemp itself.
            WriteTemp(stagedPath, contents, flushToDisk);
        }
        catch
        {
            Delete(stagedPath);
            throw;
        }

        return stagedPath;
    }

    /// <summary>
    /// Renames a <see cref="Stage"/>d file over <paramref name="path"/>. The single step a reader can
    /// observe, which is what makes the replacement atomic.
    /// </summary>
    /// <param name="stagedPath">The staged file to publish.</param>
    /// <param name="path">The file to replace. Need not exist.</param>
    /// <exception cref="IOException">The rename failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The path is not writable.</exception>
    public static void Publish(string stagedPath, string path) => File.Move(stagedPath, path, overwrite: true);

    /// <summary>
    ///     Deletes a file, given by a relative or absolute path
    /// </summary>
    /// <param name="path">The file to delete</param>
    /// <returns>
    ///     <see langword="true" /> if the file was deleted, <see langword="false" /> if the file does not exist or could
    ///     not be deleted
    /// </returns>
    public static bool Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
        }
        catch
        {
            // No Op
        }

        return false;
    }

    /// <summary>Writes the temp file, optionally not returning until the device has the bytes.</summary>
    private static void WriteTemp(string tempPath, string contents, bool flushToDisk)
    {
        if (!flushToDisk)
        {
            File.WriteAllText(tempPath, contents);
            return;
        }

        // When using WriteAllText, OS decides when to call fsync to flush to disk. This may create a situation where we "think" data has been stored even though it hasn't.
        // Under normal circumstances, this is not an issue, but if a power-off occurs in this short timeframe, we risk data loss.
        //
        // To mitigate this, we use a stream and flush it directly, which forces dotnet to call fsync.
        using var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true))
            writer.Write(contents);

        stream.Flush(flushToDisk: true);
    }
}
