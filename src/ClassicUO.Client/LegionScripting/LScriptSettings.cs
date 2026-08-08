using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration;

namespace ClassicUO.LegionScripting
{
    /// <summary>
    /// JSON-backed store for Legion script settings. Persisted to <c>lscript.json</c> in the shared
    /// <c>Data</c> folder. Saving/loading (with rotating backups) is handled by <see cref="JsonSave{T}"/>.
    /// </summary>
    public class LScriptSettings : JsonSave<LScriptSettings>, INotifyPropertyChanged
    {
        private const string LScriptSettingsFileName = "lscript.json";

        public List<string> GlobalAutoStartScripts { get; set; } = new List<string>();
        public Dictionary<string, List<string>> CharAutoStartScripts { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, bool> GroupCollapsed { get; set; } = new Dictionary<string, bool>();
        public bool DisableModuleCache { get; set; }

        /// <summary>Lives in the shared <c>Data</c> folder, matching the legacy save location.</summary>
        protected override SettingsScope Scope => SettingsScope.Global;

        protected override string FileName => LScriptSettingsFileName;

        protected override JsonTypeInfo<LScriptSettings> TypeInfo => LScriptJsonContext.Default.LScriptSettings;
    }

    [JsonSerializable(typeof(LScriptSettings))]
    public partial class LScriptJsonContext : JsonSerializerContext
    {
    }
}
