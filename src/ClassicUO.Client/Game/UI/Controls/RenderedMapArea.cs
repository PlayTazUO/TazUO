using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls;

public class RenderedMapArea : Control
{
    const int OFFSET_PIX = 0;
    const int OFFSET_PIX_HALF = 0;

    private readonly int _mapIndex;
    private readonly Rectangle _mapRenderArea;
    private static readonly ConcurrentDictionary<int, Texture2D> _textureCache = new();

    public RenderedMapArea(int mapIndex, Rectangle mapRenderArea, int x, int y, int width, int height)
    {
        _mapIndex = mapIndex;
        _mapRenderArea = mapRenderArea;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        CanMove = true;
        AcceptMouseInput = true;
        _ = LoadMapTexture(mapIndex);

        Log.Debug($"Rendering map -{mapIndex}- [Control data: {x}, {y}, {width}, {height}.] [Map area requested: {mapRenderArea.Left}, {mapRenderArea.Top}, {mapRenderArea.Right}, {mapRenderArea.Bottom}]");
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (!base.Draw(batcher, x, y)) return false;

        if (!_textureCache.TryGetValue(_mapIndex, out Texture2D _texture)) return false;

        batcher.Draw(_texture, new Rectangle(x, y, Width, Height), new Rectangle(_mapRenderArea.Left, _mapRenderArea.Top, _mapRenderArea.Width, _mapRenderArea.Height), ShaderHueTranslator.GetHueVector(0, false, Alpha));

        return true;
    }
    private static async Task LoadMapTexture(int mapIndex)
    {
        if (_textureCache.ContainsKey(mapIndex)) return;

        Client.Game.UO.FileManager.Maps.LoadMap(mapIndex); //Make sure the map is loaded

        int realWidth = Client.Game.UO.FileManager.Maps.MapsDefaultSize[mapIndex, 0];
        int realHeight = Client.Game.UO.FileManager.Maps.MapsDefaultSize[mapIndex, 1];

        uint[] _pixelBuffer = new uint[(realWidth + OFFSET_PIX) * (realHeight + OFFSET_PIX)];
        sbyte[] _zBuffer = new sbyte[(realWidth + OFFSET_PIX) * (realHeight + OFFSET_PIX)];

        int fixedWidth = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
        int fixedHeight = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];

        var _mapTexture = new Texture2D(Client.Game.GraphicsDevice, realWidth + OFFSET_PIX, realHeight + OFFSET_PIX, false, SurfaceFormat.Color);

        await Task.Run(() =>
        {
            try
            {
                unsafe
                {
                    sbyte[] allZ = _zBuffer;
                    uint[] buffer = _pixelBuffer;

                    buffer.AsSpan().Fill(0);

                    fixed (uint* pixels = &buffer[0])
                    {
                        _mapTexture.SetDataPointerEXT(0, null, (IntPtr)pixels, sizeof(uint) * _mapTexture.Width * _mapTexture.Height);
                    }

                    HuesLoader huesLoader = Client.Game.UO.FileManager.Hues;

                    int bx, by, mapX = 0, mapY = 0, x, y;

                    for (bx = 0; bx < fixedWidth; ++bx)
                    {
                        mapX = bx << 3;

                        for (by = 0; by < fixedHeight; ++by)
                        {
                            ref IndexMap indexMap = ref Client.Game.UO.FileManager.Maps.GetIndex(mapIndex, bx, by);

                            if (indexMap.MapAddress == 0)
                            {
                                continue;
                            }

                            var mapBlock = (MapBlock*)indexMap.MapAddress;
                            var cells = (MapCells*)&mapBlock->Cells;

                            mapY = by << 3;

                            for (y = 0; y < 8; ++y)
                            {
                                int block = (mapY + y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;

                                int pos = y << 3;

                                for (x = 0; x < 8; ++x, ++pos, ++block)
                                {
                                    ushort color = (ushort)(0x8000 | huesLoader.GetRadarColorData(cells[pos].TileID & 0x3FFF));

                                    buffer[block] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                                    allZ[block] = cells[pos].Z;
                                }
                            }


                            var sb = (StaticsBlock*)indexMap.StaticAddress;

                            if (sb != null)
                            {
                                int count = (int)indexMap.StaticCount;

                                for (int c = 0; c < count; ++c, ++sb)
                                {
                                    if (sb->Color != 0 && sb->Color != 0xFFFF && GameObject.CanBeDrawn(World.Instance, sb->Color))
                                    {
                                        int block = (mapY + sb->Y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + sb->X + OFFSET_PIX_HALF;

                                        if (sb->Z >= allZ[block])
                                        {
                                            ushort color = (ushort)(0x8000 | (sb->Hue != 0 ? huesLoader.GetHueColorRgba5551(16, sb->Hue) : huesLoader.GetRadarColorData(sb->Color + 0x4000)));

                                            buffer[block] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                                            allZ[block] = sb->Z;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    int real_width_less_one = realWidth - 1;
                    int real_height_less_one = realHeight - 1;
                    const float MAG_0 = 80f / 100f;
                    const float MAG_1 = 100f / 80f;

                    for (mapY = 1; mapY < real_height_less_one; ++mapY)
                    {
                        int blockCurrent = (mapY + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + OFFSET_PIX_HALF;
                        int blockNext = (mapY + 1 + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + OFFSET_PIX_HALF;

                        for (mapX = 1; mapX < real_width_less_one; ++mapX)
                        {
                            sbyte z0 = allZ[++blockCurrent];
                            sbyte z1 = allZ[blockNext++];

                            if (z0 == z1)
                            {
                                continue;
                            }

                            ref uint cc = ref buffer[blockCurrent];

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

                    realWidth += OFFSET_PIX;
                    realHeight += OFFSET_PIX;

                    fixed (uint* pixels = &buffer[0])
                    {
                        _mapTexture.SetDataPointerEXT(0, new Rectangle(0, 0, realWidth, realHeight), (IntPtr)pixels, sizeof(uint) * realWidth * realHeight);
                    }
                    _textureCache.TryAdd(mapIndex, _mapTexture);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"error loading worldmap section: {ex}");
            }
        });
    }
}
