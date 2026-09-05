#nullable enable

using System.ComponentModel;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;
using ClassicUO.IO.Persistency.Migrations;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// Everything the screen decoration systems - full-screen overlays and screen shake - are configured
/// by, for one client profile. Stored beside it as <see cref="ConfigFileName"/> rather than inside
/// profile.json: the layer stacks are large, and reading them needs IncludeFields, which would
/// change how every public field in the profile graph serializes.
/// </summary>
public class ScreenDecorations : JsonSave<ScreenDecorations>, INotifyPropertyChanged
{
    /// <summary>Name of the file these settings are stored in, inside the profile folder.</summary>
    public const string ConfigFileName = "screen_decorations.json";

    /// <summary>Which shape this file is in. Defaults to latest for a config built in memory; a file's
    /// real version is read off its raw JSON before this binds.</summary>
    public int SchemaVersion { get; set; } = ScreenDecorationsMigrations.LatestVersion;

    /// <summary>
    /// Master switch over both systems. Off means no overlay is scheduled, drawn or shaken for - not
    /// merely hidden. Off by default: these effects obscure the world, so they are opt-in.
    /// </summary>
    public bool Enabled { get; set => SetProperty(ref field, value); }

    /// <summary>The full-screen overlay system: its own switch, and the layers it draws.</summary>
    public OverlaySystemSettings Overlays { get; set => SetProperty(ref field, value); } = new();

    /// <summary>The screen shake system: its own switch, and how the shake behaves.</summary>
    public ShakeSystemSettings Shake { get; set => SetProperty(ref field, value); } = new();

    /// <summary>Whether overlays should be running: both this system and the master switch.</summary>
    public bool OverlaysActive => Enabled && Overlays.Enabled;

    /// <summary>Whether shake should be applied: both this system and the master switch.</summary>
    public bool ShakeActive => Enabled && Shake.Enabled;

    #region Persistence

    /// <summary>Lives in the profile folder alongside the other per-character configs.</summary>
    protected override SettingsScope Scope => SettingsScope.Char;

    /// <inheritdoc />
    protected override string FileName => ConfigFileName;

    /// <inheritdoc />
    protected override JsonTypeInfo<ScreenDecorations> TypeInfo => ScreenDecorationsJsonContext.DefaultToUse.ScreenDecorations;

    /// <inheritdoc />
    protected override ConfigMigrationPipeline<JsonObject> MigrationPipeline => ScreenDecorationsMigrations.Pipeline;

    private static ScreenDecorations? _current;

    /// <summary>Decoration settings for the currently loaded profile.</summary>
    public static ScreenDecorations Current => _current ??= LoadForProfile(ProfileManager.ProfilePath);

    /// <summary>
    /// Replaces <see cref="Current"/> with the settings stored beside <paramref name="profilePath"/>.
    /// </summary>
    /// <param name="profilePath">Directory of the profile being loaded. Null or empty yields defaults
    /// and touches no file: with no profile there is nowhere these belong, and loading through
    /// <see cref="SettingsScope.Char"/> would persist a copy under the account folder instead.</param>
    /// <returns>The loaded settings.</returns>
    public static ScreenDecorations LoadForProfile(string? profilePath) =>
        _current = string.IsNullOrEmpty(profilePath)
            ? new ScreenDecorations()
            : LoadFrom(Path.Combine(profilePath, ConfigFileName));

    #endregion
}
