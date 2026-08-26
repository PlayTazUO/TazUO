using System;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

/// <summary>
/// Displays a region of the world map (radar colors) as a texture. The texture is
/// sized to the requested region only and is owned by the control, so it is released
/// when the control is disposed.
/// </summary>
public class RenderedMapArea : Control
{
    private readonly int _mapIndex;
    private readonly Rectangle _mapRenderArea;
    private readonly object _sync = new();
    private volatile Texture2D _texture;

    public RenderedMapArea(int mapIndex, Rectangle mapRenderArea, int x, int y, int width, int height)
    {
        var maps = Client.Game.UO.FileManager.Maps;
        int maxMapIndex = maps.MapsDefaultSize.GetLength(0);

        if (mapIndex < 0 || mapIndex >= maxMapIndex)
        {
            mapIndex = 0;
            Log.Warn("Invalid map index for RenderedMapArea, using map 0");
        }

        _mapIndex = mapIndex;

        int realWidth = maps.MapsDefaultSize[_mapIndex, 0];
        int realHeight = maps.MapsDefaultSize[_mapIndex, 1];

        int areaX = Math.Clamp(mapRenderArea.X, 0, realWidth);
        int areaY = Math.Clamp(mapRenderArea.Y, 0, realHeight);
        int areaWidth = Math.Min(mapRenderArea.Width, realWidth - areaX);
        int areaHeight = Math.Min(mapRenderArea.Height, realHeight - areaY);

        _mapRenderArea = new Rectangle(areaX, areaY, areaWidth, areaHeight);

        X = x;
        Y = y;
        Width = width;
        Height = height;
        CanMove = true;
        AcceptMouseInput = true;

        if (areaWidth <= 0 || areaHeight <= 0)
        {
            return;
        }

        maps.LoadMap(_mapIndex);

        var texture = new Texture2D(Client.Game.GraphicsDevice, areaWidth, areaHeight, false, SurfaceFormat.Color);
        _ = Task.Run(() => LoadMapTexture(texture));

        Log.Debug($"Rendering map -{_mapIndex}- [Control data: {x}, {y}, {width}, {height}.] [Map area requested: {mapRenderArea.Left}, {mapRenderArea.Top}, {mapRenderArea.Right}, {mapRenderArea.Bottom}]");
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y))
        {
            return false;
        }

        Texture2D texture = _texture;

        if (texture == null)
        {
            return false;
        }

        // The texture holds exactly the clamped requested region, so its full bounds
        // are always a valid source rect.
        batcher.Draw(texture, new Rectangle(x, y, Width, Height), texture.Bounds, ShaderHueTranslator.GetHueVector(0, false, Alpha));

        return true;
    }

    public override void Dispose()
    {
        base.Dispose();

        lock (_sync)
        {
            _disposed = true;
            _texture?.Dispose();
        }
    }

    private bool _disposed;

    private unsafe void LoadMapTexture(Texture2D texture)
    {
        bool published = false;

        try
        {
            int regionWidth = _mapRenderArea.Width;
            int regionHeight = _mapRenderArea.Height;

            // One extra row of Z data below the region so the bottom edge can be
            // hill-shaded too; it is not part of the texture.
            uint[] buffer = new uint[regionWidth * regionHeight];
            sbyte[] allZ = new sbyte[regionWidth * (regionHeight + 1)];

            HuesLoader huesLoader = Client.Game.UO.FileManager.Hues;
            var maps = Client.Game.UO.FileManager.Maps;

            int tileX0 = _mapRenderArea.X;
            int tileY0 = _mapRenderArea.Y;
            int tileX1 = _mapRenderArea.Right;
            int tileY1 = Math.Min(_mapRenderArea.Bottom + 1, maps.MapsDefaultSize[_mapIndex, 1]);

            int bx0 = tileX0 >> 3;
            int by0 = tileY0 >> 3;
            int bx1 = (tileX1 - 1) >> 3;
            int by1 = (tileY1 - 1) >> 3;

            for (int bx = bx0; bx <= bx1; ++bx)
            {
                int mapX = bx << 3;

                for (int by = by0; by <= by1; ++by)
                {
                    int mapY = by << 3;

                    ref IndexMap indexMap = ref maps.GetIndex(_mapIndex, bx, by);

                    if (indexMap.MapAddress == 0 || indexMap.MapFile == null)
                    {
                        continue;
                    }

                    // MapAddress is an offset into the memory-mapped map file, not a
                    // pointer. Reading through the reader is the only safe access.
                    MapCellsArray cells = indexMap.MapFile.ReadAt<MapBlock>((long)indexMap.MapAddress).Cells;

                    for (int y = 0; y < 8; ++y)
                    {
                        int ty = mapY + y;

                        if (ty < tileY0 || ty >= tileY1)
                        {
                            continue;
                        }

                        int zRow = (ty - tileY0) * regionWidth;

                        for (int x = 0; x < 8; ++x)
                        {
                            int tx = mapX + x;

                            if (tx < tileX0 || tx >= tileX1)
                            {
                                continue;
                            }

                            int pos = (y << 3) | x;
                            int zIdx = zRow + (tx - tileX0);

                            allZ[zIdx] = cells[pos].Z;

                            if (ty < _mapRenderArea.Bottom)
                            {
                                ushort color = (ushort)(0x8000 | huesLoader.GetRadarColorData(cells[pos].TileID & 0x3FFF));

                                buffer[zIdx] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                            }
                        }
                    }

                    if (indexMap.StaticFile != null && indexMap.StaticAddress != 0)
                    {
                        int count = (int)indexMap.StaticCount;

                        for (int c = 0; c < count; ++c)
                        {
                            StaticsBlock sb = indexMap.StaticFile.ReadAt<StaticsBlock>((long)indexMap.StaticAddress + c * sizeof(StaticsBlock));

                            if (sb.Color == 0 || sb.Color == 0xFFFF || !GameObject.CanBeDrawn(World.Instance, sb.Color))
                            {
                                continue;
                            }

                            int tx = mapX + sb.X;
                            int ty = mapY + sb.Y;

                            if (tx < tileX0 || tx >= tileX1 || ty < tileY0 || ty >= tileY1)
                            {
                                continue;
                            }

                            int zIdx = (ty - tileY0) * regionWidth + (tx - tileX0);

                            if (sb.Z < allZ[zIdx])
                            {
                                continue;
                            }

                            allZ[zIdx] = sb.Z;

                            if (ty < _mapRenderArea.Bottom)
                            {
                                ushort color = (ushort)(0x8000 | (sb.Hue != 0 ? huesLoader.GetHueColorRgba5551(16, sb.Hue) : huesLoader.GetRadarColorData(sb.Color + 0x4000)));

                                buffer[zIdx] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                            }
                        }
                    }
                }
            }

            // Hill shading: darken/brighten each tile relative to the tile directly below it.
            const float MAG_0 = 80f / 100f;
            const float MAG_1 = 100f / 80f;

            // Rows whose below-neighbor is inside the map. The bottom row is only shaded
            // when the extra Z row is available (region does not touch the map's bottom edge).
            int shadeRows = Math.Min(regionHeight, maps.MapsDefaultSize[_mapIndex, 1] - tileY0 - 1);

            for (int ry = 0; ry < shadeRows; ++ry)
            {
                int row0 = ry * regionWidth;
                int row1 = row0 + regionWidth;

                for (int rx = 0; rx < regionWidth; ++rx)
                {
                    sbyte z0 = allZ[row0 + rx];
                    sbyte z1 = allZ[row1 + rx];

                    if (z0 == z1)
                    {
                        continue;
                    }

                    ref uint cc = ref buffer[row0 + rx];

                    if (cc == 0)
                    {
                        continue;
                    }

                    byte r = (byte)(cc & 0xFF);
                    byte g = (byte)((cc >> 8) & 0xFF);
                    byte b = (byte)((cc >> 16) & 0xFF);
                    byte a = (byte)((cc >> 24) & 0xFF);

                    if (r != 0 || g != 0 || b != 0)
                    {
                        if (z0 < z1)
                        {
                            r = (byte)Math.Min(0xFF, r * MAG_0);
                            g = (byte)Math.Min(0xFF, g * MAG_0);
                            b = (byte)Math.Min(0xFF, b * MAG_0);
                        }
                        else
                        {
                            r = (byte)Math.Min(0xFF, r * MAG_1);
                            g = (byte)Math.Min(0xFF, g * MAG_1);
                            b = (byte)Math.Min(0xFF, b * MAG_1);
                        }

                        cc = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                    }
                }
            }

            fixed (uint* pixels = &buffer[0])
            {
                texture.SetDataPointerEXT(0, null, (IntPtr)pixels, sizeof(uint) * buffer.Length);
            }

            lock (_sync)
            {
                if (!_disposed)
                {
                    _texture = texture;
                    published = true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"error loading worldmap section: {ex}");
        }
        finally
        {
            if (!published)
            {
                texture.Dispose();
            }
        }
    }
}
