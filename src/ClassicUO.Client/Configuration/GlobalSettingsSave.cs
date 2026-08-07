using System.Text.Json.Serialization.Metadata;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Machine-wide settings that live in the shared <c>Data</c> folder. Loaded once at startup via
    /// <see cref="ProfileManager.LoadGlobalSettings"/> and persisted when the client exits.
    /// </summary>
    public sealed class GlobalSettingsSave : JsonSave<GlobalSettingsSave>
    {
        protected override SettingsScope Scope => SettingsScope.Global;

        protected override string FileName => "global_settings.json";

        protected override JsonTypeInfo<GlobalSettingsSave> TypeInfo => ScopedSettingsJsonContext.DefaultToUse.GlobalSettingsSave;

        /// <summary>
        /// When true, auto-open uses doors regardless of their open/closed state, so doors the
        /// player walks past are toggled (opened or closed) instead of only opened.
        /// </summary>
        public bool AutoCloseDoors { get; set; }

        /// <summary>
        /// When true, use the modern color picker gump for selecting hues.
        /// </summary>
        public bool UseModernColorPicker { get; set; }
    }
}
