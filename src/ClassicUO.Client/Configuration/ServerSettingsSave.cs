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

        public ushort TurnDelay { get; set => SetProperty(ref field, value); } = 80;
        public bool EnableEnhancedPackets { get; set => SetProperty(ref field, value); } = true;

        /// <summary>When enabled, corpses that have already been opened are not auto-opened again.</summary>
        public bool DoNotReopenCorpses { get; set => SetProperty(ref field, value); } = false;

        /// <summary>
        /// When true, swapping an equipped item on paperdoll drop uses the KR equip macro packet
        /// (0xEC) instead of the queue-based pickup/drop sequence. Faster, but the server must
        /// support the packet. Per-server because support varies by shard.
        /// </summary>
        public bool UseKrEquipSwap { get; set => SetProperty(ref field, value); }
    }
}
