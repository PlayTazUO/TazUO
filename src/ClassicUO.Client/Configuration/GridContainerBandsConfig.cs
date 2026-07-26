using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using ClassicUO.Utility;

namespace ClassicUO.Configuration
{
    /// <summary>The kind of grid container a band group applies to.</summary>
    public enum GridContainerBandGroupType
    {
        Corpses,
        Backpack,
        Other
    }

    /// <summary>
    /// JSON-backed store for the grid-container "band" layout rules. Persisted to
    /// <c>grid_container_bands.json</c> in the current profile's save location. Bands group items in a
    /// grid container into visually separated sections (by item layer and/or graphic) with an optional
    /// custom background color.
    /// <br/>
    /// There are three independent band groups — one for corpses, one for the player's backpack, and one
    /// for every other container — each with its own default enabled flag and its own list of bands.
    /// </summary>
    public sealed class GridContainerBandsConfig
    {
        public const string FileName = "grid_container_bands.json";

        /// <summary>Default vertical gap (px) inserted between bands in the grid band layout.</summary>
        public const int DefaultBandPadding = 15;

        public GridContainerBandGroup Corpses { get; set; } = new();
        public GridContainerBandGroup Backpack { get; set; } = new();
        public GridContainerBandGroup Other { get; set; } = new();

        /// <summary>Vertical gap (px) inserted between bands in the grid band layout.</summary>
        public int BandPadding { get; set; } = DefaultBandPadding;

        /// <summary>Returns the band group for the given type.</summary>
        public GridContainerBandGroup GetGroup(GridContainerBandGroupType type) => type switch
        {
            GridContainerBandGroupType.Corpses => Corpses,
            GridContainerBandGroupType.Backpack => Backpack,
            _ => Other
        };

        /// <summary>
        /// Returns the band group that applies to a container: corpses use the corpse group, the player's
        /// backpack uses the backpack group, and everything else uses the "other" group.
        /// </summary>
        public GridContainerBandGroup GetGroupForContainer(bool isCorpse, bool isBackpack)
        {
            if (isCorpse)
                return Corpses;

            return isBackpack ? Backpack : Other;
        }

        private static GridContainerBandsConfig _current;

        /// <summary>The grid-container band config for the currently loaded profile.</summary>
        public static GridContainerBandsConfig Current => _current ??= LoadForCurrentProfile();

        private static string GetFilePath() =>
            string.IsNullOrEmpty(ProfileManager.ProfilePath) ? null : Path.Combine(ProfileManager.ProfilePath, FileName);

        /// <summary>Loads the band config for the given profile path and sets it as <see cref="Current"/>.</summary>
        public static void LoadForProfile(string profilePath)
        {
            string file = string.IsNullOrEmpty(profilePath) ? null : Path.Combine(profilePath, FileName);

            if (file != null && File.Exists(file))
            {
                _current = ConfigurationResolver.Load<GridContainerBandsConfig>(file, GridContainerBandsJsonContext.DefaultToUse.GridContainerBandsConfig)
                           ?? new GridContainerBandsConfig();
            }
            else
            {
                _current = new GridContainerBandsConfig();
            }
        }

        private static GridContainerBandsConfig LoadForCurrentProfile()
        {
            LoadForProfile(ProfileManager.ProfilePath);
            return _current;
        }

        /// <summary>
        /// Clears the cached config so it is reloaded for the next profile. Called when a profile is
        /// unloaded so edits can't be saved against the previous profile's path after logout.
        /// </summary>
        public static void Reset() => _current = null;

        public void Save()
        {
            string file = GetFilePath();
            if (file == null)
                return;

            ConfigurationResolver.Save(this, file, GridContainerBandsJsonContext.DefaultToUse.GridContainerBandsConfig);
        }
    }

    /// <summary>A named group of bands (with its own default enabled flag) applied to one kind of container.</summary>
    public sealed class GridContainerBandGroup
    {
        /// <summary>Whether band layout is enabled by default for containers of this group's type.</summary>
        public bool Enabled { get; set; } = false;

        public List<GridContainerBand> Bands { get; set; } = new();

        /// <summary>True if this group is enabled and has at least one enabled band.</summary>
        public bool HasActiveBands()
        {
            if (!Enabled || Bands == null)
                return false;

            foreach (GridContainerBand band in Bands)
                if (band is { Enabled: true })
                    return true;

            return false;
        }
    }

    /// <summary>
    /// A single grid-container band. An item belongs to the band when its graphic is in <see cref="Graphics"/>
    /// OR its layer is in <see cref="Layers"/>. Empty filters are ignored (a band with no graphics and no
    /// layers matches nothing).
    /// </summary>
    public sealed class GridContainerBand
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "";

        /// <summary>Whether the custom <see cref="BackgroundColor"/> is applied to this band's grid slots.</summary>
        public bool UseBackgroundColor { get; set; } = false;

        /// <summary>HTML hex background color (e.g. "#3366AA") used for this band's grid slots.</summary>
        public string BackgroundColor { get; set; } = "#3366AA";

        /// <summary>Item layers (see <see cref="ClassicUO.Game.Data.Layer"/>) included in this band.</summary>
        public List<byte> Layers { get; set; } = new();

        /// <summary>Item graphic (with optional hue) filters included in this band.</summary>
        public List<GridContainerBandGraphic> Graphics { get; set; } = new();

        public Color GetBackgroundColor() => (BackgroundColor ?? "").FromHtmlHex();

        public void SetBackgroundColor(Color color) => BackgroundColor = color.ToHtmlHex();

        /// <summary>Returns true if an item with the given graphic/hue/layer belongs to this band.</summary>
        public bool Matches(ushort graphic, ushort hue, byte layer)
        {
            bool hasGraphics = Graphics is { Count: > 0 };
            bool hasLayers = Layers is { Count: > 0 };

            if (!hasGraphics && !hasLayers)
                return false;

            if (hasGraphics)
            {
                foreach (GridContainerBandGraphic g in Graphics)
                {
                    if (g != null && g.Graphic == graphic && (g.Hue < 0 || g.Hue == hue))
                        return true;
                }
            }

            if (hasLayers && Layers.Contains(layer))
                return true;

            return false;
        }
    }

    /// <summary>A graphic filter entry for a band, with an optional hue (<see cref="Hue"/> = -1 matches any hue).</summary>
    public sealed class GridContainerBandGraphic
    {
        public ushort Graphic { get; set; }

        /// <summary>Hue to match, or -1 to match any hue.</summary>
        public int Hue { get; set; } = -1;
    }

    [JsonSerializable(typeof(GridContainerBandsConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class GridContainerBandsJsonContext : JsonSerializerContext
    {
        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

        public static GridContainerBandsJsonContext DefaultToUse { get; } = new GridContainerBandsJsonContext(_jsonOptions.Value);
    }
}
