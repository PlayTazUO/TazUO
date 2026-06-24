using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Data;

public class HotKeyEntry
{
    public HotKeyTrigger Trigger { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public HotKeyAction Action { get; set; } = new();
}

public class HotKeySettings
{
    public List<HotKeyEntry> Entries { get; set; } = new();
    public List<CustomConsumable> CustomConsumables { get; set; } = new();
}

[JsonSerializable(typeof(HotKeySettings), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HotKeyEntry), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HotKeyTrigger), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(HotKeyAction), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CustomConsumable), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class HotKeySettingsContext : JsonSerializerContext { }
