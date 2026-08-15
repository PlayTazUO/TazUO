#nullable enable

using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// How a layer moves and what texture it has: two scrolling samples of the tiling noise texture,
/// the second domain-warped by the first.
/// <para>
/// Authoring mirror of <see cref="OverlayNoise" />: what the composer edits and config persists.
/// <see cref="ToParams" /> flattens it to the GPU wire format, which carries no editor metadata.
/// </para>
/// </summary>
public struct NoiseSpec
{
    /// <summary>
    /// Frequency of the primary field. The X:Y ratio is the anisotropy, and the main control over
    /// whether features read as blobs (near 1:1) or streaks (4:1 and beyond).
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_basescale", "Primary frequency")]
    [LocalizedDescription(
        "visualeffects_noise_basescale_tooltip",
        "Frequency of the primary noise field.\n"
        + "The X:Y ratio is the anisotropy - near 1:1 reads as blobs,\n"
        + "4:1 and beyond as streaks."
    )]
    public Vector2 BaseScale;

    /// <summary>
    /// Texture-space velocity. On-screen speed is <c>Scroll / Scale</c>, so two layers with matching
    /// scroll but different scales visibly slide against each other. Derive it from a target screen
    /// speed.
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_basescroll", "Primary drift")]
    [LocalizedDescription(
        "visualeffects_noise_basescroll_tooltip",
        "Texture-space velocity. On-screen speed is Scroll / Scale,\n"
        + "so two layers with matching scroll but different scales visibly\n"
        + "slide against each other."
    )]
    public Vector2 BaseScroll;

    [LocalizedDisplayName("visualeffects_noise_basechannel", "Primary source")]
    [LocalizedDescription(
        "visualeffects_noise_basechannel_tooltip",
        "Which packed noise channel the primary field reads.\n"
        + "R and G are fBm (organic); B is ridged and A is Worley -\n"
        + "both draw cell outlines and suit only cracks and shattering."
    )]
    public NoiseChannel BaseChannel;

    /// <summary>Frequency of the secondary field, whose lookup is warped by the primary.</summary>
    [LocalizedDisplayName("visualeffects_noise_detailscale", "Detail frequency")]
    [LocalizedDescription(
        "visualeffects_noise_detailscale_tooltip",
        "Frequency of the secondary field, whose lookup is warped\n"
        + "by the primary."
    )]
    public Vector2 DetailScale;

    [LocalizedDisplayName("visualeffects_noise_detailscroll", "Detail drift")]
    [LocalizedDescription(
        "visualeffects_noise_detailscroll_tooltip", "Texture-space velocity of the secondary field."
    )]
    public Vector2 DetailScroll;

    [LocalizedDisplayName("visualeffects_noise_detailchannel", "Detail source")]
    [LocalizedDescription(
        "visualeffects_noise_detailchannel_tooltip", "Which packed noise channel the secondary field reads."
    )]
    public NoiseChannel DetailChannel;

    /// <summary>
    /// Static texture-space shift of both fields, never varying with time. Desyncs a layer from
    /// another sharing its scale and scroll, so the two don't read as one texture traced twice.
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_offset", "Static offset")]
    [LocalizedDescription(
        "visualeffects_noise_offset_tooltip",
        "Static texture-space shift of both fields, unaffected by time.\n"
        + "Used to desync a layer from another one sharing its scale\n"
        + "and scroll, so they don't read as the same texture traced twice."
    )]
    public Vector2 Offset;

    /// <summary>
    /// How far the primary field displaces the secondary's lookup. The gas-versus-fluid dial: high
    /// churns and billows, near-zero lets the pattern translate coherently.
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_warpstrength", "Churn")]
    [LocalizedDescription(
        "visualeffects_noise_warpstrength_tooltip",
        "How far the primary field displaces the secondary field's lookup.\n"
        + "The gas-versus-fluid dial: high values churn and billow,\n"
        + "near-zero lets the pattern translate coherently."
    )]
    public float WarpStrength;

    /// <summary>
    /// Blends in <c>(1 - |2n - 1|)²</c>, outlining the field's median. Counterintuitive twice over:
    /// it peaks at the field's most common value, so raising it makes a layer cover more, and what
    /// it draws are outlines - on a soft field, bordered cells.
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_ridgeamount", "Ridging")]
    [LocalizedDescription(
        "visualeffects_noise_ridgeamount_tooltip",
        "Outlines the field's median. Counterintuitive twice over: raising\n"
        + "it makes the layer cover more, and what it draws are outlines,\n"
        + "so on a soft field it produces bordered cells."
    )]
    public float RidgeAmount;

    /// <summary>Cut-off applied to the field. Higher keeps less: sparser, with narrower features.</summary>
    [LocalizedDisplayName("visualeffects_noise_threshold", "Coverage cut-off")]
    [LocalizedDescription(
        "visualeffects_noise_threshold_tooltip",
        "Cut-off applied to the field. Higher keeps less, so the layer gets\n"
        + "sparser and its features narrower."
    )]
    public float Threshold;

    /// <summary>Width of the fade either side of <see cref="Threshold" />. Small = hard surface,
    /// large = soft cloud.</summary>
    [LocalizedDisplayName("visualeffects_noise_softness", "Edge softness")]
    [LocalizedDescription(
        "visualeffects_noise_softness_tooltip",
        "Width of the fade either side of Threshold. Small gives hard-\n"
        + "edged surfaces; large gives soft-edged clouds."
    )]
    public float Softness;

    /// <summary>
    /// Solid fill blended under the noise. Anything above 0 makes the shape mask visible as a
    /// geometric form, so a layer reading as discrete streaks needs exactly 0. Fills the shape in;
    /// it does not add weight.
    /// </summary>
    [LocalizedDisplayName("visualeffects_noise_flatfloor", "Solid fill")]
    [LocalizedDescription(
        "visualeffects_noise_flatfloor_tooltip",
        "Solid fill blended under the noise. Anything above 0\n"
        + "makes the shape mask itself visible as a geometric form,\n"
        + "so discrete streaks or wisps need exactly 0.\n"
        + "Not a way to add weight."
    )]
    public float FlatFloor;

    /// <summary>The renderer's own defaults, as an authored spec.</summary>
    public static NoiseSpec Default => From(OverlayParams.Default.Noise);

    /// <summary>
    /// Flattens to the wire format. Paired with <see cref="From" /> - a field added to one belongs in
    /// both.
    /// </summary>
    /// <returns>The renderer struct.</returns>
    public readonly OverlayNoise ToParams() =>
        new()
        {
            BaseScale = BaseScale,
            BaseScroll = BaseScroll,
            BaseChannel = BaseChannel,
            DetailScale = DetailScale,
            DetailScroll = DetailScroll,
            DetailChannel = DetailChannel,
            Offset = Offset,
            WarpStrength = WarpStrength,
            RidgeAmount = RidgeAmount,
            Threshold = Threshold,
            Softness = Softness,
            FlatFloor = FlatFloor
        };

    /// <summary>
    /// Recovers a spec from the wire format, which is how the code-authored presets become editable
    /// profiles.
    /// </summary>
    /// <param name="noise">The flattened field.</param>
    /// <returns>The spec that flattens back to it.</returns>
    public static NoiseSpec From(in OverlayNoise noise) =>
        new()
        {
            BaseScale = noise.BaseScale,
            BaseScroll = noise.BaseScroll,
            BaseChannel = noise.BaseChannel,
            DetailScale = noise.DetailScale,
            DetailScroll = noise.DetailScroll,
            DetailChannel = noise.DetailChannel,
            Offset = noise.Offset,
            WarpStrength = noise.WarpStrength,
            RidgeAmount = noise.RidgeAmount,
            Threshold = noise.Threshold,
            Softness = noise.Softness,
            FlatFloor = noise.FlatFloor
        };
}
