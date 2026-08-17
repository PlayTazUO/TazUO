using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// Machine-wide settings that live in the shared <c>Data</c> folder. Loaded once at startup via
    /// <see cref="ProfileManager.LoadGlobalSettings"/> and persisted when the client exits.
    /// </summary>
    public sealed class GlobalSettingsSave : JsonSave<GlobalSettingsSave>, INotifyPropertyChanged
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

        /// <summary>
        /// When true, show a translucent ghost of the held item on the ground tile it would land on
        /// while dragging it over the world.
        /// </summary>
        public bool ShowDragItemPreview { get; set => SetProperty(ref field, value); } = true;
        public bool UseCircleOfTransparency { get; set => SetProperty(ref field, value); }
        public int CircleOfTransparencyRadius { get; set => SetProperty(ref field, value); } = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS / 2;
        public int CircleOfTransparencyType { get; set => SetProperty(ref field, value); } // 0 = normal, 1 = like original client
        public bool EnableSound { get; set => SetProperty(ref field, value); } = true;
        public int SoundVolume { get; set => SetProperty(ref field, value); } = 50;
        public bool EnableMusic { get; set => SetProperty(ref field, value); } = true;
        public int MusicVolume { get; set => SetProperty(ref field, value); } = 50;
        public bool EnableFootstepsSound { get; set => SetProperty(ref field, value); } = true;
        public bool EnableRainSound { get; set => SetProperty(ref field, value); } = true;
        public bool EnableCombatMusic { get; set => SetProperty(ref field, value); } = true;
        public bool ReproduceSoundsInBackground { get; set => SetProperty(ref field, value); }
        public bool UseWASDInsteadArrowKeys { get; set => SetProperty(ref field, value); }
        public bool SingleClickIconUse { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// When true, journal entries are shown without their timestamp in both the resizable
        /// journal and the classic journal gump.
        /// </summary>
        public bool HideJournalTimestamp { get; set => SetProperty(ref field, value); }
    }
}
