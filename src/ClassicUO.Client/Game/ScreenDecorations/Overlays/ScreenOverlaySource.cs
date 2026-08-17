#nullable enable

using ClassicUO.Renderer.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.ScreenDecorations.Overlays;

/// <summary>
/// The frame as it stood before an overlay pass, for layers that distort what is on screen instead
/// of painting over it.
/// <para>
/// Nothing is copied: each pass already has a texture holding what it needs, so the caller hands
/// over the one it is about to draw from. A GPU cannot read the surface it is writing to, which is
/// what rules out reading the pass's own destination.
/// </para>
/// </summary>
/// <param name="Texture">The texture holding the scene, or null where the pass has none.</param>
/// <param name="Region">The part of it corresponding to the rectangle being drawn.</param>
public readonly record struct ScreenOverlaySource(Texture2D? Texture, Rectangle Region)
{
    /// <summary>No source. Sampling layers are skipped rather than drawn undistorted.</summary>
    public static ScreenOverlaySource None { get; } = new(null, Rectangle.Empty);

    /// <summary>Whether a sampling layer can be drawn against this source.</summary>
    public bool IsAvailable => Texture is { IsDisposed: false } && !Region.IsEmpty;

    /// <summary>
    /// The uv mapping the shader needs to find the drawn rectangle inside <see cref="Texture"/>.
    /// </summary>
    /// <returns>The mapping, or <see cref="OverlaySceneMap.Full"/> where there is no usable source.</returns>
    public OverlaySceneMap ToMap() => IsAvailable ? OverlaySceneMap.From(Texture!, Region) : OverlaySceneMap.Full;
}
