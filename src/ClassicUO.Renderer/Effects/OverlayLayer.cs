using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Effects;

/// <summary>
/// How a layer composites against what is already on screen.
/// </summary>
public enum OverlayBlend
{
    /// <summary>
    /// Straight-alpha "over". The layer covers what is behind it in proportion to its alpha.
    /// </summary>
    Alpha,

    /// <summary>
    /// Adds tint * alpha to what is behind it. Brightens instead of covering, which is what a
    /// highlight/specular pass needs - a covering highlight reads as a second flat colour, an
    /// additive one reads as light caught on the layer underneath it.
    /// </summary>
    Additive
}

public static class OverlayBlendExtensions
{
    /// <summary>
    /// ScreenOverlay.fx emits straight (non-premultiplied) alpha, so both states must use
    /// SourceAlpha as the source factor. They differ only in the destination factor:
    /// InverseSourceAlpha replaces, One accumulates.
    /// </summary>
    public static BlendState ToBlendState(this OverlayBlend blend) =>
        blend == OverlayBlend.Additive ? BlendState.Additive : BlendState.NonPremultiplied;
}

/// <summary>
/// One draw call's worth of overlay: the shader uniforms plus the pipeline state they are drawn
/// with. Blend mode is deliberately not a field on <see cref="OverlayParams"/> - everything in
/// that struct maps one-to-one onto an <see cref="EffectParameter"/> and is uploaded by
/// <see cref="ScreenOverlayEffect.Apply"/>; this is consumed by the draw loop instead and never
/// reaches the shader.
/// </summary>
public struct OverlayLayer
{
    public OverlayParams Params;

    /// <summary>
    /// Defaults to <see cref="OverlayBlend.Alpha"/> so a single-layer preset that never mentions
    /// blending keeps its original behaviour.
    /// </summary>
    public OverlayBlend Blend;
}
