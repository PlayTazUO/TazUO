using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for tooltip override rules. Persisted to <c>tooltip_overrides.json</c> in the
    /// current profile's save location. Replaces the legacy parallel <c>ToolTipOverride_*</c> lists on
    /// <see cref="Profile"/>, which are migrated across during profile migration.
    /// </summary>
    public sealed class TooltipOverridesConfig
    {
        public const string FileName = "tooltip_overrides.json";

        public List<ToolTipOverrideData> Overrides { get; set; } = new();

        private static TooltipOverridesConfig _current;

        /// <summary>The tooltip-override config for the currently loaded profile.</summary>
        public static TooltipOverridesConfig Current => _current ??= Load(ProfileManager.ProfilePath);

        private static string GetFilePath() =>
            string.IsNullOrEmpty(ProfileManager.ProfilePath) ? null : Path.Combine(ProfileManager.ProfilePath, FileName);

        /// <summary>
        /// Loads the tooltip-override config from <paramref name="profilePath"/> and sets it as
        /// <see cref="Current"/>. Called on every profile load so the cache tracks the active profile.
        /// </summary>
        public static TooltipOverridesConfig Load(string profilePath)
        {
            string file = string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, FileName);

            _current = (file != null && File.Exists(file)
                           ? ConfigurationResolver.Load<TooltipOverridesConfig>(file, TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesConfig)
                           : null)
                       ?? new TooltipOverridesConfig();

            _current.Reindex();
            return _current;
        }

        public void Save()
        {
            string file = GetFilePath();
            if (file == null)
                return;

            ConfigurationResolver.Save(this, file, TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesConfig);
        }

        /// <summary>
        /// Stores <paramref name="data"/> at its <see cref="ToolTipOverrideData.Index"/>. When the index
        /// is out of range the entry is appended (and its index updated to its new position). Persists on
        /// any change.
        /// </summary>
        public void Upsert(ToolTipOverrideData data)
        {
            if (data == null)
                return;

            if (data.Index >= 0 && data.Index < Overrides.Count)
            {
                Overrides[data.Index] = data;
            }
            else
            {
                data.Index = Overrides.Count;
                Overrides.Add(data);
            }

            Save();
        }

        /// <summary>Removes the entry at <paramref name="index"/>, if present, reindexes and persists.</summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Overrides.Count)
                return;

            Overrides.RemoveAt(index);
            Reindex();
            Save();
        }

        /// <summary>Removes every override and persists.</summary>
        public void Clear()
        {
            Overrides.Clear();
            Save();
        }

        /// <summary>Keeps each entry's <see cref="ToolTipOverrideData.Index"/> in sync with its list position.</summary>
        private void Reindex()
        {
            for (int i = 0; i < Overrides.Count; i++)
            {
                if (Overrides[i] != null)
                    Overrides[i].Index = i;
            }
        }
    }

    [JsonSerializable(typeof(TooltipOverridesConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class TooltipOverridesJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name) =>
                string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            };
            return options;
        });

        public static TooltipOverridesJsonContext DefaultToUse { get; } = new TooltipOverridesJsonContext(_jsonOptions.Value);
    }
}
