using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using Microsoft.Xna.Framework;

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
        public bool UsesColor { get; set; }
        public uint ColorPackedValue { get; set; }
    }

    [JsonSerializable(typeof(List<TileMarkerEntry>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class TileMarkerJsonContext : JsonSerializerContext
    {
    }

    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<TileLocation, TileMarkerEntry> markedTiles = new Dictionary<TileLocation, TileMarkerEntry>();

        private TileMarkerManager() { Load(); }

        private string SavePath => Path.Combine(ProfileManager.ProfilePath ?? CUOEnviroment.ExecutablePath, "TileMarkers.json");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            var location = new TileLocation(x, y, map);
            var entry = new TileMarkerEntry
            {
                Location = location,
                Hue = hue
            };

            markedTiles[location] = entry;

            // Update all live tiles at this location
            UpdateLiveTilesAt(entry);
        }

        public void AddTileColor(int x, int y, int map, Color color)
        {
            var location = new TileLocation(x, y, map);
            var entry = new TileMarkerEntry
            {
                Location = location,
                UsesColor = true,
                ColorPackedValue = color.PackedValue
            };

            markedTiles[location] = entry;
            UpdateLiveTilesAt(entry);
        }

        public void RemoveTile(int x, int y, int map)
        {
            var location = new TileLocation(x, y, map);

            if (markedTiles.Remove(location))
            {
                // Reset hue to 0 for all live tiles at this location
                ClearLiveTilesAt(x, y, map);
            }
        }

        public bool IsTileMarked(int x, int y, int map, out ushort hue)
        {
            if (markedTiles.TryGetValue(new TileLocation(x, y, map), out TileMarkerEntry entry))
            {
                hue = entry.UsesColor ? (ushort)0 : entry.Hue;
                return true;
            }

            hue = 0;
            return false;
        }

        public bool TryGetTileMarker(int x, int y, int map, out TileMarkerEntry entry) =>
            markedTiles.TryGetValue(new TileLocation(x, y, map), out entry);


        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                var entries = markedTiles.Values.ToList();
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
                if (!File.Exists(SavePath)) return;

                string json = File.ReadAllText(SavePath);
                List<TileMarkerEntry> entries = JsonSerializer.Deserialize(json, TileMarkerJsonContext.Default.ListTileMarkerEntry) ?? new List<TileMarkerEntry>();
                markedTiles = entries.ToDictionary(e => e.Location, e => e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load marked tile data: {ex.Message}");
                markedTiles = new Dictionary<TileLocation, TileMarkerEntry>();
            }
        }

        private void UpdateLiveTilesAt(TileMarkerEntry entry)
        {
            int x = entry.Location.X;
            int y = entry.Location.Y;
            int map = entry.Location.Map;

            if (World.Instance.Map == null || World.Instance.Map.Index != map) return;

            Chunk chunk = World.Instance.Map.GetChunk(x, y, false);
            if (chunk == null) return;

            // Get all tiles at this location and update their hue
            for (GameObject obj = chunk.GetHeadObject(x % 8, y % 8); obj != null; obj = obj.TNext)
            {
                // Update both Land and Static tiles
                if (obj is Land || obj is Static)
                {
                        if (entry.UsesColor)
                        {
                            obj.Hue = 0;
                            obj.MarkerColor = new Color { PackedValue = entry.ColorPackedValue };
                        }
                    else
                    {
                        obj.Hue = entry.Hue;
                        obj.MarkerColor = null;
                    }
                }
            }
        }

        private void ClearLiveTilesAt(int x, int y, int map)
        {
            if (World.Instance.Map == null || World.Instance.Map.Index != map) return;

            Chunk chunk = World.Instance.Map.GetChunk(x, y, false);
            if (chunk == null) return;

            for (GameObject obj = chunk.GetHeadObject(x % 8, y % 8); obj != null; obj = obj.TNext)
            {
                if (obj is Land || obj is Static)
                {
                    obj.Hue = 0;
                    obj.MarkerColor = null;
                }
            }
        }
    }
}
