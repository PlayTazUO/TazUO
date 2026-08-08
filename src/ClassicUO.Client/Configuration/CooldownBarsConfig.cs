using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// A single cooldown-bar rule. Defines the display name, duration, color and trigger
    /// conditions for a bar shown when a matching message is received.
    /// </summary>
    public sealed class CooldownBarConfigEntry
    {
        /// <summary>The bar's hue (color)</summary>
        public ushort Hue { get; set; } = 42;

        /// <summary>Text rendered inside the bar</summary>
        public string Label { get; set; } = "Label";

        /// <summary>Message substring that triggers this bar when received</summary>
        public string Trigger { get; set; } = "Text to trigger";

        /// <summary>Duration of the bar, in seconds</summary>
        public int Cooldown { get; set; } = 10;

        /// <summary>Which message sources can trigger this rule (0 = All, 1 = Self, 2 = Other)</summary>
        public int MessageType { get; set; } = 0;

        /// <summary>When triggered again, restart an already-running bar of the same name</summary>
        public bool ReplaceIfExists { get; set; } = false;

        /// <summary>
        /// When a bar of the same name is already running, do nothing (preserve the running countdown).
        /// Takes precedence over <see cref="ReplaceIfExists"/>.
        /// </summary>
        public bool SkipIfExists { get; set; } = false;
    }

    /// <summary>
    /// JSON-backed store for cooldown-bar rules. Persisted to <c>cooldownbars.json</c> in the
    /// current profile's save location. Replaces the legacy per-list storage on <see cref="Profile"/>,
    /// which is migrated on first load. Saving/loading (with rotating backups) is handled by
    /// <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class CooldownBarsConfig : JsonSave<CooldownBarsConfig>, INotifyPropertyChanged
    {
        private const string CooldownBarsFileName = "cooldownbars.json";

        public List<CooldownBarConfigEntry> Bars { get; set; } = new();

        /// <summary>Lives in the profile folder alongside the other per-character configs.</summary>
        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => CooldownBarsFileName;

        protected override JsonTypeInfo<CooldownBarsConfig> TypeInfo => CooldownBarsJsonContext.DefaultToUse.CooldownBarsConfig;

        private static CooldownBarsConfig _current;

        /// <summary>The cooldown-bar config for the currently loaded profile.</summary>
        public static CooldownBarsConfig Current => _current ??= LoadForCurrentProfile();

        /// <summary>
        /// Loads (or migrates) the cooldown-bar config for the given profile and sets it as
        /// <see cref="Current"/>. Returns <see langword="true"/> when a migration from the legacy
        /// <see cref="Profile"/> lists was performed (and those lists were cleared), signaling that
        /// the profile itself should be re-saved.
        /// </summary>
        public static bool LoadForProfile(string profilePath, Profile profile)
        {
            string file = string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, CooldownBarsFileName);

            if (file != null && File.Exists(file))
            {
                _current = Load();
                return false;
            }

            bool migrated = MigrateFromProfile(profile, out CooldownBarsConfig config);
            _current = config;
            _current.Save();
            return migrated;
        }

        public static void Unload()
        {
            if (_current == null)
                return;
            
            _current.Save();
            _current = null;
        }

        private static CooldownBarsConfig LoadForCurrentProfile()
        {
            LoadForProfile(ProfileManager.ProfilePath, ProfileManager.CurrentProfile);
            return _current;
        }

        /// <summary>
        /// Stores <paramref name="entry"/> at <paramref name="index"/>. When <paramref name="index"/>
        /// is out of range the entry is appended if <paramref name="createIfMissing"/> is set,
        /// otherwise the call is a no-op. Persists on any change.
        /// </summary>
        public void Upsert(int index, CooldownBarConfigEntry entry, bool createIfMissing)
        {
            if (index >= 0 && index < Bars.Count)
                Bars[index] = entry;
            else if (createIfMissing)
                Bars.Add(entry);
            else
                return;

            Save();
        }

        /// <summary>Removes the entry at <paramref name="index"/>, if present, and persists.</summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Bars.Count)
                return;

            Bars.RemoveAt(index);
            Save();
        }

        /// <summary>
        /// Builds a config from the legacy parallel <see cref="Profile"/> lists and clears them.
        /// </summary>
        /// <returns><see langword="true"/> when there was legacy data to migrate.</returns>
#pragma warning disable CS0618 // Reading the obsolete legacy lists is the whole point of migration.
        private static bool MigrateFromProfile(Profile profile, out CooldownBarsConfig config)
        {
            config = new CooldownBarsConfig();

            if (profile == null)
                return false;

            int count = profile.Condition_Hue.Count;

            for (int i = 0; i < count; i++)
            {
                config.Bars.Add(new CooldownBarConfigEntry
                {
                    Hue = profile.Condition_Hue[i],
                    Label = profile.Condition_Label.ElementAtOrDefault(i) ?? string.Empty,
                    Trigger = profile.Condition_Trigger.ElementAtOrDefault(i) ?? string.Empty,
                    Cooldown = profile.Condition_Duration.ElementAtOrDefault(i),
                    MessageType = profile.Condition_Type.ElementAtOrDefault(i),
                    ReplaceIfExists = i < profile.Condition_ReplaceIfExists.Count && profile.Condition_ReplaceIfExists[i],
                    SkipIfExists = i < profile.Condition_SkipIfExists.Count && profile.Condition_SkipIfExists[i]
                });
            }

            if (count == 0)
                return false;

            // Clear the legacy lists so the "complicated" per-list storage no longer persists in the profile.
            profile.Condition_Hue.Clear();
            profile.Condition_Label.Clear();
            profile.Condition_Trigger.Clear();
            profile.Condition_Duration.Clear();
            profile.Condition_Type.Clear();
            profile.Condition_ReplaceIfExists.Clear();
            profile.Condition_SkipIfExists.Clear();

            return true;
        }
#pragma warning restore CS0618
    }

    [JsonSerializable(typeof(CooldownBarsConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class CooldownBarsJsonContext : JsonSerializerContext
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

        public static CooldownBarsJsonContext DefaultToUse { get; } = new CooldownBarsJsonContext(_jsonOptions.Value);
    }
}
