#nullable enable

using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// Disk blur - out-of-focus vision. Build one with `API.VisualEffects.NewBlurLayer`.
/// <para>
/// Like every distortion technique it reads the frame as it stood <em>before</em> the effect was
/// drawn, so it must sit below anything it is meant to affect, and two distortion layers in one
/// composition do not compose: the upper one re-reads the original frame and overwrites the lower.
/// </para>
/// </summary>
public class ApiBlurLayer : ApiLayerEffect
{
    #region Public accessors

    /// <summary>
    /// Blur disk radius as a fraction of screen width, aspect-corrected so it stays circular. Small
    /// values read as soft focus; past a couple of percent it becomes frosted glass. Capped when the
    /// layer is baked.
    /// </summary>
    public double Radius { get; set; } = OverlayParams.Default.Sampling.Radius;

    /// <summary>Samples taken per pixel, and the whole cost of the layer. Too few for the radius
    /// shows as distinct ghost copies instead of a blur.</summary>
    public ApiVisualEffects.OverlaySampleTaps Taps { get; set; } = ApiVisualEffects.OverlaySampleTaps.Twelve;

    #endregion

    #region Internal methods

    /// <summary>Projects an authored disk blur into a DTO. Named apart from the base class's
    /// <see cref="ApiLayerEffect.From"/>, which dispatches to this.</summary>
    /// <param name="blur">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiBlurLayer FromBlur(BlurEffect blur) =>
        new() { Radius = blur.Radius, Taps = (ApiVisualEffects.OverlaySampleTaps)blur.Taps };

    #endregion

    #region Protected methods

    /// <inheritdoc />
    private protected override LayerEffect CreateInternal() =>
        new BlurEffect
        {
            Radius = VfxInputValidator.Number(Radius, nameof(Radius)),
            Taps = (OverlaySampleTaps)VfxInputValidator.Defined(Taps, nameof(Taps))
        };

    #endregion
}

/// <summary>
/// Zoom blur along the ray from the shape's centre - head-spin, speed. Reads as vertigo rather than
/// poor focus because the centre of the screen stays sharp however strong it is. Build one with
/// `API.VisualEffects.NewRadialBlurLayer`.
/// </summary>
public class ApiRadialBlurLayer : ApiLayerEffect
{
    #region Public accessors

    /// <summary>How far along the centre ray the taps march, as a fraction of the distance from
    /// `ApiShapeSpec.Center`.</summary>
    public double Zoom { get; set; } = OverlayParams.Default.Sampling.Zoom;

    /// <summary>
    /// Radians the march twists through on its way to the centre. 0 is a straight ray; raising it
    /// winds the streaks into a spiral, negative turns the other way. Capped at half a turn when the
    /// layer is baked. Exactly 0 is free - it takes a cheaper pass - so leave it there unless the
    /// spiral is wanted.
    /// </summary>
    public double Swirl { get; set; } = OverlayParams.Default.Sampling.Swirl;

    /// <summary>Samples taken per pixel. Fewer than a disk blur needs: radial taps land on a line
    /// rather than spreading over an area, so the gaps between them are far less visible.</summary>
    public ApiVisualEffects.OverlaySampleTaps Taps { get; set; } = ApiVisualEffects.OverlaySampleTaps.Eight;

    #endregion

    #region Internal methods

    /// <summary>Projects an authored radial blur into a DTO.</summary>
    /// <param name="radial">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiRadialBlurLayer FromRadial(RadialBlurEffect radial) =>
        new()
        {
            Zoom = radial.Zoom,
            Swirl = radial.Swirl,
            Taps = (ApiVisualEffects.OverlaySampleTaps)radial.Taps
        };

    #endregion

    #region Protected methods

    /// <inheritdoc />
    private protected override LayerEffect CreateInternal() =>
        new RadialBlurEffect
        {
            Zoom = VfxInputValidator.Number(Zoom, nameof(Zoom)),
            Swirl = VfxInputValidator.Number(Swirl, nameof(Swirl)),
            Taps = (OverlaySampleTaps)VfxInputValidator.Defined(Taps, nameof(Taps))
        };

    #endregion
}

/// <summary>
/// Red/blue split along the ray from the shape's centre - lens fringing. Three taps whatever its
/// strength; only `ApiSaturationLayer`, at one, is cheaper. Build one with
/// `API.VisualEffects.NewChromaticLayer`.
/// </summary>
public class ApiChromaticLayer : ApiLayerEffect
{
    #region Public accessors

    /// <summary>
    /// Channel separation, as a fraction of the distance from `ApiShapeSpec.Center`. Nothing
    /// separates at the centre and the fringing grows toward the corners, which is what makes it
    /// read as a lens rather than as a broken image. Capped when the layer is baked.
    /// </summary>
    public double Aberration { get; set; } = OverlayParams.Default.Sampling.Aberration;

    #endregion

    #region Internal methods

    /// <summary>Projects an authored chromatic aberration into a DTO.</summary>
    /// <param name="chromatic">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiChromaticLayer FromChromatic(ChromaticEffect chromatic) =>
        new() { Aberration = chromatic.Aberration };

    #endregion

    #region Protected methods

    /// <inheritdoc />
    private protected override LayerEffect CreateInternal() =>
        new ChromaticEffect { Aberration = VfxInputValidator.Number(Aberration, nameof(Aberration)) };

    #endregion
}

/// <summary>
/// Pulls the frame's colour toward or away from its own brightness - drained for sickness and
/// near-death, pushed for fever. One sample per pixel: the cheapest technique that reads the frame
/// at all. Build one with `API.VisualEffects.NewSaturationLayer`.
/// </summary>
public class ApiSaturationLayer : ApiLayerEffect
{
    #region Public accessors

    /// <summary>
    /// Where the colour is pulled to, relative to the frame's own brightness: 1 untouched, 0 grey,
    /// above 1 pushed past its real colour, below 0 the complement. Capped at -1 and 3 when the
    /// layer is baked.
    /// </summary>
    public double Saturation { get; set; } = OverlayParams.Default.Sampling.Saturation;

    #endregion

    #region Internal methods

    /// <summary>Projects an authored saturation grade into a DTO.</summary>
    /// <param name="saturation">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiSaturationLayer FromSaturation(SaturationEffect saturation) =>
        new() { Saturation = saturation.Saturation };

    #endregion

    #region Protected methods

    /// <inheritdoc />
    private protected override LayerEffect CreateInternal() =>
        new SaturationEffect { Saturation = VfxInputValidator.Number(Saturation, nameof(Saturation)) };

    #endregion
}
