using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Game;
using ClassicUO.Utility.Platforms;

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

        /// <summary>Sound IDs the player has muted via the sound filter UI. Machine-wide.</summary>
        public HashSet<int> FilteredSounds { get; set => SetProperty(ref field, value); } = new HashSet<int>();

        /// <summary>Music IDs the player has muted via the music filter UI. Machine-wide.</summary>
        public HashSet<int> FilteredMusic { get; set => SetProperty(ref field, value); } = new HashSet<int>();

        public bool UseWASDInsteadArrowKeys { get; set => SetProperty(ref field, value); }
        public bool SingleClickIconUse { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// When true, journal entries are shown without their timestamp in both the resizable
        /// journal and the classic journal gump.
        /// </summary>
        public bool HideJournalTimestamp { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// When true, world map markers render at full visibility on every zoom level instead of
        /// degrading to a small dot (or disappearing) when zoomed out past their ZoomIndex.
        /// </summary>
        public bool AlwaysShowWorldMapMarkers { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// Only applies when there is only 1 server available
        /// </summary>
        public bool SkipServerSelection { get; set => SetProperty(ref field, value); } = true;
        public bool ManagedZlib { get; set => SetProperty(ref field, value); } = !PlatformHelper.IsWindows;
        public float GlobalScale { get; set => SetProperty(ref field, value); } = 1f;

        /// <summary>Web map journal panel width. Machine-wide.</summary>
        public int WebMapJournalWidth { get; set => SetProperty(ref field, value); } = 400;

        /// <summary>Web map journal panel height. Machine-wide.</summary>
        public int WebMapJournalHeight { get; set => SetProperty(ref field, value); } = 300;

        /// <summary>Whether the web map journal panel is collapsed. Machine-wide.</summary>
        public bool WebMapJournalMinimized { get; set => SetProperty(ref field, value); }

        /// <summary>Whether the web map controls panel is collapsed. Machine-wide.</summary>
        public bool WebMapControlsMinimized { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// Per-character last-equipment snapshots used by the character selection paperdoll, keyed by a
        /// composite server+account+character id. Machine-wide.
        /// </summary>
        public Dictionary<string, string> LastEquipmentData { get; set => SetProperty(ref field, value); } = new Dictionary<string, string>();

        /// <summary>
        /// Semicolon-separated poll ids the user has already voted on (see FirebasePollsManager /
        /// PollsWindow). Machine-wide.
        /// </summary>
        public string VotedPolls { get; set => SetProperty(ref field, value); }

        /// <summary>
        /// Uses the campfire/Diablo-style character selection screen instead of the classic list.
        /// Global so it can be read/written at the character-selection screen before any profile loads.
        /// </summary>
        public bool UseCampfireCharacterSelect { get; set => SetProperty(ref field, value); }

        /// <summary>UI language code used for TazLang strings. Defaults to <c>"EN"</c>.</summary>
        public string UILanguage { get; set => SetProperty(ref field, value); } = "EN";


        
        public int MigrationVersion { get; set => SetProperty(ref field, value); } = 0;
    }
}
