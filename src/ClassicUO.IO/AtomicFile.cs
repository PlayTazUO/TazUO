#nullable enable

using System;
using System.IO;

namespace ClassicUO.IO;

/// <summary>Writes a file so it is always fully the old content or fully the new one, never a
/// truncated mix, even if the process dies mid-write.</summary>
public static class AtomicFile
{
    /// <summary>
    /// Replaces <paramref name="path"/> with <paramref name="contents"/> as a single filesystem
    /// operation.
    /// </summary>
    /// <exception cref="IOException">The write or the replace failed.</exception>
    public static void Write(string path, string contents)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, contents);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            throw;
        }
    }
}
