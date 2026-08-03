using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     The room going round: a zoom blur streaking outward from the centre, breathing, under a warm
///     vignette.
///     <para>
///     Radial rather than a plain blur because the centre of a zoom blur stays sharp however strong
///     it is. That is what keeps the effect playable - whatever the player is looking at remains
///     legible while the periphery smears - and it is also what makes it read as vertigo instead of
///     as needing glasses.
///     </para>
/// </summary>
public sealed class DrunkOverlay : ScreenOverlayPreset
{
    /// <summary>Reaches nearly to the centre. The zoom's own falloff, not the mask, is what keeps
    /// the middle of the screen readable.</summary>
    private const float BLUR_REACH = 0.92f;

    private const float BLUR_FEATHER = 0.70f;

    /// <summary>High, because a swimming strength is most of what sells this as intoxication rather
    /// than as motion.</summary>
    private const float BLUR_SWIM = 0.65f;

    private const float VIGNETTE_FEATHER = 0.50f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Vignette colour. Warm and dim, not black - a black vignette reads as passing out.</summary>
    public Color Hue { get; set; } = new(64, 44, 30);

    public float Opacity { get; set; } = 0.35f;

    /// <summary>How far the streaks march outward, as a fraction of the distance from the centre.</summary>
    public float Zoom { get; set; } = 0.10f;

    /// <summary>Strength of the smear at the screen edge.</summary>
    public float Blur { get; set; } = 0.80f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        // Bottom layer: it samples the pre-pass frame, so the vignette below it would be replaced
        // rather than smeared.
        layers.Add(
            SamplingLayers.Radial(
                SamplingShape.Vignette(BLUR_REACH, BLUR_FEATHER, Blur, BLUR_SWIM),
                Zoom
            )
        );

        layers.Add(BakeVignette());
    }

    private OverlayLayer BakeVignette() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = 0.60f,
                    Feather = VIGNETTE_FEATHER,
                    EdgeBlend = 0.00f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(2.0f, 2.0f),
                    DetailScale = new Vector2(4.0f, 4.0f),
                    BaseScroll = new Vector2(0.004f, 0.005f),
                    DetailScroll = new Vector2(-0.005f, 0.007f),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.30f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.45f,
                    Softness = 0.35f,
                    FlatFloor = 0.70f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    // The one preset that should breathe visibly, but still well under the hard 3 Hz
                    // ceiling - a swaying room is slow.
                    PulseFreq = 0.14f,
                    PulseAmp = 0.22f
                }
            }
        };
}
