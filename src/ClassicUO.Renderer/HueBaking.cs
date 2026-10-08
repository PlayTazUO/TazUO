using System;
using ClassicUO.Assets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    /// <summary>
    ///     CPU-side equivalent of the hue pixel shader, for consumers that cannot run it — chiefly UI
    ///     toolkits drawing through their own <c>SpriteBatch</c>, which can only flat-multiply a tint over
    ///     a whole sprite and would therefore dye parts the shader leaves alone.
    /// </summary>
    /// <remarks>
    ///     A bake costs one pass over the sprite and allocates its result, so callers that re-hue the same
    ///     graphic repeatedly should cache rather than re-bake.
    /// </remarks>
    public static class HueBaking
    {
        /// <summary>Shades per UO hue. Every hue is a 32-entry ramp from darkest to brightest.</summary>
        public const int HUE_RAMP_LENGTH = 32;

        /// <summary>
        ///     Applies a UO hue to a sub-rectangle of a sprite's pixels.
        /// </summary>
        /// <param name="source">Row-major RGBA8888 source pixels (low byte is red, as the loaders produce).</param>
        /// <param name="sourceWidth">Stride of <paramref name="source"/>, in pixels.</param>
        /// <param name="bounds">Region of <paramref name="source"/> to bake; must lie inside it.</param>
        /// <param name="hue">UO hue, 1-based as stored in item data. 0 or out of range copies the source through.</param>
        /// <param name="partialHue">
        ///     When true only true-gray texels take the hue and everything else passes through, matching the
        ///     shader's partial-hue mode.
        /// </param>
        /// <param name="hues">Hue tables the ramp is resolved from.</param>
        /// <returns>Row-major RGBA8888 pixels, exactly <paramref name="bounds"/> in size.</returns>
        public static uint[] Bake(
            ReadOnlySpan<uint> source,
            int sourceWidth,
            Rectangle bounds,
            ushort hue,
            bool partialHue,
            HuesLoader hues
        )
        {
            Span<uint> ramp = stackalloc uint[HUE_RAMP_LENGTH];
            bool hasRamp = TryFillHueRamp(hue, ramp, hues);
            uint[] baked = new uint[bounds.Width * bounds.Height];

            for (int y = 0; y < bounds.Height; y++)
            {
                int sourceRow = (bounds.Y + y) * sourceWidth + bounds.X;
                int targetRow = y * bounds.Width;

                for (int x = 0; x < bounds.Width; x++)
                {
                    uint pixel = source[sourceRow + x];
                    baked[targetRow + x] = hasRamp ? ApplyHue(pixel, partialHue, ramp) : pixel;
                }
            }

            return baked;
        }

        /// <summary>
        ///     Expands a UO hue into its 32 RGBA8888 shades.
        /// </summary>
        /// <remarks>
        ///     Hoisted out of the per-pixel loop: the ramp is fixed for the whole sprite, so resolving the
        ///     hue file's group/entry indirection once beats repeating it per texel.
        /// </remarks>
        /// <param name="hue">UO hue, 1-based as stored in item data.</param>
        /// <param name="ramp">Receives the shades, darkest first. Must be <see cref="HUE_RAMP_LENGTH"/> long.</param>
        /// <param name="hues">Hue tables to read from.</param>
        /// <returns>False when the hue is 0 or out of range, meaning no recoloring should happen.</returns>
        public static bool TryFillHueRamp(ushort hue, Span<uint> ramp, HuesLoader hues)
        {
            // Inclusive upper bound, unlike HuesLoader's own accessors: those take a 0-based index, this takes
            // the 1-based wire hue, so HuesCount is the last valid one rather than one past the end.
            if (hue == 0 || hue > hues.HuesCount)
                return false;

            // Hues are 1-based on the wire but the file stores them packed 8 per group.
            hue -= 1;

            int group = hue >> 3;
            int entry = hue % 8;

            for (int i = 0; i < ramp.Length; i++)
                ramp[i] = HuesHelper.Color16To32(hues.HuesRange[group].Entries[entry].ColorTable[i]);

            return true;
        }

        /// <summary>
        ///     Recolors a single RGBA8888 texel through a hue ramp, matching the shader's per-texel behaviour.
        /// </summary>
        /// <param name="pixel">Source texel, RGBA8888 (low byte is red, as produced by the loaders).</param>
        /// <param name="partialHue">True to recolor only true-gray texels.</param>
        /// <param name="ramp">The 32 shades of the target hue, as filled by <see cref="TryFillHueRamp"/>.</param>
        /// <returns>The recolored texel, preserving the source alpha.</returns>
        public static uint ApplyHue(uint pixel, bool partialHue, ReadOnlySpan<uint> ramp)
        {
            uint alpha = pixel & 0xFF00_0000;

            // The shader discards fully transparent texels; keep them cleared rather than hueing garbage.
            if (alpha == 0)
                return 0;

            byte red = (byte)pixel;
            byte green = (byte)(pixel >> 8);
            byte blue = (byte)(pixel >> 16);

            // Partial hue recolors only the gray parts of a sprite and passes colored parts through untouched.
            // There is no mask asset behind this - "is this pixel gray?" IS the mask, which is exactly how the
            // dye tub keeps its brown wood while only the liquid takes the hue.
            if (partialHue && (red != green || red != blue))
                return pixel;

            // Sprites are authored as grayscale where hues apply, so the red channel doubles as the shade index:
            // >> 3 rescales 0-255 down to the ramp's 0-31. Take RGB from the ramp, keep the texel's own alpha.
            return (ramp[red >> 3] & 0x00FF_FFFF) | alpha;
        }
    }
}
