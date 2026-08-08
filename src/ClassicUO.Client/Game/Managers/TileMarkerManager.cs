using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Utility.Logging;
using System.ComponentModel;

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

    /// <summary>Legacy source-gen context used only to read the old profile-scoped TileMarkers.json.</summary>
    [JsonSerializable(typeof(List<TileMarkerEntry>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class TileMarkerJsonContext : JsonSerializerContext
    {
    }

    /// <summary>Source-gen context for the server-scoped <see cref="TileMarkerConfig"/> save.</summary>
    [JsonSerializable(typeof(TileMarkerConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class TileMarkerConfigJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON-backed store for marked tiles. Persisted to <c>TileMarkers.json</c> under the
    /// <see cref="SettingsScope.Server"/> folder. Saving/loading (with rotating backups) is handled by
    /// <see cref="JsonSave{T}"/>.
    /// </summary>
    internal sealed class TileMarkerConfig : JsonSave<TileMarkerConfig>, INotifyPropertyChanged
    {
        public List<TileMarkerEntry> Markers { get; set; } = new();

        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => "TileMarkers.json";

        protected override JsonTypeInfo<TileMarkerConfig> TypeInfo => TileMarkerConfigJsonContext.Default.TileMarkerConfig;
    }

    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<TileLocation, ushort> markedTiles = new Dictionary<TileLocation, ushort>();
        private TileMarkerConfig config;

        private TileMarkerManager() { Load(); }

        /// <summary>The old profile-scoped location, migrated to the server scope on first load.</summary>
        private static string LegacySavePath => Path.Combine(ProfileManager.ProfilePath ?? CUOEnviroment.ExecutablePath, "TileMarkers.json");

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
            config.Markers = markedTiles.Select(kvp => new TileMarkerEntry { Location = kvp.Key, Hue = kvp.Value }).ToList();
            config.Save();
        }

        private void Load()
        {
            MigrateLegacyFile();

            config = TileMarkerConfig.Load();
            markedTiles = config.Markers.ToDictionary(e => e.Location, e => e.Hue);
        }

        /// <summary>Moves the old profile-scoped TileMarkers.json into the server-scoped location, once.</summary>
        private void MigrateLegacyFile()
        {
            try
            {
                string legacy = LegacySavePath;
                if (!File.Exists(legacy)) return;

                // Don't clobber an existing server-scoped save.
                if (File.Exists(new TileMarkerConfig().FilePath))
                {
                    return;
                }

                string json = File.ReadAllText(legacy);
                List<TileMarkerEntry> entries = JsonSerializer.Deserialize(json, TileMarkerJsonContext.Default.ListTileMarkerEntry) ?? new List<TileMarkerEntry>();

                var migrated = new TileMarkerConfig { Markers = entries };
                migrated.Save();

                File.Delete(legacy);
                Log.Trace($"Migrated tile markers from '{legacy}' to '{migrated.FilePath}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to migrate tile marker data: {ex.Message}");
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
