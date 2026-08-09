using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers.SpellVisualRange;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for the spell visual range/indicator config. Persisted to
    /// <c>SpellVisualRange.json</c> in the shared <c>Data</c> folder. Saving/loading
    /// (with rotating backups, atomic writes and a cross-process lock) is handled by
    /// <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class SpellVisualRangeConfig : JsonSave<SpellVisualRangeConfig>, INotifyPropertyChanged
    {
        /// <summary>The configured spells. Keyed by <see cref="SpellRangeInfo.ID"/> once loaded into the manager.</summary>
        public List<SpellRangeInfo> Spells { get; set; } = new();

        /// <summary>Shared across all profiles, so it lives in the global <c>Server</c> folder.</summary>
        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => "SpellVisualRange.json";

        protected override JsonTypeInfo<SpellVisualRangeConfig> TypeInfo => SpellVisualRangeConfigJsonContext.DefaultToUse.SpellVisualRangeConfig;
    }

    [JsonSerializable(typeof(SpellVisualRangeConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(SpellRangeInfo), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class SpellVisualRangeConfigJsonContext : JsonSerializerContext
    {
        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
            new JsonSerializerOptions { WriteIndented = true });

        public static SpellVisualRangeConfigJsonContext DefaultToUse { get; } = new SpellVisualRangeConfigJsonContext(_jsonOptions.Value);
    }
}
