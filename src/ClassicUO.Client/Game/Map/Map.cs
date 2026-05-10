// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ClassicUO.Game.Map
{
    public sealed class Map
    {
        private static Chunk[] _terrainChunks;
        private static readonly bool[] _blockAccessList = new bool[0x1000];
        private readonly LinkedList<int> _usedIndices = new LinkedList<int>();
        private readonly World _world;
        private static readonly HashSet<int> _pendingBlocks = new();
        private static readonly ConcurrentQueue<(int block, Chunk chunk)> _loadedChunksQueue = new();
        internal static readonly object MapFileIOLock = new object();

        // Map PNG generation for web map and caching
        private static readonly object _mapPngLock = new object();
        private static readonly Dictionary<string, string> _mapPngCache = new Dictionary<string, string>();
        private static int _mapPngGenerating = 0;
        private static string _mapsCachePath;


        public Map(World world, int index)
        {
            _world = world;
            Index = index;
            BlocksCount = Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 0] * Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 1];

            if (_terrainChunks == null || BlocksCount > _terrainChunks.Length)
                _terrainChunks = new Chunk[BlocksCount];

            ClearBockAccess();
        }

        public readonly int BlocksCount;
        public readonly int Index;


        public Chunk GetChunk(int block)
        {
            if (block >= 0 && block < BlocksCount)
            {
                return _terrainChunks[block];
            }

            return null;
        }

        public Chunk GetChunk(int x, int y, bool load = true)
        {
            if (x < 0 || y < 0)
            {
                return null;
            }

            int cellX = x >> 3;
            int cellY = y >> 3;

            return GetChunk2(cellX, cellY, load);
        }

        public Chunk GetChunk2(int chunkX, int chunkY, bool load = true)
        {
            // ProcessLoadedChunks() is now called once per frame in World.Update
            // instead of on every GetChunk call to avoid hot path overhead

            int block = GetBlock(chunkX, chunkY);

            if (block >= BlocksCount || block >= _terrainChunks.Length)
            {
                return null;
            }

            ref Chunk chunk = ref _terrainChunks[block];

            if (chunk == null)
            {
                if (!load)
                {
                    return null;
                }

                LinkedListNode<int> node = _usedIndices.AddLast(block);
                chunk = Chunk.Create(_world, chunkX, chunkY);
                chunk.Load(Index);
                chunk.Node = node;
            }
            else if (chunk.IsDestroyed)
            {
                // make sure node is clear
                if (chunk.Node != null && (chunk.Node.Previous != null || chunk.Node.Next != null))
                {
                    chunk.Node.List?.Remove(chunk.Node);
                }

                LinkedListNode<int> node = _usedIndices.AddLast(block);
                chunk.X = chunkX;
                chunk.Y = chunkY;
                chunk.Load(Index);
                chunk.Node = node;
            }

            chunk.LastAccessTime = Time.Ticks;

            return chunk;
        }

        /// <summary>
        /// Processes chunks that were loaded asynchronously.
        /// This should only be called from the main thread.
        /// Called once per frame from World.Update to avoid overhead in GetChunk hot path.
        /// </summary>
        internal void ProcessLoadedChunks()
        {
            while (_loadedChunksQueue.TryDequeue(out (int block, Chunk chunk) item))
            {
                int block = item.block;
                Chunk loadedChunk = item.chunk;

                if (block >= BlocksCount || block >= _terrainChunks.Length)
                {
                    continue;
                }

                ref Chunk existingChunk = ref _terrainChunks[block];

                // Only place the chunk if the slot is still empty or destroyed
                if (existingChunk == null || existingChunk.IsDestroyed)
                {
                    // Add to the used indices list
                    LinkedListNode<int> node = _usedIndices.AddLast(block);
                    loadedChunk.Node = node;
                    loadedChunk.LastAccessTime = Time.Ticks;

                    _terrainChunks[block] = loadedChunk;
                }
                // If a chunk already exists and is valid, discard the loaded chunk
                // (this can happen if sync loading occurred while async was in progress)
            }
        }

        public Chunk PreloadChunk(int x, int y)
        {
            if (x < 0 || y < 0)
                return null;

            int cellX = x >> 3;
            int cellY = y >> 3;

            return PreloadChunk2(cellX, cellY);
        }

        public Chunk PreloadChunk2(int chunkx, int chunky)
        {
            // First, process any chunks that were loaded asynchronously
            ProcessLoadedChunks();

            int block = GetBlock(chunkx, chunky);

            if (block >= BlocksCount || block >= _terrainChunks.Length)
            {
                return null;
            }

            ref Chunk chunk = ref _terrainChunks[block];

            // If chunk is already loaded and valid, return it
            if (chunk is { IsDestroyed: false })
            {
                chunk.LastAccessTime = Time.Ticks;
                return chunk;
            }

            // Try to add to pending blocks (thread-safe)
            lock (_pendingBlocks)
            {
                if (!_pendingBlocks.Add(block))
                {
                    // Already being loaded, return null
                    return null;
                }
            }

            // Start async load
            _ = AsyncGetChunk(chunkx, chunky, block);

            return null;
        }

        private Task AsyncGetChunk(int chunkX, int chunkY, int block)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    // Create a new chunk completely independently
                    var chunk = Chunk.Create(_world, chunkX, chunkY, isAsync: true);
                    chunk.Load(Index);

                    // Queue the loaded chunk for the main thread to process
                    _loadedChunksQueue.Enqueue((block, chunk));
                }
                finally
                {
                    // Remove from pending blocks
                    lock (_pendingBlocks)
                    {
                        _pendingBlocks.Remove(block);
                    }
                }
            });

            return task;
        }

        public GameObject GetTile(int x, int y, bool load = true) => GetChunk(x, y, load)?.GetHeadObject(x % 8, y % 8);

        public sbyte GetTileZ(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return -125;
            }

            ref IndexMap blockIndex = ref GetIndex(x >> 3, y >> 3);

            if (!blockIndex.IsValid())
            {
                return -125;
            }

            int mx = x % 8;
            int my = y % 8;

            return blockIndex.MapFile.ReadAt<MapBlock>((long)blockIndex.MapAddress).Cells[(my << 3) + mx].Z;
        }

        public void GetMapZ(int x, int y, out sbyte groundZ, out sbyte staticZ)
        {
            Chunk chunk = GetChunk(x, y);
            //var obj = GetTile(x, y);
            groundZ = staticZ = 0;

            if (chunk == null)
            {
                return;
            }

            GameObject obj = chunk.Tiles[x % 8, y % 8];

            while (obj != null)
            {
                if (obj is Land)
                {
                    groundZ = obj.Z;
                }
                else if (staticZ < obj.Z)
                {
                    staticZ = obj.Z;
                }

                obj = obj.TNext;
            }
        }

        public void ClearBockAccess() => _blockAccessList.AsSpan().Fill(false);

        public sbyte CalculateNearZ(sbyte defaultZ, int x, int y, int z)
        {
            ref bool access = ref _blockAccessList[(x & 0x3F) + ((y & 0x3F) << 6)];

            if (access)
            {
                return defaultZ;
            }

            access = true;
            Chunk chunk = GetChunk(x, y, false);

            if (chunk != null)
            {
                GameObject obj = chunk.Tiles[x % 8, y % 8];

                for (; obj != null; obj = obj.TNext)
                {
                    if (!(obj is Static) && !(obj is Multi))
                    {
                        continue;
                    }

                    if (obj.Graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                    {
                        continue;
                    }

                    if (!Client.Game.UO.FileManager.TileData.StaticData[obj.Graphic].IsRoof || Math.Abs(z - obj.Z) > 6)
                    {
                        continue;
                    }

                    break;
                }

                if (obj == null)
                {
                    return defaultZ;
                }

                sbyte tileZ = obj.Z;

                if (tileZ < defaultZ)
                {
                    defaultZ = tileZ;
                }

                defaultZ = CalculateNearZ(defaultZ, x - 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x + 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y - 1, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y + 1, tileZ);
            }

            return defaultZ;
        }


        public ref IndexMap GetIndex(int blockX, int blockY)
        {
            int block = GetBlock(blockX, blockY);
            int map = Index;
            Client.Game.UO.FileManager.Maps.SanitizeMapIndex(ref map);
            IndexMap[] list = Client.Game.UO.FileManager.Maps.BlockData[map];

            return ref block >= list.Length ? ref IndexMap.Invalid : ref list[block];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBlock(int blockX, int blockY) => blockX * Client.Game.UO.FileManager.Maps.MapBlocksSize[Index, 1] + blockY;

        public IEnumerable<Chunk> GetUsedChunks()
        {
            foreach (int i in _usedIndices)
            {
                yield return GetChunk(i);
            }
        }


        public void ClearUnusedBlocks()
        {
            int count = 0;
            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            LinkedListNode<int> first = _usedIndices.First;

            while (first != null)
            {
                LinkedListNode<int> next = first.Next;

                ref Chunk block = ref _terrainChunks[first.Value];

                if (block != null && block.LastAccessTime < ticks && block.HasNoExternalData())
                {
                    block.Destroy();
                    block = null;

                    if (++count >= Constants.MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                    {
                        break;
                    }
                }

                first = next;
            }
        }

        public void Destroy()
        {
            LinkedListNode<int> first = _usedIndices.First;

            while (first != null)
            {
                LinkedListNode<int> next = first.Next;
                ref Chunk c = ref _terrainChunks[first.Value];
                c?.Destroy();
                c = null;
                first = next;
            }

            _usedIndices.Clear();
        }

        public override string ToString()
        {
            switch (Index)
            {
                default:
                case 0: return "Fel";
                case 1: return "Tram";
                case 2: return "Ilshenar";
                case 3: return "Malas";
                case 4: return "Tokuno";
                case 5: return "TerMur";
            }
        }

        #region Map PNG Generation

        /// <summary>
        /// Initializes the map cache path. Should be called once on startup.
        /// </summary>
        public static void InitializeMapPngCache()
        {
            if (_mapsCachePath == null)
            {
                _mapsCachePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");
            }
        }

        /// <summary>
        /// Generates a PNG file for the specified map index. This is CPU-intensive and should be called on a background thread.
        /// Returns the path to the generated PNG file, or null if generation failed.
        /// </summary>
        public static string GenerateMapPng(int mapIndex, Map map, World world)
        {
            if (mapIndex < 0 || mapIndex > MapLoader.MAPS_COUNT)
                return null;

            InitializeMapPngCache();

            lock (_mapPngLock)
            {
                try
                {
                    const int OFFSET_PIX = 2;
                    const int OFFSET_PIX_HALF = OFFSET_PIX / 2;

                    int realWidth = Client.Game.UO.FileManager.Maps.MapsDefaultSize[mapIndex, 0];
                    int realHeight = Client.Game.UO.FileManager.Maps.MapsDefaultSize[mapIndex, 1];

                    int fixedWidth = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
                    int fixedHeight = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];

                    FileReader mapFile = Client.Game.UO.FileManager.Maps.GetMapFile(mapIndex);
                    FileReader staticFile = Client.Game.UO.FileManager.Maps.GetStaticFile(mapIndex);

                    if (!_mapPngCache.TryGetValue(mapFile.FilePath, out string fileMapPath))
                    {
                        using var mapReader = new BinaryReader(File.Open(mapFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        using var staticsReader = new BinaryReader(File.Open(staticFile.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                        static string calculateMd5(BinaryReader file)
                        {
                            var md5Ctx = new MD5Behaviour.MD5Context();
                            MD5Behaviour.Initialize(ref md5Ctx);

                            byte[] h = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = file.Read(h)) > 0)
                                MD5Behaviour.Update(ref md5Ctx, h.AsSpan(0, bytesRead));
                            MD5Behaviour.Finalize(ref md5Ctx);

                            var strSb = new StringBuilder();
                            for (int i = 0; i < 16; ++i)
                                strSb.AppendFormat("{0:x2}", md5Ctx.Digest(i));

                            return strSb.ToString();
                        }

                        string sum = calculateMd5(mapReader) + calculateMd5(staticsReader);
                        var md5Ctx = new MD5Behaviour.MD5Context();
                        MD5Behaviour.Initialize(ref md5Ctx);
                        MD5Behaviour.Update(ref md5Ctx, MemoryMarshal.AsBytes<char>(sum));
                        MD5Behaviour.Finalize(ref md5Ctx);
                        var strSb = new StringBuilder();
                        for (int i = 0; i < 16; ++i)
                            strSb.AppendFormat("{0:x2}", md5Ctx.Digest(i));
                        string hash = strSb.ToString();

                        fileMapPath = Path.Combine(_mapsCachePath, $"map{mapIndex}_{hash}.png");
                        _mapPngCache[mapFile.FilePath] = fileMapPath;
                    }

                    if (!File.Exists(fileMapPath))
                    {
                        //Delete old map cache files
                        if (Directory.Exists(_mapsCachePath))
                            Directory.GetFiles(_mapsCachePath, "map" + mapIndex + "_*.png").ForEach(s => File.Delete(s));

                        try
                        {
                            Interlocked.Increment(ref _mapPngGenerating);

                            int size = (realWidth + OFFSET_PIX) * (realHeight + OFFSET_PIX);
                            sbyte[] allZ = new sbyte[size];
                            var staticBlocks = new StaticsBlock[32];

                            using var img = new SixLabors.ImageSharp.Image<Byte4>(new SixLabors.ImageSharp.Configuration()
                            {
                                PreferContiguousImageBuffers = true
                            }, realWidth + OFFSET_PIX, realHeight + OFFSET_PIX);

                            img.DangerousTryGetSinglePixelMemory(out Memory<Byte4> imgBuffer);
                            Span<Byte4> imgSpan = imgBuffer.Span;

                            HuesLoader huesLoader = Client.Game.UO.FileManager.Hues;

                            int bx, by, mapX = 0, mapY = 0, x, y;

                            // Workaround to avoid accessing map files from 2 sources at the same time
                            UOFile fileMap = null;
                            UOFile fileStatics = null;

                            for (bx = 0; bx < fixedWidth; ++bx)
                            {
                                mapX = bx << 3;

                                for (by = 0; by < fixedHeight; ++by)
                                {
                                    ref IndexMap indexMap = ref map.GetIndex(bx, by);

                                    if (!indexMap.IsValid())
                                    {
                                        continue;
                                    }

                                    if (fileMap == null)
                                    {
                                        fileMap = new UOFile(indexMap.MapFile.FilePath);
                                    }

                                    fileMap.Seek((long)indexMap.MapAddress, System.IO.SeekOrigin.Begin);
                                    MapCellsArray cells = fileMap.Read<MapBlock>().Cells;

                                    mapY = by << 3;

                                    for (y = 0; y < 8; ++y)
                                    {
                                        int block = (mapY + y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + OFFSET_PIX_HALF;
                                        int pos = y << 3;

                                        for (x = 0; x < 8; ++x, ++pos, ++block)
                                        {
                                            ushort color = (ushort)(0x8000 | huesLoader.GetRadarColorData(cells[pos].TileID & 0x3FFF));

                                            imgSpan[block].PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                                            allZ[block] = cells[pos].Z;
                                        }
                                    }

                                    if (fileStatics == null)
                                    {
                                        fileStatics = new UOFile(indexMap.StaticFile.FilePath);
                                    }

                                    if (fileStatics.Length == 0) //Fix for empty statics file
                                        continue;

                                    fileStatics.Seek((long)indexMap.StaticAddress, System.IO.SeekOrigin.Begin);

                                    if (staticBlocks.Length < indexMap.StaticCount)
                                        staticBlocks = new StaticsBlock[indexMap.StaticCount];

                                    Span<StaticsBlock> staticsBlocksSpan = staticBlocks.AsSpan(0, (int)indexMap.StaticCount);
                                    fileStatics.Read(MemoryMarshal.AsBytes(staticsBlocksSpan));

                                    foreach (ref StaticsBlock sb in staticsBlocksSpan)
                                    {
                                        if (sb.Color != 0 && sb.Color != 0xFFFF && GameObject.CanBeDrawn(world, sb.Color))
                                        {
                                            int block = (mapY + sb.Y + OFFSET_PIX_HALF) * (realWidth + OFFSET_PIX) + mapX + sb.X + OFFSET_PIX_HALF;

                                            if (sb.Z >= allZ[block])
                                            {
                                                ushort color = (ushort)(0x8000 | (sb.Hue != 0 ? huesLoader.GetColor16(16384, sb.Hue) : huesLoader.GetRadarColorData(sb.Color + 0x4000)));

                                                imgSpan[block].PackedValue = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                                                allZ[block] = sb.Z;
                                            }
                                        }
                                    }
                                }
                            }

                            fileMap?.Dispose();
                            fileStatics?.Dispose();

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

                                    ref Byte4 cc = ref imgSpan[blockCurrent];
                                    if (cc.PackedValue == 0)
                                    {
                                        continue;
                                    }

                                    byte r = (byte)(cc.PackedValue & 0xFF);
                                    byte g = (byte)((cc.PackedValue >> 8) & 0xFF);
                                    byte b = (byte)((cc.PackedValue >> 16) & 0xFF);
                                    byte a = (byte)((cc.PackedValue >> 24) & 0xFF);

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

                                        cc.PackedValue = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                                    }
                                }
                            }

                            var imageEncoder = new PngEncoder
                            {
                                ColorType = PngColorType.Palette,
                                CompressionLevel = PngCompressionLevel.DefaultCompression,
                                SkipMetadata = true,
                                FilterMethod = PngFilterMethod.None,
                                ChunkFilter = PngChunkFilter.ExcludeAll,
                                TransparentColorMode = PngTransparentColorMode.Clear,
                            };

                            Directory.CreateDirectory(_mapsCachePath);
                            using FileStream stream2 = File.Create(fileMapPath);
                            img.Save(stream2, imageEncoder);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"error generating map PNG: {ex}");
                            return null;
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _mapPngGenerating);
                        }
                    }

                    // Return the path to the generated PNG file
                    return fileMapPath;
                }
                catch (ThreadInterruptedException)
                {
                    _mapPngGenerating = 0;
                    return null;
                }
            }
        }

        /// <summary>
        /// Clears the map PNG cache.
        /// </summary>
        public static void ClearMapPngCache() => _mapPngCache?.Clear();

        /// <summary>
        /// Gets the lock object used for map PNG generation (for WorldMapGump texture loading).
        /// </summary>
        public static object GetMapPngLock() => _mapPngLock;

        #endregion
    }
}
