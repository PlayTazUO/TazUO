#nullable enable

using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// Breaks up the shape boundary with its own noise field. Without it the mask is a function of
/// distance to the nearest screen edge alone, so the effect terminates along a straight iso-line and
/// reads as a rectangle. Anything organic wants <see cref="ReachAmount" /> well above 0.
/// <para>
/// Authoring mirror of <see cref="OverlayJitter" />: what the composer edits and config persists.
/// <see cref="ToParams" /> flattens it to the GPU wire format, which carries no editor metadata.
/// </para>
/// </summary>
public struct JitterSpec
{
    /// <summary>
    /// How far the boundary can move, as a fraction of the shape distance. Only extends inward, so
    /// raising it raises average reach too - compensate with <see cref="ShapeSpec.Reach" />.
    /// </summary>
    [LocalizedDisplayName("visualeffects_jitter_reachamount", "Boundary flux")]
    [LocalizedDescription(
        "visualeffects_jitter_reachamount_tooltip",
        "How far the boundary can move,\n"
        + "as a fraction of the shape distance. Only extends inward,\n"
        + "so raising it raises average reach too -\n"
        + "compensate with Shape.Reach. Anything organic wants\n"
        + "this well above 0."
    )]
    public float ReachAmount;

    /// <summary>
    /// How much the same field stretches and compresses <see cref="ShapeSpec.Feather" />. Without it
    /// a deep run and a shallow one end just as abruptly.
    /// </summary>
    [LocalizedDisplayName("visualeffects_jitter_featheramount", "Falloff flux")]
    [LocalizedDescription(
        "visualeffects_jitter_featheramount_tooltip",
        "How much the same field stretches and compresses Feather.\n"
        + "Without it a deep run and a shallow one end just as abruptly."
    )]
    public float FeatherAmount;

    /// <summary>
    /// Frequency of the displacement, and the parameter most often set wrong. Must be coarser than
    /// <see cref="NoiseSpec.BaseScale" /> or the boundary buzzes at detail frequency - but X still
    /// has to cycle several times across the screen, or each edge gets one bulge and stays visibly
    /// rectangular.
    /// </summary>
    [LocalizedDisplayName("visualeffects_jitter_scale", "Flux frequency")]
    [LocalizedDescription(
        "visualeffects_jitter_scale_tooltip",
        "Frequency of the boundary displacement.\n"
        + "Must be coarser than Noise.BaseScale or the edge just buzzes,\n"
        + "but X still has to cycle several times across the screen\n"
        + "or each edge gets one gentle bulge and stays rectangular."
    )]
    public Vector2 Scale;

    /// <summary>
    /// Texture-space velocity of the displacement field. Usually matched to the layer's own scroll so
    /// the ragged edge travels with the effect rather than crawling across it.
    /// </summary>
    [LocalizedDisplayName("visualeffects_jitter_scroll", "Flux drift")]
    [LocalizedDescription(
        "visualeffects_jitter_scroll_tooltip",
        "Texture-space velocity of the displacement field.\n"
        + "Usually matched to the layer's own scroll so the ragged edge\n"
        + "travels with the effect."
    )]
    public Vector2 Scroll;

    [LocalizedDisplayName("visualeffects_jitter_channel", "Flux source")]
    [LocalizedDescription(
        "visualeffects_jitter_channel_tooltip", "Which packed noise channel drives the boundary displacement."
    )]
    public NoiseChannel Channel;

    /// <inheritdoc cref="NoiseSpec.ToParams" />
    public readonly OverlayJitter ToParams() =>
        new()
        {
            ReachAmount = ReachAmount,
            FeatherAmount = FeatherAmount,
            Scale = Scale,
            Scroll = Scroll,
            Channel = Channel
        };

    /// <inheritdoc cref="NoiseSpec.From" />
    public static JitterSpec From(in OverlayJitter jitter) =>
        new()
        {
            ReachAmount = jitter.ReachAmount,
            FeatherAmount = jitter.FeatherAmount,
            Scale = jitter.Scale,
            Scroll = jitter.Scroll,
            Channel = jitter.Channel
        };
}

/// <summary>
/// Where on screen a layer lives: vignette/border shape, how far it extends, and how its boundary
/// breaks up.
/// <para>
/// Authoring mirror of <see cref="OverlayShape" />: what the composer edits and config persists.
/// <see cref="ToParams" /> flattens it to the GPU wire format, which carries no editor metadata.
/// </para>
/// </summary>
public struct ShapeSpec
{
    /// <summary>Centre of the radial falloff in screen uv. (0.5, 0.5) is the middle.</summary>
    [LocalizedDisplayName("visualeffects_shape_center", "Centre")]
    [LocalizedDescription(
        "visualeffects_shape_center_tooltip",
        "Centre of the radial falloff in screen uv. (0.5,\n"
        + "0.5) is the middle."
    )]
    public Vector2 Center;

    /// <summary>
    /// Rate <see cref="Center" /> drifts at, in Hz per axis. Different X and Y give a wander instead
    /// of a pendulum; zero on an axis holds it still, so a preset with no wobble needs no switch.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shape_wobblefreq", "Drift rate")]
    [LocalizedDescription(
        "visualeffects_shape_wobblefreq_tooltip",
        "Rate the centre drifts at, in Hz per axis.\n"
        + "Different X and Y values give a wander instead of a pendulum;\n"
        + "zero on an axis holds it still."
    )]
    public Vector2 WobbleFreq;

    /// <summary>Peak drift of <see cref="Center" />, in screen uv. Kept small - this unsteadies the
    /// pivot rather than sending it touring the screen.</summary>
    [LocalizedDisplayName("visualeffects_shape_wobbleamp", "Drift range")]
    [LocalizedDescription(
        "visualeffects_shape_wobbleamp_tooltip",
        "Peak drift of the centre, in screen uv. Kept small -\n"
        + "this unsteadies the pivot rather than sending it touring\n"
        + "the screen."
    )]
    public float WobbleAmp;

    /// <summary>
    /// How far in from the screen edge the effect extends. Larger is thicker. Interacts with
    /// <see cref="EdgeBlend" />: a radial shape reaches further at the corners and side edges on a
    /// widescreen display, so the same value covers noticeably less.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shape_reach", "Reach")]
    [LocalizedDescription(
        "visualeffects_shape_reach_tooltip",
        "How far in from the screen edge the effect extends.\n"
        + "Larger is thicker."
    )]
    public float Reach;

    /// <summary>Width of the fade behind the boundary. Wide thins the effect into haze; narrow gives
    /// it a defined surface.</summary>
    [LocalizedDisplayName("visualeffects_shape_feather", "Falloff")]
    [LocalizedDescription(
        "visualeffects_shape_feather_tooltip",
        "Width of the fade behind the boundary. Wide thins the effect\n"
        + "into haze; narrow gives it a defined surface."
    )]
    public float Feather;

    /// <summary>
    /// 0 = radial vignette, 1 = border trim. Avoid values in between: the radial term is normalised
    /// to screen width, so any blend lands mostly on the left and right edges. Use
    /// <see cref="CornerBias" /> for corner weighting - it is per-axis and has no aspect bias.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shape_edgeblend", "Vignette / border")]
    [LocalizedDescription(
        "visualeffects_shape_edgeblend_tooltip",
        "0 = radial vignette, 1 = border trim.\n"
        + "Avoid values in between: the radial term is width-normalised,\n"
        + "so any blend lands mostly on the left and right edges.\n"
        + "Use CornerBias for corner weighting."
    )]
    public float EdgeBlend;

    /// <summary>
    /// Corner weighting of the border trim, ignored when <see cref="EdgeBlend" /> is 0. At 0 the trim
    /// is a sharp-cornered rectangle; raising it thickens and rounds the corners without favouring
    /// any one edge.
    /// </summary>
    [LocalizedDisplayName("visualeffects_shape_cornerbias", "Corner weighting")]
    [LocalizedDescription(
        "visualeffects_shape_cornerbias_tooltip",
        "Corner weighting of the border trim, ignored when EdgeBlend is 0.\n"
        + "At 0 the trim is a sharp-cornered rectangle;\n"
        + "raising it thickens and rounds the corners."
    )]
    public float CornerBias;

    [LocalizedDisplayName("visualeffects_shape_jitter", "Boundary break-up")]
    [LocalizedDescription(
        "visualeffects_shape_jitter_tooltip",
        "Breaks up the shape boundary with its own noise field.\n"
        + "Without it the effect ends along a straight iso-line and reads\n"
        + "as a rectangle."
    )]
    public JitterSpec Jitter;

    /// <summary>Unit vector biasing the effect toward one side or corner.</summary>
    [LocalizedDisplayName("visualeffects_shape_focusdir", "Bias direction")]
    [LocalizedDescription(
        "visualeffects_shape_focusdir_tooltip", "Unit vector biasing the effect toward one side or corner."
    )]
    public Vector2 FocusDir;

    /// <summary>Higher values tighten the directional lobe.</summary>
    [LocalizedDisplayName("visualeffects_shape_focuspower", "Bias tightness")]
    [LocalizedDescription(
        "visualeffects_shape_focuspower_tooltip", "Higher values tighten the directional lobe."
    )]
    public float FocusPower;

    /// <summary>0 = uniform all the way round, 1 = fully biased toward <see cref="FocusDir" />.</summary>
    [LocalizedDisplayName("visualeffects_shape_focusamount", "Bias strength")]
    [LocalizedDescription(
        "visualeffects_shape_focusamount_tooltip",
        "0 = uniform all the way round, 1 = fully biased toward FocusDir."
    )]
    public float FocusAmount;

    /// <summary>The renderer's own defaults, as an authored spec.</summary>
    public static ShapeSpec Default => From(OverlayParams.Default.Shape);

    /// <inheritdoc cref="NoiseSpec.ToParams" />
    public readonly OverlayShape ToParams() =>
        new()
        {
            Center = Center,
            WobbleFreq = WobbleFreq,
            WobbleAmp = WobbleAmp,
            Reach = Reach,
            Feather = Feather,
            EdgeBlend = EdgeBlend,
            CornerBias = CornerBias,
            Jitter = Jitter.ToParams(),
            FocusDir = FocusDir,
            FocusPower = FocusPower,
            FocusAmount = FocusAmount
        };

    /// <inheritdoc cref="NoiseSpec.From" />
    public static ShapeSpec From(in OverlayShape shape) =>
        new()
        {
            Center = shape.Center,
            WobbleFreq = shape.WobbleFreq,
            WobbleAmp = shape.WobbleAmp,
            Reach = shape.Reach,
            Feather = shape.Feather,
            EdgeBlend = shape.EdgeBlend,
            CornerBias = shape.CornerBias,
            Jitter = JitterSpec.From(shape.Jitter),
            FocusDir = shape.FocusDir,
            FocusPower = shape.FocusPower,
            FocusAmount = shape.FocusAmount
        };
}
