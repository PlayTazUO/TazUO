#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// Time-varying swell of a layer's strength. Distinct from a noise field, which varies strength
/// across the screen at one moment: this varies it everywhere at once, over time.
/// </summary>
public class ApiPulseSpec : PyApiObject
{
    #region Public accessors

    /// <summary>
    /// Breathing rate in Hz. Hard-capped at 3 Hz when the layer is baked - flashing above roughly
    /// that is a photosensitive-epilepsy hazard, and nothing may raise it.
    /// </summary>
    public double Frequency { get; set; }

    /// <summary>Depth of that breathing, as a fraction of `ApiLayerEffect.Strength`.</summary>
    public double Amplitude { get; set; }

    #endregion

    #region Internal methods

    /// <summary>Projects an authored pulse into a DTO.</summary>
    /// <param name="pulse">The pulse to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiPulseSpec From(in PulseSpec pulse) =>
        new() { Frequency = pulse.Frequency, Amplitude = pulse.Amplitude };

    /// <summary>Flattens this to the authoring type the compositor bakes from.</summary>
    /// <returns>A fresh pulse; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">Either value is not a real number.</exception>
    internal PulseSpec ToInternal() =>
        new()
        {
            Frequency = VfxInputValidator.Number(Frequency, nameof(Frequency)),
            Amplitude = VfxInputValidator.Number(Amplitude, nameof(Amplitude))
        };

    #endregion
}

/// <summary>
/// One layer of a visual effect composition. The subtype is the technique, so a technique's knobs
/// exist only on the type that reads them - there is no radius on a chromatic layer to mis-set.
/// <para>
/// Build one with `API.VisualEffects.NewTintLayer`, `API.VisualEffects.NewBlurLayer`, `API.VisualEffects.NewRadialBlurLayer` or
/// `API.VisualEffects.NewChromaticLayer`, each of which hands back the client's own defaults for that technique.
/// Fixed set of four: the shader has four techniques and there is no way to describe a fifth.
/// </para>
/// </summary>
public abstract class ApiLayerEffect : PyApiObject
{
    #region Public accessors

    /// <summary>
    /// Where on screen the layer lives and how its boundary breaks up. Every technique masks with
    /// this - a distortion layer's mask doubles as its strength.
    /// </summary>
    public ApiShapeSpec? Shape { get; set; } = ApiShapeSpec.From(ShapeSpec.Default);

    /// <summary>How the layer moves and what texture it has.</summary>
    public ApiNoiseSpec? Noise { get; set; } = ApiNoiseSpec.From(NoiseSpec.Default);

    /// <summary>
    /// Peak strength where the mask is full: the alpha a tint is painted at, or the degree to which
    /// a distortion replaces the sharp frame. The occurrence's intensity, the fade envelope and the
    /// user's own intensity setting can only attenuate this.
    /// </summary>
    public double Strength { get; set; } = OverlayParams.Default.Appearance.Opacity;

    /// <summary>How the strength swells over time, or all zero for a steady layer.</summary>
    public ApiPulseSpec? Pulse { get; set; } = new();

    #endregion

    #region Internal methods

    /// <summary>
    /// Projects an authored layer into the matching DTO.
    /// </summary>
    /// <param name="effect">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    /// <exception cref="ArgumentException">The layer is a technique this tier does not mirror,
    /// which can only mean a fifth was added without extending the DTOs.</exception>
    internal static ApiLayerEffect From(LayerEffect effect)
    {
        ApiLayerEffect projected = effect switch
        {
            TintEffect tint => ApiTintLayer.FromTint(tint),
            BlurEffect blur => ApiBlurLayer.FromBlur(blur),
            RadialBlurEffect radial => ApiRadialBlurLayer.FromRadial(radial),
            ChromaticEffect chromatic => ApiChromaticLayer.FromChromatic(chromatic),
            // Not a script's doing: it can only mean the client gained a fifth technique without
            // this tier being extended to match. Says so, or a script author hunts their own code.
            _ => throw new ArgumentException(
                $"This effect uses a layer type the scripting API does not know about yet "
                + $"({effect.GetType().Name}). This is a gap in the client, not in your script - "
                + "please report it.",
                nameof(effect)
            )
        };

        projected.Shape = ApiShapeSpec.From(effect.Shape);
        projected.Noise = ApiNoiseSpec.From(effect.Noise);
        projected.Strength = effect.Strength;
        projected.Pulse = ApiPulseSpec.From(effect.Pulse);

        return projected;
    }

    /// <summary>Flattens this to the authoring type the compositor bakes from.</summary>
    /// <returns>A fresh layer; mutating the DTO afterwards cannot reach it.</returns>
    internal LayerEffect ToInternal()
    {
        LayerEffect effect = CreateInternal();

        effect.Shape = Shape?.ToInternal() ?? ShapeSpec.Default;
        effect.Noise = Noise?.ToInternal() ?? NoiseSpec.Default;
        effect.Strength = VfxInputValidator.Number(Strength, nameof(Strength));
        effect.Pulse = Pulse?.ToInternal() ?? default;

        return effect;
    }

    #endregion

    #region Protected methods

    /// <summary>Builds the technique-specific half, with its own knobs already set.</summary>
    /// <returns>The layer, with the shared parts left for <see cref="ToInternal"/>.</returns>
    private protected abstract LayerEffect CreateInternal();

    #endregion
}
