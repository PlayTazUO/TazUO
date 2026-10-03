using System;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace ClassicUO.Renderer.Arts
{
    public sealed class Art
    {
        private readonly SpriteInfo[] _spriteInfos;
        private readonly TextureAtlas _atlas;
        private readonly PixelPicker _picker = new PixelPicker(true);
        private readonly Rectangle[] _realArtBounds;
        private readonly ArtLoader _artLoader;
        private readonly HuesLoader _huesLoader;

        public Art(ArtLoader artLoader, HuesLoader huesLoader, GraphicsDevice device)
        {
            _artLoader = artLoader;
            _huesLoader = huesLoader;
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[_artLoader.File.Entries.Length];
            _realArtBounds = new Rectangle[_spriteInfos.Length];
        }

        public ref readonly SpriteInfo GetLand(uint idx)
            => ref Get((uint)(idx & ~0x4000));

        public ref readonly SpriteInfo GetArt(uint idx)
            => ref Get(idx + 0x4000);

        public ArtInfo GetArtPixels(uint idx)
        {
            uint artIdx = idx + 0x4000;
            uint loadedIdx = artIdx;
            ArtInfo artInfo = LoadSourceArtInfo(artIdx, out bool loadedFromPNG);

            if (artInfo.Pixels.IsEmpty && artIdx > 0)
            {
                loadedIdx = 0;
                artInfo = LoadSourceArtInfo(0, out loadedFromPNG);
            }

            if (loadedFromPNG)
            {
                ExternalImageLoader.Instance.ClearArtPixelCache(loadedIdx);
            }

            return artInfo;
        }

        /// <summary>
        ///     Applies a UO hue to an art graphic on the CPU and returns the result as RGBA8888 pixels,
        ///     trimmed to the graphic's real bounds.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Mirrors what the hue pixel shader does per texel, for consumers that cannot run it —
        ///         chiefly UI toolkits drawing through their own <c>SpriteBatch</c>, which can only flat-multiply
        ///         a tint over the whole sprite and would therefore dye parts the shader leaves alone.
        ///     </para>
        ///     <para>
        ///         Reads pixels straight from the art loader, so there is no GPU readback. Cost is one pass over
        ///         the trimmed sprite; callers that hue repeatedly should cache the result rather than re-bake.
        ///     </para>
        /// </remarks>
        /// <param name="graphic">Art graphic ID, without the 0x4000 offset.</param>
        /// <param name="hue">UO hue to apply. 0, or an out-of-range hue, returns the source pixels unchanged.</param>
        /// <param name="partialHue">
        ///     When true only true-gray texels take the hue and everything else passes through, matching the
        ///     shader's partial-hue mode. Callers normally source this from <c>TileData.StaticData[graphic].IsPartialHue</c>,
        ///     which this class cannot reach on its own.
        /// </param>
        /// <param name="bounds">Receives the real (trimmed) bounds. The returned buffer is exactly this size.</param>
        /// <returns>Row-major RGBA8888 pixels, or an empty array when the graphic has no usable art.</returns>
        public uint[] GetHuedArtPixels(uint graphic, ushort hue, bool partialHue, out Rectangle bounds)
        {
            // Real bounds are only computed while building the atlas entry, so make sure that ran first;
            // for an already-loaded graphic this is just a cache hit.
            _ = GetArt(graphic);

            bounds = GetRealArtBounds(graphic);

            ArtInfo artInfo = GetArtPixels(graphic);

            // A PNG override can swap the art out between the atlas build and now, so never trust the
            // cached bounds to still fit the pixel buffer.
            if (artInfo.Pixels.IsEmpty
                || bounds.Width <= 0
                || bounds.Height <= 0
                || bounds.Right > artInfo.Width
                || bounds.Bottom > artInfo.Height)
            {
                bounds = Rectangle.Empty;

                return [];
            }

            return HueBaking.Bake(artInfo.Pixels, artInfo.Width, bounds, hue, partialHue, _huesLoader);
        }

        private ArtInfo LoadSourceArtInfo(uint idx, out bool loadedFromPNG)
        {
            ArtInfo artInfo = ExternalImageLoader.Instance.LoadArtTexture(idx);
            loadedFromPNG = !artInfo.Pixels.IsEmpty;

            if (artInfo.Pixels.IsEmpty)
            {
                artInfo = _artLoader.GetArt(idx);
            }

            return artInfo;
        }

        private ref readonly SpriteInfo Get(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref SpriteInfo spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                ArtInfo artInfo = LoadSourceArtInfo(idx, out bool loadedFromPNG);

                if (artInfo.Pixels.IsEmpty && idx > 0)
                {
                    // Trying to load a texture that does not exist in the client MULs
                    // Degrading gracefully and only crash if not even the fallback ItemID exists
                    Log.Error(
                        $"Texture not found for sprite: idx: {idx}; itemid: {(idx > 0x4000 ? idx - 0x4000 : '-')}"
                    );
                    return ref Get(0); // ItemID of "UNUSED" placeholder
                }

                if (!artInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        artInfo.Pixels,
                        artInfo.Width,
                        artInfo.Height,
                        out spriteInfo.UV
                    );

                    // Clear the pixel cache from PNG Loader since it's now in the atlas
                    if (loadedFromPNG)
                    {
                        ExternalImageLoader.Instance.ClearArtPixelCache(idx);
                    }

                    if (idx > 0x4000)
                    {
                        idx -= 0x4000;
                        _picker.Set(idx, artInfo.Width, artInfo.Height, artInfo.Pixels);

                        int pos1 = 0;
                        int minX = artInfo.Width,
                            minY = artInfo.Height,
                            maxX = 0,
                            maxY = 0;

                        for (int y = 0; y < artInfo.Height; ++y)
                        {
                            for (int x = 0; x < artInfo.Width; ++x)
                            {
                                if (artInfo.Pixels[pos1++] != 0)
                                {
                                    minX = Math.Min(minX, x);
                                    maxX = Math.Max(maxX, x);
                                    minY = Math.Min(minY, y);
                                    maxY = Math.Max(maxY, y);
                                }
                            }
                        }

                        _realArtBounds[idx] = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                    }
                }
            }

            return ref spriteInfo;
        }

        public unsafe IntPtr CreateCursorSurfacePtr(
            int index,
            ushort customHue,
            out int hotX,
            out int hotY
        )
        {
            hotX = hotY = 0;

            ArtInfo artInfo = _artLoader.GetArt((uint)(index + 0x4000));

            if (artInfo.Pixels.IsEmpty)
            {
                return IntPtr.Zero;
            }

            fixed (uint* ptr = artInfo.Pixels)
            {
                var surface = (SDL.SDL_Surface*)SDL.SDL_CreateSurfaceFrom(artInfo.Width, artInfo.Height, SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888, (IntPtr)ptr, 4 * artInfo.Width);
                // SDL2:
                // SDL.SDL_Surface* surface = (SDL.SDL_Surface*)
                //     SDL.SDL_CreateRGBSurfaceWithFormatFrom(
                //         (IntPtr)ptr,
                //         artInfo.Width,
                //         artInfo.Height,
                //         32,
                //         4 * artInfo.Width,
                //         SDL.SDL_PIXELFORMAT_ABGR8888
                //     );

                int stride = surface->pitch >> 2;
                uint* pixels_ptr = (uint*)surface->pixels;
                uint* p_line_end = pixels_ptr + artInfo.Width;
                uint* p_img_end = pixels_ptr + stride * artInfo.Height;
                int delta = stride - artInfo.Width;
                short curX = 0;
                short curY = 0;
                Color c = default;

                while (pixels_ptr < p_img_end)
                {
                    curX = 0;

                    while (pixels_ptr < p_line_end)
                    {
                        if (*pixels_ptr != 0 && *pixels_ptr != 0xFF_00_00_00)
                        {
                            if (curX >= artInfo.Width - 1 || curY >= artInfo.Height - 1)
                            {
                                *pixels_ptr = 0;
                            }
                            else if (curX == 0 || curY == 0)
                            {
                                if (*pixels_ptr == 0xFF_00_FF_00)
                                {
                                    if (curX == 0)
                                    {
                                        hotY = curY;
                                    }

                                    if (curY == 0)
                                    {
                                        hotX = curX;
                                    }
                                }

                                *pixels_ptr = 0;
                            }
                            else if (customHue > 0)
                            {
                                c.PackedValue = *pixels_ptr;
                                *pixels_ptr =
                                    _huesLoader.ApplyHueRgba8888(HuesHelper.Color32To16(*pixels_ptr), customHue);

                                     /*HuesHelper.Color16To32(
                                         _huesLoader.GetColor16(
                                             HuesHelper.ColorToHue(c),
                                             customHue
                                         )
                                     ) | 0xFF_00_00_00;*/
                            }
                        }

                        ++pixels_ptr;

                        ++curX;
                    }

                    pixels_ptr += delta;
                    p_line_end += stride;

                    ++curY;
                }

                return (IntPtr)surface;
            }
        }

        public Rectangle GetRealArtBounds(uint idx) =>
            idx < 0 || idx >= _realArtBounds.Length
                ? Rectangle.Empty
                : _realArtBounds[idx];

        public bool PixelCheck(uint idx, int x, int y, double scale = 1f) => _picker.Get(idx, x, y, scale: scale);
    }
}
