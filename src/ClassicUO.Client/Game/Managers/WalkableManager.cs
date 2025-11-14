using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    public sealed class WalkableManager
    {
        private static readonly Lazy<WalkableManager> _instance = new Lazy<WalkableManager>(() => new WalkableManager());
        private const int CHUNK_SIZE = 8;
        private const int MAX_MAP_COUNT = 6;

        private readonly Dictionary<int, WalkableMapData> _mapData = new();
        private readonly Dictionary<int, WalkableMapData> _sessionModifications = new();
        private readonly Dictionary<int, bool> _mapGenerationComplete = new();
        private readonly Dictionary<int, int> _mapChunkGenerationIndex = new();
        private readonly object _mapDataLock = new object();
        private readonly object _sessionModificationsLock = new object();
        private int _lastMapIndex = -1;
        private int _updateCounter = 0;
        private volatile bool _isGenerating = false;
        private readonly object _generationLock = new object();
        private int _chunksPerCycle = 1; // Start with 1 for performance measurement
        public int TARGET_GENERATION_TIME_MS = 2;
        private const int MIN_CHUNKS_PER_CYCLE = 1;
        private const int MAX_CHUNKS_PER_CYCLE = 500;
        private readonly Queue<double> _recentGenerationTimes = new();
        private const int PERFORMANCE_SAMPLE_SIZE = 5;

        private WalkableManager()
        {
        }

        public static WalkableManager Instance => _instance.Value;

        public void Initialize()
        {
            CreateCacheDirectory();
            // Maps are now loaded on-demand when first accessed rather than loading all maps at startup

            Client.Settings?.GetAsyncOnMainThread(SettingsScope.Global, Constants.SqlSettings.LONG_DISTANCE_PATHING_SPEED, 2, (s) => TARGET_GENERATION_TIME_MS = s);

#if DEBUG
            World.Instance.CommandManager.Register("walkable", strings =>
            {
                Task.Run(() =>
                {
                    var g = new Gump(World.Instance, 0, 0);
                    Texture2D text = GenerateWalkableTexture(2);
                    var it = new EmbeddedGumpPic(0, 0, text);
                    g.Add(it);
                    g.Disposed += (sender, args) => text?.Dispose();
                    g.WantUpdateSize = true;
                    MainThreadQueue.InvokeOnMainThread(() => UIManager.Add(g));
                });
            });

            World.Instance.CommandManager.Register("checkwalk", strings =>
            {
                int x = World.Instance.Player.X;
                int y =  World.Instance.Player.Y;
                bool walkable = IsWalkable(x, y);
                GameActions.Print($"Is walkable: {x}, {y}: {walkable}", (ushort)(walkable ? 64 : 32));
            });
#endif
        }

        public bool IsWalkable(int x, int y)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return false;

            int mapIndex = World.Instance.Map.Index;

            // Check session modifications first
            lock (_sessionModificationsLock)
                if (_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
                    if (sessionData.HasDataForTile(x, y))
                        return sessionData.GetWalkable(x, y);

            // Check persistent data
            lock (_mapDataLock)
                if (_mapData.TryGetValue(mapIndex, out WalkableMapData mapData))
                    if (mapData.HasDataForTile(x, y))
                        return mapData.GetWalkable(x, y);

            // If no data exists, calculate on demand
            return CalculateWalkabilityForTile(x, y);
        }

        public void SetSessionWalkable(int x, int y, bool walkable)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return;

            int mapIndex = World.Instance.Map.Index;

            lock (_sessionModificationsLock)
            {
                if (!_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
                {
                    sessionData = new WalkableMapData(mapIndex);
                    _sessionModifications[mapIndex] = sessionData;
                }

                sessionData.SetWalkable(x, y, walkable);
            }
        }

        public void ClearSessionWalkable(int x, int y)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return;

            int mapIndex = World.Instance.Map.Index;

            lock (_sessionModificationsLock)
                if (_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
                    sessionData.ClearWalkable(x, y);
        }

        public void Update()
        {
            if (World.Instance == null || !World.Instance.InGame)
                return;

            int mapIndex = World.Instance.Map.Index;

            // Check if map has changed
            if (_lastMapIndex != mapIndex)
            {
                _lastMapIndex = mapIndex;
                ClearSessionModifications(); // Clear session mods when changing maps

                // Ensure the new map is loaded
                EnsureMapLoaded(mapIndex);
            }

            // Only generate chunks if the map isn't fully generated yet
            if (!IsMapGenerationComplete(mapIndex))
            {
                _updateCounter++;

                // Generate chunks every other update
                if (_updateCounter % 2 == 0) GenerateNextChunks(_chunksPerCycle);
            }
        }

        private void EnsureMapLoaded(int mapIndex)
        {
            // Check if map is already loaded
            lock (_mapDataLock)
            {
                if (_mapData.ContainsKey(mapIndex))
                    return; // Map already loaded
            }

            // Load the map data from disk or start fresh generation
            Log.Info($"[WalkableManager] Loading map {mapIndex} on demand");
            LoadMapData(mapIndex);
        }

        public bool IsMapGenerationComplete(int mapIndex) => _mapGenerationComplete.TryGetValue(mapIndex, out bool isComplete) && isComplete;

        public (int current, int total) GetMapGenerationProgress(int mapIndex)
        {
            if (IsMapGenerationComplete(mapIndex))
            {
                int totalChunksX = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
                int totalChunksY = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];
                int totalChunks = totalChunksX * totalChunksY;
                return (totalChunks, totalChunks);
            }

            int currentIndex = _mapChunkGenerationIndex.TryGetValue(mapIndex, out int index) ? index : 0;
            int totalChunksXCalc = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
            int totalChunksYCalc = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];
            int totalChunksCalc = totalChunksXCalc * totalChunksYCalc;

            return (currentIndex, totalChunksCalc);
        }

        public (int current, int total) GetCurrentMapGenerationProgress()
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return (0, 0);

            return GetMapGenerationProgress(World.Instance.Map.Index);
        }

        private void GenerateNextChunks(int numChunks)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null || _isGenerating)
                return;

            lock (_generationLock)
            {
                if (_isGenerating)
                    return;

                _isGenerating = true;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                int mapIndex = World.Instance.Map.Index;

                WalkableMapData mapData;
                lock (_mapDataLock)
                    if (!_mapData.TryGetValue(mapIndex, out mapData))
                    {
                        mapData = new WalkableMapData(mapIndex);

                        // Set checksum for new map data
                        try
                        {
                            string currentChecksum = MapChecksumCalculator.CalculateMapChecksum(
                                mapIndex,
                                Client.Game.UO.FileManager.Maps.MapBlocksSize,
                                Client.Game.UO.FileManager.Maps.MapsDefaultSize,
                                Client.Game.UO.FileManager.Version.ToString()
                            );
                            mapData.MapChecksum = currentChecksum;
                        }
                        catch (Exception checksumEx)
                        {
                            Log.Warn($"[WalkableManager] Failed to calculate checksum for new map {mapIndex}: {checksumEx.Message}");
                        }

                        _mapData[mapIndex] = mapData;
                    }

                // Calculate chunks to generate
                int totalChunksX = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
                int totalChunksY = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];
                int totalChunks = totalChunksX * totalChunksY;

                int currentIndex = _mapChunkGenerationIndex.TryGetValue(mapIndex, out int index) ? index : 0;

                // Generate multiple chunks up to the requested number
                int chunksGenerated = 0;
                while (chunksGenerated < numChunks && currentIndex < totalChunks)
                {
                    int chunkX = currentIndex / totalChunksY;
                    int chunkY = currentIndex % totalChunksY;

                    GenerateChunkWalkabilitySync(chunkX, chunkY, mapData);

                    currentIndex++;
                    chunksGenerated++;
                }

                // Update the generation index
                _mapChunkGenerationIndex[mapIndex] = currentIndex;

                // Check if generation is complete
                if (currentIndex >= totalChunks)
                {
                    _mapGenerationComplete[mapIndex] = true;
                    Log.Info($"[WalkableManager] Map {mapIndex} generation completed. Total chunks: {totalChunks}");
                    GameActions.Print($"Pathfinding cache completed for map {mapIndex}!", 87);
                }
            }
            finally
            {
                stopwatch.Stop();

                // Record performance and adjust chunks per cycle
                double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                AdjustChunksPerCycleBasedOnPerformance(elapsedMs, numChunks);

                lock (_generationLock) _isGenerating = false;
            }
        }

        private void AdjustChunksPerCycleBasedOnPerformance(double elapsedMs, int chunksGenerated)
        {
            // Only adjust if we actually generated chunks
            if (chunksGenerated == 0)
                return;

            // Add to recent generation times for smoothing
            _recentGenerationTimes.Enqueue(elapsedMs);
            if (_recentGenerationTimes.Count > PERFORMANCE_SAMPLE_SIZE) _recentGenerationTimes.Dequeue();

            // Only adjust after we have enough samples
            if (_recentGenerationTimes.Count < PERFORMANCE_SAMPLE_SIZE)
                return;

            // Calculate average time from recent samples
            double avgTime = 0;
            foreach (double time in _recentGenerationTimes) avgTime += time;
            avgTime /= _recentGenerationTimes.Count;

            int oldChunksPerCycle = _chunksPerCycle;

            // Adjust chunks per cycle based on performance
            if (avgTime < TARGET_GENERATION_TIME_MS * 0.8) // If we're significantly under target
                // Increase chunks per cycle
                _chunksPerCycle = Math.Min(_chunksPerCycle + 1, MAX_CHUNKS_PER_CYCLE);
            else if (avgTime > TARGET_GENERATION_TIME_MS * 1.2) // If we're significantly over target
                // Decrease chunks per cycle
                _chunksPerCycle = Math.Max(_chunksPerCycle - 1, MIN_CHUNKS_PER_CYCLE);

            // Log performance adjustments
            if (_chunksPerCycle != oldChunksPerCycle)
            {
                Log.Debug($"[WalkableManager] Performance adjustment: {oldChunksPerCycle} -> {_chunksPerCycle} chunks/cycle (avg: {avgTime:F1}ms, target: {TARGET_GENERATION_TIME_MS}ms)");

                // Clear samples when we make an adjustment to get fresh data
                _recentGenerationTimes.Clear();
            }
        }

        private void GenerateChunkWalkabilitySync(int chunkX, int chunkY, WalkableMapData mapData)
        {
            try
            {
                // Generate walkability data for an 8x8 chunk synchronously
                for (int x = chunkX * CHUNK_SIZE; x < (chunkX + 1) * CHUNK_SIZE; x++)
                for (int y = chunkY * CHUNK_SIZE; y < (chunkY + 1) * CHUNK_SIZE; y++)
                    if (!mapData.HasDataForTile(x, y))
                    {
                        bool walkable = CalculateWalkabilityForTile(x, y);
                        mapData.SetWalkable(x, y, walkable);
                    }
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Error generating chunk ({chunkX}, {chunkY}): {ex.Message}");
            }
        }

        private static Direction _irrelevantDirection = Direction.NONE;

        private bool CalculateWalkabilityForTile(int x, int y)
        {
            try
            {
                if (World.Instance.Map == null)
                {
                    Log.Debug($"[WalkableManager] World.Instance.Map is null");
                    return false;
                }

                if (World.Instance.Player == null || World.Instance.Player.Pathfinder == null)
                {
                    Log.Debug($"[WalkableManager] Player or Pathfinder is null");
                    return false;
                }

                GameObject tile = World.Instance.Map.GetTile(x, y);
                if (tile == null)
                {
                    Log.Debug($"[WalkableManager] No tile found at ({x}, {y})");
                    return false;
                }

                sbyte z = World.Instance.Map.GetTileZ(x, y);
                return CheckTileWalkability(x, y, z);
            }
            catch (Exception ex)
            {
                Log.Warn($"[WalkableManager] Error calculating walkability at ({x}, {y}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a specific tile is walkable based on terrain, statics, items, and mobiles.
        /// This method replicates the pathfinder's walkability logic without requiring actual pathfinding.
        /// </summary>
        /// <param name="x">X coordinate of the tile</param>
        /// <param name="y">Y coordinate of the tile</param>
        /// <param name="z">Z coordinate to check from</param>
        /// <returns>True if the tile is walkable, false otherwise</returns>
        public bool CheckTileWalkability(int x, int y, sbyte z)
        {
            if (World.Instance?.Map == null || World.Instance.Player == null)
                return false;

            GameObject tile = World.Instance.Map.GetTile(x, y, false);
            if (tile == null)
                return false;

            // Find the first object in the tile chain
            GameObject obj = tile;
            while (obj.TPrevious != null)
                obj = obj.TPrevious;

            bool hasWalkableSurface = false;
            int surfaceZ = -128;

            // Iterate through all objects at this tile
            for (; obj != null; obj = obj.TNext)
            {
                // Skip objects below player in custom houses
                if (World.Instance.CustomHouseManager != null && obj.Z < World.Instance.Player.Z)
                    continue;

                switch (obj)
                {
                    case Land land:
                        // Check if land tile is valid (not a "no draw" tile)
                        ushort landGraphic = land.Graphic;
                        if (landGraphic < 0x01AE && landGraphic != 2 || landGraphic > 0x01B5 && landGraphic != 0x01DB)
                            if (!land.TileData.IsImpassable)
                            {
                                int landZ = land.AverageZ;
                                if (Math.Abs(landZ - z) <= Constants.DEFAULT_BLOCK_HEIGHT)
                                {
                                    hasWalkableSurface = true;
                                    if (landZ > surfaceZ)
                                        surfaceZ = landZ;
                                }
                            }

                        break;

                    case GameEffect:
                        // Ignore effects
                    case Mobile:
                        // Ignore mobiles
                        break;

                    case Static stat:
                        if (stat.ItemData.IsImpassable) return false;
                        if (stat.ItemData.IsWall) return false;
                        break;

                    case Item item:
                        ushort itemGraphic = item.Graphic;
                        ref StaticTiles itemData = ref Client.Game.UO.FileManager.TileData.StaticData[itemGraphic];

                        // Certain graphics are always passable
                        bool dropFlags = itemGraphic >= 0x3946 && itemGraphic <= 0x3964 || itemGraphic == 0x0082;

                        if (!dropFlags)
                        {
                            if (itemData.IsImpassable)
                            {
                                int itemZ = item.Z;
                                int itemTop = itemZ + itemData.Height;
                                // Check if item blocks at current Z
                                if (itemZ <= z && itemTop > z) return false; // Blocked by item
                            }
                            else if (itemData.IsSurface || itemData.IsBridge)
                            {
                                int itemZ = item.Z;
                                int surfaceHeight = itemData.Height;

                                if (itemData.IsBridge)
                                    surfaceHeight /= 2;

                                int itemSurfaceZ = itemZ + surfaceHeight;

                                if (Math.Abs(itemSurfaceZ - z) <= Constants.DEFAULT_BLOCK_HEIGHT)
                                {
                                    hasWalkableSurface = true;
                                    if (itemSurfaceZ > surfaceZ)
                                        surfaceZ = itemSurfaceZ;
                                }
                            }
                        }
                        break;

                    case Multi multi:
                        // Handle multi objects (houses, etc.)
                        if ((World.Instance.CustomHouseManager != null && multi.IsCustom &&
                            (multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0) ||
                            multi.IsHousePreview)
                            break; // Skip these multis

                        if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0) break; // Skip ignored multis

                        ushort multiGraphic = multi.Graphic;
                        ref StaticTiles multiData = ref Client.Game.UO.FileManager.TileData.StaticData[multiGraphic];

                        if (multiData.IsImpassable)
                        {
                            int multiZ = multi.Z;
                            int multiTop = multiZ + multiData.Height;
                            if (multiZ <= z && multiTop > z) return false; // Blocked by multi
                        }
                        else if (multiData.IsSurface || multiData.IsBridge)
                        {
                            int multiZ = multi.Z;
                            int surfaceHeight = multiData.Height;

                            if (multiData.IsBridge)
                                surfaceHeight /= 2;

                            int multiSurfaceZ = multiZ + surfaceHeight;

                            if (Math.Abs(multiSurfaceZ - z) <= Constants.DEFAULT_BLOCK_HEIGHT)
                            {
                                hasWalkableSurface = true;
                                if (multiSurfaceZ > surfaceZ)
                                    surfaceZ = multiSurfaceZ;
                            }
                        }
                        break;
                }
            }

            return hasWalkableSurface;
        }

        /// <summary>
        /// Generates a texture visualization of the current map's walkable status.
        /// Green pixels represent walkable tiles, red pixels represent non-walkable tiles.
        /// Black pixels represent tiles that haven't been generated yet.
        /// </summary>
        /// <param name="scale">Scale factor for the texture. 1 = 1 pixel per tile, 2 = 1 pixel per 2x2 tiles, etc.</param>
        /// <returns>A Texture2D showing the walkable status, or null if unavailable</returns>
        public Texture2D GenerateWalkableTexture(int scale = 1)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
            {
                Log.Warn("[WalkableManager] Cannot generate texture: World or Map not available");
                return null;
            }

            if (Client.Game?.GraphicsDevice == null)
            {
                Log.Warn("[WalkableManager] Cannot generate texture: GraphicsDevice not available");
                return null;
            }

            if (scale < 1)
                scale = 1;

            int mapIndex = World.Instance.Map.Index;

            // Get map dimensions in tiles
            int mapWidthInBlocks = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
            int mapHeightInBlocks = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];
            int mapWidthInTiles = mapWidthInBlocks * 8;
            int mapHeightInTiles = mapHeightInBlocks * 8;

            // Calculate texture dimensions based on scale
            int textureWidth = (mapWidthInTiles + scale - 1) / scale;
            int textureHeight = (mapHeightInTiles + scale - 1) / scale;

            Log.Info($"[WalkableManager] Generating walkable texture for map {mapIndex}: {textureWidth}x{textureHeight} (scale {scale})");

            // Create color array for the texture
            var colors = new Color[textureWidth * textureHeight];

            // Define colors
            Color walkableColor = Color.Green;
            Color nonWalkableColor = Color.Red;
            Color unknownColor = Color.Black;

            WalkableMapData mapData = null;
            lock (_mapDataLock)
                if (_mapData.TryGetValue(mapIndex, out mapData))
                {
                    // Map data exists, use it
                }

            // Fill the color array
            for (int pixelY = 0; pixelY < textureHeight; pixelY++)
            for (int pixelX = 0; pixelX < textureWidth; pixelX++)
            {
                // Calculate the tile position this pixel represents
                int tileX = pixelX * scale;
                int tileY = pixelY * scale;

                // For scaled textures, check multiple tiles and use majority vote
                int walkableCount = 0;
                int nonWalkableCount = 0;
                int unknownCount = 0;

                for (int dy = 0; dy < scale && (tileY + dy) < mapHeightInTiles; dy++)
                for (int dx = 0; dx < scale && (tileX + dx) < mapWidthInTiles; dx++)
                {
                    int checkX = tileX + dx;
                    int checkY = tileY + dy;

                    if (mapData != null && mapData.HasDataForTile(checkX, checkY))
                    {
                        if (mapData.GetWalkable(checkX, checkY))
                            walkableCount++;
                        else
                            nonWalkableCount++;
                    }
                    else
                        unknownCount++;
                }

                // Determine pixel color based on majority
                Color pixelColor;
                if (unknownCount > (walkableCount + nonWalkableCount))
                    pixelColor = unknownColor;
                else if (walkableCount > nonWalkableCount)
                    pixelColor = walkableColor;
                else
                    pixelColor = nonWalkableColor;

                colors[pixelY * textureWidth + pixelX] = pixelColor;
            }

            // Create the texture
            try
            {
                var texture = new Texture2D(
                    Client.Game.GraphicsDevice,
                    textureWidth,
                    textureHeight,
                    false,
                    SurfaceFormat.Color
                );

                texture.SetData(colors);

                Log.Info($"[WalkableManager] Walkable texture generated successfully: {textureWidth}x{textureHeight}");
                return texture;
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to create walkable texture: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates and saves a walkable texture to a PNG file in the Data directory.
        /// </summary>
        /// <param name="scale">Scale factor for the texture. Higher values = smaller file size</param>
        /// <param name="filename">Optional custom filename (without extension)</param>
        /// <returns>The file path if successful, null otherwise</returns>
        public string SaveWalkableTextureToPng(int scale = 4, string filename = null)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
            {
                Log.Warn("[WalkableManager] Cannot save texture: World or Map not available");
                return null;
            }

            try
            {
                Texture2D texture = GenerateWalkableTexture(scale);
                if (texture == null)
                {
                    Log.Warn("[WalkableManager] Failed to generate texture for saving");
                    return null;
                }

                // Create output directory
                string outputDir = Path.Combine(
                    CUOEnviroment.ExecutablePath,
                    "Data",
                    FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName)
                );

                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                // Generate filename
                if (string.IsNullOrEmpty(filename))
                {
                    int mapIndex = World.Instance.Map.Index;
                    filename = $"walkable_map_{mapIndex}_scale{scale}_{DateTime.Now:yyyyMMdd_HHmmss}";
                }

                string filePath = Path.Combine(outputDir, filename + ".png");

                // Save texture to file
                using (FileStream fileStream = File.Create(filePath)) texture.SaveAsPng(fileStream, texture.Width, texture.Height);

                // Dispose the texture
                texture.Dispose();

                Log.Info($"[WalkableManager] Walkable texture saved to: {filePath}");
                GameActions.Print($"Walkable map saved to: {filePath}", 87);

                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to save walkable texture: {ex.Message}");
                return null;
            }
        }

        private void CreateCacheDirectory()
        {
            try
            {
                string cacheDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName));

                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to create cache directory: {ex.Message}");
            }
        }

        private void LoadAllMapData()
        {
            for (int mapIndex = 0; mapIndex < MAX_MAP_COUNT; mapIndex++) LoadMapData(mapIndex);
        }

        private void LoadMapData(int mapIndex)
        {
            try
            {
                string filename = GetMapDataFileName(mapIndex);
                if (File.Exists(filename))
                {
                    var mapData = new WalkableMapData(mapIndex);
                    bool loadSuccess = false;

                    try
                    {
                        mapData.LoadFromFile(filename);
                        loadSuccess = true;
                    }
                    catch (IOException ex) when (ex.Message.Contains("Old file format detected"))
                    {
                        // Delete old format file and start fresh
                        Log.Info($"[WalkableManager] Map {mapIndex} has old format without checksum, deleting and regenerating...");
                        try
                        {
                            File.Delete(filename);
                            Log.Info($"[WalkableManager] Deleted old format file: {filename}");
                        }
                        catch (Exception deleteEx)
                        {
                            Log.Warn($"[WalkableManager] Failed to delete old format file {filename}: {deleteEx.Message}");
                        }
                        loadSuccess = false;
                    }
                    catch (Exception loadEx)
                    {
                        Log.Warn($"[WalkableManager] Failed to load map data file for map {mapIndex}: {loadEx.Message}");
                        loadSuccess = false;
                    }

                    if (!loadSuccess)
                    {
                        // Start fresh with new format
                        StartFreshGeneration(mapIndex);
                        return;
                    }

                    // File loaded successfully, validate checksum
                    bool checksumValid = false;
                    string currentChecksum;

                    try
                    {
                        currentChecksum = MapChecksumCalculator.CalculateMapChecksum(
                            mapIndex,
                            Client.Game.UO.FileManager.Maps.MapBlocksSize,
                            Client.Game.UO.FileManager.Maps.MapsDefaultSize,
                            Client.Game.UO.FileManager.Version.ToString()
                        );

                        checksumValid = MapChecksumCalculator.ValidateChecksum(mapData.MapChecksum, currentChecksum);

                        if (!checksumValid)
                        {
                            Log.Info($"[WalkableManager] Map {mapIndex} checksum mismatch - map data may have changed, regenerating...");
                            Log.Debug($"[WalkableManager] Stored checksum: {mapData.MapChecksum}");
                            Log.Debug($"[WalkableManager] Current checksum: {currentChecksum}");
                        }
                        else
                            Log.Info($"[WalkableManager] Map {mapIndex} checksum valid, using cached data");
                    }
                    catch (Exception checksumEx)
                    {
                        Log.Warn($"[WalkableManager] Failed to validate checksum for map {mapIndex}: {checksumEx.Message}");
                        checksumValid = false; // Force regeneration if checksum validation fails
                    }

                    // If checksum is invalid, start fresh generation
                    if (!checksumValid)
                    {
                        StartFreshGeneration(mapIndex);
                        return;
                    }

                    // Checksum is valid, proceed with normal loading
                    lock (_mapDataLock) _mapData[mapIndex] = mapData;

                    // Check if this map was fully generated
                    int totalChunksX = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
                    int totalChunksY = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];
                    int totalChunks = totalChunksX * totalChunksY;

                    // Calculate how many generation chunks we have completed
                    int completedChunks = mapData.CalculateGenerationProgress(mapIndex);

                    if (completedChunks >= totalChunks)
                    {
                        // Map is fully generated
                        _mapGenerationComplete[mapIndex] = true;
                        Log.Info($"[WalkableManager] Map {mapIndex} loaded - fully generated ({completedChunks}/{totalChunks} chunks)");
                    }
                    else
                    {
                        // Continue generation from where we left off
                        _mapChunkGenerationIndex[mapIndex] = completedChunks;
                        Log.Info($"[WalkableManager] Map {mapIndex} loaded - continuing generation from chunk {completedChunks}/{totalChunks}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to load map data for map {mapIndex}: {ex.Message}");
                // If anything goes wrong, start fresh
                StartFreshGeneration(mapIndex);
            }
        }

        internal void StartFreshGeneration(int mapIndex)
        {
            var mapData = new WalkableMapData(mapIndex);

            // Set checksum for new map data
            try
            {
                string currentChecksum = MapChecksumCalculator.CalculateMapChecksum(
                    mapIndex,
                    Client.Game.UO.FileManager.Maps.MapBlocksSize,
                    Client.Game.UO.FileManager.Maps.MapsDefaultSize,
                    Client.Game.UO.FileManager.Version.ToString()
                );
                mapData.MapChecksum = currentChecksum;
            }
            catch (Exception checksumEx)
            {
                Log.Warn($"[WalkableManager] Failed to calculate checksum for new map {mapIndex}: {checksumEx.Message}");
            }

            lock (_mapDataLock) _mapData[mapIndex] = mapData;

            // Reset generation state to start from beginning
            _mapChunkGenerationIndex[mapIndex] = 0;
            _mapGenerationComplete[mapIndex] = false;

            Log.Info($"[WalkableManager] Map {mapIndex} will be generated from scratch with new checksum format");
        }

        public void SaveMapData(int mapIndex)
        {
            try
            {
                WalkableMapData mapData;
                lock (_mapDataLock)
                    if (!_mapData.TryGetValue(mapIndex, out mapData))
                        return;

                // Ensure checksum is current before saving
                try
                {
                    string currentChecksum = MapChecksumCalculator.CalculateMapChecksum(
                        mapIndex,
                        Client.Game.UO.FileManager.Maps.MapBlocksSize,
                        Client.Game.UO.FileManager.Maps.MapsDefaultSize,
                        Client.Game.UO.FileManager.Version.ToString()
                    );
                    mapData.MapChecksum = currentChecksum;
                }
                catch (Exception checksumEx)
                {
                    Log.Warn($"[WalkableManager] Failed to calculate checksum for map {mapIndex} before saving: {checksumEx.Message}");
                    // Continue with save even if checksum calculation fails
                }

                string filename = GetMapDataFileName(mapIndex);
                mapData.SaveToFile(filename);
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to save map data for map {mapIndex}: {ex.Message}");
            }
        }

        public void SaveAllMapData()
        {
            int[] mapIndices;
            lock (_mapDataLock)
            {
                mapIndices = new int[_mapData.Count];
                _mapData.Keys.CopyTo(mapIndices, 0);
            }

            foreach (int mapIndex in mapIndices) SaveMapData(mapIndex);
        }

        private string GetMapDataFileName(int mapIndex) => Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName), $"walkable_map_{mapIndex}.dat");

        public void ClearSessionModifications()
        {
            lock (_sessionModificationsLock) _sessionModifications.Clear();
        }

        public void Shutdown()
        {
            SaveAllMapData();

            lock (_mapDataLock) _mapData.Clear();

            lock (_sessionModificationsLock) _sessionModifications.Clear();
        }
    }

    internal sealed class WalkableMapData
    {
        private const int FILE_VERSION = 2; // Version 1: original, Version 2: with checksum

        private readonly int _mapIndex;
        private readonly Dictionary<long, BitArray8x8> _chunks = new();
        private readonly object _dataLock = new object();
        private string _mapChecksum = string.Empty;

        public WalkableMapData(int mapIndex)
        {
            _mapIndex = mapIndex;
        }

        public string MapChecksum
        {
            get => _mapChecksum;
            set => _mapChecksum = value ?? string.Empty;
        }

        public bool HasDataForTile(int x, int y)
        {
            lock (_dataLock) return HasDataForTileUnsafe(x, y);
        }

        private bool HasDataForTileUnsafe(int x, int y)
        {
            long chunkKey = GetChunkKey(x >> 3, y >> 3); // Changed from >> 5 to >> 3 for 8x8 chunks
            if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                // Check if this specific tile has been set (not just if the chunk exists)
                return chunk.IsSet(x & 7, y & 7);
            return false;
        }

        public bool GetWalkable(int x, int y)
        {
            long chunkKey = GetChunkKey(x >> 3, y >> 3); // Changed from >> 5 to >> 3 for 8x8 chunks
            lock (_dataLock)
                if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                    return chunk.Get(x & 7, y & 7); // Changed from & 31 to & 7 for 8x8 chunks

            return false;
        }

        public void SetWalkable(int x, int y, bool walkable)
        {
            long chunkKey = GetChunkKey(x >> 3, y >> 3); // Changed from >> 5 to >> 3 for 8x8 chunks
            lock (_dataLock)
            {
                if (!_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                {
                    chunk = new BitArray8x8(); // Changed from BitArray32x32 to BitArray8x8
                    _chunks[chunkKey] = chunk;
                }
                chunk.Set(x & 7, y & 7, walkable); // Changed from & 31 to & 7 for 8x8 chunks
            }
        }

        public void ClearWalkable(int x, int y)
        {
            long chunkKey = GetChunkKey(x >> 3, y >> 3);
            lock (_dataLock)
                if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                    chunk.Clear(x & 7, y & 7);
        }

        public int CalculateGenerationProgress(int mapIndex)
        {
            // Calculate how many 8x8 map chunks we have data for
            // by checking which generation chunks have any walkable data
            int totalChunksX = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
            int totalChunksY = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];

            int completedChunks = 0;

            lock (_dataLock)
                // Check each 8x8 map chunk to see if we have data for it
                for (int chunkIndex = 0; chunkIndex < totalChunksX * totalChunksY; chunkIndex++)
                {
                    int chunkX = chunkIndex / totalChunksY;
                    int chunkY = chunkIndex % totalChunksY;

                    // Check if this 8x8 chunk has any data by sampling a few tiles
                    bool hasData = false;
                    for (int x = chunkX * 8; x < (chunkX + 1) * 8 && !hasData; x++)
                    for (int y = chunkY * 8; y < (chunkY + 1) * 8 && !hasData; y++)
                        if (HasDataForTileUnsafe(x, y))
                            hasData = true;

                    if (hasData)
                        completedChunks = chunkIndex + 1; // +1 because we completed this chunk
                    else
                        break; // Sequential generation, so we can stop here
                }

            return completedChunks;
        }

        private static long GetChunkKey(int chunkX, int chunkY) => ((long)chunkX << 32) | (uint)chunkY;

        public void SaveToFile(string filename)
        {
            string tempFilename = filename + ".tmp";

            try
            {
                using (var stream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(stream))
                    lock (_dataLock)
                    {
                        // Write file version first for future compatibility
                        writer.Write(FILE_VERSION);

                        // Write original data
                        writer.Write(_mapIndex);
                        writer.Write(_chunks.Count);

                        foreach (KeyValuePair<long, BitArray8x8> kvp in _chunks)
                        {
                            writer.Write(kvp.Key);
                            kvp.Value.WriteTo(writer);
                        }

                        // Write checksum (new in version 2)
                        writer.Write(_mapChecksum ?? string.Empty);
                    }

                // Atomic replace
                if (File.Exists(filename)) File.Delete(filename);
                File.Move(tempFilename, filename);
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tempFilename)) File.Delete(tempFilename);
                }
                catch { }

                throw new IOException($"Failed to save walkable data: {ex.Message}", ex);
            }
        }

        public void LoadFromFile(string filename)
        {
            try
            {
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    // Read version - new format files start with version number
                    int fileVersion = reader.ReadInt32();

                    // Only accept version 2+ files (with checksums)
                    if (fileVersion < 2) throw new InvalidDataException($"Old file format detected (version {fileVersion}), file will be deleted and regenerated");

                    int mapIndex = reader.ReadInt32();

                    if (mapIndex != _mapIndex) throw new InvalidDataException($"Map index mismatch: expected {_mapIndex}, got {mapIndex}");

                    int chunkCount = reader.ReadInt32();

                    lock (_dataLock)
                    {
                        _chunks.Clear();

                        for (int i = 0; i < chunkCount; i++)
                        {
                            long chunkKey = reader.ReadInt64();
                            var chunk = new BitArray8x8();
                            chunk.ReadFrom(reader);
                            _chunks[chunkKey] = chunk;
                        }

                        // Read checksum (required in version 2+)
                        if (stream.Position < stream.Length)
                            _mapChecksum = reader.ReadString();
                        else
                            throw new InvalidDataException("Checksum missing from file");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to load walkable data: {ex.Message}", ex);
            }
        }
    }

    internal sealed class BitArray8x8
    {
        private readonly byte[] _data = new byte[8];
        private readonly byte[] _isset = new byte[8]; // Track which bits have been explicitly set

        public bool Get(int x, int y)
        {
            if (x < 0 || x >= 8 || y < 0 || y >= 8)
                return false;

            return (_data[y] & (1 << x)) != 0;
        }

        public bool IsSet(int x, int y)
        {
            if (x < 0 || x >= 8 || y < 0 || y >= 8)
                return false;

            return (_isset[y] & (1 << x)) != 0;
        }

        public void Set(int x, int y, bool value)
        {
            if (x < 0 || x >= 8 || y < 0 || y >= 8)
                return;

            // Mark this position as set
            _isset[y] |= (byte)(1 << x);

            // Set the actual data value
            if (value)
                _data[y] |= (byte)(1 << x);
            else
                _data[y] &= (byte)~(1 << x);
        }

        public void Clear(int x, int y)
        {
            if (x < 0 || x >= 8 || y < 0 || y >= 8)
                return;

            // Clear the set bit
            _isset[y] &= (byte)~(1 << x);
            // Clear the data bit
            _data[y] &= (byte)~(1 << x);
        }

        public void WriteTo(BinaryWriter writer)
        {
            // Write data array
            for (int i = 0; i < 8; i++) writer.Write(_data[i]);
            // Write isset array
            for (int i = 0; i < 8; i++) writer.Write(_isset[i]);
        }

        public void ReadFrom(BinaryReader reader)
        {
            // Read data array
            for (int i = 0; i < 8; i++) _data[i] = reader.ReadByte();
            // Read isset array
            for (int i = 0; i < 8; i++) _isset[i] = reader.ReadByte();
        }
    }
}
