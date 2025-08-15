using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;

namespace ClassicUO.Game.Managers
{
    [Serializable]
    internal class TileMarkerData
    {
        public ushort MarkerHue { get; set; }
        public ushort OriginalHue { get; set; }
    }

    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<string, TileMarkerData> markedTiles = new Dictionary<string, TileMarkerData>();

        private TileMarkerManager() { Load(); }

        private string SavePath => Path.Combine(ProfileManager.ProfilePath ?? CUOEnviroment.ExecutablePath, "TileMarkers.json");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            string key = FormatLocKey(x, y, map);
            
            // Store original hue if not already marked (use 0 as default for new markers)
            ushort originalHue = 0;
            if (markedTiles.ContainsKey(key))
            {
                // Keep the existing original hue
                originalHue = markedTiles[key].OriginalHue;
            }
            
            markedTiles[key] = new TileMarkerData { MarkerHue = hue, OriginalHue = originalHue };
            
            // Update all live tiles at this location
            UpdateLiveTilesAt(x, y, hue);
        }

        public void RemoveTile(int x, int y, int map)
        {
            string key = FormatLocKey(x, y, map);
            
            if (markedTiles.TryGetValue(key, out TileMarkerData data))
            {
                markedTiles.Remove(key);
                
                // Restore original hue to all live tiles at this location
                UpdateLiveTilesAt(x, y, data.OriginalHue);
            }
        }

        public bool IsTileMarked(int x, int y, int map, out ushort hue)
        {
            if (markedTiles.TryGetValue(FormatLocKey(x, y, map), out TileMarkerData data))
            {
                hue = data.MarkerHue;
                return true;
            }
            hue = 0;
            return false;
        }

        private string FormatLocKey(int x, int y, int map)
        {
            return $"{x}.{y}.{map}";
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(markedTiles, options);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save marked tile data: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    markedTiles = JsonSerializer.Deserialize<Dictionary<string, TileMarkerData>>(json) ?? new Dictionary<string, TileMarkerData>();
                }
                else
                {
                    // Try to migrate from old binary format
                    MigrateFromLegacyFormat();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load marked tile data: {ex.Message}");
                markedTiles = new Dictionary<string, TileMarkerData>();
            }
        }
        
        private void MigrateFromLegacyFormat()
        {
            string legacyPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "TileMarkers.bin");
            if (File.Exists(legacyPath))
            {
                try
                {
                    using (FileStream fs = File.OpenRead(legacyPath))
                    {
                        var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        var oldData = (Dictionary<string, ushort>)bf.Deserialize(fs);
                        
                        foreach (var kvp in oldData)
                        {
                            markedTiles[kvp.Key] = new TileMarkerData { MarkerHue = kvp.Value, OriginalHue = 0 };
                        }
                        
                        // Save in new format and delete old file
                        Save();
                        File.Delete(legacyPath);
                    }
                }
                catch
                {
                    // Migration failed, start fresh
                }
            }
        }
        
        private void UpdateLiveTilesAt(int x, int y, ushort hue)
        {
            if (World.Map == null) return;
            
            var chunk = World.Map.GetChunk(x, y, false);
            if (chunk == null) return;
            
            // Get all tiles at this location and update their hue
            for (GameObject obj = chunk.GetHeadObject(x % 8, y % 8); obj != null; obj = obj.TNext)
            {
                // Update both Land and Static tiles
                if (obj is Land || obj is Static)
                {
                    obj.Hue = hue;
                }
            }
        }
    }
}
