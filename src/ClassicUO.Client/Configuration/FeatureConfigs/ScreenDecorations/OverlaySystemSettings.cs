#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// The overlay half of <see cref="ScreenDecorations"/>: whether full-screen tints run at all, how
/// strongly, and the per-effect settings underneath.
/// </summary>
public class OverlaySystemSettings : ObservableSettings
{
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    /// <summary>Scales every overlay's intensity. Clamped to [0, 1] where it is consumed.</summary>
    public float Intensity { get; set => SetField(ref field, value); } = 1f;

    public OverlayEffectGeneralSettings Bleed { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Poison { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings MortalStrike { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Fog { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Drunk { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Concussion { get; set => SetField(ref field, value); } = new();

    public static IReadOnlyList<OverlayEffect> AllEffects { get; } = Enum.GetValues<OverlayEffect>();

    /// <summary>
    /// The settings block backing <paramref name="effect"/>.
    /// </summary>
    /// <param name="effect">The effect to look up.</param>
    /// <returns>Its settings; never null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The effect has no settings block, which means
    /// one was added to <see cref="OverlayEffect"/> without a home here.</exception>
    public OverlayEffectGeneralSettings GetSettings(OverlayEffect effect) =>
        effect switch
        {
            OverlayEffect.Bleed => Bleed,
            OverlayEffect.Poison => Poison,
            OverlayEffect.MortalStrike => MortalStrike,
            OverlayEffect.Fog => Fog,
            OverlayEffect.Drunk => Drunk,
            OverlayEffect.Concussion => Concussion,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
        };
}
