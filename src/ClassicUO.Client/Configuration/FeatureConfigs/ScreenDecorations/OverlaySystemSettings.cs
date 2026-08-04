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
    public bool Enabled { get; set => SetField(ref field, value); }

    /// <summary>Scales every overlay's intensity. Clamped to [0, 1] where it is consumed.</summary>
    public float Intensity { get; set => SetField(ref field, value); } = 1f;

    public OverlayEffectGeneralSettings Bleed { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Poison { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings MortalStrike { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Fog { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Drunk { get; set => SetField(ref field, value); } = new();
    public OverlayEffectGeneralSettings Concussion { get; set => SetField(ref field, value); } = new();

    public static IReadOnlyList<OverlayEffectSlot> AllEffects { get; } = Enum.GetValues<OverlayEffectSlot>();

    /// <summary>
    /// The settings block backing <paramref name="effectSlot"/>.
    /// </summary>
    /// <param name="effectSlot">The effect to look up.</param>
    /// <returns>Its settings; never null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The effect has no settings block, which means
    /// one was added to <see cref="OverlayEffectSlot"/> without a home here.</exception>
    public OverlayEffectGeneralSettings GetSettings(OverlayEffectSlot effectSlot) =>
        effectSlot switch
        {
            OverlayEffectSlot.Bleed => Bleed,
            OverlayEffectSlot.Poison => Poison,
            OverlayEffectSlot.MortalStrike => MortalStrike,
            OverlayEffectSlot.Fog => Fog,
            OverlayEffectSlot.Drunk => Drunk,
            OverlayEffectSlot.Concussion => Concussion,
            _ => throw new ArgumentOutOfRangeException(nameof(effectSlot), effectSlot, null)
        };
}
