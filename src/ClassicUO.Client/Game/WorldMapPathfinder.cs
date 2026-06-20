using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game
{
    /// <summary>
    /// Long-distance pathfinder designed for WorldMap click-to-walk.
    /// Reads tile walkability directly from the memory-mapped map.mul + statics.mul files;
    /// never loads chunks into game state, so it is safe to run on a background thread
    /// and cannot corrupt the renderer by loading thousands of chunks synchronously.
    ///
    /// A* with a large node budget computes the ENTIRE path upfront — no chunking, no
    /// re-planning mid-walk, no "walk forward then backward" artefacts.
    /// </summary>
    public static class WorldMapPathfinder
    {
        /// <summary>Default hard cap on A* nodes. 1M ≈ 100MB peak; reached only for truly impossible destinations.</summary>
        private const int MAX_NODES = 1_000_000;

        /// <summary>
        /// Default wall-clock cap on a single search. Prevents a hopeless query (unreachable island,
        /// wrong map, etc.) from locking out new clicks.
        /// </summary>
        private const int MAX_SEARCH_MS = 5000;

        // Heuristic weight: 5/4 = 1.25× — biased enough to cut exploration in half,
        // low enough that A* still backtracks out of dead-end rooms.
        private const int HEURISTIC_WEIGHT_NUM = 5;
        private const int HEURISTIC_WEIGHT_DEN = 4;

        /// <summary>
        /// Incremented on every FindPathAsync call so an in-flight search can detect that
        /// a newer request has arrived and bail out promptly.
        /// </summary>
        private static int _searchVersion;

        /// <summary>
        /// Tiles the server/client rejected when the walker tried to step onto them —
        /// lamp posts, rocks, placed doors, and other dynamic items not in statics.mul.
        /// Cleared at the start of each new nav session (new Ctrl+Click).
        /// ConcurrentDictionary used as a thread-safe set: main thread mutates it while
        /// an in-flight background search may read it via ContainsKey.
        /// </summary>
        private static readonly ConcurrentDictionary<long, byte> _dynamicBlocks = new();

        /// <summary>Clears all dynamically-discovered blocked tiles accumulated during the current navigation session.</summary>
        public static void ClearDynamicBlocks() => _dynamicBlocks.Clear();

        /// <summary>Add a tile to the dynamic-block set so the next search routes around it.</summary>
        public static void MarkDynamicBlock(int x, int y) => _dynamicBlocks.TryAdd(PackKey(x, y), 0);

        /// <summary>
        /// Per-search snapshot of tiles blocked by player houses.
        /// Captured on the main thread before the worker starts, then read-only for the
        /// duration of the search. Protected by _running: only one search can own this field.
        /// </summary>
        private static HashSet<long> _houseBlocks;

        // Direction layout matching ClassicUO.Game.Data.Direction:
        // 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };

        private static int _running;

        /// <summary>
        /// A single step in a computed path.
        /// <see cref="X"/> and <see cref="Y"/> are world tile coordinates;
        /// <see cref="Z"/> is elevation; <see cref="Direction"/> is an index into
        /// <c>ClassicUO.Game.Data.Direction</c> (0=N, 1=NE, … 7=NW).
        /// </summary>
        public struct PathPoint
        {
            public int X;
            public int Y;
            public int Z;
            public int Direction;
        }

        /// <summary>
        /// Shallow snapshot of a Multi component taken on the main thread.
        /// Contains only primitive fields — no references to live game objects — so it can
        /// be safely read from the background search thread.
        /// </summary>
        public struct HouseMultiSnapshot
        {
            public int X;
            public int Y;
            public sbyte Z;
            public ushort Graphic;
            public int State;
            public bool IsHousePreview;
            public bool IsDestroyed;
        }

        /// <summary>Cancels any in-flight background search without starting a new one.</summary>
        public static void Cancel() => Interlocked.Increment(ref _searchVersion);

        /// <summary>Returns <c>true</c> when a background pathfinding search is currently active.</summary>
        public static bool IsRunning => Volatile.Read(ref _running) != 0;

        /// <summary>
        /// Computes a path from (startX,startY,startZ) to (destX,destY) on the given map,
        /// asynchronously on a background thread.
        /// If the clicked tile is impassable the pathfinder snaps to the nearest walkable
        /// tile within <paramref name="snapRadius"/>.
        /// <paramref name="onComplete"/> fires on the main thread (may be an empty list on failure).
        /// Only one search runs at a time; concurrent calls return immediately with an empty list.
        /// </summary>
        public static void FindPathAsync(int mapIndex, int startX, int startY, sbyte startZ,
                                         int destX, int destY, int snapRadius,
                                         Action<List<PathPoint>> onComplete,
                                         List<HouseMultiSnapshot> houseMultis = null)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                onComplete?.Invoke(new List<PathPoint>());
                return;
            }

            int myVersion = Interlocked.Increment(ref _searchVersion);

            // Read user-configurable limits on the main thread (the background worker must not
            // touch ProfileManager). Fall back to the defaults when no profile is loaded.
            var profile = ProfileManager.CurrentProfile;
            int maxNodes = profile?.WorldMapPathfindingMaxNodes > 0 ? profile.WorldMapPathfindingMaxNodes : MAX_NODES;
            int maxSearchMs = profile?.WorldMapPathfindingTimeout > 0 ? profile.WorldMapPathfindingTimeout : MAX_SEARCH_MS;

            Task.Run(() =>
            {
                var result = new List<PathPoint>();
                try
                {
                    _houseBlocks = BuildHouseBlockSet(mapIndex, houseMultis);

                    var chunkCache = new Dictionary<long, ChunkWalkData>(1024);

                    int targetX = destX;
                    int targetY = destY;
                    bool canSearch = true;

                    if (!TryGetWalkable(mapIndex, destX, destY, startZ, chunkCache, out _))
                    {
                        if (!SnapToNearestWalkable(mapIndex, destX, destY, startZ, snapRadius, chunkCache,
                                                    out targetX, out targetY))
                        {
                            canSearch = false;
                        }
                    }

                    if (canSearch)
                    {
                        // Environment.TickCount64 is a monotonic clock safe to read from any thread.
                        // Time.Ticks is a uint game-loop counter — not volatile and wraps at ~49 days.
                        long deadline = Environment.TickCount64 + maxSearchMs;
                        result = FindPath(mapIndex, startX, startY, startZ, targetX, targetY, chunkCache, myVersion, deadline, maxNodes);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"WorldMapPathfinder: {ex.Message}");
                }
                finally
                {
                    Volatile.Write(ref _houseBlocks, null);
                    Volatile.Write(ref _running, 0);
                }

                MainThreadQueue.EnqueueAction(() => onComplete?.Invoke(result));
            });
        }

        /// <summary>
        /// Walks the caller-supplied Multi snapshots and returns a HashSet of (x,y) keys
        /// for tiles that are part of player-house ground-level footprints.
        /// Runs entirely on the worker thread.
        /// </summary>
        private static HashSet<long> BuildHouseBlockSet(int mapIndex, List<HouseMultiSnapshot> multis)
        {
            if (multis == null || multis.Count == 0)
                return null;

            var set = new HashSet<long>(multis.Count);
            var tileData = Client.Game.UO.FileManager.TileData;
            var maps = Client.Game.UO.FileManager.Maps;

            // Block the entire ground-level footprint of each house (matches OrionUO behaviour).
            // Ground-floor tiles sit at roughly landZ+7; band of 14 catches the full first
            // storey while excluding upper floors (~+20) and rooftops.
            const int GROUND_LEVEL_BAND = 14;
            const int CHMOF_IGNORE_IN_RENDER = 0x40;

            foreach (var m in multis)
            {
                if (m.IsDestroyed) continue;
                if (m.IsHousePreview) continue;
                if ((m.State & CHMOF_IGNORE_IN_RENDER) != 0) continue;

                ushort graphic = m.Graphic;
                if (graphic >= tileData.StaticData.Length) continue;

                ref StaticTiles sd = ref tileData.StaticData[graphic];
                if (sd.IsRoof) continue;

                sbyte landZ = 0;
                try
                {
                    ref IndexMap im = ref maps.GetIndex(mapIndex, m.X >> 3, m.Y >> 3);
                    if (im.IsValid())
                    {
                        MapCellsArray cells = im.MapFile.ReadAt<MapBlock>((long)im.MapAddress).Cells;
                        landZ = cells[ChunkCellIndex(m.X, m.Y)].Z;
                    }
                }
                catch { }

                if (m.Z > landZ + GROUND_LEVEL_BAND) continue;

                set.Add(PackKey(m.X, m.Y));
            }

            return set.Count == 0 ? null : set;
        }

        /// <summary>
        /// Finds the nearest walkable tile to (cx, cy) within <paramref name="radius"/>,
        /// searching in expanding Chebyshev rings.
        /// </summary>
        private static bool SnapToNearestWalkable(int mapIndex, int cx, int cy, sbyte fromZ, int radius,
                                                   Dictionary<long, ChunkWalkData> cache, out int outX, out int outY)
        {
            for (int r = 1; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (TryGetWalkable(mapIndex, cx + dx, cy - r, fromZ, cache, out _))
                    {
                        outX = cx + dx; outY = cy - r; return true;
                    }
                    if (TryGetWalkable(mapIndex, cx + dx, cy + r, fromZ, cache, out _))
                    {
                        outX = cx + dx; outY = cy + r; return true;
                    }
                }
                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    if (TryGetWalkable(mapIndex, cx - r, cy + dy, fromZ, cache, out _))
                    {
                        outX = cx - r; outY = cy + dy; return true;
                    }
                    if (TryGetWalkable(mapIndex, cx + r, cy + dy, fromZ, cache, out _))
                    {
                        outX = cx + r; outY = cy + dy; return true;
                    }
                }
            }
            outX = cx; outY = cy;
            return false;
        }

        // -----------------------------------------------------------------
        // A*
        // -----------------------------------------------------------------

        private sealed class Node
        {
            public int X;
            public int Y;
            public sbyte Z;
            public int G;
            public int F;
            public Node Parent;
            public byte Direction;
        }

        // Octile distance × 1.25 heuristic weight.
        // D_cardinal=10, D_diagonal=14. Formula: 10*(dx+dy) + (14-20)*min(dx,dy)
        // = 10*max + 4*min, which is the standard octile formula.
        private static int Heuristic(int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);
            int octile = 10 * (dx + dy) + (14 - 20) * Math.Min(dx, dy);
            return octile * HEURISTIC_WEIGHT_NUM / HEURISTIC_WEIGHT_DEN;
        }

        private static List<PathPoint> FindPath(int mapIndex, int startX, int startY, sbyte startZ,
                                                 int destX, int destY, Dictionary<long, ChunkWalkData> chunkCache,
                                                 int myVersion, long deadlineTicks, int maxNodes)
        {
            var result = new List<PathPoint>();
            var openQueue = new System.Collections.Generic.PriorityQueue<Node, int>();
            var bestG = new Dictionary<long, int>(8192);
            int nodeCount = 0;

            var startNode = new Node
            {
                X = startX, Y = startY, Z = startZ,
                G = 0, F = Heuristic(startX, startY, destX, destY),
                Parent = null, Direction = 0
            };
            openQueue.Enqueue(startNode, startNode.F);
            bestG[PackKey(startX, startY)] = 0;

            Node goal = null;

            while (openQueue.Count > 0 && nodeCount < maxNodes)
            {
                Node current = openQueue.Dequeue();
                long key = PackKey(current.X, current.Y);

                // Stale entry: PriorityQueue has no decrease-key so we push duplicates.
                if (bestG.TryGetValue(key, out int knownG) && knownG < current.G)
                    continue;

                if (current.X == destX && current.Y == destY)
                {
                    goal = current;
                    break;
                }

                nodeCount++;

                // Check cancellation / timeout every 4096 real expansions (not stale-node drains).
                if ((nodeCount & 0xFFF) == 0)
                {
                    if (Volatile.Read(ref _searchVersion) != myVersion) return result;
                    if (Environment.TickCount64 >= deadlineTicks) return result;
                }

                for (int d = 0; d < 8; d++)
                {
                    int nx = current.X + DX[d];
                    int ny = current.Y + DY[d];

                    if (nx < 0 || ny < 0) continue;

                    if (!TryGetWalkable(mapIndex, nx, ny, current.Z, chunkCache, out sbyte nz))
                        continue;

                    // Diagonal corner-cutting: both flanking cardinal tiles must be walkable.
                    if ((d & 1) == 1)
                    {
                        if (!TryGetWalkable(mapIndex, current.X + DX[d], current.Y, current.Z, chunkCache, out _) ||
                            !TryGetWalkable(mapIndex, current.X, current.Y + DY[d], current.Z, chunkCache, out _))
                            continue;
                    }

                    int stepCost = (d & 1) == 0 ? 10 : 14;
                    int tentativeG = current.G + stepCost;
                    long nKey = PackKey(nx, ny);

                    if (bestG.TryGetValue(nKey, out int existingG) && existingG <= tentativeG)
                        continue;

                    bestG[nKey] = tentativeG;
                    var neighbor = new Node
                    {
                        X = nx, Y = ny, Z = nz,
                        G = tentativeG,
                        F = tentativeG + Heuristic(nx, ny, destX, destY),
                        Parent = current,
                        Direction = (byte)d
                    };
                    openQueue.Enqueue(neighbor, neighbor.F);
                }
            }

            if (goal == null)
                return result;

            // Reconstruct path in order (goal → start via Parent, then reverse).
            var stack = new Stack<PathPoint>();
            for (Node n = goal; n != null && n.Parent != null; n = n.Parent)
            {
                stack.Push(new PathPoint { X = n.X, Y = n.Y, Z = n.Z, Direction = n.Direction });
            }
            while (stack.Count > 0)
                result.Add(stack.Pop());

            return result;
        }

        private static long PackKey(int x, int y) => ((long)(uint)x << 32) | (uint)y;

        /// <summary>
        /// Returns the flat index (0–63) into an 8×8 chunk's cell array for world
        /// coordinates <paramref name="x"/> and <paramref name="y"/>.
        /// Within-chunk offsets are <c>x &amp; 7</c> / <c>y &amp; 7</c>; the row stride is 8
        /// (implemented as a left-shift by 3 bits).
        /// </summary>
        private static int ChunkCellIndex(int x, int y) => ((y & 7) << 3) + (x & 7);

        // -----------------------------------------------------------------
        // Raw-file walkability
        // -----------------------------------------------------------------

        private sealed class ChunkWalkData
        {
            public readonly bool[] Walkable = new bool[64];
            /// <summary>Immutable land Z from map.mul. Do NOT modify with statics.</summary>
            public readonly sbyte[] LandZ = new sbyte[64];
            /// <summary>Highest walkable surface Z; grows as Surface/Bridge statics are processed.</summary>
            public readonly sbyte[] SurfaceZ = new sbyte[64];
            public bool Invalid;
        }

        private static bool TryGetWalkable(int mapIndex, int x, int y, sbyte fromZ,
                                           Dictionary<long, ChunkWalkData> cache, out sbyte surfaceZ)
        {
            long key = PackKey(x, y);

            if (_dynamicBlocks.ContainsKey(key))
            {
                surfaceZ = 0;
                return false;
            }

            var houseBlocks = _houseBlocks;
            if (houseBlocks != null && houseBlocks.Contains(key))
            {
                surfaceZ = 0;
                return false;
            }

            int cx = x >> 3;
            int cy = y >> 3;
            long chunkKey = PackKey(cx, cy);

            if (!cache.TryGetValue(chunkKey, out ChunkWalkData data))
            {
                data = LoadChunk(mapIndex, cx, cy);
                cache[chunkKey] = data;
            }

            if (data == null || data.Invalid)
            {
                surfaceZ = 0;
                return false;
            }

            int idx = ChunkCellIndex(x, y);
            surfaceZ = data.SurfaceZ[idx];
            return data.Walkable[idx];
        }

        private static ChunkWalkData LoadChunk(int mapIndex, int cx, int cy)
        {
            try
            {
                var maps = Client.Game.UO.FileManager.Maps;
                if (cx < 0 || cy < 0 || cx >= maps.MapBlocksSize[mapIndex, 0] || cy >= maps.MapBlocksSize[mapIndex, 1])
                    return new ChunkWalkData { Invalid = true };

                ref IndexMap im = ref maps.GetIndex(mapIndex, cx, cy);
                if (!im.IsValid())
                    return new ChunkWalkData { Invalid = true };

                var data = new ChunkWalkData();
                var tileData = Client.Game.UO.FileManager.TileData;

                // --- Land layer ---
                MapCellsArray cells = im.MapFile.ReadAt<MapBlock>((long)im.MapAddress).Cells;
                for (int i = 0; i < 64; i++)
                {
                    ref readonly MapCells c = ref cells[i];
                    sbyte lz = c.Z;
                    ushort landId = (ushort)(c.TileID & 0x3FFF);
                    bool landOk = landId < tileData.LandData.Length
                                  && !tileData.LandData[landId].IsImpassable;
                    data.Walkable[i] = landOk;
                    data.LandZ[i] = lz;
                    data.SurfaceZ[i] = lz;
                }

                // --- Static layer ---
                // Two-bitmap rule:
                //   hardBlocked = walls/trees/rocks/impassable furniture at ground level
                //   hasSurface  = floor/bridge static you CAN stand on (lets you cross water)
                // Final: (!hardBlocked) && (land_walkable || hasSurface)
                // Elevation rule: only statics within ~6 Z of land count (ignores overhead arches, upper floors).
                const int GROUND_LEVEL_THRESHOLD = 6;

                if (im.StaticCount > 0)
                {
                    Span<bool> hardBlocked = stackalloc bool[64];
                    Span<bool> hasSurface  = stackalloc bool[64];

                    var buf = ArrayPool<StaticsBlock>.Shared.Rent((int)im.StaticCount);
                    try
                    {
                        Span<StaticsBlock> span = buf.AsSpan(0, (int)im.StaticCount);
                        im.StaticFile.ReadAt((long)im.StaticAddress, MemoryMarshal.AsBytes(span));

                        foreach (ref StaticsBlock sb in span)
                        {
                            int idx = ChunkCellIndex(sb.X, sb.Y);
                            if ((uint)idx >= 64) continue;

                            ushort staticId = sb.Color;
                            if (staticId >= tileData.StaticData.Length) continue;

                            ref StaticTiles sd = ref tileData.StaticData[staticId];

                            if (sd.IsRoof) continue;

                            if (sd.IsImpassable || sd.IsWall)
                            {
                                // Compare against LandZ (not SurfaceZ) so that elevated walls
                                // (arch tops, plateau retaining walls) don't block ground level.
                                if (sb.Z <= data.LandZ[idx] + GROUND_LEVEL_THRESHOLD)
                                    hardBlocked[idx] = true;
                            }
                            else if (sd.IsSurface || sd.IsBridge)
                            {
                                hasSurface[idx] = true;
                                sbyte top = (sbyte)Math.Clamp(sb.Z + (int)sd.Height, sbyte.MinValue, sbyte.MaxValue);
                                if (top > data.SurfaceZ[idx])
                                    data.SurfaceZ[idx] = top;
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<StaticsBlock>.Shared.Return(buf);
                    }

                    for (int i = 0; i < 64; i++)
                    {
                        if (hardBlocked[i])
                            data.Walkable[i] = false;
                        else if (hasSurface[i])
                            data.Walkable[i] = true;
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Log.Error($"WorldMapPathfinder.LoadChunk({cx},{cy}): {ex.Message}");
                return new ChunkWalkData { Invalid = true };
            }
        }
    }
}
