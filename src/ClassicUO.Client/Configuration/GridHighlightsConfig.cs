using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps.GridHighLight;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for grid-highlight rules. Persisted to <c>grid_highlights.json</c> in the
    /// current profile's save location. Replaces the in-profile <see cref="Profile.GridHighlightSetup"/>
    /// storage, which is migrated on first load so existing highlights are preserved. Saving/loading
    /// (with rotating backups) is handled by <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class GridHighlightsConfig : JsonSave<GridHighlightsConfig>, INotifyPropertyChanged
    {
        private const string GridHighlightsFileName = "grid_highlights.json";

        public List<GridHighlightSetupEntry> Highlights { get; set; } = new();

        /// <summary>Lives in the profile folder alongside the other per-character configs.</summary>
        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => GridHighlightsFileName;

        protected override JsonTypeInfo<GridHighlightsConfig> TypeInfo => GridHighlightsJsonContext.DefaultToUse.GridHighlightsConfig;

        private static GridHighlightsConfig _current;

        /// <summary>The grid-highlight config for the currently loaded profile.</summary>
        public static GridHighlightsConfig Current => _current ??= LoadForCurrentProfile();

        public static void Unload()
        {
            if (_current == null)
                return;
            
            _current.Save();
            _current.Highlights.Clear();
            _current = null;
        }

        /// <summary>
        /// Loads (or migrates) the grid-highlight config for the given profile and sets it as
        /// <see cref="Current"/>. Returns <see langword="true"/> when a migration from the legacy
        /// <see cref="Profile.GridHighlightSetup"/> storage was performed (and that list was cleared),
        /// signaling that the profile itself should be re-saved.
        /// </summary>
        public static bool LoadForProfile(string profilePath, Profile profile)
        {
            string file = string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, GridHighlightsFileName);

            if (file != null && File.Exists(file))
            {
                _current = Load();
                return false;
            }

            bool migrated = MigrateFromProfile(profile, out GridHighlightsConfig config);
            _current = config;
            _current.Save();
            return migrated;
        }

        private static GridHighlightsConfig LoadForCurrentProfile()
        {
            LoadForProfile(ProfileManager.ProfilePath, ProfileManager.CurrentProfile);
            return _current;
        }

        /// <summary>
        /// Moves the highlights parsed into <see cref="Profile.GridHighlightSetup"/> (migrating from the even
        /// older parallel <c>GridHighlight_*</c> lists first, when needed) into a standalone config and clears
        /// the profile-side storage.
        /// </summary>
        /// <returns><see langword="true"/> when there was data to migrate.</returns>
#pragma warning disable CS0618 // Reading the obsolete GridHighlightSetup is the whole point of migration.
        private static bool MigrateFromProfile(Profile profile, out GridHighlightsConfig config)
        {
            config = new GridHighlightsConfig();

            if (profile == null)
                return false;

            // Pull the even-older per-list storage into GridHighlightSetup first, if that hasn't happened yet.
            if (profile.GridHighlightSetup.Count == 0)
                GridHighLightProfile.MigrateGridHighlightToSetup(profile);

            if (profile.GridHighlightSetup.Count == 0)
                return false;

            config.Highlights = new List<GridHighlightSetupEntry>(profile.GridHighlightSetup);

            // Clear the in-profile storage so grid highlights live only in grid_highlights.json going forward.
            profile.GridHighlightSetup.Clear();

            return true;
        }
#pragma warning restore CS0618
    }

    [JsonSerializable(typeof(GridHighlightsConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<GridHighlightSetupEntry>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class GridHighlightsJsonContext : JsonSerializerContext
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

        public static GridHighlightsJsonContext DefaultToUse { get; } = new GridHighlightsJsonContext(_jsonOptions.Value);
    }
}
