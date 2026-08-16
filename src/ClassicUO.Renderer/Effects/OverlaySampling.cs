using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Effects;

/// <summary>
/// What a layer does with the frame behind it. Anything other than <see cref="None"/> makes the
/// layer read the scene, which the compositor has to supply a texture for - a sampling layer with
/// no source available is skipped rather than drawn wrong.
/// </summary>
public enum OverlaySampleMode
{
    /// <summary>Paints <see cref="OverlayAppearance.Tint"/> and never reads the scene.</summary>
    None,

    /// <summary>Disk blur. Out-of-focus vision.</summary>
    Blur,

    /// <summary>Zoom blur along the ray from <see cref="OverlayShape.Center"/>. Head-spin, speed.</summary>
    Radial,

    /// <summary>Red/blue split along that same ray. Lens fringing.</summary>
    Chromatic
}

/// <summary>
/// Samples a distortion takes per pixel, which is its entire cost.
/// <para>
/// A closed set rather than a number: the count is the bound of an unrolled loop in the shader,
/// so each one is separately compiled and only these exist. Named for the count because that is
/// the cost - "Medium" would hide the one thing worth knowing.
/// </para>
/// </summary>
public enum OverlaySampleTaps
{
    /// <summary>Cheapest. Enough only for a small radius; wider blurs break into ghost copies.</summary>
    Four = 4,

    /// <summary>Enough for a subtle blur or for radial, whose taps fall on a line rather than
    /// spreading over an area.</summary>
    Eight = 8,

    /// <summary>The general-purpose disk count.</summary>
    Twelve = 12,

    /// <summary>Four times the cost of <see cref="Four"/>. Needed only at large radii.</summary>
    Sixteen = 16
}

/// <summary>
/// Distortion of what is already on screen, as opposed to colour painted over it.
/// <para>
/// The layer's shape mask doubles as the strength of the distortion: the sampled result is
/// composited at the layer's own alpha, so straight-alpha blending resolves to a crossfade
/// between the sharp frame and the distorted one. Every shape, jitter and noise control therefore
/// applies here unchanged, and a noise-driven mask gives blur that swims.
/// </para>
/// <para>
/// A sampling layer reads the frame as it stood <em>before</em> the overlay pass, so it must sit
/// below the layers it is meant to affect. Placed above a tint layer it does not leave that tint
/// sharp - it replaces it, in proportion to its own alpha.
/// </para>
/// </summary>
public struct OverlaySampling
{
    public OverlaySampleMode Mode;

    /// <summary>
    /// Blur disk radius as a fraction of screen width, aspect-corrected on upload so it stays
    /// circular. Small values read as soft focus; past a couple of percent it stops being vision
    /// and becomes frosted glass.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Samples taken per pixel, and the entire cost of the layer. Too few for the radius in use
    /// resolves into distinct ghost copies rather than a blur; the honest fix is fewer pixels (a
    /// tighter mask) rather than more taps. Blur and Radial only - Chromatic is a fixed three-tap
    /// split with no technique variant to select.
    /// </summary>
    public OverlaySampleTaps Taps;

    /// <summary>
    /// How far along the centre ray the radial taps march, as a fraction of the distance from
    /// <see cref="OverlayShape.Center"/>. Scales with that distance, so the centre stays sharp
    /// however high this goes.
    /// </summary>
    public float Zoom;

    /// <summary>
    /// Red/blue separation, again as a fraction of the distance from
    /// <see cref="OverlayShape.Center"/>. Nothing separates at the centre; the fringing grows
    /// toward the corners, which is what makes it read as a lens rather than as a broken image.
    /// </summary>
    public float Aberration;

    /// <summary>Whether this layer needs the scene bound to sample from.</summary>
    public readonly bool ReadsScene => Mode != OverlaySampleMode.None;
}

/// <summary>
/// Where the quad being drawn sits inside the scene texture, in uv. Lets a sampling layer read a
/// texture that covers more than it does - the viewport pass draws into the game viewport but
/// samples a texture holding the whole world.
/// </summary>
/// <param name="Offset">uv of the quad's origin within the scene texture.</param>
/// <param name="Scale">uv size of the quad within the scene texture.</param>
public readonly record struct OverlaySceneMap(Vector2 Offset, Vector2 Scale)
{
    /// <summary>The quad covers the whole texture.</summary>
    public static OverlaySceneMap Full { get; } = new(Vector2.Zero, Vector2.One);

    /// <summary>
    /// The mapping for a quad that corresponds to <paramref name="region"/> of
    /// <paramref name="texture"/>.
    /// </summary>
    /// <param name="texture">The scene texture; null or empty yields <see cref="Full"/>.</param>
    /// <param name="region">The part of it the quad covers.</param>
    /// <returns>The uv mapping.</returns>
    public static OverlaySceneMap From(Texture2D texture, Rectangle region)
    {
        if (texture == null || texture.Width <= 0 || texture.Height <= 0 || region.IsEmpty)
            return Full;

        var size = new Vector2(texture.Width, texture.Height);

        return new OverlaySceneMap(
            new Vector2(region.X / size.X, region.Y / size.Y),
            new Vector2(region.Width / size.X, region.Height / size.Y)
        );
    }
}
