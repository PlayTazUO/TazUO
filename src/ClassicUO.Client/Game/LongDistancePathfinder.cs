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

        private static readonly Dictionary<(int x, int y), LongPathNode> _closedSet = new();
        private static readonly PriorityQueue<LongPathNode> _openSet = new();
        private static readonly List<Point> _pathResult = new();
        private static int _targetX, _targetY;
        private static bool _isPathfinding;
        private static volatile bool _pathfindingInProgress = false;
        private static readonly ConcurrentQueue<Point> _fullTilePath = new();
        private static volatile bool _pathGenerationComplete = false;
        private static volatile bool _walkingStarted = false;
        private static CancellationTokenSource _pathfindingCancellation;
        private static DateTime _lastPathfindingAttempt = DateTime.MinValue;
        private static volatile bool _disableLongDistanceForWaypoints = false;
        private static int _currentChunkSize = 10;
        private static List<Point> _failedTiles = new();

        public static bool WalkLongDistance(int targetX, int targetY)
        {
            Log.Info($"[LongDistancePathfinder] WalkLongDistance() called to ({targetX}, {targetY})");

            if (World.Instance == null || !World.Instance.InGame || World.Instance.Player == null)
            {
                Log.Warn("[LongDistancePathfinder] Cannot start pathfinding: not in game or no player");
                return false;
            }

            // If we're currently processing chunks, don't allow new long distance pathfinding
            // This prevents infinite recursion when walking to chunks
            if (_disableLongDistanceForWaypoints)
            {
                Log.Info("[LongDistancePathfinder] Long distance pathfinding temporarily disabled for chunk processing");
                return false;
            }

            // Prevent rapid re-attempts that could cause infinite loops
            DateTime now = DateTime.Now;
            if (now - _lastPathfindingAttempt < TimeSpan.FromMilliseconds(500))
            {
                Log.Warn($"[LongDistancePathfinder] Too many rapid pathfinding attempts, ignoring. Last attempt: {_lastPathfindingAttempt}, current: {now}");
                return false;
            }
            _lastPathfindingAttempt = now;

            // Cancel any existing pathfinding first
            if (_pathfindingInProgress)
            {
                Log.Info("[LongDistancePathfinder] Stopping existing pathfinding to start new one");
                StopPathfinding();
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
            _currentChunkSize = 10; // Start with 10 tiles
            _pathfindingCancellation = new CancellationTokenSource();

            // Clear the full tile path queue and failed tiles
            while (_fullTilePath.TryDequeue(out _)) { }
            _failedTiles.Clear();

            // Start full path generation in background
            StartFullPathGeneration(playerX, playerY, targetX, targetY);
            return true;
        }

        private static async void StartFullPathGeneration(int startX, int startY, int targetX, int targetY)
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
                    if((World.Instance.Player.X == _targetX && World.Instance.Player.Y == _targetY) || !World.Instance.Player.Pathfinder.WalkTo(_targetX, _targetY, World.Instance.Player.Z, 0))
                    {
                        GameActions.Print("Destination reached!");
                        Log.Info("[LongDistancePathfinder] Path completed successfully");
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

            // Add any failed tiles back to the front first
            for (int i = _failedTiles.Count - 1; i >= 0 && tilesCollected < _currentChunkSize; i--)
            {
                chunkTiles.Add(_failedTiles[i]);
                tilesCollected++;
            }
            _failedTiles.RemoveRange(Math.Max(0, _failedTiles.Count - tilesCollected), tilesCollected);

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
                if (distance <= 15)
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
                _failedTiles.AddRange(chunkTiles);
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
                // Reset chunk size to 10 on success
                _currentChunkSize = 10;

                // Put any tiles after the target back as failed (tiles beyond where we're walking)
                if (targetIndex < chunkTiles.Count - 1)
                {
                    List<Point> remainingTiles = chunkTiles.GetRange(targetIndex + 1, chunkTiles.Count - targetIndex - 1);
                    _failedTiles.AddRange(remainingTiles);
                    Log.Info($"[LongDistancePathfinder] Put {remainingTiles.Count} tiles beyond target back for later processing");
                }
            }
            else
            {
                Log.Warn($"[LongDistancePathfinder] Failed to walk to chunk target, reducing chunk size from {_currentChunkSize}");

                // Put the tiles back at the front for retry with smaller chunk
                _failedTiles.AddRange(chunkTiles);

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
            if (!_walkingStarted && (_fullTilePath.Count >= 5 || _pathGenerationComplete))
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
            // else if (_walkingStarted && Pathfinder.AutoWalking)
            // {
            //     Log.Info("[LongDistancePathfinder] Waiting for regular pathfinder to complete current chunk...");
            // }
        }

        public static void StopPathfinding()
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
            _currentChunkSize = 10;

            // Clear the full tile path queue and failed tiles
            int queueSize = _fullTilePath.Count;
            while (_fullTilePath.TryDequeue(out _)) { }
            _failedTiles.Clear();

            Log.Info($"[LongDistancePathfinder] Pathfinding stopped - cleared {queueSize} tiles from queue");
        }

        public static void StopPathfindingWithMessage()
        {
            if (!_pathfindingInProgress)
                return; // Already stopped

            StopPathfinding();
            GameActions.Print("Long distance pathfinding stopped");
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

                    _disableLongDistanceForWaypoints = true;
                    try
                    {
                        bool result = World.Instance.Player.Pathfinder.WalkTo(x, y, z, distance);
                        Log.Info($"[LongDistancePathfinder] Regular pathfinder result: {result}");
                        return result;
                    }
                    finally
                    {
                        _disableLongDistanceForWaypoints = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _disableLongDistanceForWaypoints = false;
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

            // Clear previous data
            ClearPathfinding();

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

            _openSet.Enqueue(startNode, startNode.Cost);

            LongPathNode goalNode = null;
            int nodesProcessed = 0;

            Log.Info($"[LongDistancePathfinder] Starting A* search for full tile path");

            while (_openSet.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                LongPathNode currentNode = _openSet.Dequeue();
                (int X, int Y) key = (currentNode.X, currentNode.Y);

                if (_closedSet.ContainsKey(key))
                    continue;

                _closedSet[key] = currentNode;
                nodesProcessed++;

                // Check if we reached the exact target
                if (currentNode.X == targetX && currentNode.Y == targetY)
                {
                    goalNode = currentNode;
                    Log.Info($"[LongDistancePathfinder] Found exact target at ({currentNode.X}, {currentNode.Y})");
                    break;
                }

                // Generate neighboring nodes (now using single-tile steps for full path)
                GenerateNeighborsForFullPath(currentNode);

                // Yield periodically to prevent blocking
                if (nodesProcessed % 100 == 0)
                {
                    Thread.Sleep(1); // Brief yield
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            Log.Info($"[LongDistancePathfinder] A* search completed. Nodes processed: {nodesProcessed}, Goal found: {goalNode != null}, OpenSet remaining: {_openSet.Count}");

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
                LongPathNode bestNode = FindClosestNodeToTarget();

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

        private static void GenerateNeighborsForFullPath(LongPathNode currentNode)
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
                int newX = currentNode.X;
                int newY = currentNode.Y;

                // Calculate direction offsets (single tile moves)
                switch (dir)
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

                // Check bounds
                if (newX < 0 || newY < 0 || newX >= 65536 || newY >= 65536)
                    continue;

                (int newX, int newY) key = (newX, newY);
                if (_closedSet.ContainsKey(key))
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

                _openSet.Enqueue(neighborNode, newCost);
                neighborsGenerated++;
            }

            // If no good directions found, try all directions as fallback
            if (!foundGoodDirection)
            {
                for (int dir = 0; dir < 8; dir++)
                {
                    if (directions.Contains(dir)) continue; // Already tried

                    int newX = currentNode.X;
                    int newY = currentNode.Y;

                    // Calculate direction offsets (single tile moves)
                    switch (dir)
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

                    // Check bounds
                    if (newX < 0 || newY < 0 || newX >= 65536 || newY >= 65536)
                        continue;

                    (int newX, int newY) key = (newX, newY);
                    if (_closedSet.ContainsKey(key))
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

                    _openSet.Enqueue(neighborNode, newCost);
                    neighborsGenerated++;
                }
            }

            //Log.Debug($"[LongDistancePathfinder] Generated {neighborsGenerated} prioritized neighbors from ({currentNode.X}, {currentNode.Y})");
        }

        private static bool IsGenerallyWalkable(int x, int y) => WalkableManager.Instance.IsWalkable(x, y);

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

        private static LongPathNode FindClosestNodeToTarget()
        {
            LongPathNode bestNode = null;
            int bestDistance = int.MaxValue;

            foreach (LongPathNode node in _closedSet.Values)
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

        private static void ClearPathfinding()
        {
            _closedSet.Clear();
            _openSet.Clear();
            _pathResult.Clear();
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

        private class PriorityQueue<T>
        {
            private readonly List<(T item, int priority)> _items = new();

            public int Count => _items.Count;

            public void Enqueue(T item, int priority) => _items.Add((item, priority));

            public T Dequeue()
            {
                if (_items.Count == 0)
                    throw new InvalidOperationException("Queue is empty");

                int bestIndex = 0;
                for (int i = 1; i < _items.Count; i++)
                {
                    if (_items[i].priority < _items[bestIndex].priority)
                    {
                        bestIndex = i;
                    }
                }

                (T item, int priority) best = _items[bestIndex];
                _items.RemoveAt(bestIndex);
                return best.item;
            }

            public void Clear() => _items.Clear();
        }
    }
}
