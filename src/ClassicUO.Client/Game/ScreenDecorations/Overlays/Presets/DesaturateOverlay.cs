#nullable enable

using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
/// The whole view drained of colour, evenly. No vignette, no noise and no tint - the one preset
/// that is purely a colour grade, and the reference for how a layer asks to cover the screen
/// uniformly rather than as a shape.
/// </summary>
public sealed class DesaturateOverlay : ScreenOverlayPreset
{
    /// <summary>
    /// Falloff length. Arbitrary, because at <see cref="OverlayParams.MaxReach" /> the gradient it
    /// describes sits entirely off screen - kept short only so that a later change to that ceiling
    /// cannot start to expose it.
    /// </summary>
    private const float OFF_SCREEN_FEATHER = 0.01f;

    /// <summary>How much of the grade is applied where the layer is at full strength. Below 1 this
    /// crossfades against the untouched frame, which is a second way to reach a partial drain.</summary>
    public float Strength { get; set; } = 1f;

    /// <summary>Where the colour is pulled to: 0 is fully grey, 1 would leave the frame alone.</summary>
    public float Saturation { get; set; }

    public DesaturateOverlay()
    {
        FadeInSeconds = 1.8f;
        FadeOutSeconds = 3.6f;
    }

    /// <inheritdoc />
    protected override void Bake(List<OverlayLayer> layers) =>
        layers.Add(
            SamplingLayers.Saturation(
                // Reach past the centre is what makes this even rather than a vignette; Vignette()
                // at reach 1 still falls to nothing at the exact middle.
                new SamplingShape(OverlayParams.MaxReach, OFF_SCREEN_FEATHER, 0f, Strength),
                Saturation
            )
        );
}
