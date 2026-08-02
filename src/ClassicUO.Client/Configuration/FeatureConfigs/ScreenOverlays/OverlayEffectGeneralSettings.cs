#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;

/// <summary>
/// Settings shared by every overlay effect, plus the effect's own pool of profiles.
/// </summary>
public class OverlayEffectGeneralSettings : ObservableSettings
{
    public bool Enabled { get; set => SetField(ref field, value); }

    public bool FullScreen { get; set => SetField(ref field, value); }

    /// <summary>
    /// <see cref="OverlayEffectProfile.Name"/> of the profile this effect draws with. Null or
    /// unresolvable means the built-in preset is used instead.
    /// </summary>
    public string? EffectiveProfile { get; set => SetField(ref field, value); }

    /// <summary>User-authored profiles for this effect. Built-in presets are not stored here.</summary>
    public List<OverlayEffectProfile> Profiles { get; set => SetField(ref field, value); } = [];

    /// <summary>Null when no profile is selected, or the selected one no longer exists.</summary>
    public OverlayEffectProfile? ResolveProfile() => FindProfile(EffectiveProfile);

    public OverlayEffectProfile? FindProfile(string? name) =>
        string.IsNullOrEmpty(name)
            ? null
            : Profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Adds <paramref name="profile"/>, renaming it to stay unique. Returns the name it was stored
    /// under.
    /// </summary>
    public string AddProfile(OverlayEffectProfile profile)
    {
        string baseName = string.IsNullOrWhiteSpace(profile.Name) ? "Overlay" : profile.Name;
        string name = baseName;

        for (int i = 2; FindProfile(name) != null; i++)
            name = $"{baseName} ({i})";

        profile.Name = name;
        Profiles.Add(profile);
        OnPropertyChanged(nameof(Profiles));

        return name;
    }
}
