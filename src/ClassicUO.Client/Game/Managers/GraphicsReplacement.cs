using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    public enum GraphicObjectType : byte
    {
        Unknown = 0,
        Mobile = 1,    // Animation system (Mobile class only)
        Land = 2,      // Art system using GetLand() (Land class)
        Static = 3     // Art system using GetArt() (Item, Static classes)
    }

    [JsonSerializable(typeof(List<GraphicChangeFilter>))]
    [JsonSerializable(typeof(Dictionary<ushort, GraphicChangeFilter>))]  // Keep for migration
    [JsonSerializable(typeof(GraphicChangeFilter))]
    public partial class GraphicsReplacementJsonContext : JsonSerializerContext
    {
    }
    internal static class GraphicsReplacement
    {
        private static Dictionary<(ushort, byte), GraphicChangeFilter> graphicChangeFilters = new Dictionary<(ushort, byte), GraphicChangeFilter>();
        public static Dictionary<(ushort, byte), GraphicChangeFilter> GraphicFilters => graphicChangeFilters;
        private static HashSet<(ushort, byte)> quickLookup = new HashSet<(ushort, byte)>();
        public static void Load()
        {
            if (File.Exists(GetSavePath()))
            {
                try
                {
                    // Try new list format first
                    List<GraphicChangeFilter> filterList = JsonSerializer.Deserialize(
                        File.ReadAllText(GetSavePath()),
                        GraphicsReplacementJsonContext.Default.ListGraphicChangeFilter
                    );

                    if (filterList != null)
                    {
                        graphicChangeFilters = new Dictionary<(ushort, byte), GraphicChangeFilter>();
                        quickLookup = new HashSet<(ushort, byte)>();

                        foreach (GraphicChangeFilter filter in filterList)
                        {
                            (ushort OriginalGraphic, byte OriginalType) key = (filter.OriginalGraphic, filter.OriginalType);
                            graphicChangeFilters[key] = filter;
                            quickLookup.Add(key);
                        }
                    }
                }
                catch
                {
                    // Migration from old format - assume all are Mobile type
                    try
                    {
                        Dictionary<ushort, GraphicChangeFilter> oldFormat = JsonSerializer.Deserialize(
                            File.ReadAllText(GetSavePath()),
                            GraphicsReplacementJsonContext.Default.DictionaryUInt16GraphicChangeFilter
                        );

                        if (oldFormat != null)
                        {
                            graphicChangeFilters = new Dictionary<(ushort, byte), GraphicChangeFilter>();
                            quickLookup = new HashSet<(ushort, byte)>();

                            foreach (KeyValuePair<ushort, GraphicChangeFilter> kvp in oldFormat)
                            {
                                // Migrate to new format, defaulting to Mobile type
                                kvp.Value.OriginalType = 1; // Mobile
                                kvp.Value.ReplacementType = 1; // Mobile
                                graphicChangeFilters.Add((kvp.Key, 1), kvp.Value);
                                quickLookup.Add((kvp.Key, 1));
                            }

                            // Save immediately to persist migration
                            Save();
                            Console.WriteLine("Migrated graphic replacement filters to new format");
                        }
                    }
                    catch (Exception migrationError)
                    {
                        Console.WriteLine($"Failed to load or migrate graphic filters: {migrationError}");
                    }
                }
            }
        }

        public static void Save()
        {
            if (graphicChangeFilters.Count > 0)
            {
                try
                {
                    // Convert dictionary to list for serialization
                    var filterList = new List<GraphicChangeFilter>(graphicChangeFilters.Values);

                    FileSystemHelper.WriteAllTextSafe(
                        GetSavePath(),
                        JsonSerializer.Serialize(
                            filterList,
                            GraphicsReplacementJsonContext.Default.ListGraphicChangeFilter
                        )
                    );
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to save mobile graphic change filter. {e.Message}");
                }
            }
            else
            {
                if (File.Exists(GetSavePath()))
                    File.Delete(GetSavePath());
            }
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

        private static string GetSavePath() => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "MobileReplacementFilter.json");
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
