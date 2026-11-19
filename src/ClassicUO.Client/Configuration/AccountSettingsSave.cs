using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game.Managers;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Per-account settings that live in <c>Data/&lt;ServerName&gt;/&lt;Account&gt;</c>. Loaded once both the
    /// server and account are known (see <see cref="ProfileManager.LoadAccountSettings"/>) and persisted when
    /// leaving the account/server.
    /// </summary>
    public sealed class AccountSettingsSave : JsonSave<AccountSettingsSave>, INotifyPropertyChanged
    {
        protected override SettingsScope Scope => SettingsScope.Account;

        protected override string FileName => "account_settings.json";

        protected override JsonTypeInfo<AccountSettingsSave> TypeInfo => ScopedSettingsJsonContext.DefaultToUse.AccountSettingsSave;

        /// <summary>
        /// Maximum distance in tiles for the bandage agent to heal friends/allies. Defaults
        /// to the legacy hardcoded bandage range.
        /// </summary>
        public int BandageAgentDistance { get; set => SetProperty(ref field, value); } = 3;

        /// <summary>
        /// Season override filters keyed by the season the server sends. Per-account.
        /// </summary>
        public Dictionary<Season, Season> SeasonFilters { get; set => SetProperty(ref field, value); } = new Dictionary<Season, Season>();

        /// <summary>
        /// Per-character last-equipment snapshots used by the character selection paperdoll, keyed by a
        /// composite server+account+character id. Per-account so it follows the account across characters.
        /// </summary>
        public Dictionary<string, string> LastEquipmentData { get; set => SetProperty(ref field, value); } = new Dictionary<string, string>();
    }
}
