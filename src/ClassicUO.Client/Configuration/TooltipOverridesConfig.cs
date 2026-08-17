using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for tooltip override rules. Persisted to <c>tooltip_overrides.json</c> in the
    /// current profile's save location. Replaces the legacy parallel <c>ToolTipOverride_*</c> lists on
    /// <see cref="Profile"/>, which are migrated across during profile migration. Saving/loading (with
    /// rotating backups) is handled by <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class TooltipOverridesConfig : JsonSave<TooltipOverridesConfig>, INotifyPropertyChanged
    {
        public const string TooltipOverridesFileName = "tooltip_overrides.json";

        public List<ToolTipOverrideData> Overrides { get; set; } = new();

        /// <summary>Lives in the profile folder alongside the other per-character configs.</summary>
        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => TooltipOverridesFileName;

        protected override JsonTypeInfo<TooltipOverridesConfig> TypeInfo => TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesConfig;

        private static TooltipOverridesConfig _current;

        /// <summary>The tooltip-override config for the currently loaded profile.</summary>
        public static TooltipOverridesConfig Current => _current ??= Load(ProfileManager.ProfilePath);

        /// <summary>
        /// Loads the tooltip-override config for the given profile and sets it as <see cref="Current"/>.
        /// Called on every profile load so the cache tracks the active profile. The <paramref name="profilePath"/>
        /// is the current profile folder, which is also the <see cref="SettingsScope.Char"/> location.
        /// </summary>
        public static new TooltipOverridesConfig Load(string profilePath)
        {
            _current = JsonSave<TooltipOverridesConfig>.Load();
            _current.Reindex();
            return _current;
        }

        /// <summary>Persists the current config and drops the cache so the next profile reloads fresh.</summary>
        public static void Unload()
        {
            if (_current == null)
                return;

            _current.Save();
            _current = null;
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

        /// <summary>
        /// Swaps the entry at <paramref name="index"/> with its neighbour <paramref name="index"/> + <paramref name="delta"/>
        /// (i.e. -1 moves it one position up, +1 one position down), reindexes and persists. A no-op when the move
        /// would fall outside the list.
        /// </summary>
        public void Move(int index, int delta)
        {
            int target = index + delta;
            if (index < 0 || target < 0 || target >= Overrides.Count)
                return;

            (Overrides[index], Overrides[target]) = (Overrides[target], Overrides[index]);
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
