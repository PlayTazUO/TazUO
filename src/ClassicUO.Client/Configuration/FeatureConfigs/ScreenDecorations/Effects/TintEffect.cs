#nullable enable

using System.ComponentModel;
using System.Text.Json.Serialization;
using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Properties;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// Colour painted over the scene. The only technique that reads a tint, and the only one that never
/// touches the frame behind it - which also makes it the only one that may sit anywhere in a stack.
/// </summary>
public sealed class TintEffect : LayerEffect
{
    /// <summary>Persisted discriminator. Stable across releases.</summary>
    internal const string Discriminator = "tint";

    /// <summary>Colour of the layer.</summary>
    [LocalizedDisplayName("visualeffects_layer_tint", "Colour")]
    [LocalizedDescription("visualeffects_layer_tint_tooltip", "Colour of the layer.")]
    public Color Tint { get; set; } = Color.White;

    /// <inheritdoc />
    [JsonIgnore]
    [Browsable(false)]
    public override string TechniqueName => TazLang.Get("overlaytechnique_tint", "Tint");

    /// <inheritdoc />
    public override LayerEffect Clone()
    {
        var copy = new TintEffect { Tint = Tint };
        CopyCommonTo(copy);

        return copy;
    }

    /// <inheritdoc />
    private protected override void ApplyTechnique(ref OverlayParams parameters)
    {
        parameters.Appearance.Tint = Tint;
        parameters.Sampling = new OverlaySampling { Mode = OverlaySampleMode.None };
    }
}
