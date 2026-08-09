using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Per-server settings that live in <c>Data/&lt;ServerName&gt;</c>. Loaded once the server is known
    /// (see <see cref="ProfileManager.LoadServerSettings"/>) and persisted when leaving the server.
    /// </summary>
    public sealed class ServerSettingsSave : JsonSave<ServerSettingsSave>, INotifyPropertyChanged
    {
        protected override SettingsScope Scope => SettingsScope.Server;

        protected override string FileName => "server_settings.json";

        protected override JsonTypeInfo<ServerSettingsSave> TypeInfo => ScopedSettingsJsonContext.DefaultToUse.ServerSettingsSave;
    }
}
