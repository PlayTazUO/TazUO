using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Data
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<PersistentCacheEntry>))]
    [JsonSerializable(typeof(PersistentCacheEntry))]
    [JsonSerializable(typeof(DynamicSpellDefinition))]
    internal partial class SpellbookCacheJsonContext : JsonSerializerContext
    {
    }
}
