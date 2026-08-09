using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     A struck head: the image splitting into its colour channels toward the edges, under a dark
///     unsteady vignette.
///     <para>
///     Cheapest of the sampling presets by a wide margin - the split is three taps whatever its
///     strength, where a blur pays per tap. Worth reaching for first when something needs to look
///     wrong without costing anything.
///     </para>
/// </summary>
public sealed class ConcussionOverlay : ScreenOverlayPreset
{
    /// <summary>Covers most of the screen: the separation scales with distance from the centre on
    /// its own, so the mask only has to keep it off the middle.</summary>
    private const float SPLIT_REACH = 0.95f;

    private const float SPLIT_FEATHER = 0.65f;

    /// <summary>Modest. Fringing that pulses hard stops reading as an injury and starts reading as a
    /// broken display.</summary>
    private const float SPLIT_SWIM = 0.30f;

    private const float VIGNETTE_FEATHER = 0.42f;

    /// <summary>How far short of the split the vignette stops, so the fringing is already visible
    /// before the frame starts darkening.</summary>
    private const float VIGNETTE_REACH_MARGIN = 0.43f;

    #region Ctor

    public ConcussionOverlay()
    {
        // Shorter than the base default - a struck head clears faster than it hits.
        FadeOutSeconds = 1.6f;
    }

    #endregion

    public float Intensity { get; set; } = 1.0f;

    public Color Hue { get; set; } = new(28, 20, 24);

    public float Opacity { get; set; } = 0.45f;

    /// <summary>Channel separation at the screen edge, as a fraction of the distance from the
    /// centre. Small: past a few thousandths the three channels stop reading as one image.</summary>
    public float Aberration { get; set; } = 0.017f;

    /// <summary>Strength of the split where the mask is full.</summary>
    public float Split { get; set; } = 0.90f;

    protected override void Bake(List<OverlayLayer> layers)
    {
        // Bottom layer: it samples the pre-pass frame, so the vignette below it would be replaced
        // rather than fringed.
        layers.Add(
            SamplingLayers.Chromatic(
                SamplingShape.Vignette(SPLIT_REACH, SPLIT_FEATHER, Split, SPLIT_SWIM)
                with
                {
                    // Slow enough to read as the split swimming in and out rather than flickering.
                    PulseFreq = 0.3f,
                    PulseAmp = 0.3f
                },
                Aberration
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
                    Reach = LayerReach.Shallower(SPLIT_REACH, VIGNETTE_REACH_MARGIN),
                    Feather = VIGNETTE_FEATHER,
                    EdgeBlend = 0.00f,
                    Jitter = new OverlayJitter
                    {
                        // The unsteadiness lives here rather than in a pulse: a boundary that moves
                        // reads as swaying vision, where a pulsing one reads as a flashing screen.
                        ReachAmount = 0.30f,
                        FeatherAmount = 0.40f,
                        Scale = new Vector2(1.4f, 1.1f),
                        Scroll = new Vector2(0.003f, 0.004f),
                        Channel = NoiseChannel.Green
                    },
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(2.2f, 2.2f),
                    DetailScale = new Vector2(4.4f, 4.4f),
                    BaseScroll = new Vector2(0.003f, 0.004f),
                    DetailScroll = new Vector2(-0.004f, 0.006f),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.25f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.45f,
                    Softness = 0.32f,
                    FlatFloor = 0.75f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    PulseFreq = 0.18f,
                    PulseAmp = 0.15f
                }
            }
        };
}
