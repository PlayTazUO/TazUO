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
    /// Forces the new content onto the device before the rename publishes it. Without this a power
    /// loss can leave the rename committed while the data is still only in the page cache - the file
    /// then reads as zeroes, which is worse than either version. Costs a device flush per write, so
    /// a hot caller writing recreatable data can turn it off.
    /// </param>
    /// <exception cref="IOException">The write or the rename failed.</exception>
    /// <exception cref="UnauthorizedAccessException">The path is not writable.</exception>
    public static void Write(string path, string contents, bool flushToDisk = true)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            WriteTemp(tempPath, contents, flushToDisk);

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            throw;
        }
    }

    /// <summary>Writes the temp file, optionally not returning until the device has the bytes.</summary>
    private static void WriteTemp(string tempPath, string contents, bool flushToDisk)
    {
        if (!flushToDisk)
        {
            File.WriteAllText(tempPath, contents);

            return;
        }

        // Written through a stream rather than File.WriteAllText so the handle is still open to flush
        // through. Encoding matches WriteAllText's default: UTF-8, no BOM.
        using var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true))
        {
            writer.Write(contents);
        }

        stream.Flush(flushToDisk: true);
    }
}
