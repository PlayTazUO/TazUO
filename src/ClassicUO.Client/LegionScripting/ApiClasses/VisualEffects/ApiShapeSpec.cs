#nullable enable

using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// Breaks up a layer's shape boundary with its own noise field. Without it the effect terminates
/// along a straight iso-line and reads as a rectangle, so anything organic wants `ReachAmount` well
/// above 0.
/// </summary>
public class ApiJitterSpec : PyApiObject
{
    #region Public accessors

    /// <summary>
    /// How far the boundary can move, as a fraction of the shape distance. Only extends inward, so
    /// raising it raises average reach too - compensate with `ApiShapeSpec.Reach`.
    /// </summary>
    public double ReachAmount { get; set; }

    /// <summary>
    /// How much the same field stretches and compresses `ApiShapeSpec.Feather`. Without it a deep
    /// run and a shallow one end just as abruptly.
    /// </summary>
    public double FeatherAmount { get; set; }

    /// <summary>
    /// Frequency of the displacement, and the knob most often set wrong. Must be coarser than
    /// `ApiNoiseSpec.BaseScale` or the boundary buzzes at detail frequency, but X still has to cycle
    /// several times across the screen or each edge gets one bulge and stays visibly rectangular.
    /// </summary>
    public ApiVector2? Scale { get; set; } = new();

    /// <summary>
    /// Texture-space velocity of the displacement field. Usually matched to the layer's own scroll
    /// so the ragged edge travels with the effect rather than crawling across it.
    /// </summary>
    public ApiVector2? Scroll { get; set; } = new();

    /// <summary>Which packed noise channel drives the boundary displacement.</summary>
    public ApiVisualEffects.NoiseChannel Channel { get; set; }

    #endregion

    #region Internal methods

    /// <summary>Projects an authored boundary displacement into a DTO.</summary>
    /// <param name="jitter">The displacement to copy.</param>
    /// <returns>The DTO, with its own vectors.</returns>
    internal static ApiJitterSpec From(in JitterSpec jitter) =>
        new()
        {
            ReachAmount = jitter.ReachAmount,
            FeatherAmount = jitter.FeatherAmount,
            Scale = ApiVector2.From(jitter.Scale),
            Scroll = ApiVector2.From(jitter.Scroll),
            Channel = (ApiVisualEffects.NoiseChannel)jitter.Channel
        };

    /// <summary>Flattens this to the authoring type the compositor bakes from.</summary>
    /// <returns>A fresh displacement; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">A value is not a real number, or <see cref="Channel"/> is
    /// set to something the API does not offer.</exception>
    internal JitterSpec ToInternal() =>
        new()
        {
            ReachAmount = VfxInputValidator.Number(ReachAmount, nameof(ReachAmount)),
            FeatherAmount = VfxInputValidator.Number(FeatherAmount, nameof(FeatherAmount)),
            Scale = VfxInputValidator.Vector(Scale, nameof(Scale)),
            Scroll = VfxInputValidator.Vector(Scroll, nameof(Scroll)),
            Channel = (Renderer.Effects.NoiseChannel)VfxInputValidator.Defined(Channel, nameof(Channel))
        };

    #endregion
}

/// <summary>
/// Where on screen a layer lives: vignette or border shape, how far it extends, and how its boundary
/// breaks up.
/// </summary>
public class ApiShapeSpec : PyApiObject
{
    #region Public accessors

    /// <summary>Centre of the radial falloff in screen uv. (0.5, 0.5) is the middle.</summary>
    public ApiVector2? Center { get; set; } = new();

    /// <summary>
    /// Rate `Center` drifts at, in Hz per axis. Different X and Y give a wander instead of a
    /// pendulum; zero on an axis holds it still.
    /// </summary>
    public ApiVector2? WobbleFreq { get; set; } = new();

    /// <summary>Peak drift of `Center`, in screen uv. Kept small - this unsteadies the pivot rather
    /// than sending it touring the screen.</summary>
    public double WobbleAmp { get; set; }

    /// <summary>How far in from the screen edge the effect extends. Larger is thicker.</summary>
    public double Reach { get; set; }

    /// <summary>Width of the fade behind the boundary. Wide thins the effect into haze; narrow gives
    /// it a defined surface.</summary>
    public double Feather { get; set; }

    /// <summary>
    /// 0 = radial vignette, 1 = border trim. Avoid values in between: the radial term is normalised
    /// to screen width, so any blend lands mostly on the left and right edges. Use `CornerBias` for
    /// corner weighting instead - it is per-axis and has no aspect bias.
    /// </summary>
    public double EdgeBlend { get; set; }

    /// <summary>Corner weighting of the border trim, ignored when `EdgeBlend` is 0. Raising it
    /// thickens and rounds the corners without favouring any one edge.</summary>
    public double CornerBias { get; set; }

    /// <summary>How the boundary is broken up, or all zero for a clean one. A smooth edge reads as
    /// a drawn shape; a displaced one reads as something growing in from the frame.</summary>
    public ApiJitterSpec? Jitter { get; set; } = new();

    /// <summary>Unit vector biasing the effect toward one side or corner.</summary>
    public ApiVector2? FocusDir { get; set; } = new();

    /// <summary>Higher values tighten the directional lobe.</summary>
    public double FocusPower { get; set; }

    /// <summary>0 = uniform all the way round, 1 = fully biased toward `FocusDir`.</summary>
    public double FocusAmount { get; set; }

    #endregion

    #region Internal methods

    /// <summary>Projects an authored mask into a DTO, including its boundary displacement.</summary>
    /// <param name="shape">The mask to copy.</param>
    /// <returns>The DTO, with its own vectors.</returns>
    internal static ApiShapeSpec From(in ShapeSpec shape) =>
        new()
        {
            Center = ApiVector2.From(shape.Center),
            WobbleFreq = ApiVector2.From(shape.WobbleFreq),
            WobbleAmp = shape.WobbleAmp,
            Reach = shape.Reach,
            Feather = shape.Feather,
            EdgeBlend = shape.EdgeBlend,
            CornerBias = shape.CornerBias,
            Jitter = ApiJitterSpec.From(shape.Jitter),
            FocusDir = ApiVector2.From(shape.FocusDir),
            FocusPower = shape.FocusPower,
            FocusAmount = shape.FocusAmount
        };

    /// <summary>Flattens this to the authoring type the compositor bakes from.</summary>
    /// <returns>A fresh mask; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">A value is not a real number, or <see cref="Jitter"/>
    /// carries a channel the API does not offer.</exception>
    internal ShapeSpec ToInternal() =>
        new()
        {
            Center = VfxInputValidator.Vector(Center, nameof(Center)),
            WobbleFreq = VfxInputValidator.Vector(WobbleFreq, nameof(WobbleFreq)),
            WobbleAmp = VfxInputValidator.Number(WobbleAmp, nameof(WobbleAmp)),
            Reach = VfxInputValidator.Number(Reach, nameof(Reach)),
            Feather = VfxInputValidator.Number(Feather, nameof(Feather)),
            EdgeBlend = VfxInputValidator.Number(EdgeBlend, nameof(EdgeBlend)),
            CornerBias = VfxInputValidator.Number(CornerBias, nameof(CornerBias)),
            Jitter = Jitter?.ToInternal() ?? default,
            FocusDir = VfxInputValidator.Vector(FocusDir, nameof(FocusDir)),
            FocusPower = VfxInputValidator.Number(FocusPower, nameof(FocusPower)),
            FocusAmount = VfxInputValidator.Number(FocusAmount, nameof(FocusAmount))
        };

    #endregion
}
