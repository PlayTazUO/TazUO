#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// A visual effect composition: which layers, in what order, blended how, plus how it arrives and
/// leaves and what it shakes.
/// <para>
/// Get an editable copy of one the client already has with `API.VisualEffects.GetProfile`, or start
/// from nothing with `API.VisualEffects.NewProfile`. Either way it is yours: nothing a script passes
/// is ever saved, and editing this after `API.VisualEffects.Start` cannot reach the running effect.
/// </para>
/// </summary>
public class ApiEffectProfile : PyApiObject
{
    #region Public accessors

    /// <summary>Only ever used in log messages for a script-built composition.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this covers the whole window rather than the game world alone. Governs both halves of
    /// scope: which pass draws the layers, and which rectangle `Shake` displaces.
    /// <para>
    /// Leave it off unless the effect really is meant to cover the gumps and the cursor - doing so
    /// makes the UI hard to use for as long as the effect lasts.
    /// </para>
    /// </summary>
    public bool FullScreen { get; set; }

    /// <summary>Back to front: index 0 is drawn first and ends up underneath. At most 4.</summary>
    public IList<ApiProfileLayer> Layers { get; set; } = new List<ApiProfileLayer>();

    /// <summary>How the effect arrives and leaves. Starts at the client's own timing.</summary>
    public ApiFadeSpec? Fade { get; set; } = new();

    /// <summary>Screen shake the effect includes, fired once on arrival, or null for a purely
    /// visual one. Build one with `API.VisualEffects.NewShakeSpec`.</summary>
    public ApiShakeSpec? Shake { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Appends a layer, which is the usual way to build a composition up. At most 4, and each one is a
    /// full-screen pass per frame while the effect runs.
    /// </summary>
    /// <param name="effect">The layer to draw.</param>
    /// <param name="blend">How it composites against what is already beneath it.</param>
    /// <returns>The wrapper that was added, so the blend can be changed later.</returns>
    public ApiProfileLayer AddLayer(ApiLayerEffect effect, ApiVisualEffects.OverlayBlend blend = ApiVisualEffects.OverlayBlend.Alpha)
    {
        var layer = new ApiProfileLayer { Effect = effect, Blend = blend };
        Layers.Add(layer);

        return layer;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Projects an authored composition into a DTO, copying every value down to the leaves. That
    /// matters more here than anywhere else in this tier: the shipped compositions are a cached
    /// singleton list, so letting one reference escape would let a script retune every rule using
    /// that look for the rest of the session.
    /// </summary>
    /// <param name="profile">The composition to copy.</param>
    /// <returns>The DTO, sharing nothing with <paramref name="profile"/>.</returns>
    internal static ApiEffectProfile From(EffectProfile profile)
    {
        var projected = new ApiEffectProfile
        {
            Name = profile.Name,
            FullScreen = profile.FullScreen,
            Fade = ApiFadeSpec.From(profile.Fade),
            Shake = profile.Shake == null ? null : ApiShakeSpec.From(profile.Shake)
        };

        foreach (ProfileLayer layer in profile.Layers)
            projected.Layers.Add(ApiProfileLayer.From(layer));

        return projected;
    }

    /// <summary>
    /// Flattens this to the type the compositor bakes from. The single validation point for anything
    /// a script can get wrong structurally; the per-layer safety ceilings are applied by the bake
    /// itself and are not this method's to anticipate.
    /// </summary>
    /// <returns>A fresh composition; mutating the DTO afterwards cannot reach it.</returns>
    /// <exception cref="ArgumentException">The effect has no layers, has more than
    /// <see cref="OverlayLayerStack.MaxLayers"/>, or one of its layers is empty.</exception>
    internal EffectProfile ToInternal()
    {
        // A shake-only composition is a legitimate thing to want, but not through here: this path
        // raises a drawn effect, and there would be nothing to draw. StartShake is that.
        if (Layers == null || Layers.Count == 0)
        {
            throw new ArgumentException(
                "This effect has no layers, so there is nothing to draw. Add one with AddLayer - for "
                + "example AddLayer(API.VisualEffects.NewTintLayer()). For a shake with no visuals, "
                + "use API.VisualEffects.StartShake instead.",
                nameof(Layers)
            );
        }

        if (Layers.Count > OverlayLayerStack.MaxLayers)
        {
            throw new ArgumentException(
                $"This effect has {Layers.Count} layers, and an effect can have at most "
                + $"{OverlayLayerStack.MaxLayers}. Drop some, or split it into two effects and start both.",
                nameof(Layers)
            );
        }

        var profile = new EffectProfile
        {
            Name = Name,
            FullScreen = FullScreen,
            Fade = Fade?.ToInternal() ?? new FadeSpec(),
            Shake = Shake?.ToInternal()
        };

        foreach (ApiProfileLayer layer in Layers)
        {
            if (layer == null)
            {
                throw new ArgumentException(
                    "One of this effect's layers is empty. Something was added to Layers that is not a "
                    + "layer, or set to None afterwards.",
                    nameof(Layers)
                );
            }

            profile.Layers.Add(layer.ToInternal());
        }

        return profile;
    }

    #endregion
}
