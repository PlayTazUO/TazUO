using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker(true);
        private readonly GumpsLoader _gumpsLoader;
        private readonly HuesLoader _huesLoader;

        public GumpsLoader GetGumpsLoader => _gumpsLoader;

        /// <summary>Builds the gump sprite cache and its atlas.</summary>
        /// <param name="gumpsLoader">Source of gump pixels; its entry count fixes the cache's size.</param>
        /// <param name="huesLoader">Hue tables, needed only by <see cref="GetHuedGumpPixels" />.</param>
        /// <param name="device">Device the atlas is allocated on.</param>
        public Gump(GumpsLoader gumpsLoader, HuesLoader huesLoader, GraphicsDevice device)
        {
            _gumpsLoader = gumpsLoader;
            _huesLoader = huesLoader;
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[gumpsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref SpriteInfo spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                GumpInfo gumpInfo = LoadSourceGumpInfo(idx, out bool loadedFromPNG);

                if (!gumpInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        gumpInfo.Pixels,
                        gumpInfo.Width,
                        gumpInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, gumpInfo.Width, gumpInfo.Height, gumpInfo.Pixels);

                    // Clear the pixel cache from PNG Loader since it's now in the atlas
                    if (loadedFromPNG)
                    {
                        ExternalImageLoader.Instance.ClearGumpPixelCache(idx);
                    }
                }
            }

            return ref spriteInfo;
        }

        /// <summary>
        ///     Applies a UO hue to a gump graphic on the CPU and returns the result as RGBA8888 pixels.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         See <see cref="HueBaking"/> for why a CPU bake exists at all. Gumps are untrimmed, so the
        ///         returned buffer covers the whole sprite rather than a sub-rectangle.
        ///     </para>
        ///     <para>
        ///         Reads pixels straight from the loaders, so there is no GPU readback, but the cost is one pass
        ///         over the sprite plus the allocation; callers that re-hue should cache the result.
        ///     </para>
        /// </remarks>
        /// <param name="idx">Gump graphic ID.</param>
        /// <param name="hue">UO hue to apply. 0, or an out-of-range hue, returns the source pixels unchanged.</param>
        /// <param name="partialHue">When true only true-gray texels take the hue.</param>
        /// <param name="bounds">Receives the sprite's size. The returned buffer is exactly this size.</param>
        /// <returns>Row-major RGBA8888 pixels, or an empty array when the graphic has no usable art.</returns>
        public uint[] GetHuedGumpPixels(uint idx, ushort hue, bool partialHue, out Rectangle bounds)
        {
            GumpInfo gumpInfo = LoadSourceGumpInfo(idx, out bool loadedFromPNG);

            if (gumpInfo.Pixels.IsEmpty || gumpInfo.Width <= 0 || gumpInfo.Height <= 0)
            {
                bounds = Rectangle.Empty;

                return [];
            }

            bounds = new Rectangle(0, 0, gumpInfo.Width, gumpInfo.Height);
            uint[] baked = HueBaking.Bake(gumpInfo.Pixels, gumpInfo.Width, bounds, hue, partialHue, _huesLoader);

            // The PNG loader caches what it decodes; this is a one-off read, so don't leave it held.
            if (loadedFromPNG)
            {
                ExternalImageLoader.Instance.ClearGumpPixelCache(idx);
            }

            return baked;
        }

        public bool PixelCheck(uint idx, int x, int y, double scale = 1f) => _picker.Get(idx, x, y, scale: scale);

        /// <summary>Reads a gump's source pixels, preferring a PNG override over the MUL entry.</summary>
        /// <param name="idx">Gump graphic ID.</param>
        /// <param name="loadedFromPNG">Whether the pixels came from an override, and so sit in the PNG cache.</param>
        /// <returns>The sprite's pixels, or an empty <see cref="GumpInfo"/> when neither source has it.</returns>
        private GumpInfo LoadSourceGumpInfo(uint idx, out bool loadedFromPNG)
        {
            GumpInfo gumpInfo = ExternalImageLoader.Instance.LoadGumpTexture(idx);
            loadedFromPNG = !gumpInfo.Pixels.IsEmpty;

            if (gumpInfo.Pixels.IsEmpty)
            {
                gumpInfo = _gumpsLoader.GetGump(idx);
            }

            return gumpInfo;
        }
    }
}
