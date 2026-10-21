#nullable enable

using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// How a visual effect layer moves and what texture it has: two scrolling samples of the tiling
/// noise texture, the second domain-warped by the first.
/// </summary>
public class ApiNoiseSpec
{
    #region Public accessors

    /// <summary>
    /// Frequency of the primary field. The X:Y ratio is the anisotropy, and the main control over
    /// whether features read as blobs (near 1:1) or streaks (4:1 and beyond).
    /// </summary>
    public ApiVector2? BaseScale { get; set; } = new();

    /// <summary>
    /// Texture-space velocity. On-screen speed is `Scroll / Scale`, so two layers with matching
    /// scroll but different scales visibly slide against each other. Derive it from a target screen
    /// speed.
    /// </summary>
    public ApiVector2? BaseScroll { get; set; } = new();

    /// <summary>
    /// Which packed noise channel the primary field reads. Red and Green are fBm and read as
    /// organic; Blue is ridged and Alpha is Worley, and both draw cell outlines.
    /// </summary>
    public LegionAPI.NoiseChannel BaseChannel { get; set; }

    /// <summary>Frequency of the secondary field, whose lookup is warped by the primary.</summary>
    public ApiVector2? DetailScale { get; set; } = new();

    /// <summary>Texture-space velocity of the secondary field.</summary>
    public ApiVector2? DetailScroll { get; set; } = new();

    /// <summary>Which packed noise channel the secondary field reads.</summary>
    public LegionAPI.NoiseChannel DetailChannel { get; set; }

    /// <summary>
    /// Static texture-space shift of both fields, never varying with time. Desyncs a layer from
    /// another sharing its scale and scroll, so the two don't read as one texture traced twice.
    /// </summary>
    public ApiVector2? Offset { get; set; } = new();

    /// <summary>
    /// How far the primary field displaces the secondary's lookup. The gas-versus-fluid dial: high
    /// churns and billows, near-zero lets the pattern translate coherently.
    /// </summary>
    public double WarpStrength { get; set; }

    /// <summary>
    /// Outlines the field's median. Counterintuitive twice over: raising it makes the layer cover
    /// more, and what it draws are outlines, so on a soft field it produces bordered cells.
    /// </summary>
    public double RidgeAmount { get; set; }

    /// <summary>Cut-off applied to the field. Higher keeps less: sparser, with narrower features.</summary>
    public double Threshold { get; set; }

    /// <summary>Width of the fade either side of `Threshold`. Small = hard surface, large = cloud.</summary>
    public double Softness { get; set; }

    /// <summary>
    /// Solid fill blended under the noise. Anything above 0 makes the shape mask visible as a
    /// geometric form, so a layer reading as discrete streaks needs exactly 0. It fills the shape
    /// in; it is not a way to add weight.
    /// </summary>
    public double FlatFloor { get; set; }

    #endregion

    #region Internal methods

    internal static ApiNoiseSpec From(in NoiseSpec noise) =>
        new()
        {
            BaseScale = ApiVector2.From(noise.BaseScale),
            BaseScroll = ApiVector2.From(noise.BaseScroll),
            BaseChannel = (LegionAPI.NoiseChannel)noise.BaseChannel,
            DetailScale = ApiVector2.From(noise.DetailScale),
            DetailScroll = ApiVector2.From(noise.DetailScroll),
            DetailChannel = (LegionAPI.NoiseChannel)noise.DetailChannel,
            Offset = ApiVector2.From(noise.Offset),
            WarpStrength = noise.WarpStrength,
            RidgeAmount = noise.RidgeAmount,
            Threshold = noise.Threshold,
            Softness = noise.Softness,
            FlatFloor = noise.FlatFloor
        };

    internal NoiseSpec ToInternal() =>
        new()
        {
            BaseScale = ApiOverlayGuard.Vector(BaseScale),
            BaseScroll = ApiOverlayGuard.Vector(BaseScroll),
            BaseChannel = (Renderer.Effects.NoiseChannel)ApiOverlayGuard.Defined(BaseChannel, nameof(BaseChannel)),
            DetailScale = ApiOverlayGuard.Vector(DetailScale),
            DetailScroll = ApiOverlayGuard.Vector(DetailScroll),
            DetailChannel = (Renderer.Effects.NoiseChannel)ApiOverlayGuard.Defined(DetailChannel, nameof(DetailChannel)),
            Offset = ApiOverlayGuard.Vector(Offset),
            WarpStrength = (float)WarpStrength,
            RidgeAmount = (float)RidgeAmount,
            Threshold = (float)Threshold,
            Softness = (float)Softness,
            FlatFloor = (float)FlatFloor
        };

    #endregion
}
