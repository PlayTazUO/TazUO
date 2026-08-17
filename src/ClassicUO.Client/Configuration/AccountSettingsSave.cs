using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

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
    }
}
