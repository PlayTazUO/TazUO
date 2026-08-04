#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// A user-authored overlay effect: its layer stack and fade timing.
/// <para>
/// Profiles are hand-editable JSON and are not trusted. Every layer is re-clamped by
/// <see cref="OverlayParams.Clamp"/> when baked, so a file cannot raise the pulse frequency past
/// the photosensitivity ceiling or exceed
/// <see cref="ScreenOverlayPreset.MaxLayers"/>.
/// </para>
/// </summary>
public class OverlayEffectProfile : ObservableSettings, IProfile
{
    /// <summary>Bumped when stored fields change meaning, not when one is added.</summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Set on the throwaway profile the options UI bakes from a code preset so it can be shown and
    /// copied but not edited. Never persisted - built-ins live in code.
    /// </summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public bool Deletable => !IsBuiltIn;

    /// <summary>Referenced by <see cref="OverlayEffectGeneralSettings.EffectiveProfile"/>.</summary>
    public string Name { get; set => SetField(ref field, value); } = string.Empty;

    public int Version { get; set => SetField(ref field, value); } = CurrentVersion;

    /// <summary>Informational: which built-in preset this was seeded from.</summary>
    public string? BasePreset { get; set => SetField(ref field, value); }

    // Deliberately not observable. The options UI rebuilds an editor whenever its profile raises
    // PropertyChanged, which would tear the fade inputs out from under the user mid-edit.
    public float FadeInSeconds { get; set; } = 0.6f;

    public float FadeOutSeconds { get; set; } = 2f;

    /// <summary>Back-to-front: index 0 is drawn first. One draw call each.</summary>
    public List<OverlayLayer> Layers { get; set => SetField(ref field, value); } = [];

    /// <summary>
    /// Deep copy. <see cref="OverlayLayer"/> is a value type throughout, so copying the list is
    /// enough to leave the original untouched by later edits.
    /// </summary>
    public OverlayEffectProfile Clone() =>
        new()
        {
            Name = Name,
            Version = Version,
            BasePreset = BasePreset,
            FadeInSeconds = FadeInSeconds,
            FadeOutSeconds = FadeOutSeconds,
            Layers = [.. Layers]
        };
}
