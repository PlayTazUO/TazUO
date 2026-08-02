using ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;
using ClassicUO.Game.ScreenOverlays.Presets;

namespace ClassicUO.Game.ScreenOverlays;

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
            _ => null
        };
}
