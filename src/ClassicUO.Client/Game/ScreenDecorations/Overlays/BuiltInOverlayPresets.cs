using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>
/// Maps a configurable effect to the code preset that supplies its stock look.
/// </summary>
public static class BuiltInOverlayPresets
{
    /// <summary>
    /// Null for effects that have no preset yet - those start from
    /// <see cref="Renderer.Effects.OverlayParams.Default"/> instead. TunnelVision and Fracture
    /// exist as presets but have no effect slot to hang off.
    /// </summary>
    public static ScreenOverlayPreset Create(OverlayEffectSlot effectSlot) =>
        effectSlot switch
        {
            OverlayEffectSlot.Bleed => new BleedOverlay(),
            OverlayEffectSlot.Poison => new PoisonOverlay(),
            OverlayEffectSlot.Fog => new FogOverlay(),
            OverlayEffectSlot.Drunk => new DrunkOverlay(),
            OverlayEffectSlot.Concussion => new ConcussionOverlay(),
            _ => null
        };
}
