using System;

namespace ClassicUO.Game.ScreenDecorations.Overlays.Presets.Layers;

/// <summary>
/// Depth of one layer of a stacked preset, relative to the preset's own reach knob.
/// <para>
/// A preset built from several layers wants their boundaries staggered: three masks ending at the
/// same depth read as one hard ring, however different the colours above them are. That stagger has
/// to survive the whole range of the knob, not just its default, which rules out deriving the
/// layers by multiplication - a multiple gives a separation proportional to the knob, so it vanishes
/// exactly when the knob is small and the layers are most crowded.
/// </para>
/// <para>
/// So: fixed margins off a base reach, chained layer to layer. Chaining rather than measuring every
/// layer from the knob is what keeps them ordered - both operations are monotone, so a stack can
/// compress against the end of the range but can never invert.
/// </para>
/// <para>
/// The two ends of the range are guarded differently, deliberately. Running out of room going deeper
/// costs nothing worse than layers converging on a full-screen mask, all still drawn; running out
/// going shallower takes a layer to zero reach, where it stops being drawn at all. Only the second
/// is a correctness problem, so only <see cref="Shallower" /> trims its margin to fit.
/// </para>
/// </summary>
public static class LayerReach
{
    /// <summary>
    /// Ceiling for a staggered layer, which is lower than the one
    /// <see cref="ClassicUO.Renderer.Effects.OverlayShape.Reach" /> itself allows. A stack like this
    /// is a vignette: its deepest layer is meant to arrive at the centre, not to cover it, so the
    /// range past 1 that <see cref="ClassicUO.Renderer.Effects.OverlayParams.MaxReach" /> opens up
    /// for even full-screen cover would only flatten the stagger these margins exist to create.
    /// </summary>
    public const float Max = 1f;

    /// <summary>
    /// Most of the remaining depth a single margin may consume on the way down. Only ever binds when
    /// the base is already shallow enough that paying the margin in full would reach zero.
    /// </summary>
    private const float MAX_CONSUMED_FRACTION = 0.55f;

    /// <summary>
    /// A layer sitting deeper than <paramref name="reach" /> - reaching further in from the screen
    /// edge, so it is already underway before the shallower layer arrives.
    /// </summary>
    /// <param name="reach">Depth of the layer this one sits behind.</param>
    /// <param name="margin">How much deeper to place it.</param>
    /// <returns>The deeper layer's reach, never past <see cref="Max" />.</returns>
    public static float Deeper(float reach, float margin) => Math.Min(reach + margin, Max);

    /// <summary>
    /// A layer stopping short of <paramref name="reach" />, so the composite thins toward the middle
    /// of the screen instead of every pass ending together.
    /// </summary>
    /// <param name="reach">Depth of the layer this one sits inside.</param>
    /// <param name="margin">How much shorter to stop.</param>
    /// <returns>The shallower layer's reach, above zero for any non-zero <paramref name="reach" />.</returns>
    public static float Shallower(float reach, float margin) =>
        reach - Math.Min(margin, Math.Max(reach, 0f) * MAX_CONSUMED_FRACTION);
}
