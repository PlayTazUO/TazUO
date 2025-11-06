using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public sealed class WalkableManager
    {
        private static readonly object _lock = new object();
        private const int CHUNK_SIZE = 8;
        private const int MAX_MAP_COUNT = 6;

        private readonly Dictionary<int, WalkableMapData> _mapData = new();
        private readonly Dictionary<int, WalkableMapData> _sessionModifications = new();
        private readonly Dictionary<int, bool> _mapGenerationComplete = new();
        private readonly Dictionary<int, int> _mapChunkGenerationIndex = new();
        private int _lastMapIndex = -1;
        private int _updateCounter = 0;
        private volatile bool _isGenerating = false;
        private readonly object _generationLock = new object();
        private int _chunksPerCycle = 1; // Start with 1 for performance measurement
        private const int TARGET_GENERATION_TIME_MS = 5;
        private const int MIN_CHUNKS_PER_CYCLE = 1;
        private const int MAX_CHUNKS_PER_CYCLE = 500;
        private readonly List<double> _recentGenerationTimes = new();
        private const int PERFORMANCE_SAMPLE_SIZE = 5;

        private WalkableManager()
        {
        }

        public static WalkableManager Instance
        {
            get
            {
                if (field == null)
                {
                    lock (_lock)
                    {
                        if (field == null)
                            field = new WalkableManager();
                    }
                }
                return field;
            }
        }

        public void Initialize()
        {
            CreateCacheDirectory();
            LoadAllMapData();
        }

        public bool IsWalkable(int x, int y)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return false;

            int mapIndex = World.Instance.Map.Index;

            // Check session modifications first
            if (_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
            {
                if (sessionData.HasDataForTile(x, y))
                {
                    return sessionData.GetWalkable(x, y);
                }
            }

            // Check persistent data
            if (_mapData.TryGetValue(mapIndex, out WalkableMapData mapData))
            {
                if (mapData.HasDataForTile(x, y))
                {
                    return mapData.GetWalkable(x, y);
                }
            }

            // If no data exists, calculate on demand
            return CalculateWalkabilityForTile(x, y);
        }

        public void SetSessionWalkable(int x, int y, bool walkable)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return;

            int mapIndex = World.Instance.Map.Index;

            if (!_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
            {
                sessionData = new WalkableMapData(mapIndex);
                _sessionModifications[mapIndex] = sessionData;
            }

            sessionData.SetWalkable(x, y, walkable);
        }

        public void ClearSessionWalkable(int x, int y)
        {
            if (World.Instance == null || !World.Instance.InGame || World.Instance.Map == null)
                return;

            int mapIndex = World.Instance.Map.Index;

            if (_sessionModifications.TryGetValue(mapIndex, out WalkableMapData sessionData))
            {
                sessionData.ClearWalkable(x, y);
            }
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
            }

            // Only generate chunks if the map isn't fully generated yet
            if (!IsMapGenerationComplete(mapIndex))
            {
                _updateCounter++;

                // Generate chunks every other update
                if (_updateCounter % 2 == 0)
                {
                    GenerateNextChunks(_chunksPerCycle);
                }
            }
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

        private ulong nextUpdateMessage = Time.Ticks;

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

                if (!_mapData.TryGetValue(mapIndex, out WalkableMapData mapData))
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

                if (Time.Ticks > nextUpdateMessage)
                {
                    (int current, int total) val = GetCurrentMapGenerationProgress();
                    GameActions.Print($"Generating pathfinding cache. {MathHelper.PercetangeOf(val.current, val.total)}% ({val.current}/{val.total})", 84);
                    nextUpdateMessage = Time.Ticks + 5000;
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

                lock (_generationLock)
                {
                    _isGenerating = false;
                }
            }
        }

        private void AdjustChunksPerCycleBasedOnPerformance(double elapsedMs, int chunksGenerated)
        {
            // Only adjust if we actually generated chunks
            if (chunksGenerated == 0)
                return;

            // Add to recent generation times for smoothing
            _recentGenerationTimes.Add(elapsedMs);
            if (_recentGenerationTimes.Count > PERFORMANCE_SAMPLE_SIZE)
            {
                _recentGenerationTimes.RemoveAt(0);
            }

            // Only adjust after we have enough samples
            if (_recentGenerationTimes.Count < PERFORMANCE_SAMPLE_SIZE)
                return;

            // Calculate average time from recent samples
            double avgTime = 0;
            foreach (double time in _recentGenerationTimes)
            {
                avgTime += time;
            }
            avgTime /= _recentGenerationTimes.Count;

            int oldChunksPerCycle = _chunksPerCycle;

            // Adjust chunks per cycle based on performance
            if (avgTime < TARGET_GENERATION_TIME_MS * 0.8) // If we're significantly under target
            {
                // Increase chunks per cycle
                _chunksPerCycle = Math.Min(_chunksPerCycle + 1, MAX_CHUNKS_PER_CYCLE);
            }
            else if (avgTime > TARGET_GENERATION_TIME_MS * 1.2) // If we're significantly over target
            {
                // Decrease chunks per cycle
                _chunksPerCycle = Math.Max(_chunksPerCycle - 1, MIN_CHUNKS_PER_CYCLE);
            }

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
                {
                    for (int y = chunkY * CHUNK_SIZE; y < (chunkY + 1) * CHUNK_SIZE; y++)
                    {
                        if (!mapData.HasDataForTile(x, y))
                        {
                            bool walkable = CalculateWalkabilityForTile(x, y);
                            mapData.SetWalkable(x, y, walkable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Error generating chunk ({chunkX}, {chunkY}): {ex.Message}");
            }
        }

        private static Direction _unreleventDirection = Direction.NONE;

        private bool CalculateWalkabilityForTile(int x, int y)
        {
            try
            {
                if (World.Instance.Map == null)
                {
                    Log.Debug($"[WalkableManager] World.Instance.Map is null");
                    return false;
                }

                GameObject tile = World.Instance.Map.GetTile(x, y);
                if (tile == null)
                {
                    Log.Debug($"[WalkableManager] No tile found at ({x}, {y})");
                    return false;
                }

                // For now, let's use a very basic walkability check
                // If we can get a tile, and it's a land tile, consider it walkable
                // if (tile is Land land)
                // {
                //     //Log.Debug($"[WalkableManager] Land tile at ({x}, {y}) z={z}: true");
                //
                //     return true; // Very permissive for now
                // }
                sbyte z = World.Instance.Map.GetTileZ(x, y);
                return World.Instance.Player.Pathfinder.CanWalk(ref _unreleventDirection, ref x, ref y, ref z, true);
            }
            catch (Exception ex)
            {
                Log.Warn($"[WalkableManager] Error calculating walkability at ({x}, {y}): {ex.Message}");
                return false;
            }
        }

        private void CreateCacheDirectory()
        {
            try
            {
                string cacheDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName));

                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to create cache directory: {ex.Message}");
            }
        }

        private void LoadAllMapData()
        {
            for (int mapIndex = 0; mapIndex < MAX_MAP_COUNT; mapIndex++)
            {
                LoadMapData(mapIndex);
            }
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
                        {
                            Log.Info($"[WalkableManager] Map {mapIndex} checksum valid, using cached data");
                        }
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
                    _mapData[mapIndex] = mapData;

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

        private void StartFreshGeneration(int mapIndex)
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

            _mapData[mapIndex] = mapData;

            // Reset generation state to start from beginning
            _mapChunkGenerationIndex[mapIndex] = 0;
            _mapGenerationComplete[mapIndex] = false;

            Log.Info($"[WalkableManager] Map {mapIndex} will be generated from scratch with new checksum format");
        }

        public void SaveMapData(int mapIndex)
        {
            try
            {
                if (_mapData.TryGetValue(mapIndex, out WalkableMapData mapData))
                {
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
            }
            catch (Exception ex)
            {
                Log.Error($"[WalkableManager] Failed to save map data for map {mapIndex}: {ex.Message}");
            }
        }

        public void SaveAllMapData()
        {
            foreach (KeyValuePair<int, WalkableMapData> kvp in _mapData)
            {
                SaveMapData(kvp.Key);
            }
        }

        private string GetMapDataFileName(int mapIndex) => Path.Combine(CUOEnviroment.ExecutablePath, "Data", FileSystemHelper.RemoveInvalidChars(World.Instance.ServerName), $"walkable_map_{mapIndex}.dat");

        public void ClearSessionModifications() => _sessionModifications.Clear();

        public void Shutdown()
        {
            SaveAllMapData();
            _mapData.Clear();
            _sessionModifications.Clear();
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
            long chunkKey = GetChunkKey(x >> 3, y >> 3); // Changed from >> 5 to >> 3 for 8x8 chunks
            lock (_dataLock)
            {
                if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                {
                    // Check if this specific tile has been set (not just if the chunk exists)
                    return chunk.IsSet(x & 7, y & 7);
                }
                return false;
            }
        }

        public bool GetWalkable(int x, int y)
        {
            long chunkKey = GetChunkKey(x >> 3, y >> 3); // Changed from >> 5 to >> 3 for 8x8 chunks
            lock (_dataLock)
            {
                if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                {
                    return chunk.Get(x & 7, y & 7); // Changed from & 31 to & 7 for 8x8 chunks
                }
            }
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
            {
                if (_chunks.TryGetValue(chunkKey, out BitArray8x8 chunk))
                {
                    chunk.Clear(x & 7, y & 7);
                }
            }
        }

        public int CalculateGenerationProgress(int mapIndex)
        {
            // Calculate how many 8x8 map chunks we have data for
            // by checking which generation chunks have any walkable data
            int totalChunksX = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 0];
            int totalChunksY = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapIndex, 1];

            int completedChunks = 0;

            lock (_dataLock)
            {
                // Check each 8x8 map chunk to see if we have data for it
                for (int chunkIndex = 0; chunkIndex < totalChunksX * totalChunksY; chunkIndex++)
                {
                    int chunkX = chunkIndex / totalChunksY;
                    int chunkY = chunkIndex % totalChunksY;

                    // Check if this 8x8 chunk has any data by sampling a few tiles
                    bool hasData = false;
                    for (int x = chunkX * 8; x < (chunkX + 1) * 8 && !hasData; x++)
                    {
                        for (int y = chunkY * 8; y < (chunkY + 1) * 8 && !hasData; y++)
                        {
                            if (HasDataForTile(x, y))
                            {
                                hasData = true;
                            }
                        }
                    }

                    if (hasData)
                    {
                        completedChunks = chunkIndex + 1; // +1 because we completed this chunk
                    }
                    else
                    {
                        break; // Sequential generation, so we can stop here
                    }
                }
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
                {
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
                }

                // Atomic replace
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                File.Move(tempFilename, filename);
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tempFilename))
                    {
                        File.Delete(tempFilename);
                    }
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
                    if (fileVersion < 2)
                    {
                        throw new InvalidDataException($"Old file format detected (version {fileVersion}), file will be deleted and regenerated");
                    }

                    int mapIndex = reader.ReadInt32();

                    if (mapIndex != _mapIndex)
                    {
                        throw new InvalidDataException($"Map index mismatch: expected {_mapIndex}, got {mapIndex}");
                    }

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
                        {
                            _mapChecksum = reader.ReadString();
                        }
                        else
                        {
                            throw new InvalidDataException("Checksum missing from file");
                        }
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
            for (int i = 0; i < 8; i++)
            {
                writer.Write(_data[i]);
            }
            // Write isset array
            for (int i = 0; i < 8; i++)
            {
                writer.Write(_isset[i]);
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            // Read data array
            for (int i = 0; i < 8; i++)
            {
                _data[i] = reader.ReadByte();
            }
            // Read isset array
            for (int i = 0; i < 8; i++)
            {
                _isset[i] = reader.ReadByte();
            }
        }
    }
}
