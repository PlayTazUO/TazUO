#nullable enable

using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// One effect's participation in a composition: the layer, and how it combines with what is already
/// beneath it. Order is the index in `ApiEffectProfile.Layers`, back to front.
/// <para>
/// A wrapper rather than a blend field on the layer itself, because blending is a property of the
/// stack and not of the look - the same blur belongs in two compositions blended differently.
/// </para>
/// </summary>
public class ApiProfileLayer : PyApiObject
{
    #region Public accessors

    /// <summary>What this layer draws.</summary>
    public ApiLayerEffect? Effect { get; set; }

    /// <summary>
    /// How the layer composites against what is beneath it. A highlight pass wants Additive - a
    /// covering highlight reads as a second flat colour, an additive one as light caught on the
    /// layer under it.
    /// </summary>
    public ApiVisualEffects.OverlayBlend Blend { get; set; }

    #endregion

    #region Internal methods

    /// <summary>Projects an authored layer wrapper into a DTO, including the effect it carries.</summary>
    /// <param name="layer">The wrapper to copy.</param>
    /// <returns>The DTO, sharing nothing with <paramref name="layer"/>.</returns>
    internal static ApiProfileLayer From(ProfileLayer layer) =>
        new()
        {
            Effect = layer.Effect == null ? null : ApiLayerEffect.From(layer.Effect),
            Blend = (ApiVisualEffects.OverlayBlend)layer.Blend
        };

    /// <summary>Flattens this to the authoring type the compositor bakes from.</summary>
    /// <returns>A fresh layer wrapper.</returns>
    /// <exception cref="ArgumentException">The layer's <see cref="Effect"/> is not set, so there is
    /// nothing to draw.</exception>
    internal ProfileLayer ToInternal()
    {
        if (Effect == null)
        {
            throw new ArgumentException(
                "This layer's Effect is not set, so there is nothing for it to draw. Set it to a layer "
                + "from API.VisualEffects - NewTintLayer, NewBlurLayer, NewRadialBlurLayer or "
                + "NewChromaticLayer.",
                nameof(Effect)
            );
        }

        return new ProfileLayer
        {
            Effect = Effect.ToInternal(),
            Blend = (OverlayBlend)VfxInputValidator.Defined(Blend, nameof(Blend))
        };
    }

    #endregion
}
