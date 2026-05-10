using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Data
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<SpellbookCacheEntry>))]
    [JsonSerializable(typeof(SpellbookCacheEntry))]
    [JsonSerializable(typeof(DynamicSpellDefinition))]
    [JsonSerializable(typeof(SpellbookInfoPage))]
    [JsonSerializable(typeof(SpellbookBookmarkInfo))]
    internal partial class SpellbookCacheJsonContext : JsonSerializerContext
    {
    }
}
