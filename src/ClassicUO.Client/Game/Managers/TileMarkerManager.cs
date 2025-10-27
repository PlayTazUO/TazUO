using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;

namespace ClassicUO.Game.Managers
{
    [Serializable]
    internal record struct TileLocation
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Map { get; set; }

        public TileLocation(int x, int y, int map)
        {
            X = x;
            Y = y;
            Map = map;
        }
    }

    [Serializable]
    internal struct TileMarkerEntry
    {
        public TileLocation Location { get; set; }
        public ushort Hue { get; set; }
    }

    [JsonSerializable(typeof(List<TileMarkerEntry>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class TileMarkerJsonContext : JsonSerializerContext
    {
    }

    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<TileLocation, ushort> markedTiles = new Dictionary<TileLocation, ushort>();

        private TileMarkerManager() { Load(); }

        private string SavePath => Path.Combine(ProfileManager.ProfilePath ?? CUOEnviroment.ExecutablePath, "TileMarkers.json");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            var location = new TileLocation(x, y, map);
            markedTiles[location] = hue;

            // Update all live tiles at this location
            UpdateLiveTilesAt(x, y, map, hue);
        }

        public void RemoveTile(int x, int y, int map)
        {
            var location = new TileLocation(x, y, map);

            if (markedTiles.Remove(location))
            {
                // Reset hue to 0 for all live tiles at this location
                UpdateLiveTilesAt(x, y, map, 0);
            }
        }

        public bool IsTileMarked(int x, int y, int map, out ushort hue) => markedTiles.TryGetValue(new TileLocation(x, y, map), out hue);


        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var entries = markedTiles.Select(kvp => new TileMarkerEntry { Location = kvp.Key, Hue = kvp.Value }).ToList();
                string json = JsonSerializer.Serialize(entries, TileMarkerJsonContext.Default.ListTileMarkerEntry);
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
                    List<TileMarkerEntry> entries = JsonSerializer.Deserialize(json, TileMarkerJsonContext.Default.ListTileMarkerEntry) ?? new List<TileMarkerEntry>();
                    markedTiles = entries.ToDictionary(e => e.Location, e => e.Hue);
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
                markedTiles = new Dictionary<TileLocation, ushort>();
            }
        }

        [Obsolete("Obsolete")]
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

                        foreach (KeyValuePair<string, ushort> kvp in oldData)
                        {
                            // Parse old string key format "x.y.map"
                            string[] parts = kvp.Key.Split('.');
                            if (parts.Length == 3 &&
                                int.TryParse(parts[0], out int x) &&
                                int.TryParse(parts[1], out int y) &&
                                int.TryParse(parts[2], out int map))
                            {
                                markedTiles[new TileLocation(x, y, map)] = kvp.Value;
                            }
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

        private void UpdateLiveTilesAt(int x, int y, int map, ushort hue)
        {
            if (World.Instance.Map == null || World.Instance.Map.Index != map) return;

            Chunk chunk = World.Instance.Map.GetChunk(x, y, false);
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
