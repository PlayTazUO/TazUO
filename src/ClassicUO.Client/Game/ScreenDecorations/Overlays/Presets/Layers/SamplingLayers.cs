using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;

/// <summary>
/// Ready-made distortion layers, for composing into any preset rather than for use on their own.
/// <para>
/// Two rules govern where one may sit in a layer stack, both following from a sampling layer reading
/// the frame as it stood <em>before</em> the overlay pass:
/// </para>
/// <list type="bullet">
/// <item>It must sit below anything it is meant to affect. Above a tint layer it does not leave that
/// tint sharp, it replaces it - the sampled frame never contained it.</item>
/// <item>Two sampling layers do not compose. Both read the same pre-pass frame, so the upper one
/// overwrites the lower rather than distorting its result. One per preset.</item>
/// </list>
/// </summary>
public static class SamplingLayers
{
    /// <summary>
    /// Enough taps that the default radius resolves as a blur rather than as separate ghosts, and no
    /// more. Raise it with the radius, not on its own.
    /// </summary>
    private const OverlaySampleTaps DEFAULT_BLUR_TAPS = OverlaySampleTaps.Twelve;

    /// <summary>
    /// Fewer than the disk needs: radial taps land on a line rather than spread over an area, so the
    /// gaps between them are far less visible.
    /// </summary>
    private const OverlaySampleTaps DEFAULT_RADIAL_TAPS = OverlaySampleTaps.Eight;

    /// <summary>Frequency of the field that breaks up the strength when a shape asks to swim. Coarse
    /// on purpose - at detail frequency the distortion boils instead of drifting.</summary>
    private static readonly Vector2 _swimScale = new(1.6f, 1.6f);

    private static readonly Vector2 _swimScroll = new(0.006f, -0.004f);

    /// <summary>
    /// Out-of-focus vision: a disk blur, evenly in all directions.
    /// </summary>
    /// <param name="shape">Where it applies and how hard.</param>
    /// <param name="radius">Blur radius as a fraction of screen width.</param>
    /// <param name="taps">Samples taken per pixel; the whole cost of the layer.</param>
    /// <returns>The layer.</returns>
    public static OverlayLayer Blur(SamplingShape shape, float radius, OverlaySampleTaps taps = DEFAULT_BLUR_TAPS) =>
        Build(
            shape,
            new OverlaySampling
            {
                Mode = OverlaySampleMode.Blur,
                Radius = radius,
                Taps = taps
            }
        );

    /// <summary>
    /// Zoom blur streaking away from the centre. Reads as head-spin or speed rather than as poor
    /// focus, because the centre of the screen stays sharp however strong it is.
    /// </summary>
    /// <param name="shape">Where it applies and how hard.</param>
    /// <param name="zoom">How far along the centre ray the taps march, as a fraction of the distance
    /// from the centre.</param>
    /// <param name="taps">Samples taken per pixel; the whole cost of the layer.</param>
    /// <returns>The layer.</returns>
    public static OverlayLayer Radial(SamplingShape shape, float zoom, OverlaySampleTaps taps = DEFAULT_RADIAL_TAPS) =>
        Build(
            shape,
            new OverlaySampling
            {
                Mode = OverlaySampleMode.Radial,
                Zoom = zoom,
                Taps = taps
            }
        );

    /// <summary>
    /// Lens fringing: red and blue pulled apart along the centre ray. The cheapest of the three by a
    /// wide margin - three taps regardless of strength.
    /// </summary>
    /// <param name="shape">Where it applies and how hard.</param>
    /// <param name="aberration">Channel separation, as a fraction of the distance from the centre.</param>
    /// <returns>The layer.</returns>
    public static OverlayLayer Chromatic(SamplingShape shape, float aberration) =>
        Build(
            shape,
            new OverlaySampling
            {
                Mode = OverlaySampleMode.Chromatic,
                Aberration = aberration
            }
        );

    /// <summary>
    /// The parts every distortion layer shares. Tint is irrelevant - the shader returns sampled
    /// colour instead of it - and the noise block exists only to modulate strength, so it is flat
    /// unless the shape asked to swim.
    /// </summary>
    private static OverlayLayer Build(SamplingShape shape, OverlaySampling sampling) =>
        new()
        {
            Blend = OverlayBlend.Alpha,
            Params = new OverlayParams
            {
                Shape = shape.ToShape(),
                Noise = new OverlayNoise
                {
                    BaseScale = _swimScale,
                    BaseScroll = _swimScroll,
                    BaseChannel = NoiseChannel.Red,
                    DetailScale = _swimScale * 2f,
                    DetailScroll = _swimScroll * 1.6f,
                    DetailChannel = NoiseChannel.Green,
                    WarpStrength = 0.20f,
                    RidgeAmount = 0.00f,
                    Threshold = 0.45f,
                    Softness = 0.35f,
                    // The floor is the steady part of the strength, so swim is what is taken off it.
                    FlatFloor = 1f - shape.Swim
                },
                Appearance = new OverlayAppearance
                {
                    // Tint is unused - the sampling techniques return scene colour in its place - but
                    // white keeps it harmless if a profile is later switched to a painting mode.
                    Tint = Color.White,
                    Opacity = shape.Strength,
                    Intensity = 1f,
                    PulseFreq = shape.PulseFreq,
                    PulseAmp = shape.PulseAmp
                },
                Sampling = sampling
            }
        };
}
