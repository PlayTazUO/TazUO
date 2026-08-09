using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;

/// <summary>
/// Where a sampling layer distorts and how hard, without the caller having to restate the whole
/// shape block. The mask is the distortion strength - it is what the sampled frame is composited at
/// - so these are the only shape controls a distortion normally needs.
/// </summary>
/// <param name="Reach">How far in from the screen edge the distortion extends. 1 covers everything.</param>
/// <param name="Feather">Length of the fade behind that boundary.</param>
/// <param name="EdgeBlend">0 = radial vignette, 1 = border trim.</param>
/// <param name="Strength">Peak strength where the mask is full: 1 fully replaces the sharp frame.</param>
/// <param name="Swim">How much noise breaks the strength up, 0 = perfectly even. Gives blur that
/// drifts and breathes rather than sitting still, at no extra cost - the noise field is sampled
/// either way.</param>
public readonly record struct SamplingShape(
    float Reach,
    float Feather,
    float EdgeBlend,
    float Strength,
    float Swim = 0f
)
{
    /// <summary>
    /// Rate the whole distortion breathes at, in Hz. Init-only rather than a constructor parameter
    /// because most distortions want none, and the two pulse controls are worth naming at the call
    /// site - a bare pair of floats on the end of six others says nothing.
    /// <para>
    /// Distinct from <see cref="Swim" />: swim varies strength across the screen at one moment, pulse
    /// varies it across time everywhere at once. Together they give waves that arrive and recede
    /// rather than a texture that merely churns.
    /// </para>
    /// </summary>
    public float PulseFreq { get; init; }

    /// <summary>Depth of that breathing, as a fraction of <see cref="Strength" />.</summary>
    public float PulseAmp { get; init; }

    /// <summary>Rate the shape's own centre drifts at, in Hz per axis. See
    /// <see cref="ClassicUO.Renderer.Effects.OverlayParams.OverlayShape.WobbleFreq" />.</summary>
    public Vector2 WobbleFreq { get; init; }

    /// <summary>Peak drift of the centre, in screen uv.</summary>
    public float WobbleAmp { get; init; }

    /// <summary>Distortion around the screen edge, fading toward a clear centre.</summary>
    /// <param name="reach">How far in it extends.</param>
    /// <param name="feather">Length of the fade.</param>
    /// <param name="strength">Peak strength at the edge.</param>
    /// <param name="swim">Noise modulation of that strength.</param>
    /// <returns>The shape.</returns>
    public static SamplingShape Vignette(float reach, float feather, float strength, float swim = 0f) =>
        new(reach, feather, 0f, strength, swim);

    /// <summary>The shape block this describes.</summary>
    /// <returns>Shape parameters ready for an <see cref="OverlayParams"/>.</returns>
    internal OverlayShape ToShape() =>
        new()
        {
            Center = new Vector2(0.5f, 0.5f),
            WobbleFreq = WobbleFreq,
            WobbleAmp = WobbleAmp,
            Reach = Reach,
            Feather = Feather,
            EdgeBlend = EdgeBlend,
            CornerBias = EdgeBlend > 0f ? 1f : 0f,
            FocusDir = new Vector2(0f, -1f),
            FocusPower = 1f,
            FocusAmount = 0f
        };
}
