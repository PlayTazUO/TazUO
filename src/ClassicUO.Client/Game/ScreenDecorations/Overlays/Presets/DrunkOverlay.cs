using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     The room going round: a zoom blur streaking outward from the centre, breathing, under a warm
///     vignette. Both layers share a slow wander of their own pivot, so the point everything swims
///     around drifts rather than sitting locked to screen centre - meant to cost the player some
///     legibility, not just decorate the border.
///     <para>
///     Radial rather than a plain blur because the centre of a zoom blur stays sharp however strong
///     it is. That is what keeps the effect playable - whatever the player is looking at remains
///     legible while the periphery smears - and it is also what makes it read as vertigo instead of
///     as needing glasses.
///     </para>
/// </summary>
public sealed class DrunkOverlay : ScreenOverlayPreset
{
    /// <summary>Nearly full screen. Pushed out from the old default along with the vignette - this
    /// preset is meant to cost the player something, not just decorate the border.</summary>
    private const float BLUR_REACH = 0.98f;

    private const float BLUR_FEATHER = 0.70f;

    /// <summary>High, because a swimming strength is most of what sells this as intoxication rather
    /// than as motion.</summary>
    private const float BLUR_SWIM = 0.65f;

    /// <summary>Rate the smear itself swells at, on top of the vignette's own slower breathing.</summary>
    private const float BLUR_PULSE_FREQ = 0.22f;

    /// <summary>Depth of that swell, as a fraction of <see cref="Blur" />.</summary>
    private const float BLUR_PULSE_AMP = 0.28f;

    private const float VIGNETTE_FEATHER = 0.50f;

    /// <summary>How far short of the blur the vignette stops. Narrowed along with the reach increase
    /// - the smear now establishes so much earlier that the old wide margin left the vignette barely
    /// reaching past the corners.</summary>
    private const float VIGNETTE_REACH_MARGIN = 0.16f;

    /// <summary>Drift rate shared by both layers. Different X and Y so the pivot wanders rather than
    /// swinging like a pendulum; both layers must use the exact same rate and phase or the smear's
    /// convergence point and the vignette's dark patch visibly split apart.</summary>
    private static readonly Vector2 WOBBLE_FREQ = new(0.11f, 0.085f);

    /// <summary>Peak drift, in screen uv. Small - this unsteadies the pivot, it does not tour it.</summary>
    private const float WOBBLE_AMP = 0.2f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>Vignette colour. Warm and dim, not black - a black vignette reads as passing out.</summary>
    public Color Hue { get; set; } = new(64, 44, 30);

    public float Opacity { get; set; } = 0.35f;

    /// <summary>How far the streaks march outward, as a fraction of the distance from the centre.</summary>
    public float Zoom { get; set; } = 0.10f;

    /// <summary>Strength of the smear at the screen edge. Pulled back from the old default - it read
    /// as too deep, more like heavy fog than a light-headed blur.</summary>
    public float Blur { get; set; } = 0.62f;

    public DrunkOverlay()
    {
        FadeInSeconds = 4.5f;
        FadeOutSeconds = 7f;
    }
    protected override void Bake(List<OverlayLayer> layers)
    {
        SamplingShape blurShape = SamplingShape.Vignette(BLUR_REACH, BLUR_FEATHER, Blur, BLUR_SWIM) with
        {
            PulseFreq = BLUR_PULSE_FREQ,
            PulseAmp = BLUR_PULSE_AMP,
            WobbleFreq = WOBBLE_FREQ,
            WobbleAmp = WOBBLE_AMP
        };

        // Bottom layer: it samples the pre-pass frame, so the vignette below it would be replaced
        // rather than smeared.
        layers.Add(SamplingLayers.Radial(blurShape, Zoom));

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
                    // Shared with the blur layer's shape - same rate and phase, or the smear's
                    // convergence point and this vignette's dark patch drift apart from each other.
                    WobbleFreq = WOBBLE_FREQ,
                    WobbleAmp = WOBBLE_AMP,
                    Reach = LayerReach.Shallower(BLUR_REACH, VIGNETTE_REACH_MARGIN),
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
