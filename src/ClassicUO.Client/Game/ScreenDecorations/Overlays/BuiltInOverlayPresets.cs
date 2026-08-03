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
    public static ScreenOverlayPreset Create(OverlayEffect effect) =>
        effect switch
        {
            OverlayEffect.Bleed => new BleedOverlay(),
            OverlayEffect.Poison => new PoisonOverlay(),
            OverlayEffect.Fog => new FogOverlay(),
            OverlayEffect.Drunk => new DrunkOverlay(),
            OverlayEffect.Concussion => new ConcussionOverlay(),
            _ => null
        };
}
