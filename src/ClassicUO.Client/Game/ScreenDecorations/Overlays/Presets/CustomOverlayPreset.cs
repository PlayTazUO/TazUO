using System;
using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Plays back a user-authored <see cref="OverlayEffectProfile"/>. Built-in presets derive a layer
///     stack from a few tunables; this one is handed its stack, so all of <see cref="OverlayParams"/>
///     is editable.
///     <para>
///     Routed through <see cref="ScreenOverlayPreset"/> rather than pushed at the manager directly so
///     that untrusted profiles still get <see cref="ScreenOverlayPreset.BakeClamped"/>'s per-layer
///     clamp and layer cap.
///     </para>
///     <para>
///     Only safety is enforced, not quality. The invariants that make <see cref="BleedOverlay"/> read
///     as fluid are compositional advice, and a profile is free to break them.
///     </para>
/// </summary>
public sealed class CustomOverlayPreset : ScreenOverlayPreset
{
    private readonly OverlayEffectProfile _profile;

    public CustomOverlayPreset(OverlayEffectProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));

        FadeInSeconds = profile.FadeInSeconds;
        FadeOutSeconds = profile.FadeOutSeconds;
    }

    /// <summary>
    /// Caller's strength dial, multiplied into every layer so the profile's own per-layer balance is
    /// preserved as it is turned down.
    /// </summary>
    public float Intensity { get; set; } = 1f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        foreach (OverlayLayer layer in _profile.Layers)
        {
            // Value type: this is a copy, so scaling it cannot write back into the stored profile.
            OverlayLayer scaled = layer;
            scaled.Params.Appearance.Intensity *= Intensity;

            layers.Add(scaled);
        }
    }
}
