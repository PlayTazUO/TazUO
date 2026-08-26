using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public class SoundFilterManager
    {
        private static readonly HashSet<int> _emptyFilters = new();

        public static SoundFilterManager Instance { get; } = new SoundFilterManager();

        #warning Remove migration >= 10/26/26
        /// <summary>
        /// One-time migration of the pre-<see cref="GlobalSettingsSave"/> per-account sound/music filter
        /// lists from SQL settings into the machine-wide global settings. The legacy rows are cleared so
        /// this is idempotent per account. Requires the current profile to be loaded so the account scope
        /// resolves to the account that actually stored the lists.
        /// </summary>
        public static void MigrateLegacySqlSettings()
        {
            if (Client.Settings == null || ProfileManager.CurrentProfile == null || ProfileManager.GlobalSettings == null)
            {
                return;
            }

            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;

            MigrateLegacySet(Constants.SqlSettings.SOUND_FILTER_IDS, globalSettings.FilteredSounds);
            MigrateLegacySet(Constants.SqlSettings.MUSIC_FILTER_IDS, globalSettings.FilteredMusic);
        }

        private static bool MigrateLegacySet(string sqlKey, HashSet<int> target)
        {
            string json = Client.Settings.Get(SettingsScope.Account, sqlKey, null);

            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return false;
            }

            HashSet<int> legacy;
            try
            {
                legacy = JsonSerializer.Deserialize(json, HashSetIntContext.Default.HashSetInt32) ?? new HashSet<int>();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to migrate legacy SQL setting '{sqlKey}': {ex.Message}");
                return false;
            }

            bool changed = false;

            foreach (int id in legacy)
            {
                changed |= target.Add(id);
            }

            Client.Settings.Set(SettingsScope.Account, sqlKey, "[]");

            return changed;
        }

        public HashSet<int> FilteredSounds => ProfileManager.GlobalSettings?.FilteredSounds ?? _emptyFilters;

        public HashSet<int> FilteredMusic => ProfileManager.GlobalSettings?.FilteredMusic ?? _emptyFilters;

        public void AddFilter(int soundId, bool isMusic = false)
        {
            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
            if (globalSettings == null)
                return;

            HashSet<int> filters = isMusic ? globalSettings.FilteredMusic : globalSettings.FilteredSounds;

            filters.Add(soundId);
        }

        public void RemoveFilter(int soundId, bool isMusic = false)
        {
            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
            if (globalSettings == null)
                return;

            HashSet<int> filters = isMusic ? globalSettings.FilteredMusic : globalSettings.FilteredSounds;

            filters.Remove(soundId);
        }

        public bool IsSoundFiltered(int soundId, bool isMusic = false)
        {
            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
            if (globalSettings == null)
                return false;

            return isMusic
                ? globalSettings.FilteredMusic.Contains(soundId)
                : globalSettings.FilteredSounds.Contains(soundId);
        }

        public void Clear(bool isMusic = false)
        {
            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
            if (globalSettings == null)
                return;

            HashSet<int> filters = isMusic ? globalSettings.FilteredMusic : globalSettings.FilteredSounds;

            if (filters.Count > 0)
                filters.Clear();
        }

        public void Reset(bool isMusic = false)
        {
            GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
            if (globalSettings == null)
            {
                return;
            }

            (isMusic ? globalSettings.FilteredMusic : globalSettings.FilteredSounds).Clear();
        }
    }

    [JsonSerializable(typeof(HashSet<int>))]
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        IgnoreReadOnlyProperties = false,
        IncludeFields = false)]
    public partial class HashSetIntContext : JsonSerializerContext
    {
    }
}
