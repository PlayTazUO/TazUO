#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;

/// <summary>
/// Screen overlay settings for one client profile. Stored beside it as
/// <see cref="FileName"/> rather than inside profile.json: the layer stacks are large, and reading
/// them needs IncludeFields, which would change how every public field in the profile graph
/// serializes.
/// </summary>
public class ScreenOverlays : ObservableSettings
{
    public const string FileName = "screen_overlays.json";

    public OverlayEffectGeneralSettings Bleed { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Poison { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings MortalStrike { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Fog { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Drunk { get; set => SetField(ref field, value); } = new();

    public static IReadOnlyList<OverlayEffect> AllEffects { get; } = Enum.GetValues<OverlayEffect>();

    public OverlayEffectGeneralSettings GetSettings(OverlayEffect effect) =>
        effect switch
        {
            OverlayEffect.Bleed => Bleed,
            OverlayEffect.Poison => Poison,
            OverlayEffect.MortalStrike => MortalStrike,
            OverlayEffect.Fog => Fog,
            OverlayEffect.Drunk => Drunk,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
        };

    #region Persistence

    private static ScreenOverlays? _current;

    /// <summary>Overlay settings for the currently loaded profile.</summary>
    public static ScreenOverlays Current => _current ??= LoadForProfile(ProfileManager.ProfilePath);

    private static string? GetFilePath(string? profilePath) =>
        string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, FileName);

    public static ScreenOverlays LoadForProfile(string? profilePath)
    {
        string? file = GetFilePath(profilePath);

        _current = file != null && File.Exists(file)
            ? ConfigurationResolver.Load(file, ScreenOverlaysJsonContext.DefaultToUse.ScreenOverlays) ?? new ScreenOverlays()
            : new ScreenOverlays();

        return _current;
    }

    public void Save()
    {
        string? file = GetFilePath(ProfileManager.ProfilePath);

        if (file == null)
            return;

        ConfigurationResolver.Save(this, file, ScreenOverlaysJsonContext.DefaultToUse.ScreenOverlays);
    }

    #endregion
}
