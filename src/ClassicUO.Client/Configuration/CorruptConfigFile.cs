#nullable enable

namespace ClassicUO.Configuration;

/// <summary>What the client fell back to after a config file failed to load.</summary>
public enum CorruptConfigFallback
{
    /// <summary>Nothing on disk could be used, so the settings are back at their defaults.</summary>
    Defaults,

    /// <summary>An older copy of the same file loaded in its place, so the settings survived.</summary>
    Backup
}

/// <summary>
///     A config file that could not be loaded, and where its contents were copied before the
///     fallback took over - the notice has to name a path the user can go and look at.
/// </summary>
/// <param name="Path">The file that failed to load.</param>
/// <param name="BackupPath">Where it was copied to, or null if no copy could be taken.</param>
/// <param name="Fallback">What answered in its place, which is what the notice reports.</param>
public readonly record struct CorruptConfigFile(
    string Path,
    string? BackupPath,
    CorruptConfigFallback Fallback = CorruptConfigFallback.Defaults
)
{
    /// <summary>
    ///     The original file's name, without extension
    /// </summary>
    public string Name => System.IO.Path.GetFileName(Path);
}
