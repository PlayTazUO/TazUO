using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    public class SeasonFilter
    {
        private static readonly Dictionary<Season, Season> _emptyFilters = new();

        public static SeasonFilter Instance { get; } = new SeasonFilter();

        #warning Remove migration >= 10/26/26
        /// <summary>
        /// One-time migration of the pre-<see cref="AccountSettingsSave"/> per-account season filter
        /// dictionary from SQL settings into the per-account settings. The legacy row is cleared so this is
        /// idempotent per account. Requires the current account settings to be loaded so the account scope
        /// resolves to the account that actually stored the filters.
        /// </summary>
        public static void MigrateLegacySqlSettings()
        {
            if (Client.Settings == null || ProfileManager.AccountSettings == null)
            {
                return;
            }

            AccountSettingsSave accountSettings = ProfileManager.AccountSettings;

            string json = Client.Settings.Get(SettingsScope.Account, Constants.SqlSettings.SEASON_FILTER, null);

            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                return;
            }

            Dictionary<Season, Season> legacy;
            try
            {
                legacy = JsonSerializer.Deserialize(json, SeasonFilterJsonContext.Default.DictionarySeasonSeason) ?? new Dictionary<Season, Season>();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to migrate legacy SQL setting '{Constants.SqlSettings.SEASON_FILTER}': {ex.Message}");
                return;
            }

            bool changed = false;

            foreach (KeyValuePair<Season, Season> kvp in legacy)
            {
                if (!accountSettings.SeasonFilters.TryGetValue(kvp.Key, out Season current) || current != kvp.Value)
                {
                    accountSettings.SeasonFilters[kvp.Key] = kvp.Value;
                    changed = true;
                }
            }

            Client.Settings.Set(SettingsScope.Account, Constants.SqlSettings.SEASON_FILTER, "{}");

            if (changed)
            {
                accountSettings.Save();
            }
        }

        public Dictionary<Season, Season> Filters => ProfileManager.AccountSettings?.SeasonFilters ?? _emptyFilters;

        public Season ApplyFilter(Season incoming)
        {
            if (Filters.TryGetValue(incoming, out Season replacement)) return replacement;

            return incoming;
        }

        public void SetFilter(Season from, Season to)
        {
            AccountSettingsSave accountSettings = ProfileManager.AccountSettings;
            if (accountSettings == null)
                return;

            accountSettings.SeasonFilters[from] = to;

            if (World.Instance != null && World.Instance.RealSeason == from) World.Instance.ChangeSeason(to);
        }

        public void RemoveFilter(Season from)
        {
            AccountSettingsSave accountSettings = ProfileManager.AccountSettings;
            if (accountSettings == null)
                return;

            accountSettings.SeasonFilters.Remove(from);
        }

        public void Clear()
        {
            AccountSettingsSave accountSettings = ProfileManager.AccountSettings;
            if (accountSettings == null)
                return;

            accountSettings.SeasonFilters.Clear();
        }
    }

    [JsonSerializable(typeof(Dictionary<Season, Season>))]
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        IgnoreReadOnlyProperties = false,
        IncludeFields = false)]
    public partial class SeasonFilterJsonContext : JsonSerializerContext
    {
    }
}
