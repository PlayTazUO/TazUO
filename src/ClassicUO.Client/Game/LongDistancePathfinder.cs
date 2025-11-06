using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game
{
    public static class LongDistancePathfinder
    {
        private const int CLOSE_DISTANCE_THRESHOLD = 10;
        private const int MAX_PATHFIND_ATTEMPTS = 100;
        private const int REGULAR_PATHFINDER_MAX_RANGE = 15;
        private const int MIN_TILES_TO_START_WALKING = 5;
        private const int INITIAL_CHUNK_SIZE = 10;
        private const int MAX_PATHFINDING_TIME_MS = 30000; // 30 seconds

        // Thread synchronization
        private static readonly object _stateLock = new object();

        // Shared state (accessed from multiple threads)
        private static volatile int _targetX, _targetY;
        private static volatile bool _pathfindingInProgress = false;
        private static readonly ConcurrentQueue<Point> _fullTilePath = new();
        private static volatile bool _pathGenerationComplete = false;
        private static volatile bool _walkingStarted = false;
        private static CancellationTokenSource _pathfindingCancellation;
        private static volatile int _disableLongDistanceForWaypoints = 0; // Using int for Interlocked operations
        private static volatile int _currentChunkSize = 10;
        private static readonly ConcurrentStack<Point> _failedTiles = new();
        private static long _nextAttempt = 0; // Protected by Interlocked operations
        private static long _pathfindingStartTime; // Set before background thread starts, read during execution

        public static bool WalkLongDistance(int targetX, int targetY)
        {
            Log.Info($"[LongDistancePathfinder] WalkLongDistance() called to ({targetX}, {targetY})");

            if (World.Instance == null || !World.Instance.InGame || World.Instance.Player == null)
            {
                Log.Warn("[LongDistancePathfinder] Cannot start pathfinding: not in game or no player");
                return false;
            }

            if (!WalkableManager.Instance.IsMapGenerationComplete(World.Instance.MapIndex) && Time.Ticks > _nextAttempt)
            {
                (int current, int total) val = WalkableManager.Instance.GetCurrentMapGenerationProgress();
                GameActions.Print("Long distance pathfinding is in process, pathfinding may be degraded until completed.");
                GameActions.Print($"Generating pathfinding cache. {Utility.MathHelper.PercentageOf(val.current, val.total)}% ({val.current}/{val.total})", 84);
            }

            // If we're currently processing chunks, don't allow new long distance pathfinding
            // This prevents infinite recursion when walking to chunks
            if (Interlocked.CompareExchange(ref _disableLongDistanceForWaypoints, 0, 0) != 0)
            {
                Log.Info("[LongDistancePathfinder] Long distance pathfinding temporarily disabled for chunk processing");
                return false;
            }

            // Prevent rapid re-attempts that could cause infinite loops
            long currentTicks = Time.Ticks;
            long nextAttempt = Interlocked.Read(ref _nextAttempt);
            if (currentTicks < nextAttempt)
                return false;

            Interlocked.Exchange(ref _nextAttempt, currentTicks + 500);

            // Use lock to prevent race conditions during initialization
            lock (_stateLock)
            {
                // Cancel any existing pathfinding first
                if (_pathfindingInProgress)
                {
                    Log.Info("[LongDistancePathfinder] Stopping existing pathfinding to start new one");
                    StopPathfindingInternal();
                }

                int playerX = World.Instance.Player.X;
                int playerY = World.Instance.Player.Y;
                int distance = Math.Max(Math.Abs(targetX - playerX), Math.Abs(targetY - playerY));

                Log.Info($"[LongDistancePathfinder] Starting full tile path generation from ({playerX}, {playerY}) to ({targetX}, {targetY}), distance: {distance}");
                GameActions.Print($"Generating full path to ({targetX}, {targetY})...");

                // Initialize pathfinding state
                _pathfindingInProgress = true;
                _pathGenerationComplete = false;
                _walkingStarted = false;
                _currentChunkSize = INITIAL_CHUNK_SIZE;
                _pathfindingStartTime = Time.Ticks;

                // Cancel old operation and create new cancellation token
                // Don't dispose immediately - the background thread might still be using it
                // The GC will clean it up once the background task completes
                _pathfindingCancellation?.Cancel();
                _pathfindingCancellation = new CancellationTokenSource();

                // Clear the full tile path queue and failed tiles
                while (_fullTilePath.TryDequeue(out _)) { }
                _failedTiles.Clear();

                World world = World.Instance;
                if (world?.Player?.Pathfinder != null)
                    world.Player.Pathfinder.AutoWalking = true;

                // Start full path generation in background (fire-and-forget)
                _ = StartFullPathGeneration(playerX, playerY, targetX, targetY);
            }

            return true;
        }

        private static async Task StartFullPathGeneration(int startX, int startY, int targetX, int targetY)
        {
            try
            {
                await Task.Run(() => GenerateFullTilePath(startX, startY, targetX, targetY, _pathfindingCancellation.Token));
            }
            catch (OperationCanceledException)
            {
                Log.Info("[LongDistancePathfinder] Path generation was cancelled");
                GameActions.Print("Path generation cancelled");
            }
            catch (Exception ex)
            {
                GameActions.Print("Path generation failed - error occurred");
                Log.Error($"[LongDistancePathfinder] Error during path generation: {ex.Message}");
            }
            finally
            {
                _pathGenerationComplete = true;
            }
        }

        private static void ProcessTileChunks()
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Player == null)
            {
                Log.Warn("[LongDistancePathfinder] Cannot process tiles: not in game or no player");
                StopPathfinding();
                return;
            }

            // Check if we have tiles to process
            if (_fullTilePath.Count == 0)
            {
                // No more tiles available
                if (_pathGenerationComplete)
                {
                    // Check if player is within 1 tile of target (using Chebyshev distance)
                    int distanceToTarget = Math.Max(
                        Math.Abs(World.Instance.Player.X - _targetX),
                        Math.Abs(World.Instance.Player.Y - _targetY)
                    );

                    // Check if we've reached the destination
                    if (distanceToTarget <= 1)
                    {
                        // We're close enough - try one final walk to exact position
                        World.Instance.Player.Pathfinder.WalkTo(_targetX, _targetY, World.Instance.Player.Z, 0);
                        GameActions.Print("Destination reached!");
                        Log.Info("[LongDistancePathfinder] Path completed successfully");
                        StopPathfinding();
                        return;
                    }

                    // Try to continue walking to target
                    if (!World.Instance.Player.Pathfinder.WalkTo(_targetX, _targetY, World.Instance.Player.Z, 0))
                    {
                        // Can't walk to target - we're as close as we can get
                        Log.Warn("[LongDistancePathfinder] Cannot reach exact target - stopping at current position");
                        GameActions.Print("Destination reached (as close as possible)!");
                        StopPathfinding();
                        return;
                    }
                }
                // If path generation is still in progress, wait for more tiles
                Log.Info("[LongDistancePathfinder] Waiting for more tiles...");
                return;
            }

            // Collect tiles for the current chunk (up to _currentChunkSize)
            var chunkTiles = new List<Point>();
            int tilesCollected = 0;

            // Add any failed tiles back to the front first (thread-safe with ConcurrentStack)
            while (tilesCollected < _currentChunkSize && _failedTiles.TryPop(out Point failedTile))
            {
                chunkTiles.Add(failedTile);
                tilesCollected++;
            }

            // Fill remaining chunk with new tiles from queue
            while (tilesCollected < _currentChunkSize && _fullTilePath.TryDequeue(out Point tile))
            {
                chunkTiles.Add(tile);
                tilesCollected++;
            }

            if (chunkTiles.Count == 0)
            {
                Log.Info("[LongDistancePathfinder] No tiles available for chunk processing");
                return;
            }

            // Try to find the furthest reachable tile in the chunk
            Point? targetTile = null;
            int targetIndex = -1;

            // Try from furthest to nearest to find a reachable tile
            for (int i = chunkTiles.Count - 1; i >= 0; i--)
            {
                Point tile = chunkTiles[i];
                int distance = Math.Max(Math.Abs(tile.X - World.Instance.Player.X), Math.Abs(tile.Y - World.Instance.Player.Y));

                // Only try tiles that are within reasonable regular pathfinding range
                if (distance <= REGULAR_PATHFINDER_MAX_RANGE)
                {
                    targetTile = tile;
                    targetIndex = i;
                    Log.Info($"[LongDistancePathfinder] Selected tile #{i+1}/{chunkTiles.Count} at ({tile.X}, {tile.Y}), distance: {distance}");
                    break;
                }
                else
                {
                    Log.Debug($"[LongDistancePathfinder] Skipping tile #{i+1} at ({tile.X}, {tile.Y}), distance too far: {distance}");
                }
            }

            if (!targetTile.HasValue)
            {
                Log.Warn($"[LongDistancePathfinder] No reachable tiles in chunk of {chunkTiles.Count}, all too far from player");
                // Put all tiles back as failed and reduce chunk size
                _failedTiles.PushRange(chunkTiles.ToArray());
                _currentChunkSize = Math.Max(1, _currentChunkSize - 1);

                if (_currentChunkSize == 1)
                {
                    Log.Warn($"[LongDistancePathfinder] No reachable tiles and chunk size reduced to 1 - halting pathfinding");
                    GameActions.Print("Long distance pathfinding failed - no reachable path found");
                    StopPathfinding();
                }
                return;
            }

            Log.Info($"[LongDistancePathfinder] Processing chunk of {chunkTiles.Count} tiles, target: ({targetTile.Value.X}, {targetTile.Value.Y})");

            // Try to walk to the selected target tile
            bool success = CallRegularPathfinder(targetTile.Value.X, targetTile.Value.Y, World.Instance.Player.Z, 0);

            if (success)
            {
                Log.Info($"[LongDistancePathfinder] Successfully started walking to chunk target ({targetTile.Value.X}, {targetTile.Value.Y})");
                // Reset chunk size on success
                _currentChunkSize = INITIAL_CHUNK_SIZE;

                // Put any tiles after the target back as failed (tiles beyond where we're walking)
                if (targetIndex < chunkTiles.Count - 1)
                {
                    List<Point> remainingTiles = chunkTiles.GetRange(targetIndex + 1, chunkTiles.Count - targetIndex - 1);
                    _failedTiles.PushRange(remainingTiles.ToArray());
                    Log.Info($"[LongDistancePathfinder] Put {remainingTiles.Count} tiles beyond target back for later processing");
                }
            }
            else
            {
                Log.Warn($"[LongDistancePathfinder] Failed to walk to chunk target, reducing chunk size from {_currentChunkSize}");

                // Put the tiles back at the front for retry with smaller chunk
                _failedTiles.PushRange(chunkTiles.ToArray());

                // Reduce chunk size (10 -> 9 -> 8 -> ... -> 1)
                _currentChunkSize = Math.Max(1, _currentChunkSize - 1);

                if (_currentChunkSize == 1)
                {
                    Log.Warn($"[LongDistancePathfinder] Chunk size reduced to 1, this indicates pathfinding issues - halting");
                    GameActions.Print("Long distance pathfinding failed - destination may be unreachable");
                    StopPathfinding();
                    return;
                }
            }
        }

        public static void Update()
        {
            // Only process if we have active long distance pathfinding
            if (!_pathfindingInProgress)
                return;

            //Log.Info($"[LongDistancePathfinder] Update() - walkingStarted: {_walkingStarted}, pathComplete: {_pathGenerationComplete}, tileCount: {_fullTilePath.Count}, failedTiles: {_failedTiles.Count}, chunkSize: {_currentChunkSize}, autoWalking: {Pathfinder.AutoWalking}");

            // Start walking once we have some tiles or path generation is complete
            if (!_walkingStarted && (_fullTilePath.Count >= MIN_TILES_TO_START_WALKING || _pathGenerationComplete))
            {
                _walkingStarted = true;
                GameActions.Print($"Path ready! Starting movement...");
                Log.Info($"[LongDistancePathfinder] Starting to process tiles with {_fullTilePath.Count} tiles available");
            }

            // Continue processing tile chunks if we've started and regular pathfinder isn't busy
            if (_walkingStarted && !World.Instance.Player.Pathfinder.AutoWalking)
            {
                Log.Info("[LongDistancePathfinder] Processing next tile chunk");
                ProcessTileChunks();
            }
        }

        public static void StopPathfinding()
        {
            lock (_stateLock)
            {
                StopPathfindingInternal();
            }
        }

        // Internal method - must be called from within _stateLock
        private static void StopPathfindingInternal()
        {
            Log.Info($"[LongDistancePathfinder] StopPathfinding() called - currently in progress: {_pathfindingInProgress}");

            if (!_pathfindingInProgress)
            {
                Log.Info("[LongDistancePathfinder] StopPathfinding() - already stopped, returning");
                return; // Already stopped
            }

            _pathfindingCancellation?.Cancel();
            _pathfindingInProgress = false;
            _pathGenerationComplete = false;
            _walkingStarted = false;
            _currentChunkSize = INITIAL_CHUNK_SIZE;

            // Clear the full tile path queue and failed tiles
            int queueSize = _fullTilePath.Count;
            while (_fullTilePath.TryDequeue(out _)) { }
            _failedTiles.Clear();

            World world = World.Instance;
            if (world?.Player?.Pathfinder != null)
                world.Player.Pathfinder.AutoWalking = false;

            Log.Info($"[LongDistancePathfinder] Pathfinding stopped - cleared {queueSize} tiles from queue");
        }

        public static void StopPathfindingWithMessage()
        {
            if (!_pathfindingInProgress)
                return; // Already stopped

            StopPathfinding();
            GameActions.Print("Long distance pathfinding stopped");
        }

        /// <summary>
        /// Resets the pathfinder state to initial values. For testing purposes only.
        /// </summary>
        internal static void Reset()
        {
            StopPathfinding();
            _nextAttempt = 0;
            Interlocked.Exchange(ref _disableLongDistanceForWaypoints, 0);
        }

        private static bool CallRegularPathfinder(int x, int y, int z, int distance)
        {
            // This bypasses the long-distance check in Pathfinder.WalkTo() to prevent infinite recursion
            // We need to call the regular pathfinding logic directly
            try
            {
                // Check if player exists and can move
                if (World.Instance.Player == null || World.Instance.Player.IsParalyzed)
                {
                    Log.Warn("[LongDistancePathfinder] Cannot use regular pathfinder: player null or paralyzed");
                    return false;
                }

                // Calculate distance to see if it would normally trigger long distance pathfinding
                int playerDistance = Math.Max(Math.Abs(x - World.Instance.Player.X), Math.Abs(y - World.Instance.Player.Y));

                if (playerDistance <= CLOSE_DISTANCE_THRESHOLD)
                {
                    // Distance is small enough, just use regular pathfinder directly
                    return World.Instance.Player.Pathfinder.WalkTo(x, y, z, distance);
                }
                else
                {
                    // Distance would trigger long distance pathfinding, but we want to force regular pathfinding
                    // So we temporarily disable long distance pathfinding and accept the result
                    Log.Info($"[LongDistancePathfinder] Forcing regular pathfinder for waypoint at distance {playerDistance}");

                    Interlocked.Increment(ref _disableLongDistanceForWaypoints);
                    try
                    {
                        bool result = World.Instance.Player.Pathfinder.WalkTo(x, y, z, distance);
                        Log.Info($"[LongDistancePathfinder] Regular pathfinder result: {result}");
                        return result;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _disableLongDistanceForWaypoints);
                    }
                }
            }
            catch (Exception ex)
            {
                // Reset the flag to 0 in case of exception
                Interlocked.Exchange(ref _disableLongDistanceForWaypoints, 0);
                Log.Error($"[LongDistancePathfinder] Error in CallRegularPathfinder: {ex.Message}");
                return false;
            }
        }

        private static void GenerateFullTilePath(int startX, int startY, int targetX, int targetY, CancellationToken cancellationToken)
        {
            Log.Info($"[LongDistancePathfinder] Starting full tile path generation from ({startX}, {startY}) to ({targetX}, {targetY})");

            // Test basic walkability
            bool startWalkable = IsGenerallyWalkable(startX, startY);
            Log.Info($"[LongDistancePathfinder] Start position walkable: {startWalkable}");

            _targetX = targetX;
            _targetY = targetY;

            // Local collections for this pathfinding operation (thread-safe by design)
            var closedSet = new Dictionary<(int x, int y), LongPathNode>();
            var openSet = new PriorityQueue<LongPathNode, int>();

            // If we're already within close distance, use regular pathfinder and add to queue
            int distance = Math.Max(Math.Abs(targetX - startX), Math.Abs(targetY - startY));
            if (distance <= CLOSE_DISTANCE_THRESHOLD)
            {
                List<Point> shortPath = ConvertToPointList(World.Instance.Player.Pathfinder.GetPathTo(targetX, targetY, World.Instance.Player.Z, 0));
                if (shortPath != null)
                {
                    foreach (Point point in shortPath)
                    {
                        _fullTilePath.Enqueue(point);
                    }
                }
                return;
            }

            // Start long distance pathfinding with full tile path generation
            var startNode = new LongPathNode
            {
                X = startX,
                Y = startY,
                DistFromStart = 0,
                DistToGoal = GetDistance(startX, startY, targetX, targetY),
                Parent = null
            };
            startNode.Cost = startNode.DistFromStart + startNode.DistToGoal;

            openSet.Enqueue(startNode, startNode.Cost);

            LongPathNode goalNode = null;
            int nodesProcessed = 0;

            Log.Info($"[LongDistancePathfinder] Starting A* search for full tile path");

            while (openSet.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                // Check for timeout
                if (Time.Ticks - _pathfindingStartTime > MAX_PATHFINDING_TIME_MS)
                {
                    Log.Warn("[LongDistancePathfinder] Pathfinding timeout - exceeded maximum time");
                    GameActions.Print("Pathfinding timeout - path too complex");
                    return;
                }

                LongPathNode currentNode = openSet.Dequeue();
                (int X, int Y) key = (currentNode.X, currentNode.Y);

                if (closedSet.ContainsKey(key))
                    continue;

                closedSet[key] = currentNode;
                nodesProcessed++;

                // Check if we reached the exact target
                if (currentNode.X == targetX && currentNode.Y == targetY)
                {
                    goalNode = currentNode;
                    Log.Info($"[LongDistancePathfinder] Found exact target at ({currentNode.X}, {currentNode.Y})");
                    break;
                }

                // Generate neighboring nodes (now using single-tile steps for full path)
                GenerateNeighborsForFullPath(currentNode, openSet, closedSet);

                // Yield periodically to prevent blocking
                if (nodesProcessed % 100 == 0)
                {
                    Thread.Sleep(1); // Brief yield
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            Log.Info($"[LongDistancePathfinder] A* search completed. Nodes processed: {nodesProcessed}, Goal found: {goalNode != null}, OpenSet remaining: {openSet.Count}");

            if (goalNode != null)
            {
                // Reconstruct the complete tile-by-tile path
                List<Point> fullPath = ReconstructPath(goalNode);
                Log.Info($"[LongDistancePathfinder] Reconstructed full path with {fullPath.Count} tiles");

                // Add ALL tiles to the queue - this is the full tile-by-tile path
                Point? previousPoint = null;
                foreach (Point point in fullPath)
                {
                    if (previousPoint.HasValue)
                    {
                        int stepDistance = GetDistance(point.X, point.Y, previousPoint.Value.X, previousPoint.Value.Y);
                        if (stepDistance > 2)
                        {
                            Log.Warn($"[LongDistancePathfinder] Large step detected in path: from ({previousPoint.Value.X}, {previousPoint.Value.Y}) to ({point.X}, {point.Y}), distance: {stepDistance}");
                        }
                    }
                    _fullTilePath.Enqueue(point);
                    previousPoint = point;
                }

                // Always add the exact target as final destination (even if not walkable, regular pathfinder will handle it)
                Point lastPoint = fullPath[fullPath.Count - 1];
                if (lastPoint.X != targetX || lastPoint.Y != targetY)
                {
                    _fullTilePath.Enqueue(new Point(targetX, targetY));
                    Log.Info($"[LongDistancePathfinder] Added exact target ({targetX}, {targetY}) as final tile");
                }

                Log.Info($"[LongDistancePathfinder] Added {_fullTilePath.Count} tiles to full path queue");
                GameActions.Print($"Generated path with {_fullTilePath.Count} tiles!");
            }
            else
            {
                // No exact path found, try to find the closest reachable point
                Log.Warn($"[LongDistancePathfinder] No exact path found, finding closest reachable point");
                LongPathNode bestNode = FindClosestNodeToTarget(closedSet);

                if (bestNode != null)
                {
                    List<Point> partialPath = ReconstructPath(bestNode);
                    Log.Info($"[LongDistancePathfinder] Found partial path to closest point with {partialPath.Count} tiles");

                    // Add the partial path
                    foreach (Point point in partialPath)
                    {
                        _fullTilePath.Enqueue(point);
                    }

                    // Still try to add the exact target at the end - regular pathfinder might be able to reach it
                    Point lastPoint = partialPath[partialPath.Count - 1];
                    if (lastPoint.X != targetX || lastPoint.Y != targetY)
                    {
                        _fullTilePath.Enqueue(new Point(targetX, targetY));
                        Log.Info($"[LongDistancePathfinder] Added target ({targetX}, {targetY}) after closest reachable point");
                    }

                    Log.Info($"[LongDistancePathfinder] Added {_fullTilePath.Count} tiles to partial path queue");
                    GameActions.Print($"Generated partial path with {_fullTilePath.Count} tiles (closest reachable point).");
                }
                else
                {
                    // Last resort: try direct line approach
                    Log.Warn($"[LongDistancePathfinder] No reachable points found, trying direct path");
                    List<Point> directPath = CreateDirectPathWithAvoidance(startX, startY, targetX, targetY);
                    if (directPath != null && directPath.Count > 1)
                    {
                        foreach (Point point in directPath)
                        {
                            _fullTilePath.Enqueue(point);
                        }
                        Log.Info($"[LongDistancePathfinder] Added direct path with {directPath.Count} tiles");
                        GameActions.Print($"Generated direct path with {directPath.Count} tiles.");
                    }
                    else
                    {
                        GameActions.Print($"Could not find any viable path to target.");
                    }
                }
            }
        }

        private static void GenerateNeighborsForFullPath(LongPathNode currentNode, PriorityQueue<LongPathNode, int> openSet, Dictionary<(int x, int y), LongPathNode> closedSet)
        {
            // Use single-tile steps for full path generation
            const int stepSize = 1;

            // Calculate direction to target for prioritization
            int deltaX = _targetX - currentNode.X;
            int deltaY = _targetY - currentNode.Y;

            // Determine primary direction(s) toward target
            var directions = new List<int>();

            // Add primary direction first (highest priority)
            if (deltaX > 0 && deltaY < 0) directions.Add(1); // Northeast
            else if (deltaX > 0 && deltaY > 0) directions.Add(3); // Southeast
            else if (deltaX < 0 && deltaY > 0) directions.Add(5); // Southwest
            else if (deltaX < 0 && deltaY < 0) directions.Add(7); // Northwest
            else if (deltaX > 0) directions.Add(2); // East
            else if (deltaX < 0) directions.Add(6); // West
            else if (deltaY < 0) directions.Add(0); // North
            else if (deltaY > 0) directions.Add(4); // South

            // Add secondary directions (adjacent to primary)
            if (deltaX != 0 && deltaY != 0)
            {
                // For diagonal movement, also try the cardinal directions
                if (deltaX > 0) directions.Add(2); // East
                if (deltaX < 0) directions.Add(6); // West
                if (deltaY < 0) directions.Add(0); // North
                if (deltaY > 0) directions.Add(4); // South
            }
            else
            {
                // For cardinal movement, try adjacent diagonals
                if (deltaX > 0) { directions.Add(1); directions.Add(3); } // NE, SE
                if (deltaX < 0) { directions.Add(5); directions.Add(7); } // SW, NW
                if (deltaY < 0) { directions.Add(1); directions.Add(7); } // NE, NW
                if (deltaY > 0) { directions.Add(3); directions.Add(5); } // SE, SW
            }

            // Only add other directions if we can't move in preferred directions
            bool foundGoodDirection = false;
            int neighborsGenerated = 0;

            // Try preferred directions first
            foreach (int dir in directions)
            {
                // Calculate direction offsets (single tile moves)
                (int newX, int newY) = ApplyDirectionOffset(currentNode.X, currentNode.Y, dir, stepSize);

                // Check bounds
                if (newX < 0 || newY < 0 || newX >= 65536 || newY >= 65536)
                    continue;

                (int newX, int newY) key = (newX, newY);
                if (closedSet.ContainsKey(key))
                    continue;

                // Check if the tile is walkable using our walkable manager
                bool walkable = IsGenerallyWalkable(newX, newY);
                if (!walkable)
                    continue;

                foundGoodDirection = true;
                int newDistFromStart = currentNode.DistFromStart + stepSize;
                int newDistToGoal = GetDistance(newX, newY, _targetX, _targetY);
                int newCost = newDistFromStart + newDistToGoal;

                var neighborNode = new LongPathNode
                {
                    X = newX,
                    Y = newY,
                    DistFromStart = newDistFromStart,
                    DistToGoal = newDistToGoal,
                    Cost = newCost,
                    Parent = currentNode
                };

                openSet.Enqueue(neighborNode, newCost);
                neighborsGenerated++;
            }

            // If no good directions found, try all directions as fallback
            if (!foundGoodDirection)
            {
                for (int dir = 0; dir < 8; dir++)
                {
                    if (directions.Contains(dir)) continue; // Already tried

                    // Calculate direction offsets (single tile moves)
                    (int newX, int newY) = ApplyDirectionOffset(currentNode.X, currentNode.Y, dir, stepSize);

                    // Check bounds
                    if (newX < 0 || newY < 0 || newX >= 65536 || newY >= 65536)
                        continue;

                    (int newX, int newY) key = (newX, newY);
                    if (closedSet.ContainsKey(key))
                        continue;

                    // Check if the tile is walkable using our walkable manager
                    bool walkable = IsGenerallyWalkable(newX, newY);
                    if (!walkable)
                        continue;

                    int newDistFromStart = currentNode.DistFromStart + stepSize;
                    int newDistToGoal = GetDistance(newX, newY, _targetX, _targetY);
                    int newCost = newDistFromStart + newDistToGoal;

                    var neighborNode = new LongPathNode
                    {
                        X = newX,
                        Y = newY,
                        DistFromStart = newDistFromStart,
                        DistToGoal = newDistToGoal,
                        Cost = newCost,
                        Parent = currentNode
                    };

                    openSet.Enqueue(neighborNode, newCost);
                    neighborsGenerated++;
                }
            }

            //Log.Debug($"[LongDistancePathfinder] Generated {neighborsGenerated} prioritized neighbors from ({currentNode.X}, {currentNode.Y})");
        }

        private static bool IsGenerallyWalkable(int x, int y) => WalkableManager.Instance.IsWalkable(x, y);

        private static (int newX, int newY) ApplyDirectionOffset(int x, int y, int direction, int stepSize)
        {
            int newX = x, newY = y;
            switch (direction)
            {
                case 0: newY -= stepSize; break;           // North
                case 1: newX += stepSize; newY -= stepSize; break; // Northeast
                case 2: newX += stepSize; break;           // East
                case 3: newX += stepSize; newY += stepSize; break; // Southeast
                case 4: newY += stepSize; break;           // South
                case 5: newX -= stepSize; newY += stepSize; break; // Southwest
                case 6: newX -= stepSize; break;           // West
                case 7: newX -= stepSize; newY -= stepSize; break; // Northwest
            }
            return (newX, newY);
        }

        private static List<Point> ReconstructPath(LongPathNode goalNode)
        {
            var path = new List<Point>();
            LongPathNode current = goalNode;

            while (current != null)
            {
                path.Add(new Point(current.X, current.Y));
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private static LongPathNode FindClosestNodeToTarget(Dictionary<(int x, int y), LongPathNode> closedSet)
        {
            LongPathNode bestNode = null;
            int bestDistance = int.MaxValue;

            foreach (LongPathNode node in closedSet.Values)
            {
                int distance = GetDistance(node.X, node.Y, _targetX, _targetY);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        private static List<Point> CreateDirectPathWithAvoidance(int startX, int startY, int targetX, int targetY)
        {
            var path = new List<Point>();

            // Simple line algorithm with obstacle avoidance
            int dx = Math.Sign(targetX - startX);
            int dy = Math.Sign(targetY - startY);

            int currentX = startX;
            int currentY = startY;
            int attempts = 0;

            while ((currentX != targetX || currentY != targetY) && attempts < MAX_PATHFIND_ATTEMPTS)
            {
                path.Add(new Point(currentX, currentY));

                // Try to move towards target
                int nextX = currentX;
                int nextY = currentY;

                if (currentX != targetX)
                    nextX += dx;
                if (currentY != targetY)
                    nextY += dy;

                // Check if the next position is walkable
                if (IsGenerallyWalkable(nextX, nextY))
                {
                    currentX = nextX;
                    currentY = nextY;
                }
                else
                {
                    // Try alternative directions
                    bool moved = false;

                    // Try horizontal then vertical
                    if (currentX != targetX && IsGenerallyWalkable(currentX + dx, currentY))
                    {
                        currentX += dx;
                        moved = true;
                    }
                    else if (currentY != targetY && IsGenerallyWalkable(currentX, currentY + dy))
                    {
                        currentY += dy;
                        moved = true;
                    }

                    if (!moved)
                    {
                        break; // Completely blocked
                    }
                }

                attempts++;
            }

            if (path.Count == 0)
            {
                path.Add(new Point(startX, startY));
            }

            return path;
        }

        private static int GetDistance(int x1, int y1, int x2, int y2) => Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));

        private static List<Point> ConvertToPointList(List<(int X, int Y, int Z)> path)
        {
            if (path == null)
                return null;

            var result = new List<Point>(path.Count);
            foreach ((int X, int Y, int Z) point in path)
            {
                result.Add(new Point(point.X, point.Y));
            }
            return result;
        }

        private class LongPathNode
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int DistFromStart { get; set; }
            public int DistToGoal { get; set; }
            public int Cost { get; set; }
            public LongPathNode Parent { get; set; }
        }
    }
}
