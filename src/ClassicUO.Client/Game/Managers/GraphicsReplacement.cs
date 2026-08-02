using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public enum GraphicObjectType : byte
    {
        Unknown = 0,
        Mobile = 1,    // Animation system (Mobile class only)
        Land = 2,      // Art system using GetLand() (Land class)
        Static = 3     // Art system using GetArt() (Item, Static classes)
    }

    [JsonSerializable(typeof(GraphicsReplacementSave))]
    [JsonSerializable(typeof(List<GraphicChangeFilter>))]
    [JsonSerializable(typeof(Dictionary<ushort, GraphicChangeFilter>))]  // Keep for migration
    [JsonSerializable(typeof(GraphicChangeFilter))]
    public partial class GraphicsReplacementJsonContext : JsonSerializerContext
    {
        private static readonly Lazy<GraphicsReplacementJsonContext> _indented =
            new(() => new GraphicsReplacementJsonContext(new JsonSerializerOptions { WriteIndented = true }));

        /// <summary>Indented context used to persist the scoped save file.</summary>
        public static GraphicsReplacementJsonContext DefaultToUse => _indented.Value;
    }

    /// <summary>
    /// Server-scoped JSON save holding the graphic replacement filters. Saving/loading (with rotating
    /// backups) is handled by <see cref="JsonSave{T}"/>. The legacy global file is migrated on first load.
    /// </summary>
    public sealed class GraphicsReplacementSave : JsonSave<GraphicsReplacementSave>
    {
        private const string SaveFileName = "GraphicReplacementFilters.json";

        public List<GraphicChangeFilter> Filters { get; set; } = new();

        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => SaveFileName;

        protected override JsonTypeInfo<GraphicsReplacementSave> TypeInfo => GraphicsReplacementJsonContext.DefaultToUse.GraphicsReplacementSave;

        /// <summary>The pre-scope save location that this system used to write to.</summary>
        private static string OldSavePath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "MobileReplacementFilter.json");

        /// <summary>
        /// Loads the save, migrating from the old global <c>MobileReplacementFilter.json</c> the first time
        /// the new scoped file does not yet exist. A successful migration removes the legacy file.
        /// </summary>
        public static GraphicsReplacementSave LoadWithMigration()
        {
            GraphicsReplacementSave instance = new();

            if (!File.Exists(instance.FilePath) && File.Exists(OldSavePath) && TryMigrateOldFile(instance))
            {
                instance.Save();     // Persist to the new scoped location.
                CleanupOldFile();    // Remove the legacy file after a successful migration.
                Log.Trace("Migrated graphic replacement filters to server-scoped save.");
                return instance;
            }

            return Load();
        }

        private static bool TryMigrateOldFile(GraphicsReplacementSave instance)
        {
            string json;
            try
            {
                json = File.ReadAllText(OldSavePath);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read legacy graphic filters: {e}");
                return false;
            }

            // Try the newer list format first.
            try
            {
                List<GraphicChangeFilter> list = JsonSerializer.Deserialize(json, GraphicsReplacementJsonContext.Default.ListGraphicChangeFilter);
                if (list != null)
                {
                    instance.Filters = list;
                    return true;
                }
            }
            catch
            {
                // Fall through to the legacy dictionary format.
            }

            // Legacy dictionary format - keyed by graphic, all assumed to be Mobile.
            try
            {
                Dictionary<ushort, GraphicChangeFilter> old = JsonSerializer.Deserialize(json, GraphicsReplacementJsonContext.Default.DictionaryUInt16GraphicChangeFilter);
                if (old != null)
                {
                    foreach (KeyValuePair<ushort, GraphicChangeFilter> kvp in old)
                    {
                        kvp.Value.OriginalGraphic = kvp.Key;
                        kvp.Value.OriginalType = 1;    // Mobile
                        kvp.Value.ReplacementType = 1; // Mobile
                        instance.Filters.Add(kvp.Value);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to migrate legacy graphic filters: {e}");
            }

            return false;
        }

        private static void CleanupOldFile()
        {
            try
            {
                if (File.Exists(OldSavePath))
                    File.Delete(OldSavePath);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to remove legacy graphic filter file: {e}");
            }
        }
    }

    internal static class GraphicsReplacement
    {
        private static GraphicsReplacementSave _save = new();
        private static Dictionary<(ushort, byte), GraphicChangeFilter> graphicChangeFilters = new Dictionary<(ushort, byte), GraphicChangeFilter>();
        public static Dictionary<(ushort, byte), GraphicChangeFilter> GraphicFilters => graphicChangeFilters;
        private static HashSet<(ushort, byte)> quickLookup = new HashSet<(ushort, byte)>();

        public static void Load()
        {
            _save = GraphicsReplacementSave.LoadWithMigration();
            RebuildLookup();
        }

        private static void RebuildLookup()
        {
            graphicChangeFilters = new Dictionary<(ushort, byte), GraphicChangeFilter>();
            quickLookup = new HashSet<(ushort, byte)>();

            foreach (GraphicChangeFilter filter in _save.Filters)
            {
                (ushort OriginalGraphic, byte OriginalType) key = (filter.OriginalGraphic, filter.OriginalType);
                graphicChangeFilters[key] = filter;
                quickLookup.Add(key);
            }
        }

        public static void Save()
        {
            _save.Filters = new List<GraphicChangeFilter>(graphicChangeFilters.Values);
            _save.Save();
        }

        public static void Replace(ushort graphic, byte type, ref ushort newgraphic, ref ushort hue, ref byte newtype)
        {
            if (quickLookup.Contains((graphic, type)))
            {
                GraphicChangeFilter filter = graphicChangeFilters[(graphic, type)];
                newgraphic = filter.ReplacementGraphic;
                newtype = filter.ReplacementType;
                if (filter.NewHue != ushort.MaxValue)
                    hue = filter.NewHue;
            }
        }

        public static void ReplaceHue(ushort graphic, byte type, ref ushort hue)
        {
            if (quickLookup.Contains((graphic, type)))
            {
                GraphicChangeFilter filter = graphicChangeFilters[(graphic, type)];
                if (filter.NewHue != ushort.MaxValue)
                    hue = filter.NewHue;
            }
        }

        public static void ResetLists()
        {
            var newList = new Dictionary<(ushort, byte), GraphicChangeFilter>();
            quickLookup.Clear();

            foreach (KeyValuePair<(ushort, byte), GraphicChangeFilter> item in graphicChangeFilters)
            {
                (ushort OriginalGraphic, byte OriginalType) key = (item.Value.OriginalGraphic, item.Value.OriginalType);
                newList.Add(key, item.Value);
                quickLookup.Add(key);
            }
            graphicChangeFilters = newList;
        }

        public static GraphicChangeFilter NewFilter(ushort originalGraphic, byte originalType, ushort newGraphic, byte newType, ushort newHue = ushort.MaxValue)
        {
            (ushort originalGraphic, byte originalType) key = (originalGraphic, originalType);
            if (!graphicChangeFilters.ContainsKey(key))
            {
                var f = new GraphicChangeFilter()
                {
                    OriginalGraphic = originalGraphic,
                    OriginalType = originalType,
                    ReplacementGraphic = newGraphic,
                    ReplacementType = newType,
                    NewHue = newHue
                };
                graphicChangeFilters.Add(key, f);
                quickLookup.Add(key);
                return f;
            }
            return null;
        }

        public static void DeleteFilter(ushort originalGraphic, byte originalType)
        {
            (ushort originalGraphic, byte originalType) key = (originalGraphic, originalType);
            if (graphicChangeFilters.ContainsKey(key))
                graphicChangeFilters.Remove(key);

            if (quickLookup.Contains(key))
                quickLookup.Remove(key);
        }

        #nullable enable
        public static string? GetJsonExport()
        {
            try
            {
                var filterList = new List<GraphicChangeFilter>(graphicChangeFilters.Values);
                return JsonSerializer.Serialize(filterList, GraphicsReplacementJsonContext.Default.ListGraphicChangeFilter);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error exporting graphic filters to JSON: {e}");
            }

            return null;
        }
        #nullable disable

        public static bool ImportFromJson(string json)
        {
            try
            {
                List<GraphicChangeFilter> importedFilters = JsonSerializer.Deserialize(json, GraphicsReplacementJsonContext.Default.ListGraphicChangeFilter);

                if (importedFilters != null)
                {
                    int addedCount = 0;
                    int duplicateCount = 0;

                    foreach (GraphicChangeFilter filter in importedFilters)
                    {
                        (ushort OriginalGraphic, byte OriginalType) key = (filter.OriginalGraphic, filter.OriginalType);
                        if (!graphicChangeFilters.ContainsKey(key))
                        {
                            graphicChangeFilters[key] = filter;
                            quickLookup.Add(key);
                            addedCount++;
                        }
                        else
                        {
                            duplicateCount++;
                        }
                    }

                    string message = $"Imported {addedCount} graphic filters from clipboard";
                    if (duplicateCount > 0)
                        message += $" ({duplicateCount} duplicates skipped)";
                    GameActions.Print(message, Constants.HUE_SUCCESS);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error importing graphic filters from JSON: {e}");
            }

            return false;
        }
    }

    public class GraphicChangeFilter
    {
        public ushort OriginalGraphic { get; set; }
        public byte OriginalType { get; set; } = 1; // Default Mobile
        public ushort ReplacementGraphic { get; set; }
        public byte ReplacementType { get; set; } = 1; // Default Mobile
        public ushort NewHue { get; set; } = ushort.MaxValue;
    }
}
