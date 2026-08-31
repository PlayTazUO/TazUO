#nullable enable

using System.IO;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.IO.Persistency.Migrations;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// Everything the screen decoration systems - full-screen overlays and screen shake - are configured
/// by, for one client profile. Stored beside it as <see cref="FileName"/> rather than inside
/// profile.json: the layer stacks are large, and reading them needs IncludeFields, which would
/// change how every public field in the profile graph serializes.
/// </summary>
public class ScreenDecorations : ObservableSettings
{
    public const string FileName = "screen_decorations.json";

    /// <summary>Which shape this file is in. Defaults to latest, so a config built in memory needs no
    /// migration; a file's real version is read off its raw JSON before this ever binds.</summary>
    public int SchemaVersion { get; set; } = ScreenDecorationsMigrations.LatestVersion;

    /// <summary>
    /// Master switch over both systems. Off means no overlay is scheduled, drawn or shaken for - not
    /// merely hidden. Off by default: these effects obscure the world, so they are opt-in.
    /// </summary>
    public bool Enabled { get; set => SetField(ref field, value); }

    public OverlaySystemSettings Overlays { get; set => SetField(ref field, value); } = new();

    public ShakeSystemSettings Shake { get; set => SetField(ref field, value); } = new();

    /// <summary>Whether overlays should be running: both this system and the master switch.</summary>
    public bool OverlaysActive => Enabled && Overlays.Enabled;

    /// <summary>Whether shake should be applied: both this system and the master switch.</summary>
    public bool ShakeActive => Enabled && Shake.Enabled;

    #region Persistence

    private static ScreenDecorations? _current;

    /// <summary>Decoration settings for the currently loaded profile.</summary>
    public static ScreenDecorations Current => _current ??= LoadForProfile(ProfileManager.ProfilePath);

    /// <summary>
    /// Replaces <see cref="Current"/> with the settings stored beside <paramref name="profilePath"/>,
    /// or with defaults where there are none.
    /// </summary>
    /// <param name="profilePath">Directory of the profile being loaded; may be null.</param>
    /// <returns>The loaded settings.</returns>
    public static ScreenDecorations LoadForProfile(string? profilePath)
    {
        string? file = GetFilePath(profilePath);

        if (file == null)
        {
            _current = new ScreenDecorations();
            return _current;
        }

        try
        {
            _current = VersionedJsonConfig.Load(
                file,
                ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations,
                ScreenDecorationsMigrations.Pipeline
            ) ?? new ScreenDecorations();
        }
        catch (ConfigMigrationException e)
        {
            // Covers an unreadable file, one a newer client wrote, and one whose migrated shape will
            // not bind. All three end the same way: this build cannot run these settings, so it starts
            // clean. Backed up first, because the next Save() overwrites what is on disk.
            Log.Error($"Failed to load configuration file '{file}' - {e}");
            ConfigurationResolver.BackupAndReportCorruptFile(file);
            _current = new ScreenDecorations();
        }

        return _current;
    }

    /// <summary>
    /// Writes these settings beside the current profile. A no-op while no profile is loaded.
    /// </summary>
    public void Save()
    {
        string? file = GetFilePath(ProfileManager.ProfilePath);

        if (file == null)
            return;

        ConfigurationResolver.Save(this, file, ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations);
    }

    private static string? GetFilePath(string? profilePath) =>
        string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, FileName);

    #endregion
}
