namespace ClassicUO.Configuration
{
    /// <summary>A config file that could not be loaded, and where its contents were copied before
    /// defaults took over - the notice has to name a path the user can go and look at.</summary>
    /// <param name="Path">The file that failed to load.</param>
    /// <param name="BackupPath">Where it was copied to, or null if no copy could be taken.</param>
    public readonly record struct CorruptConfigFile(string Path, string BackupPath);
}
