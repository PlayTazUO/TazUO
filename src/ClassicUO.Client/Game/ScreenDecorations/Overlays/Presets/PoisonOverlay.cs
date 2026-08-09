using System;
using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets;

/// <summary>
///     Billowy gas creeping in from the screen edge and rising, over a dark occlusive wash that
///     gives it something to sit on, over a drifting soft-focus blur.
///     <para>
///         The blur is bottom of the stack because a sampling layer reads the frame from before the
///         overlay pass: above the wash it would sample a screen that has no wash on it yet and
///         replace it. Being at the bottom also means it softens the world rather than the gas, which
///         is the point - poisoned vision, not a smeared filter.
///     </para>
///     <para>
///         The wash is what makes this read as being poisoned rather than as a green filter: the gas
///         layer alone is thin everywhere it is not dense, so it tints without ever obscuring. The
///         wash is mostly <see cref="OverlayNoise.FlatFloor" /> - deliberately, unlike the fluid
///         presets, since a soft radial vignette is exactly what is wanted here.
///     </para>
///     <para>
///         Both layers rise. Gas going the other way reads as falling ash, and matching the bleed
///         preset's downward flow would make the two effects look like the same substance recoloured.
///     </para>
/// </summary>
public sealed class PoisonOverlay : ScreenOverlayPreset
{
    /// <summary>
    ///     Screen heights per second the gas travels upward. Scroll is derived from this
    ///     and the layer's noise scale, so retuning a scale does not change the speed.
    /// </summary>
    private const float RISE_SCREEN_SPEED = 0.0095f;

    /// <summary>
    ///     Lateral drift of the primary field. The detail field takes the negative of this;
    ///     the two sliding against each other is what makes the gas churn rather than merely travel.
    /// </summary>
    private const float DRIFT_SCREEN_SPEED = 0.004f;

    /// <summary>Detail rises slightly faster than the field warping it, so the two never lock.</summary>
    private const float DETAIL_RISE_SCALE = 1.15f;

    /// <summary>
    ///     The gas outruns the wash it sits on. Parallax: the near, lighter layer moving faster than
    ///     the heavy one behind it is most of what gives the pair any depth. Kept small - past about
    ///     1.5 the two stop being one volume of gas and read as two separate sheets.
    /// </summary>
    private const float GAS_RISE_SCALE = 1.22f;

    /// <summary>
    ///     How much deeper than the gas the wash reaches, so the gas sits inside a darkened field
    ///     instead of the two boundaries ending together and reading as one hard ring.
    /// </summary>
    private const float WASH_REACH_MARGIN = 0.06f;

    private const float WASH_OPACITY_SCALE = 0.80f;

    /// <summary>
    ///     Soft focus, not frosted glass. Small enough that text under it stays readable - the blur
    ///     is meant to be noticed as impairment, not to hide what the player is trying to click.
    /// </summary>
    private const float BLUR_RADIUS = 0.012f;

    /// <summary>
    ///     How much deeper than the wash the blur reaches. Much the largest of the two margins: the
    ///     distortion has to be well underway before any colour arrives, or the boundaries coincide
    ///     and the blur reads as an edge rather than as bad vision.
    /// </summary>
    private const float BLUR_REACH_MARGIN = 0.46f;

    /// <summary>Longest gradient of the three layers, since it is the one that must not be seen
    ///     to begin.</summary>
    private const float BLUR_FEATHER = 0.62f;

    /// <summary>Held near half so the sharp frame keeps showing through and the screen never
    ///     stops being legible at full poison.</summary>
    private const float BLUR_STRENGTH = 0.72f;

    /// <summary>Part of the strength is noise-driven, so the soft patches drift instead of sitting
    ///     in fixed positions - a static blur reads as a dirty screen. Kept off the pulse's share so
    ///     there is still a steady base for it to swell against.</summary>
    private const float BLUR_SWIM = 0.45f;

    /// <summary>
    ///     Deliberately not a multiple of the gas rate: the two drift in and out of phase over about
    ///     half a minute, so no single beat is ever there to be counted.
    /// </summary>
    private const float BLUR_PULSE_FREQ = 0.19f;

    /// <summary>Deep enough to feel like waves of it arriving rather than a constant haze.</summary>
    private const float BLUR_PULSE_AMP = 0.4f;

    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    ///     Colour of the gas. The wash is darkened from this rather than exposed separately,
    ///     so changing it keeps the two layers the same substance.
    /// </summary>
    public Color Hue { get; set; } = new(96, 202, 74);

    public float Opacity { get; set; } = 0.65f;

    /// <summary>How far in from the screen edge the gas reaches. Larger is thicker.</summary>
    public float Reach { get; set; } = 0.50f;

    /// <summary>Deeper than the gas, so the darkening is underway before the gas edge arrives.</summary>
    private float WashReach => LayerReach.Deeper(Reach, WASH_REACH_MARGIN);

    /// <summary>Deeper again, off the wash rather than off <see cref="Reach" />, so the three stay
    ///     ordered however the knob is set.</summary>
    private float BlurReach => LayerReach.Deeper(WashReach, BLUR_REACH_MARGIN);

    protected override void Bake(List<OverlayLayer> layers)
    {
        SamplingShape blurShape = SamplingShape.Vignette(
            BlurReach,
            BLUR_FEATHER,
            BLUR_STRENGTH,
            BLUR_SWIM
        ) with
        {
            PulseFreq = BLUR_PULSE_FREQ,
            PulseAmp = BLUR_PULSE_AMP
        };

        layers.Add(SamplingLayers.Blur(blurShape, BLUR_RADIUS));
        layers.Add(BakeWash());
        layers.Add(BakeGas());
    }

    /// <summary>
    ///     Dark, near-solid backdrop. Low frequency and heavily floored so it is a field rather
    ///     than a pattern - it is doing the occluding, and anything legible in it would just fight
    ///     the gas above.
    /// </summary>
    private OverlayLayer BakeWash() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = WashReach,
                    // Long gradient: the wash has to arrive without a visible boundary, since
                    // its whole job is to darken rather than to be seen.
                    Feather = 0.78f,
                    EdgeBlend = 0.25f,
                    Jitter = Jitter(0.22f, 0.35f, new Vector2(1.6f, 1.2f), NoiseChannel.Green),
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(1.4f, 1.4f),
                    DetailScale = new Vector2(2.8f, 2.8f),
                    BaseScroll = Scroll(new Vector2(1.4f, 1.4f), DRIFT_SCREEN_SPEED, 1f),
                    DetailScroll = Scroll(new Vector2(2.8f, 2.8f), -DRIFT_SCREEN_SPEED, DETAIL_RISE_SCALE),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.18f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.40f,
                    Softness = 0.35f,
                    // Most of the layer. The noise only mottles it.
                    FlatFloor = 0.70f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Scale(Hue, 0.30f, 0.42f, 0.26f),
                    Opacity = Opacity * WASH_OPACITY_SCALE,
                    Intensity = Intensity,
                    // Steady: a breathing occluder makes the whole screen flicker in brightness.
                    PulseFreq = 0.00f,
                    PulseAmp = 0.00f
                }
            }
        };

    /// <summary>
    ///     The visible gas: the caller's full colour, thresholded so it resolves into distinct
    ///     billows, and the only layer that pulses.
    /// </summary>
    private OverlayLayer BakeGas() =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = new OverlayShape
                {
                    Center = new Vector2(0.5f, 0.5f),
                    Reach = Reach,
                    Feather = 0.62f,
                    EdgeBlend = 0.35f,
                    FocusDir = new Vector2(0f, -1f),
                    FocusPower = 1f,
                    FocusAmount = 0f
                },
                Noise = new OverlayNoise
                {
                    BaseScale = new Vector2(2.5f, 2.5f),
                    DetailScale = new Vector2(5.0f, 5.0f),
                    BaseScroll = Scroll(new Vector2(2.5f, 2.5f), DRIFT_SCREEN_SPEED, GAS_RISE_SCALE),
                    DetailScroll = Scroll(
                        new Vector2(5.0f, 5.0f),
                        -DRIFT_SCREEN_SPEED,
                        GAS_RISE_SCALE * DETAIL_RISE_SCALE
                    ),
                    BaseChannel = NoiseChannel.Red,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.35f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.45f,
                    Softness = 0.30f,
                    FlatFloor = 0.15f
                },
                Appearance = new OverlayAppearance
                {
                    Tint = Hue,
                    Opacity = Opacity,
                    Intensity = Intensity,
                    // ~9s period at a tenth amplitude: a slow swell that is felt rather than
                    // watched. Anything faster or stronger reads as the overlay flashing.
                    PulseFreq = 0.11f,
                    PulseAmp = 0.10f
                }
            }
        };

    /// <summary>
    ///     Scroll for a field of the given noise scale. Positive V moves the sample point down the
    ///     texture, which carries the pattern up the screen.
    /// </summary>
    private static Vector2 Scroll(Vector2 scale, float driftSpeed, float riseSpeedScale) =>
        new(driftSpeed * scale.X, RISE_SCREEN_SPEED * riseSpeedScale * scale.Y);

    /// <summary>
    ///     Boundary flux, locked to the rise so the ragged edge travels with the gas rather than
    ///     crawling across it. Coarser than the layer's own noise - at detail frequency a jittered
    ///     boundary just buzzes.
    /// </summary>
    private static OverlayJitter Jitter(float reachAmount, float featherAmount, Vector2 scale, NoiseChannel channel) =>
        new()
        {
            ReachAmount = reachAmount,
            FeatherAmount = featherAmount,
            Scale = scale,
            Scroll = Scroll(scale, 0f, 1f),
            Channel = channel
        };

    private static Color Scale(Color color, float red, float green, float blue) =>
        new((int)(color.R * red), (int)(color.G * green), (int)(color.B * blue));
}
