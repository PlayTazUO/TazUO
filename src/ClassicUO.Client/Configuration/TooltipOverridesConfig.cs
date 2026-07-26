using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for tooltip override rules. Persisted to <c>tooltip_overrides.json</c> in the
    /// current profile's save location. Replaces the legacy parallel <c>ToolTipOverride_*</c> lists on
    /// <see cref="Profile"/>, which are migrated on first load so existing overrides are preserved.
    /// </summary>
    public sealed class TooltipOverridesConfig
    {
        public const string FileName = "tooltip_overrides.json";

        public List<ToolTipOverrideData> Overrides { get; set; } = new();

        private static TooltipOverridesConfig _current;

        /// <summary>The tooltip-override config for the currently loaded profile.</summary>
        public static TooltipOverridesConfig Current => _current ??= LoadForCurrentProfile();

        private static string GetFilePath() =>
            string.IsNullOrEmpty(ProfileManager.ProfilePath) ? null : Path.Combine(ProfileManager.ProfilePath, FileName);

        /// <summary>
        /// Loads (or migrates) the tooltip-override config for the given profile and sets it as
        /// <see cref="Current"/>. Returns <see langword="true"/> when a migration from the legacy
        /// <see cref="Profile"/> lists was performed (and those lists were cleared), signaling that
        /// the profile itself should be re-saved.
        /// </summary>
        public static bool LoadForProfile(string profilePath, Profile profile)
        {
            string file = string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, FileName);

            if (file != null && File.Exists(file))
            {
                _current = ConfigurationResolver.Load<TooltipOverridesConfig>(file, TooltipOverridesJsonContext.DefaultToUse.TooltipOverridesConfig)
                           ?? new TooltipOverridesConfig();
                _current.Reindex();
                return false;
            }

            bool migrated = MigrateFromProfile(profile, out TooltipOverridesConfig config);
            _current = config;
            _current.Reindex();
            _current.Save();
            return migrated;
        }

        private static TooltipOverridesConfig LoadForCurrentProfile()
        {
            LoadForProfile(ProfileManager.ProfilePath, ProfileManager.CurrentProfile);
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

        /// <summary>
        /// Builds a config from the legacy parallel <see cref="Profile"/> lists and clears them.
        /// </summary>
        /// <returns><see langword="true"/> when there was legacy data to migrate.</returns>
#pragma warning disable CS0618 // Reading the obsolete legacy lists is the whole point of migration.
        private static bool MigrateFromProfile(Profile profile, out TooltipOverridesConfig config)
        {
            config = new TooltipOverridesConfig();

            if (profile == null)
                return false;

            int count = profile.ToolTipOverride_SearchText.Count;

            for (int i = 0; i < count; i++)
            {
                config.Overrides.Add(new ToolTipOverrideData(
                    i,
                    profile.ToolTipOverride_SearchText[i],
                    profile.ToolTipOverride_NewFormat.ElementAtOrDefault(i) ?? string.Empty,
                    i < profile.ToolTipOverride_MinVal1.Count ? profile.ToolTipOverride_MinVal1[i] : -1,
                    i < profile.ToolTipOverride_MaxVal1.Count ? profile.ToolTipOverride_MaxVal1[i] : 100,
                    i < profile.ToolTipOverride_MinVal2.Count ? profile.ToolTipOverride_MinVal2[i] : -1,
                    i < profile.ToolTipOverride_MaxVal2.Count ? profile.ToolTipOverride_MaxVal2[i] : 100,
                    i < profile.ToolTipOverride_Layer.Count ? profile.ToolTipOverride_Layer[i] : (byte)TooltipLayers.Any,
                    i < profile.ToolTipOverride_BorderHue.Count ? profile.ToolTipOverride_BorderHue[i] : -1));
            }

            if (count == 0)
                return false;

            // Clear the legacy lists so the parallel-list storage no longer persists in the profile.
            profile.ToolTipOverride_SearchText.Clear();
            profile.ToolTipOverride_NewFormat.Clear();
            profile.ToolTipOverride_MinVal1.Clear();
            profile.ToolTipOverride_MinVal2.Clear();
            profile.ToolTipOverride_MaxVal1.Clear();
            profile.ToolTipOverride_MaxVal2.Clear();
            profile.ToolTipOverride_Layer.Clear();
            profile.ToolTipOverride_BorderHue.Clear();

            return true;
        }
#pragma warning restore CS0618
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
