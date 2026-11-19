#nullable enable

using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// Colour painted over the scene. The only technique that reads a colour and the only one that never
/// touches the frame behind it, which also makes it the only one that may sit anywhere in a stack.
/// Build one with `API.VisualEffects.NewTintLayer`.
/// </summary>
public class ApiTintLayer : ApiLayerEffect
{
    #region Public accessors

    /// <summary>Red channel of the layer's colour, 0-255. Clamped into range when the effect starts.</summary>
    public int R { get; set; } = byte.MaxValue;

    /// <summary>Green channel of the layer's colour, 0-255.</summary>
    public int G { get; set; } = byte.MaxValue;

    /// <summary>Blue channel of the layer's colour, 0-255.</summary>
    public int B { get; set; } = byte.MaxValue;

    /// <summary>
    /// Alpha channel of the layer's colour, 0-255. Note that `ApiLayerEffect.Strength` is what the
    /// layer is actually painted at; this one tints rather than fades.
    /// </summary>
    public int A { get; set; } = byte.MaxValue;

    #endregion

    #region Internal methods

    /// <summary>Projects an authored tint into a DTO, unpacking its colour into the four channels a
    /// script sets.</summary>
    /// <param name="tint">The layer to copy.</param>
    /// <returns>The DTO.</returns>
    internal static ApiTintLayer FromTint(TintEffect tint)
    {
        Color colour = tint.Tint;

        return new ApiTintLayer { R = colour.R, G = colour.G, B = colour.B, A = colour.A };
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    private protected override LayerEffect CreateInternal() => new TintEffect { Tint = VfxInputValidator.Rgba(R, G, B, A) };

    #endregion
}
