using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Managers.Hotkeys
{
    /// <summary>
    /// On-disk shape of <c>hotkeys.json</c>. Persisted per-profile alongside <c>macros.xml</c>.
    /// </summary>
    public sealed class HotkeysFile
    {
        public List<HotkeyEntryDto> Entries { get; set; } = new();
    }

    public sealed class HotkeyEntryDto
    {
        public string Id { get; set; }
        public int Key { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        public int MouseButton { get; set; }
        public bool WheelScroll { get; set; }
        public bool WheelUp { get; set; }
        public int[] ControllerButtons { get; set; }
        public bool Enabled { get; set; } = true;
    }

    [JsonSourceGenerationOptions(WriteIndented = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HotkeysFile))]
    internal partial class HotkeysJsonContext : JsonSerializerContext
    {
    }
}
