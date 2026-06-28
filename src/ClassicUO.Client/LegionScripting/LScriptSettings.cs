using System.Collections.Generic;

namespace ClassicUO.LegionScripting
{
    public class LScriptSettings
    {
        public List<string> GlobalAutoStartScripts { get; set; } = new List<string>();
        public Dictionary<string, List<string>> CharAutoStartScripts { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, bool> GroupCollapsed { get; set; } = new Dictionary<string, bool>();
        public bool DisableModuleCache { get; set; }

        /// <summary>
        /// Hotkeys bound to scripts, keyed by the script's <see cref="ScriptFile.RelativePath"/> so
        /// scripts that share a file name in different groups don't collide. Persisted globally (not
        /// per-profile) alongside the rest of the Legion script settings.
        /// </summary>
        public Dictionary<string, ScriptHotkeyData> ScriptHotkeys { get; set; } = new Dictionary<string, ScriptHotkeyData>();
    }

    /// <summary>
    /// Serializable on-disk shape of a single script hotkey binding. Mirrors the fields of the
    /// central <c>HotkeyBinding</c>; conversion happens in <see cref="ScriptHotkeysManager"/>.
    /// </summary>
    public class ScriptHotkeyData
    {
        public int Key { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        public int MouseButton { get; set; }
        public bool WheelScroll { get; set; }
        public bool WheelUp { get; set; }
        public int[] ControllerButtons { get; set; }
    }
}
