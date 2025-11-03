using System.Collections.Generic;
using System.Numerics;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// A collection of color definitions for a consistent ImGui theme.
    /// </summary>
    public class ImGuiTheme
    {
        private static readonly Dictionary<string, ImGuiTheme> _themes = new();
        private static ImGuiTheme _currentTheme;
        private static string _currentThemeName = "Default";

        static ImGuiTheme()
        {
            // Register default themes
            RegisterTheme("Default", CreateDefaultTheme());
            RegisterTheme("Light", CreateLightTheme());
            SetTheme("Default");
        }

        public ImGuiTheme()
        {
            Colors = new ThemeColors();
        }

        /// <summary>
        /// Colors for this theme instance
        /// </summary>
        public ThemeColors Colors { get; set; }

        /// <summary>
        /// The currently active theme's colors
        /// </summary>
        public static ThemeColors Current => _currentTheme.Colors;

        /// <summary>
        /// The name of the currently active theme
        /// </summary>
        public static string CurrentThemeName => _currentThemeName;

        /// <summary>
        /// Register a new theme
        /// </summary>
        public static void RegisterTheme(string name, ImGuiTheme theme) => _themes[name] = theme;

        /// <summary>
        /// Get all available theme names
        /// </summary>
        public static string[] GetThemes()
        {
            string[] themes = new string[_themes.Count];
            _themes.Keys.CopyTo(themes, 0);
            return themes;
        }

        /// <summary>
        /// Set the active theme by name
        /// </summary>
        public static bool SetTheme(string name)
        {
            if (_themes.TryGetValue(name, out ImGuiTheme theme))
            {
                _currentTheme = theme;
                _currentThemeName = name;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a theme instance by name
        /// </summary>
        public static ImGuiTheme GetTheme(string name) => _themes.TryGetValue(name, out ImGuiTheme theme) ? theme : null;

        private static ImGuiTheme CreateDefaultTheme() =>
            new()
            {
                Colors = new ThemeColors
                {
                    // Bases
                    Base100 = new Vector4(0.118f, 0.115f, 0.143f, 1.00f), // #1E1D24FF
                    Base200 = new Vector4(0.196f, 0.200f, 0.220f, 1.00f), // #323338ff
                    Base300 = new Vector4(0.314f, 0.314f, 0.314f, 1.00f), // #505050ff
                    BaseContent = new Vector4(0.941f, 0.941f, 0.941f, 1.00f), // #f0f0f0ff

                    // Principal Palette
                    Primary = new Vector4(0.667f, 0.412f, 0.051f, 1.00f), // #aa690dff
                    PrimaryContent = new Vector4(0.118f, 0.115f, 0.143f, 1.00f), // #1E1D24FF

                    Secondary = new Vector4(0.82f, 0.68f, 0.32f, 1.00f), // #D1AD52FF
                    SecondaryContent = new Vector4(0.118f, 0.115f, 0.143f, 1.00f), // #1E1D24FF

                    Accent = new Vector4(0.88f, 0.74f, 0.38f, 1.00f), // #E0BD61FF
                    AccentContent = new Vector4(0.10f, 0.08f, 0.15f, 1.00f), // #191428FF

                    // Neutral
                    Neutral = new Vector4(0.097f, 0.094f, 0.118f, 1.00f), // #191820FF
                    NeutralContent = new Vector4(0.941f, 0.941f, 0.941f, 1.00f), // #f0f0f0ff

                    // Info - States
                    Info = new Vector4(0.36f, 0.72f, 0.62f, 1.00f), // #5CB89EFF
                    InfoContent = new Vector4(0.10f, 0.15f, 0.25f, 1.00f), // #192640FF
                    Success = new Vector4(0.40f, 0.75f, 0.58f, 1.00f), // #66BF94FF
                    SuccessContent = new Vector4(0.10f, 0.16f, 0.25f, 1.00f), // #1A2940FF
                    Warning = new Vector4(0.96f, 0.78f, 0.32f, 1.00f), // #F5C753FF
                    WarningContent = new Vector4(0.18f, 0.18f, 0.10f, 1.00f), // #2E2E1AFF
                    Error = new Vector4(0.74f, 0.20f, 0.18f, 1.00f), // #BD332EFF
                    ErrorContent = new Vector4(0.12f, 0.10f, 0.10f, 1.00f), // #1F1A1AFF

                    // Extras for UI commons
                    Border = new Vector4(0.097f, 0.094f, 0.118f, 0.50f), // #19182080
                    BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.00f), // #00000000
                    ScrollbarBg = new Vector4(0.089f, 0.087f, 0.110f, 1.00f), // #16161CFF
                    ScrollbarGrab = new Vector4(0.300f, 0.350f, 0.400f, 1.00f), // #4C5966FF
                    ScrollbarGrabHovered = new Vector4(0.400f, 0.500f, 0.550f, 1.00f), // #66808CFF
                    ScrollbarGrabActive = new Vector4(0.450f, 0.550f, 0.600f, 1.00f), // #738C99FF
                }
            };

        private static ImGuiTheme CreateLightTheme() =>
            new()
            {
                Colors = new ThemeColors
                {
                    // Bases - Light theme uses inverted colors
                    Base100 = new Vector4(0.98f, 0.98f, 0.98f, 1.00f), // Light gray
                    Base200 = new Vector4(0.92f, 0.92f, 0.92f, 1.00f), // Medium light gray
                    Base300 = new Vector4(0.85f, 0.85f, 0.85f, 1.00f), // Medium gray
                    BaseContent = new Vector4(0.10f, 0.10f, 0.10f, 1.00f), // Dark text

                    // Principal Palette
                    Primary = new Vector4(0.20f, 0.40f, 0.70f, 1.00f), // Blue
                    PrimaryContent = new Vector4(1.00f, 1.00f, 1.00f, 1.00f), // White text

                    Secondary = new Vector4(0.45f, 0.35f, 0.75f, 1.00f), // Purple
                    SecondaryContent = new Vector4(1.00f, 1.00f, 1.00f, 1.00f), // White text

                    Accent = new Vector4(0.95f, 0.60f, 0.20f, 1.00f), // Orange
                    AccentContent = new Vector4(0.10f, 0.10f, 0.10f, 1.00f), // Dark text

                    // Neutral
                    Neutral = new Vector4(0.88f, 0.88f, 0.88f, 1.00f), // Light gray
                    NeutralContent = new Vector4(0.10f, 0.10f, 0.10f, 1.00f), // Dark text

                    // Info - States
                    Info = new Vector4(0.20f, 0.60f, 0.86f, 1.00f), // Light blue
                    InfoContent = new Vector4(1.00f, 1.00f, 1.00f, 1.00f),
                    Success = new Vector4(0.30f, 0.70f, 0.40f, 1.00f), // Green
                    SuccessContent = new Vector4(1.00f, 1.00f, 1.00f, 1.00f),
                    Warning = new Vector4(0.95f, 0.70f, 0.20f, 1.00f), // Yellow/Orange
                    WarningContent = new Vector4(0.10f, 0.10f, 0.10f, 1.00f),
                    Error = new Vector4(0.85f, 0.20f, 0.20f, 1.00f), // Red
                    ErrorContent = new Vector4(1.00f, 1.00f, 1.00f, 1.00f),

                    // Extras for UI commons
                    Border = new Vector4(0.70f, 0.70f, 0.70f, 0.80f),
                    BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.10f),
                    ScrollbarBg = new Vector4(0.95f, 0.95f, 0.95f, 1.00f),
                    ScrollbarGrab = new Vector4(0.60f, 0.60f, 0.60f, 1.00f),
                    ScrollbarGrabHovered = new Vector4(0.50f, 0.50f, 0.50f, 1.00f),
                    ScrollbarGrabActive = new Vector4(0.40f, 0.40f, 0.40f, 1.00f),
                }
            };

        public class ThemeColors
        {
            // Bases
            public Vector4 Base100 { get; set; }
            public Vector4 Base200 { get; set; }
            public Vector4 Base300 { get; set; }
            public Vector4 BaseContent { get; set; }

            // Principal Palette
            public Vector4 Primary { get; set; }
            public Vector4 PrimaryContent { get; set; }
            public Vector4 Secondary { get; set; }
            public Vector4 SecondaryContent { get; set; }
            public Vector4 Accent { get; set; }
            public Vector4 AccentContent { get; set; }

            // Neutral
            public Vector4 Neutral { get; set; }
            public Vector4 NeutralContent { get; set; }

            // Info - States
            public Vector4 Info { get; set; }
            public Vector4 InfoContent { get; set; }
            public Vector4 Success { get; set; }
            public Vector4 SuccessContent { get; set; }
            public Vector4 Warning { get; set; }
            public Vector4 WarningContent { get; set; }
            public Vector4 Error { get; set; }
            public Vector4 ErrorContent { get; set; }

            // Extras for UI commons
            public Vector4 Border { get; set; }
            public Vector4 BorderShadow { get; set; }
            public Vector4 ScrollbarBg { get; set; }
            public Vector4 ScrollbarGrab { get; set; }
            public Vector4 ScrollbarGrabHovered { get; set; }
            public Vector4 ScrollbarGrabActive { get; set; }
        }

        public static class Dimensions
        {
            public const float STANDARD_INPUT_WIDTH = 80f;
            public const float STANDARD_TABLE_SCROLL_HEIGHT = 200f;
        }
    }
}
