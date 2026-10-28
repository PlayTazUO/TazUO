#nullable enable

using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Renderer.Effects;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// Disk blur. Out-of-focus vision.
/// <para>
/// Like every sampling technique it reads the frame as it stood <em>before</em> the overlay pass, so
/// it must sit below anything it is meant to affect, and two sampling layers in one profile do not
/// compose - the upper one re-reads the original frame and overwrites the lower.
/// </para>
/// </summary>
public sealed class BlurEffect : LayerEffect
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "blur";

    /// <summary>
    /// Blur disk radius as a fraction of screen width, aspect-corrected on upload so it stays
    /// circular. Capped at <see cref="OverlayParams.MaxSampleRadius" />, past which the frame is
    /// unreadable rather than merely blurred.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_radius", "Radius")]
    [LocalizedDescription(
        "visualeffects_layer_radius_tooltip",
        "Blur disk radius as a fraction of screen width.\n"
        + "Small values read as soft focus; past a couple of percent\n"
        + "it becomes frosted glass."
    )]
    public float Radius { get; set; } = OverlayParams.Default.Sampling.Radius;

    /// <summary>Samples taken per pixel, and the whole cost of the layer.</summary>
    [LocalizedDisplayName("visualeffects_layer_taps", "Samples")]
    [LocalizedDescription(
        "visualeffects_layer_taps_tooltip",
        "Samples taken per pixel, and the whole cost of the layer.\n"
        + "Too few for the radius shows as distinct ghost copies\n"
        + "instead of a blur."
    )]
    public OverlaySampleTaps Taps { get; set; } = OverlaySampleTaps.Twelve;

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public override string TechniqueName => TazLang.Get("overlaytechnique_blur", "Blur");

    /// <inheritdoc />
    public override LayerEffect Clone()
    {
        var copy = new BlurEffect { Radius = Radius, Taps = Taps };
        CopyCommonTo(copy);

        return copy;
    }

    /// <inheritdoc />
    private protected override void ApplyTechnique(ref OverlayParams parameters) =>
        parameters.Sampling = new OverlaySampling
        {
            Mode = OverlaySampleMode.Blur,
            Radius = Radius,
            Taps = Taps
        };
}

/// <summary>
/// Zoom blur along the ray from the shape's centre. Head-spin, speed. Reads as vertigo rather than
/// poor focus because the centre of the screen stays sharp however strong it is.
/// </summary>
public sealed class RadialBlurEffect : LayerEffect
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "radial_blur";

    /// <summary>
    /// How far along the centre ray the taps march, as a fraction of the distance from
    /// <see cref="OverlayShape.Center" />.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_zoom", "Zoom")]
    [LocalizedDescription(
        "visualeffects_layer_zoom_tooltip",
        "How far along the centre ray the taps march,\n"
        + "as a fraction of the distance from Shape.Center.\n"
        + "The centre stays sharp however high this goes."
    )]
    public float Zoom { get; set; } = OverlayParams.Default.Sampling.Zoom;

    /// <summary>
    /// Radians the march twists through on its way to the centre. 0 is a straight ray - plain
    /// forward motion; raising it winds the streaks into a spiral. Capped at
    /// <see cref="OverlayParams.MaxSampleSwirl" />, past which they wrap and stop reading as travel.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_swirl", "Swirl")]
    [LocalizedDescription(
        "visualeffects_layer_swirl_tooltip",
        "Radians the streaks twist through on their way to the centre.\n"
        + "0 is straight rays - plain forward motion. Raising it winds them\n"
        + "into a spiral; negative turns the other way."
    )]
    public float Swirl { get; set; } = OverlayParams.Default.Sampling.Swirl;

    /// <summary>
    /// Samples taken per pixel. Fewer than a disk blur needs: radial taps land on a line rather than
    /// spreading over an area, so the gaps between them are far less visible.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_taps", "Samples")]
    [LocalizedDescription(
        "visualeffects_layer_taps_radial_tooltip",
        "Samples taken per pixel, and the whole cost of the layer.\n"
        + "Radial needs fewer than a disk blur - its taps land on a line."
    )]
    public OverlaySampleTaps Taps { get; set; } = OverlaySampleTaps.Eight;

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public override string TechniqueName => TazLang.Get("overlaytechnique_radialblur", "Radial blur");

    /// <inheritdoc />
    public override LayerEffect Clone()
    {
        var copy = new RadialBlurEffect { Zoom = Zoom, Swirl = Swirl, Taps = Taps };
        CopyCommonTo(copy);

        return copy;
    }

    /// <inheritdoc />
    private protected override void ApplyTechnique(ref OverlayParams parameters) =>
        parameters.Sampling = new OverlaySampling
        {
            Mode = OverlaySampleMode.Radial,
            Zoom = Zoom,
            Swirl = Swirl,
            Taps = Taps
        };
}

/// <summary>
/// Red/blue split along the ray from the shape's centre. Lens fringing. The cheapest technique by a
/// wide margin - three taps whatever its strength - so it is worth reaching for first when something
/// needs to look wrong without costing anything.
/// </summary>
public sealed class ChromaticEffect : LayerEffect
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "chromatic";

    /// <summary>
    /// Channel separation, as a fraction of the distance from <see cref="OverlayShape.Center" />.
    /// Nothing separates at the centre and the fringing grows toward the corners, which is what
    /// makes it read as a lens rather than as a broken image. Capped at
    /// <see cref="OverlayParams.MaxSampleAberration" />.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_aberration", "Separation")]
    [LocalizedDescription(
        "visualeffects_layer_aberration_tooltip",
        "Red/blue separation, as a fraction of the distance\n"
        + "from Shape.Center. Nothing separates at the centre and fringing\n"
        + "grows toward the corners."
    )]
    public float Aberration { get; set; } = OverlayParams.Default.Sampling.Aberration;

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public override string TechniqueName => TazLang.Get("overlaytechnique_chromatic", "Chromatic split");

    /// <inheritdoc />
    public override LayerEffect Clone()
    {
        var copy = new ChromaticEffect { Aberration = Aberration };
        CopyCommonTo(copy);

        return copy;
    }

    /// <inheritdoc />
    private protected override void ApplyTechnique(ref OverlayParams parameters) =>
        parameters.Sampling = new OverlaySampling
        {
            Mode = OverlaySampleMode.Chromatic,
            Aberration = Aberration
        };
}

/// <summary>
/// Pulls the frame's colour toward or away from its own brightness. Drained reads as sickness,
/// exhaustion and near-death; pushed reads as fever and hallucination. One sample per pixel, so it
/// is the cheapest technique that reads the frame at all.
/// </summary>
public sealed class SaturationEffect : LayerEffect
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "saturation";

    /// <summary>
    /// Where the colour is pulled to, relative to the frame's own brightness: 1 leaves it untouched,
    /// 0 drains it to grey, above 1 pushes it past its real colour, and below 0 lands on the
    /// complement. Bounded by <see cref="OverlayParams.MinSampleSaturation" /> and
    /// <see cref="OverlayParams.MaxSampleSaturation" />.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_saturation", "Saturation")]
    [LocalizedDescription(
        "visualeffects_layer_saturation_tooltip",
        "Where the colour is pulled to: 1 leaves the frame untouched,\n"
        + "0 drains it to grey, above 1 pushes it past its real colour,\n"
        + "below 0 lands on the opposite one."
    )]
    public float Saturation { get; set; } = OverlayParams.Default.Sampling.Saturation;

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public override string TechniqueName => TazLang.Get("overlaytechnique_saturation", "Saturation");

    /// <inheritdoc />
    public override LayerEffect Clone()
    {
        var copy = new SaturationEffect { Saturation = Saturation };
        CopyCommonTo(copy);

        return copy;
    }

    /// <inheritdoc />
    private protected override void ApplyTechnique(ref OverlayParams parameters) =>
        parameters.Sampling = new OverlaySampling
        {
            Mode = OverlaySampleMode.Saturation,
            Saturation = Saturation
        };
}
