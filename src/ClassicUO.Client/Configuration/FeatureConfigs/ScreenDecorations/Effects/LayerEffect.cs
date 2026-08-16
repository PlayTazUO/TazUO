#nullable enable

using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// Time-varying swell of a layer's strength. Distinct from a noise field, which varies strength
/// across the screen at one moment: this varies it everywhere at once, over time.
/// </summary>
public struct PulseSpec
{
    /// <summary>
    /// Breathing rate in Hz. Hard-capped at <see cref="OverlayParams.MaxPulseFreqHz" /> when baked -
    /// flashing above roughly 3 Hz is a photosensitive-epilepsy hazard, and no profile, rule or
    /// setting may raise it.
    /// </summary>
    [LocalizedDisplayName("visualeffects_pulse_frequency", "Rate (Hz)")]
    [LocalizedDescription(
        "visualeffects_pulse_frequency_tooltip",
        "Breathing rate in Hz. Hard-capped at 3 Hz for photosensitivity\n"
        + "reasons."
    )]
    public float Frequency;

    /// <summary>Depth of that breathing, as a fraction of <see cref="LayerEffect.Strength" />.</summary>
    [LocalizedDisplayName("visualeffects_pulse_amplitude", "Depth")]
    [LocalizedDescription(
        "visualeffects_pulse_amplitude_tooltip",
        "Depth of that breathing, as a fraction of Strength."
    )]
    public float Amplitude;
}

/// <summary>
/// One layer of a composition. The subtype is the technique, so a technique's knobs exist only on
/// the technique that reads them - there is no radius on a chromatic layer to mis-set or to show in
/// an editor.
/// <para>
/// Authoring model only. <see cref="Bake" /> flattens to the uniform struct the shader wants, which
/// is why the renderer is untouched by any of this. The flat form is the GPU wire format and
/// <em>should</em> be flat: every field of it maps one-to-one onto an effect parameter.
/// </para>
/// <para>
/// Fixed set of four - the shader has four techniques, and there is no way to describe a fifth from
/// config.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "technique")]
[JsonDerivedType(typeof(TintEffect), TintEffect.Discriminator)]
[JsonDerivedType(typeof(BlurEffect), BlurEffect.Discriminator)]
[JsonDerivedType(typeof(RadialBlurEffect), RadialBlurEffect.Discriminator)]
[JsonDerivedType(typeof(ChromaticEffect), ChromaticEffect.Discriminator)]
public abstract class LayerEffect
{
    #region Public accessors

    /// <summary>
    /// Where on screen it lives and how its boundary breaks up. Every technique masks with this - a
    /// sampling layer's mask doubles as its strength.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_shape", "Shape")]
    [LocalizedDescription(
        "visualeffects_layer_shape_tooltip",
        "Where on screen the effect lives: vignette or border shape,\n"
        + "how far it extends, and how its boundary breaks up."
    )]
    public ShapeSpec Shape { get; set; } = ShapeSpec.Default;

    /// <summary>How it moves and what texture it has.</summary>
    [LocalizedDisplayName("visualeffects_layer_noise", "Texture")]
    [LocalizedDescription(
        "visualeffects_layer_noise_tooltip",
        "How the effect moves and what texture it has."
    )]
    public NoiseSpec Noise { get; set; } = NoiseSpec.Default;

    /// <summary>
    /// Peak strength where the mask is full: the alpha a tint is painted at, or the degree to which
    /// a distortion replaces the sharp frame. This is the authored value the runtime stack scales -
    /// trigger intensity, fade envelope and the global setting can only attenuate it.
    /// </summary>
    [LocalizedDisplayName("visualeffects_layer_strength", "Strength")]
    [LocalizedDescription(
        "visualeffects_layer_strength_tooltip",
        "Peak strength where the mask is full: a tint's alpha,\n"
        + "or how far a distortion replaces the sharp frame."
    )]
    public float Strength { get; set; } = OverlayParams.Default.Appearance.Opacity;

    /// <summary>How the strength swells over time, or all zero for a steady layer.</summary>
    [LocalizedDisplayName("visualeffects_layer_pulse", "Pulse")]
    [LocalizedDescription(
        "visualeffects_layer_pulse_tooltip",
        "Time-varying swell of the layer's strength."
    )]
    public PulseSpec Pulse { get; set; }

    /// <summary>Display name for the composer's "add layer" list.</summary>
    [JsonIgnore]
    [Browsable(false)]
    public abstract string TechniqueName { get; }

    #endregion

    #region Public methods

    /// <summary>Copy, so editing one profile's layer cannot write into another's.</summary>
    /// <returns>An independent copy.</returns>
    public abstract LayerEffect Clone();

    #endregion

    #region Internal methods

    /// <summary>
    /// Flattens this to the uniform struct the shader is uploaded from. Clamped here rather than by
    /// the caller: the safety ceilings are not optional, and a layer that reached the compositor
    /// unclamped would be one that routed around them.
    /// </summary>
    /// <returns>The baked, clamped parameters.</returns>
    internal OverlayParams Bake()
    {
        var parameters = new OverlayParams
        {
            Shape = Shape.ToParams(),
            Noise = Noise.ToParams(),
            Appearance = new OverlayAppearance
            {
                // Overwritten by a tint layer. Sampling techniques return scene colour in its place,
                // but white keeps it harmless if the layer is later switched to a painting one.
                Tint = Color.White,
                Opacity = Strength,

                // Always 1: the authored strength is Strength, and everything that scales it at
                // runtime is applied by the compositor rather than baked in.
                Intensity = 1f,
                PulseFreq = Pulse.Frequency,
                PulseAmp = Pulse.Amplitude
            }
        };

        ApplyTechnique(ref parameters);
        parameters.Clamp();

        return parameters;
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Writes the technique's own contribution: its sampling block, and for a tint layer its colour.
    /// The whole sampling struct is assigned rather than only the fields the mode reads, so two
    /// layers of one technique bake identically whatever the others were left at.
    /// </summary>
    /// <param name="parameters">The parameters being built.</param>
    private protected abstract void ApplyTechnique(ref OverlayParams parameters);

    /// <summary>Copies the parts every technique shares onto a fresh instance.</summary>
    /// <param name="target">The copy being built.</param>
    private protected void CopyCommonTo(LayerEffect target)
    {
        target.Shape = Shape;
        target.Noise = Noise;
        target.Strength = Strength;
        target.Pulse = Pulse;
    }

    #endregion
}
