#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;

/// <summary>
/// Builds <see cref="LayerEffect" />s: fresh ones for the composer's "add layer" list, and narrow
/// ones recovered from an already-flattened layer.
/// </summary>
public static class LayerEffectFactory
{
    /// <summary>
    /// One fresh instance of every technique, in the order the composer should offer them. New
    /// instances each call - the caller keeps whichever it takes.
    /// </summary>
    /// <returns>The techniques.</returns>
    public static IReadOnlyList<LayerEffect> CreateAll() =>
    [
        new TintEffect(),
        new BlurEffect(),
        new RadialBlurEffect(),
        new ChromaticEffect()
    ];

    /// <summary>
    /// Swaps a layer's technique, carrying across everything the two have in common - where it sits,
    /// how it moves, how strong it is. Only the knobs that belonged to the old technique are lost,
    /// which is the point: they had no meaning for the new one.
    /// </summary>
    /// <param name="source">The effect being replaced.</param>
    /// <param name="replacement">A fresh effect of the wanted technique.</param>
    /// <returns><paramref name="replacement"/>, filled in.</returns>
    public static LayerEffect ChangeTechnique(LayerEffect source, LayerEffect replacement)
    {
        replacement.Shape = source.Shape;
        replacement.Noise = source.Noise;
        replacement.Strength = source.Strength;
        replacement.Pulse = source.Pulse;

        return replacement;
    }

    /// <summary>
    /// Recovers the narrow effect a flat parameter block describes. The inverse of
    /// <see cref="LayerEffect.Bake" />, and how the shipped presets - which are still authored
    /// against the flat struct - become built-in profiles without being retyped by hand.
    /// </summary>
    /// <param name="parameters">The flattened layer.</param>
    /// <returns>The effect that bakes back to it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sampling mode has no technique, which means
    /// one was added to <see cref="OverlaySampleMode" /> without a home here.</exception>
    public static LayerEffect FromParams(in OverlayParams parameters)
    {
        LayerEffect effect = parameters.Sampling.Mode switch
        {
            OverlaySampleMode.None => new TintEffect { Tint = parameters.Appearance.Tint },
            OverlaySampleMode.Blur => new BlurEffect
            {
                Radius = parameters.Sampling.Radius,
                Taps = parameters.Sampling.Taps
            },
            OverlaySampleMode.Radial => new RadialBlurEffect
            {
                Zoom = parameters.Sampling.Zoom,
                Taps = parameters.Sampling.Taps
            },
            OverlaySampleMode.Chromatic => new ChromaticEffect
            {
                Aberration = parameters.Sampling.Aberration
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(parameters),
                parameters.Sampling.Mode,
                @"No layer effect for this sampling mode"
            )
        };

        effect.Shape = ShapeSpec.From(parameters.Shape);
        effect.Noise = NoiseSpec.From(parameters.Noise);
        effect.Strength = parameters.Appearance.Opacity;
        effect.Pulse = new PulseSpec
        {
            Frequency = parameters.Appearance.PulseFreq,
            Amplitude = parameters.Appearance.PulseAmp
        };

        return effect;
    }
}
