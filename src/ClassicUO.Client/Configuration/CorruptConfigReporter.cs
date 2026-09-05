#nullable enable

using System.Collections.Concurrent;

namespace ClassicUO.Configuration;

/// <summary>
/// Collects the config files that could not be loaded, so the UI can tell the user about them once
/// they are in-world - detection happens at boot, long before the viewport exists.
/// <para>
/// Shared by every storage facade rather than owned by one, so the notice does not depend on which
/// of them happened to load the file. Drained by the reader; a file is reported once per load.
/// </para>
/// </summary>
public static class CorruptConfigReporter
{
    /// <summary>The files reported so far, oldest first. Drain with <c>TryDequeue</c>.</summary>
    public static readonly ConcurrentQueue<CorruptConfigFile> Files = new();

    /// <summary>Records a file that could not be loaded and had to be answered with defaults.</summary>
    /// <param name="path">The file that failed to load.</param>
    /// <param name="backupPath">Where its contents were copied, or null if no copy could be taken.</param>
    public static void Report(string path, string? backupPath) => Files.Enqueue(new CorruptConfigFile(path, backupPath));
}
