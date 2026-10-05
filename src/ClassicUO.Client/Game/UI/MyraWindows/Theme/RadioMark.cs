#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.MyraWindows.Theme;

/// <summary>Which face of a radio mark to draw.</summary>
public enum RadioMarkState
{
    /// <summary>Unselected, at rest.</summary>
    Off,

    /// <summary>Unselected, pointer over the option.</summary>
    Over,

    /// <summary>Selected.</summary>
    On
}

/// <summary>
///     Draws the radio button marks: the check box's brushed-metal frame and recessed well, but round,
///     so a set of exclusive options is distinguishable from a set of independent ones at a glance.
/// </summary>
/// <remarks>
///     <para>
///         Generated rather than shipped as art so the mark is crisp at whatever size the UI font asks
///         for; a bitmap would have to pick one size and blur at the rest, and an SVG would mean a
///         rasterizer this project does not otherwise need.
///     </para>
///     <para>
///         Theme-change work, never per-frame: three marks at the default size cost well under a
///         millisecond together, and the textures are then static for the renderer's purposes. The
///         caller owns every texture returned and must dispose it - see the warning on
///         <see cref="Create" /> before calling it from anywhere but a style rebuild.
///     </para>
/// </remarks>
public static class RadioMark
{
    #region Private members

    /// <summary>
    ///     Samples per axis per pixel - the same thing as drawing six times oversized and box-filtering
    ///     down, without ever allocating the large texture or paying a resample pass. Six is the point
    ///     past which the difference stops being visible at mark sizes.
    /// </summary>
    private const int SUPERSAMPLE = 6;

    /// <summary>
    ///     Gap between the circle and the edge of its texture, as a fraction of the size. The check box
    ///     art carries about a pixel of padding at 17px, so a circle drawn edge to edge sits visibly
    ///     larger than the check box beside it despite the two textures matching.
    /// </summary>
    private const float MARK_INSET = 0.06f;

    /// <summary>Band radii, as a fraction of the outer radius, mirroring the check box's proportions.</summary>
    private const float RING_INNER = 0.70f;

    private const float WELL_EDGE = 0.62f;
    private const float DOT_RADIUS = 0.50f;

    /// <summary>Bottom-left, the direction the check box's bevel is lit from.</summary>
    private static readonly Vector2 _lightDirection = Vector2.Normalize(new Vector2(-0.78f, 0.62f));

    /// <summary>
    ///     Tightens the glint to a corner. The check box's frame is flat and dark nearly all the way
    ///     round with one bright corner, so a plain cosine falloff - which lights a whole half - reads as
    ///     a chrome donut instead.
    /// </summary>
    private const float BEVEL_FALLOFF = 9f;

    /// <summary>How much of the glint the inner edge of the band keeps; the outer edge takes it all.</summary>
    private const float BEVEL_INNER_SHARE = 0.35f;

    /// <summary>Faint counter-light on the inner wall opposite the glint, which is what reads as depth.</summary>
    private const float RIM_FALLOFF = 6f;

    private const float RIM_STRENGTH = 0.5f;

    private static readonly Vector3 _ringBase = Rgb(72, 64, 55);
    private static readonly Vector3 _ringBaseOver = Rgb(126, 112, 96);
    private static readonly Vector3 _ringHighlight = Rgb(228, 223, 215);
    private static readonly Vector3 _ringRim = Rgb(150, 142, 130);
    private static readonly Vector3 _ringInset = Rgb(10, 7, 6);

    private static readonly Vector3 _wellEdgeColor = Rgb(35, 16, 14);
    private static readonly Vector3 _wellCenter = Rgb(55, 24, 22);
    private static readonly Vector3 _wellEdgeOver = Rgb(52, 26, 23);
    private static readonly Vector3 _wellCenterOver = Rgb(78, 38, 34);

    /// <summary>The selected dot is lit from the top, where the frame is lit from below - as the check box's is.</summary>
    private static readonly Vector3 _dotSpecular = Rgb(255, 240, 234);

    private static readonly Vector3 _dotMid = Rgb(238, 100, 108);
    private static readonly Vector3 _dotLow = Rgb(236, 74, 62);

    /// <summary>Where the dot's gradient stops being specular and becomes body colour.</summary>
    private const float DOT_SPECULAR_SPAN = 0.26f;

    /// <summary>Amplitude of the metallic grain, in 0-1 colour.</summary>
    private const float GRAIN = 0.035f;

    #endregion

    #region Public methods

    /// <summary>
    ///     Draws one face of the mark.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>
    ///             Expensive, and deliberately uncached. Do not call this per widget, per state change or
    ///             per frame.
    ///         </b>
    ///         It evaluates <c>size² × <see cref="SUPERSAMPLE" />²</c> shading samples on
    ///         the CPU and then uploads a texture: cost is quadratic in <paramref name="size" />, which
    ///         measures around 0.1 ms per face at 17px but roughly 3 ms at 64px.
    ///     </para>
    ///     <para>
    ///         The intended use is a handful of faces built once per theme change and shared through the
    ///         stylesheet, which is what <c>MyraStyle</c> does. Anything needing marks at more than one
    ///         size must put a cache in front of this keyed on size and state, and own the eviction -
    ///         every call allocates a new texture, and nothing here reuses or frees one.
    ///     </para>
    /// </remarks>
    /// <param name="device">Device to upload to.</param>
    /// <param name="size">Side length in pixels. Below about 10 the bands stop resolving.</param>
    /// <param name="state">Which face to draw.</param>
    /// <returns>A new texture the caller owns and must dispose.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size" /> is not positive.</exception>
    public static Texture2D Create(GraphicsDevice device, int size, RadioMarkState state)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var pixels = new Color[size * size];
        float center = size / 2f;
        float outerRadius = center - size * MARK_INSET;

        const float sampleStep = 1f / SUPERSAMPLE;
        const float samplesPerPixel = SUPERSAMPLE * SUPERSAMPLE;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector3 sum = Vector3.Zero;
            float coverage = 0f;

            for (int sy = 0; sy < SUPERSAMPLE; sy++)
            for (int sx = 0; sx < SUPERSAMPLE; sx++)
            {
                float dx = x + (sx + 0.5f) * sampleStep - center;
                float dy = y + (sy + 0.5f) * sampleStep - center;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                float radius = distance / outerRadius;

                if (radius > 1f)
                    continue;

                sum += Sample(dx, dy, distance, radius, outerRadius, state);
                coverage++;
            }

            // Coverage doubles as the alpha, which is what antialiases the outer edge.
            pixels[y * size + x] = coverage == 0f
                ? Color.Transparent
                : ToColor(sum / coverage, coverage / samplesPerPixel);
        }

        var texture = new Texture2D(device, size, size, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        return texture;
    }

    #endregion

    #region Private methods

    /// <summary>Colour of one sample, by which band of the mark it falls in.</summary>
    /// <param name="dx">Offset from centre, in pixels.</param>
    /// <param name="dy">Offset from centre, in pixels.</param>
    /// <param name="distance">That offset's length, in pixels.</param>
    /// <param name="radius">That length as a fraction of the outer radius.</param>
    /// <param name="outerRadius">The outer radius, in pixels.</param>
    /// <param name="state">Which face is being drawn.</param>
    private static Vector3 Sample(float dx, float dy, float distance, float radius, float outerRadius, RadioMarkState state)
    {
        bool isOver = state == RadioMarkState.Over;

        switch (radius)
        {
            case > RING_INNER:
                return Bevel(dx, dy, distance, radius, isOver);

            // A dark step between frame and well, which is what makes the well read as recessed.
            case > WELL_EDGE:
                return _ringInset;
        }

        if (state == RadioMarkState.On && radius < DOT_RADIUS)
            return Dot(dx, dy, outerRadius);

        return Vector3.Lerp(
            isOver ? _wellEdgeOver : _wellEdgeColor,
            isOver ? _wellCenterOver : _wellCenter,
            1f - radius / WELL_EDGE
        );
    }

    /// <summary>The metallic frame: a corner glint, a counter-lit inner wall, and a little grain.</summary>
    private static Vector3 Bevel(float dx, float dy, float distance, float radius, bool isOver)
    {
        float facing = distance == 0f ? 0f : (dx * _lightDirection.X + dy * _lightDirection.Y) / distance;

        // 0 at the inner edge of the band, 1 at the outer.
        float acrossBand = (radius - RING_INNER) / (1f - RING_INNER);

        float lit = MathF.Pow(MathF.Max(facing, 0f), BEVEL_FALLOFF)
                    * (BEVEL_INNER_SHARE + (1f - BEVEL_INNER_SHARE) * acrossBand);

        var color = Vector3.Lerp(isOver ? _ringBaseOver : _ringBase, _ringHighlight, lit);

        float rim = MathF.Pow(MathF.Max(-facing, 0f), RIM_FALLOFF) * (1f - acrossBand) * RIM_STRENGTH;

        return Vector3.Lerp(color, _ringRim, rim) + new Vector3(Grain(dx, dy));
    }

    /// <summary>The selected dot, lit from the top so it reads as a domed gem rather than a flat disc.</summary>
    private static Vector3 Dot(float dx, float dy, float outerRadius)
    {
        float span = DOT_RADIUS * outerRadius;

        // Biased by x as well as y, so the highlight sits up and slightly left rather than dead centre.
        float down = Math.Clamp((dy + dx * 0.25f) / (span * 2f) + 0.5f, 0f, 1f);

        return down < DOT_SPECULAR_SPAN
            ? Vector3.Lerp(_dotSpecular, _dotMid, down / DOT_SPECULAR_SPAN)
            : Vector3.Lerp(_dotMid, _dotLow, (down - DOT_SPECULAR_SPAN) / (1f - DOT_SPECULAR_SPAN));
    }

    /// <summary>
    ///     Deterministic value noise, so the frame has the check box's brushed-metal speckle and every
    ///     run produces the identical texture.
    /// </summary>
    private static float Grain(float dx, float dy)
    {
        int hash = HashCode.Combine((int)MathF.Round(dx * 3f), (int)MathF.Round(dy * 3f));

        return ((hash & 0xFF) / 255f - 0.5f) * 2f * GRAIN;
    }

    /// <summary>A palette entry, written in the 0-255 terms the check box art was sampled in.</summary>
    private static Vector3 Rgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f);

    /// <summary>Packs a sample into a texel.</summary>
    /// <remarks>
    ///     Premultiplied: the channels are scaled by the alpha, which is what the sprite batch expects
    ///     and what keeps a partly covered edge pixel from fringing against the page behind it.
    /// </remarks>
    /// <param name="rgb">The averaged colour, 0-1 per channel.</param>
    /// <param name="alpha">Coverage of this texel, 0-1.</param>
    /// <returns>The texel.</returns>
    private static Color ToColor(Vector3 rgb, float alpha) => new(
        Math.Clamp(rgb.X, 0f, 1f) * alpha,
        Math.Clamp(rgb.Y, 0f, 1f) * alpha,
        Math.Clamp(rgb.Z, 0f, 1f) * alpha,
        alpha
    );

    #endregion
}
