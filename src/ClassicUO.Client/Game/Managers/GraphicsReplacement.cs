using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(Dictionary<ushort, GraphicChangeFilter>))]
    [JsonSerializable(typeof(GraphicChangeFilter))]
    public partial class GraphicsReplacementJsonContext : JsonSerializerContext
    {
    }
    internal static class GraphicsReplacement
    {
        private static Dictionary<ushort, GraphicChangeFilter> graphicChangeFilters = new Dictionary<ushort, GraphicChangeFilter>();
        public static Dictionary<ushort, GraphicChangeFilter> GraphicFilters => graphicChangeFilters;
        private static HashSet<ushort> quickLookup = new HashSet<ushort>();
        public static void Load()
        {
            if (File.Exists(GetSavePath()))
            {
                try
                {
                    graphicChangeFilters = JsonSerializer.Deserialize(File.ReadAllText(GetSavePath()), GraphicsReplacementJsonContext.Default.DictionaryUInt16GraphicChangeFilter);
                    foreach (KeyValuePair<ushort, GraphicChangeFilter> filter in graphicChangeFilters)
                        quickLookup.Add(filter.Key);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static void Save()
        {
            if (graphicChangeFilters.Count > 0)
            {
                try
                {
                    File.WriteAllText(GetSavePath(), JsonSerializer.Serialize(graphicChangeFilters, GraphicsReplacementJsonContext.Default.DictionaryUInt16GraphicChangeFilter));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to save mobile graphic change filter. {e.Message}");
                }
                graphicChangeFilters.Clear();
                quickLookup.Clear();
            }
            else
            {
                if (File.Exists(GetSavePath()))
                    File.Delete(GetSavePath());
            }
        }

        public static void Replace(ushort graphic, ref ushort newgraphic, ref ushort hue)
        {
            if (quickLookup.Contains(graphic))
            {
                GraphicChangeFilter filter = graphicChangeFilters[graphic];
                newgraphic = filter.ReplacementGraphic;
                if (filter.NewHue != ushort.MaxValue)
                    hue = filter.NewHue;
            }
        }

        public static void ReplaceHue(ushort graphic, ref ushort hue)
        {
            if (quickLookup.Contains(graphic))
            {
                GraphicChangeFilter filter = graphicChangeFilters[graphic];
                if (filter.NewHue != ushort.MaxValue)
                    hue = filter.NewHue;
            }
        }

        public static void ResetLists()
        {
            var newList = new Dictionary<ushort, GraphicChangeFilter>();
            quickLookup.Clear();

            foreach (KeyValuePair<ushort, GraphicChangeFilter> item in graphicChangeFilters)
            {
                newList.Add(item.Value.OriginalGraphic, item.Value);
                quickLookup.Add(item.Value.OriginalGraphic);
            }
            graphicChangeFilters = newList;
        }

        public static GraphicChangeFilter NewFilter(ushort originalGraphic, ushort newGraphic, ushort newHue = ushort.MaxValue)
        {
            if (!graphicChangeFilters.ContainsKey(originalGraphic))
            {
                GraphicChangeFilter f;
                graphicChangeFilters.Add(originalGraphic, f = new GraphicChangeFilter()
                {
                    OriginalGraphic = originalGraphic,
                    ReplacementGraphic = newGraphic,
                    NewHue = newHue
                });

                quickLookup.Add(originalGraphic);
                return f;

            }
            return null;
        }

        public static void DeleteFilter(ushort originalGraphic)
        {
            if (graphicChangeFilters.ContainsKey(originalGraphic))
                graphicChangeFilters.Remove(originalGraphic);

            if (quickLookup.Contains(originalGraphic))
                quickLookup.Remove(originalGraphic);
        }

        private static string GetSavePath() => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "MobileReplacementFilter.json");
    }

    public class GraphicChangeFilter
    {
        public ushort OriginalGraphic { get; set; }
        public ushort ReplacementGraphic { get; set; }
        public ushort NewHue { get; set; } = ushort.MaxValue;
    }
}
