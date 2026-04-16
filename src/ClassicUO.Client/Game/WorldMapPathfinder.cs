// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Assets;
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
    ///
    /// Limitations (intentional, for v1):
    ///  - Ignores dynamic items and mobiles (not available from the files anyway).
    ///  - Does not account for private houses / custom multis (TODO: follow-up).
    ///  - 2D walkability with a coarse Z step check.
    /// </summary>
    public static class WorldMapPathfinder
    {
        /// <summary>Hard cap on A* nodes. 1M ≈ 100MB peak; reached only for truly impossible destinations.</summary>
        private const int MAX_NODES = 1_000_000;

        /// <summary>
        /// Wall-clock cap on a single search. Prevents a hopeless query (click on an unreachable
        /// island, wrong map, etc.) from locking out new clicks. Typical successful search is 10–200ms;
        /// large maze interiors (Luna Bank, multi-room dungeons) can take 1–3s even with weighted A*.
        /// </summary>
        private const int MAX_SEARCH_MS = 5000;

        /// <summary>
        /// Heuristic weight. 1.0 = optimal A*; higher values trade optimality for speed.
        /// 1.25 is a good balance — biased enough to cut exploration in half, low enough
        /// that A* still backtracks when it hits a dead-end room (Luna Bank / Britain bank).
        /// </summary>
        private const int HEURISTIC_WEIGHT_NUM = 5;  // 1.25x = 5/4
        private const int HEURISTIC_WEIGHT_DEN = 4;

        /// <summary>
        /// Token the current search checks regularly. New calls to FindPathAsync increment
        /// the version so an in-flight search sees its token as stale and bails out promptly.
        /// </summary>
        private static int _searchVersion;

        /// <summary>
        /// Tiles the server/client rejected when the walker tried to step onto them —
        /// lamp posts, rocks, placed doors, and other dynamic items that aren't in
        /// statics.mul and therefore invisible to our file-based A*.  Cleared at the
        /// start of each new FindPathAsync call.
        /// <para/>
        /// Uses ConcurrentDictionary as a thread-safe set: the main thread mutates it
        /// (Clear on a new Ctrl+Click, TryAdd from the step-fail hook) while an in-flight
        /// background search may be reading it via ContainsKey in TryGetWalkable.
        /// </summary>
        private static readonly ConcurrentDictionary<long, byte> _dynamicBlocks = new();
        public static void ClearDynamicBlocks() => _dynamicBlocks.Clear();

        /// <summary>Add a tile to the dynamic-block set so the next search routes around it.</summary>
        public static void MarkDynamicBlock(int x, int y) => _dynamicBlocks.TryAdd(PackKey(x, y), 0);

        /// <summary>
        /// Per-search snapshot of tiles blocked by player houses (custom house walls, signposts,
        /// placed multis).  Captured on the main thread before the worker starts, then read-only
        /// for the duration of the search — so a plain HashSet is safe as long as nobody mutates
        /// it after Task.Run.  Protected by _running: only one search can own this field at a time.
        /// </summary>
        private static HashSet<long> _houseBlocks;

        // Direction enum layout (matches ClassicUO.Game.Data.Direction):
        // 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };

        private static int _running;

        public struct PathPoint
        {
            public int X;
            public int Y;
            public int Z;
            public int Direction;
        }

        /// <summary>
        /// Shallow snapshot of a Multi component taken on the main thread.  Contains only the
        /// fields needed by the filtering logic — no references to live game objects — so it
        /// can be iterated safely from the background search thread even if the underlying
        /// Multi is disposed or moved mid-search.
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

        public static bool IsRunning => Volatile.Read(ref _running) != 0;

        /// <summary>
        /// Computes a path from (startX,startY,startZ) to (destX,destY) on the given map, asynchronously.
        /// If the clicked destination tile is impassable (tree, wall, water, etc.) the pathfinder
        /// automatically targets the nearest walkable tile within <paramref name="snapRadius"/>.
        /// The callback fires on the main thread with the full path (empty list = no path found).
        /// Only one search at a time; concurrent calls return immediately with an empty list.
        /// </summary>
        /// <param name="houseMultis">
        /// Optional list of shallow-copied Multi snapshots taken on the caller's thread.
        /// The worker filters this list (flag checks, ground-level Z check via GetTileZ) to
        /// produce the blocked-tile HashSet — keeping the main thread free of heavy work
        /// even in dense areas with thousands of components.  Pass null when no houses
        /// need to be considered.
        /// </param>
        public static void FindPathAsync(int mapIndex, int startX, int startY, sbyte startZ,
                                         int destX, int destY, int snapRadius,
                                         Action<List<PathPoint>> onComplete,
                                         List<HouseMultiSnapshot> houseMultis = null)
        {
            // Only one search runs at a time; if another is still running, drop this request
            // rather than cancelling the in-flight search (which would leave BOTH empty).
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                onComplete?.Invoke(new List<PathPoint>());
                return;
            }

            // We now own the lock — bump the version so any PREVIOUS (already-finishing)
            // search that hasn't fully torn down yet sees the mismatch and bails out.
            // Placed after the lock to avoid the old race: increment-then-fail-lock would
            // cancel the running search AND return empty, so both calls produced nothing.
            int myVersion = Interlocked.Increment(ref _searchVersion);

            Task.Run(() =>
            {
                var result = new List<PathPoint>();
                try
                {
                    // Build the house-block set on the worker thread — filtering + GetTileZ
                    // for each Multi is the expensive part; the main thread only paid the
                    // cost of a shallow copy.
                    _houseBlocks = BuildHouseBlockSet(mapIndex, houseMultis);

                    var chunkCache = new Dictionary<long, ChunkWalkData>(1024);

                    // If the click landed on an impassable tile, snap to the nearest walkable one.
                    // Use a flag instead of early-return so execution falls through to the
                    // completion callback below — WorldMapGump relies on it to clear _navDest
                    // and _navPath when a nav fails, and the previous early-return skipped it.
                    int targetX = destX;
                    int targetY = destY;
                    bool canSearch = true;
                    if (!TryGetWalkable(mapIndex, destX, destY, startZ, chunkCache, out _))
                    {
                        if (!SnapToNearestWalkable(mapIndex, destX, destY, startZ, snapRadius, chunkCache,
                                                    out targetX, out targetY))
                        {
                            canSearch = false;  // no walkable tile within radius → empty result
                        }
                    }

                    if (canSearch)
                    {
                        long deadline = Time.Ticks + MAX_SEARCH_MS;
                        result = FindPath(mapIndex, startX, startY, startZ, targetX, targetY, chunkCache, myVersion, deadline);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"WorldMapPathfinder: {ex.Message}");
                }
                finally
                {
                    _houseBlocks = null;
                    Volatile.Write(ref _running, 0);
                }

                MainThreadQueue.EnqueueAction(() => onComplete?.Invoke(result));
            });
        }

        /// <summary>
        /// Walks the caller-supplied Multi snapshots, applies the same walkability rules used
        /// for statics (IsImpassable / IsWall at ground level, skipping IsRoof / previews /
        /// destroyed / render-ignored), and returns a HashSet of blocked (x,y) keys.
        /// Runs entirely on the worker thread; uses raw MMF reads via Maps.GetIndex for land Z.
        /// </summary>
        private static HashSet<long> BuildHouseBlockSet(int mapIndex, List<HouseMultiSnapshot> multis)
        {
            if (multis == null || multis.Count == 0)
                return null;

            var set = new HashSet<long>(multis.Count);
            var tileData = Client.Game.UO.FileManager.TileData;
            var maps = Client.Game.UO.FileManager.Maps;

            // Matches OrionUO's "circumnavigate the house" behaviour: block the ENTIRE
            // ground-level footprint, not just individual wall tiles.  Doors, windows,
            // interior floors, stairs and ramps are all treated as "part of the house" —
            // the navigator routes around, never through.
            //
            // GROUND_LEVEL_BAND = 14: ground-floor tiles sit at roughly landZ+7 (UO floor
            // is 7 Z above the foundation), so 14 catches the first storey while excluding
            // upper storeys (~20 Z above ground floor) and rooftops.
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
                        landZ = cells[((m.Y & 7) << 3) + (m.X & 7)].Z;
                    }
                }
                catch { }

                if (m.Z > landZ + GROUND_LEVEL_BAND) continue;

                set.Add(PackKey(m.X, m.Y));
            }

            return set.Count == 0 ? null : set;
        }

        /// <summary>
        /// Finds the nearest walkable tile to (cx, cy), searching in expanding rings up to <paramref name="radius"/>.
        /// Returns false if no walkable tile is found within the radius.
        /// </summary>
        private static bool SnapToNearestWalkable(int mapIndex, int cx, int cy, sbyte fromZ, int radius,
                                                   Dictionary<long, ChunkWalkData> cache, out int outX, out int outY)
        {
            for (int r = 1; r <= radius; r++)
            {
                // Scan the border of a square of side (2r+1) — Chebyshev distance = r
                for (int dx = -r; dx <= r; dx++)
                {
                    // Top and bottom edges of the ring
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
                    // Left and right edges (excluding corners already checked)
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
            public int G;       // cost-so-far
            public int F;       // G + heuristic
            public Node Parent;
            public byte Direction;  // dir taken from Parent to reach here
        }

        // Octile distance, weighted for speed. D1=10, D2=14. Weight × 1.5 gives a strong
        // goal bias that prunes ~10× more exploration in maze-like interiors.
        private static int Heuristic(int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);
            int octile = 10 * (dx + dy) + (14 - 20) * Math.Min(dx, dy);
            return octile * HEURISTIC_WEIGHT_NUM / HEURISTIC_WEIGHT_DEN;
        }

        private static List<PathPoint> FindPath(int mapIndex, int startX, int startY, sbyte startZ,
                                                 int destX, int destY, Dictionary<long, ChunkWalkData> chunkCache,
                                                 int myVersion, long deadlineTicks)
        {
            var result = new List<PathPoint>();
            var openQueue = new System.Collections.Generic.PriorityQueue<Node, int>();
            var bestG = new Dictionary<long, int>(8192);  // key = (x,y) packed, value = best G cost so far
            int nodeCount = 0;

            var startNode = new Node
            {
                X = startX,
                Y = startY,
                Z = startZ,
                G = 0,
                F = Heuristic(startX, startY, destX, destY),
                Parent = null,
                Direction = 0
            };
            openQueue.Enqueue(startNode, startNode.F);
            bestG[PackKey(startX, startY)] = 0;

            Node goal = null;

            while (openQueue.Count > 0 && nodeCount < MAX_NODES)
            {
                // Check cancellation / timeout every 4096 nodes — cheap and responsive.
                if ((nodeCount & 0xFFF) == 0)
                {
                    if (Volatile.Read(ref _searchVersion) != myVersion) return result;      // a newer click superseded us
                    if (Time.Ticks >= deadlineTicks) return result;                         // hit wall-clock cap
                }

                Node current = openQueue.Dequeue();
                long key = PackKey(current.X, current.Y);

                // Stale entry check (PriorityQueue has no decrease-key; we push duplicates)
                if (bestG.TryGetValue(key, out int knownG) && knownG < current.G)
                    continue;

                if (current.X == destX && current.Y == destY)
                {
                    goal = current;
                    break;
                }

                nodeCount++;

                for (int d = 0; d < 8; d++)
                {
                    int nx = current.X + DX[d];
                    int ny = current.Y + DY[d];

                    if (nx < 0 || ny < 0) continue;

                    if (!TryGetWalkable(mapIndex, nx, ny, current.Z, chunkCache, out sbyte nz))
                        continue;

                    // No Z-step check — the server resolves actual Z when the walk packet is sent.
                    // For 2D overworld pathing (including under-roof, ramps, stairs) this is what we want.

                    // Diagonal corner-cutting check: both adjacent cardinal tiles must be walkable,
                    // otherwise we'd slide through a wall corner.
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
                        X = nx,
                        Y = ny,
                        Z = nz,
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

            // Reconstruct
            var stack = new Stack<PathPoint>();
            for (Node n = goal; n != null && n.Parent != null; n = n.Parent)
            {
                stack.Push(new PathPoint
                {
                    X = n.X,
                    Y = n.Y,
                    Z = n.Z,
                    Direction = n.Direction
                });
            }
            while (stack.Count > 0)
                result.Add(stack.Pop());

            return result;
        }

        private static long PackKey(int x, int y) => ((long)(uint)x << 32) | (uint)y;

        // -----------------------------------------------------------------
        // Raw-file walkability
        // -----------------------------------------------------------------

        /// <summary>Per-chunk precomputed walkability, cached for the duration of one search.</summary>
        private sealed class ChunkWalkData
        {
            /// <summary>Walkable flag for each of the 64 tiles in this 8×8 chunk.</summary>
            public readonly bool[] Walkable = new bool[64];
            /// <summary>
            /// IMMUTABLE land Z from map.mul. Used for the "is this wall at ground level?"
            /// check. Must NOT be modified by statics — if Surface/Bridge statics raised it,
            /// plateau retaining walls at the platform's Z would slide into the ground-level
            /// window and block the whole plateau (Luna Bank symptom).
            /// </summary>
            public readonly sbyte[] LandZ = new sbyte[64];
            /// <summary>
            /// Highest walkable surface Z for each tile. Grows as Surface/Bridge statics are
            /// processed. Reported for diagnostics and used by the walker for Z hints.
            /// </summary>
            public readonly sbyte[] SurfaceZ = new sbyte[64];
            public bool Invalid;
        }

        private static bool TryGetWalkable(int mapIndex, int x, int y, sbyte fromZ,
                                           Dictionary<long, ChunkWalkData> cache, out sbyte surfaceZ)
        {
            long key = PackKey(x, y);

            // Dynamic-block override first — lamp posts, rocks, placed doors, etc.
            // learned during walk-time failures.
            if (_dynamicBlocks.ContainsKey(key))
            {
                surfaceZ = 0;
                return false;
            }

            // House-block snapshot — walls and other impassable components of player houses
            // captured when the search started. Read-only for the worker's lifetime.
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

            int idx = ((y & 7) << 3) + (x & 7);
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
                    data.SurfaceZ[i] = lz;  // starts at land Z, may grow via surface statics
                }

                // --- Static layer ---
                // Two-bitmap rule:
                //   hardBlocked  = walls, trees, rocks, impassable furniture at ground level
                //   hasSurface   = floor/bridge static that you CAN stand on (lets you cross water)
                // Final walkability = (!hardBlocked) && (land_walkable || hasSurface)
                //
                // Elevation rule: only statics within ~6 Z of the land count.
                // Above that they're treated as overhead (archways, battlements, upper floors).
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
                            int idx = ((sb.Y & 7) << 3) + (sb.X & 7);
                            if ((uint)idx >= 64) continue;

                            ushort staticId = sb.Color;
                            if (staticId >= tileData.StaticData.Length) continue;

                            ref StaticTiles sd = ref tileData.StaticData[staticId];

                            // Roof tiles never affect ground-level pathing.
                            if (sd.IsRoof) continue;

                            if (sd.IsImpassable || sd.IsWall)
                            {
                                // Block only if the wall sits at actual ground (land) level.
                                // IMPORTANT: compare against LandZ, NOT SurfaceZ. SurfaceZ may have
                                // been raised by a prior Surface/Bridge static in this chunk; using
                                // it would slide the threshold upward and wrongly block elevated
                                // walls (arch tops, plateau retaining walls).
                                if (sb.Z <= data.LandZ[idx] + GROUND_LEVEL_THRESHOLD)
                                    hardBlocked[idx] = true;
                            }
                            else if (sd.IsSurface || sd.IsBridge)
                            {
                                // A walkable surface / bridge. Accept it at any reasonable Z —
                                // bridges span water (land impassable), stairs rise, etc.
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
                            data.Walkable[i] = false;       // wall always wins
                        else if (hasSurface[i])
                            data.Walkable[i] = true;        // bridge/floor unblocks impassable land
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
