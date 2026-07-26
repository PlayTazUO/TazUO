using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using ClassicUO.Utility;

namespace ClassicUO.Configuration
{
    /// <summary>
    /// JSON-backed store for the grid-container "band" layout rules. Persisted to
    /// <c>grid_container_bands.json</c> in the current profile's save location. Bands group items in a
    /// grid container into visually separated sections (by item layer and/or graphic) with an optional
    /// custom background color.
    /// </summary>
    public sealed class GridContainerBandsConfig
    {
        public const string FileName = "grid_container_bands.json";

        public List<GridContainerBand> Bands { get; set; } = new();

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

        /// <summary>Forces a reload from disk on the next <see cref="Current"/> access (used on profile switch).</summary>
        public static void Reset() => _current = null;

        public void Save()
        {
            string file = GetFilePath();
            if (file == null)
                return;

            ConfigurationResolver.Save(this, file, GridContainerBandsJsonContext.DefaultToUse.GridContainerBandsConfig);
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

        /// <summary>Item graphics included in this band.</summary>
        public List<ushort> Graphics { get; set; } = new();

        public Color GetBackgroundColor() => BackgroundColor.FromHtmlHex();

        public void SetBackgroundColor(Color color) => BackgroundColor = color.ToHtmlHex();

        /// <summary>Returns true if an item with the given graphic/layer belongs to this band.</summary>
        public bool Matches(ushort graphic, byte layer)
        {
            bool hasGraphics = Graphics is { Count: > 0 };
            bool hasLayers = Layers is { Count: > 0 };

            if (!hasGraphics && !hasLayers)
                return false;

            if (hasGraphics && Graphics.Contains(graphic))
                return true;

            if (hasLayers && Layers.Contains(layer))
                return true;

            return false;
        }
    }

    [JsonSerializable(typeof(GridContainerBandsConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    sealed partial class GridContainerBandsJsonContext : JsonSerializerContext
    {
        sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

            public override string ConvertName(string name) =>
                string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static Lazy<JsonSerializerOptions> _jsonOptions { get; } = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            };
            return options;
        });

        public static GridContainerBandsJsonContext DefaultToUse { get; } = new GridContainerBandsJsonContext(_jsonOptions.Value);
    }
}
