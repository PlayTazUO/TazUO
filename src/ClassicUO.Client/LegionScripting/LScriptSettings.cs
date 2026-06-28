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
        /// Relative paths (<see cref="ScriptFile.RelativePath"/>) of scripts that have a hotkey
        /// assigned. The actual key binding lives in the central hotkey system (hotkeys.json); this
        /// list only records which scripts to re-register on load so the central system can re-apply
        /// their saved bindings. The relative path is used so identically named scripts in different
        /// groups don't collide. Entries whose script no longer exists are pruned on load.
        /// </summary>
        public List<string> ScriptHotkeys { get; set; } = new List<string>();
    }
}
