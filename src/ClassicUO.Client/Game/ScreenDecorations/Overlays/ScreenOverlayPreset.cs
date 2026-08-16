#nullable enable

using System.Collections.Generic;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>
/// Bakes a small, call-site-friendly set of tunables down to an ordered <see cref="OverlayLayer"/>
/// list. Concrete presets expose only the handful of values worth tuning per use; how many layers
/// they decompose into is an implementation detail of the preset.
/// <para>
/// These are the authoring source for the shipped looks, not a runtime path: what actually reaches
/// the compositor is an <c>EffectProfile</c>, and <c>BuiltInProfiles</c> is what turns one of these
/// into the other. Retuning a preset therefore retunes the built-in profile it backs.
/// </para>
/// </summary>
public abstract class ScreenOverlayPreset
{
    /// <summary>
    /// Onset is quicker than release, and both are unhurried. Arriving reads as something happening
    /// to the player, so it wants to be noticed; leaving is only the absence of that, and a fast
    /// fade-out draws attention to the effect ending rather than to being well again.
    /// </summary>
    public float FadeInSeconds { get; set; } = 0.6f;

    public float FadeOutSeconds { get; set; } = 2f;

    /// <summary>
    /// Appends this preset's layers back-to-front: index 0 is drawn first and ends up underneath.
    /// A single-layer preset appends exactly one.
    /// </summary>
    /// <param name="layers">The list to append to.</param>
    protected abstract void Bake(List<OverlayLayer> layers);

    /// <summary>
    /// Refills <paramref name="layers"/> with clamped, budget-capped layers. Every layer is clamped
    /// independently, so composing layers can never be used to route around the pulse-frequency
    /// ceiling in <see cref="OverlayParams.Clamp"/>.
    /// </summary>
    /// <param name="layers">The list to fill; cleared first.</param>
    internal void BakeClamped(List<OverlayLayer> layers)
    {
        layers.Clear();
        Bake(layers);

        for (int i = 0; i < layers.Count; i++)
        {
            OverlayLayer layer = layers[i];
            layer.Params.Clamp();
            layers[i] = layer;
        }

        OverlayLayerStack.Finish(layers, GetType().Name);
    }
}
