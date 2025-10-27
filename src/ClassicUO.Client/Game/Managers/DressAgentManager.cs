using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    public class DressAgentManager
    {
        public static DressAgentManager Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        public List<DressConfig> CurrentPlayerConfigs { get; private set; } = new();
        public List<DressConfig> OtherCharacterConfigs { get; private set; } = new();
        public bool IsLoaded { get; private set; }

        private readonly string _saveFileName = "dress_configs.json";

        private DressAgentManager() { }

        public void Load()
        {
            if (ProfileManager.ProfilePath == null)
                return;

            string savePath = Path.Combine(ProfileManager.ProfilePath, _saveFileName);
            string characterName = ProfileManager.CurrentProfile?.CharacterName ?? "";

            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    CurrentPlayerConfigs = JsonSerializer.Deserialize(json, DressAgentJsonContext.Default.ListDressConfig) ?? new List<DressConfig>();

                    // Ensure all configs have the correct character name
                    foreach (DressConfig config in CurrentPlayerConfigs)
                    {
                        config.CharacterName = characterName;
                    }
                }
                catch (Exception ex)
                {
                    Utility.Logging.Log.Error($"Error loading dress configs: {ex.Message}");
                    CurrentPlayerConfigs = new List<DressConfig>();
                }
            }
            else
            {
                CurrentPlayerConfigs = new List<DressConfig>();
            }

            LoadOtherCharacterConfigs();

            IsLoaded = true;
        }

        public void Unload() => Instance = null;

        private void LoadOtherCharacterConfigs()
        {
            OtherCharacterConfigs.Clear();

            string rootpath;
            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
            {
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            }
            else
            {
                rootpath = Settings.GlobalSettings.ProfilesPath;
            }

            if (!Directory.Exists(rootpath))
                return;

            try
            {
                string[] allAccounts = Directory.GetDirectories(rootpath);
                string currentCharacterName = ProfileManager.CurrentProfile?.CharacterName ?? "";

                foreach (string account in allAccounts)
                {
                    string[] allServers = Directory.GetDirectories(account);

                    foreach (string server in allServers)
                    {
                        string[] allCharacters = Directory.GetDirectories(server);

                        foreach (string characterPath in allCharacters)
                        {
                            string characterName = Path.GetFileName(characterPath);

                            // Skip current character (already loaded above)
                            if (characterName == currentCharacterName)
                                continue;

                            string configFilePath = Path.Combine(characterPath, _saveFileName);
                            if (File.Exists(configFilePath))
                            {
                                try
                                {
                                    string json = File.ReadAllText(configFilePath);
                                    List<DressConfig> configs = JsonSerializer.Deserialize(json, DressAgentJsonContext.Default.ListDressConfig) ?? new List<DressConfig>();

                                    foreach (DressConfig config in configs)
                                    {
                                        config.CharacterName = characterName; // Ensure character name is set
                                        OtherCharacterConfigs.Add(config);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Utility.Logging.Log.Error($"Error loading dress configs for {characterName}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Error scanning for other character dress configs: {ex.Message}");
            }
        }

        public void DressAgentCommand(string[] args)
        {
            if (args.Length < 3)
            {
                GameActions.Print(World.Instance, "Usage: -dressagent <dress|undress> \"<config name>\"");
                return;
            }

            string action = args[1].ToLower();
            string configName = string.Join(" ", args.Skip(2)).Trim('"');

            DressConfig config = CurrentPlayerConfigs.FirstOrDefault(c => c.Name.Equals(configName, StringComparison.OrdinalIgnoreCase));
            if (config == null)
            {
                GameActions.Print(World.Instance, $"Dress config '{configName}' not found");
                return;
            }

            switch (action)
            {
                case "dress":
                    DressFromConfig(config);
                    GameActions.Print(World.Instance, $"Dressing from config: {configName}");
                    break;
                case "undress":
                    UndressFromConfig(config);
                    GameActions.Print(World.Instance, $"Undressing from config: {configName}");
                    break;
                default:
                    GameActions.Print(World.Instance, "Usage: -dressagent <dress|undress> \"<config name>\"");
                    break;
            }
        }

        public void Save()
        {
            if (ProfileManager.ProfilePath == null || !IsLoaded)
                return;

            try
            {
                string json = JsonSerializer.Serialize(CurrentPlayerConfigs, DressAgentJsonContext.Default.ListDressConfig);
                string savePath = Path.Combine(ProfileManager.ProfilePath, _saveFileName);
                File.WriteAllText(savePath, json);
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Error saving dress configs: {ex.Message}");
            }
        }

        public DressConfig CreateNewConfig(string name)
        {
            var config = new DressConfig { Name = name, CharacterName = ProfileManager.CurrentProfile?.CharacterName ?? "", Items = new List<DressItem>() };

            CurrentPlayerConfigs.Add(config);
            Save();
            return config;
        }

        public void DeleteConfig(DressConfig config)
        {
            if (CurrentPlayerConfigs.Remove(config))
            {
                Save();
            }
        }

        public void AddItemToConfig(DressConfig config, uint serial, string name)
        {
            if (config.Items.Any(i => i.Serial == serial))
                return;

            Item item = World.Instance.Items.Get(serial);
            byte layer = item?.ItemData.Layer ?? 0;

            config.Items.Add(new DressItem { Serial = serial, Name = name, Layer = layer });
            Save();
        }

        public void RemoveItemFromConfig(DressConfig config, uint serial)
        {
            DressItem item = config.Items.FirstOrDefault(i => i.Serial == serial);
            if (item != null)
            {
                config.Items.Remove(item);
                Save();
            }
        }

        public void ClearConfig(DressConfig config)
        {
            config.Items.Clear();
            Save();
        }

        public void SetUndressBag(DressConfig config, uint serial)
        {
            config.UndressBagSerial = serial;
            Save();
        }

        public uint GetUndressBag(DressConfig config) => config.UndressBagSerial != 0 ? config.UndressBagSerial : World.Instance.Player?.Backpack?.Serial ?? 0;

        public void AddCurrentlyEquippedItems(DressConfig config)
        {
            if (World.Instance.Player == null)
                return;

            for (int i = 0; i <= Constants.USED_LAYER_COUNT; i++)
            {
                var layer = (Layer)i;
                if (layer == Layer.Backpack || layer == Layer.Face || layer == Layer.Hair || layer == Layer.Beard)
                    continue;

                Item item = World.Instance.Player.FindItemByLayer(layer);
                if (item != null)
                {
                    AddItemToConfig(config, item.Serial, item.Name);
                }
            }
        }

        public void DressFromConfig(DressConfig config)
        {
            if (config == null)
                return;

            Dress(config);
        }

        public void UndressFromConfig(DressConfig config)
        {
            if (config == null)
                return;
            Undress(config);
        }

        private void Dress(DressConfig config)
        {
            if (config.UseKREquipPacket)
            {

                // Then collect items to equip
                var itemsToEquip = new List<uint>();

                foreach (DressItem dressItem in config.Items)
                {
                    // Check if the item is already equipped on the player
                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial)
                    {
                        continue;
                    }

                    itemsToEquip.Add(dressItem.Serial);
                }

                if (itemsToEquip.Count > 0)
                {
                    // Send KR equip packet for all items at once
                    AsyncNetClient.Socket.Send_EquipMacroKR(itemsToEquip.ToArray().AsSpan());
                }
            }
            else
            {
                // Use traditional queue-based approach
                // Collect layers that need to be unequipped
                var layersToUnequip = new List<byte>();
                foreach (DressItem dressItem in config.Items)
                {
                    if (!layersToUnequip.Contains(dressItem.Layer))
                    {
                        if (dressItem.Layer == (byte)Layer.TwoHanded && !layersToUnequip.Contains((byte)Layer.OneHanded))
                            layersToUnequip.Add((byte)Layer.OneHanded);
                        if (dressItem.Layer == (byte)Layer.OneHanded && !layersToUnequip.Contains((byte)Layer.TwoHanded))
                            layersToUnequip.Add((byte)Layer.TwoHanded);
                        layersToUnequip.Add(dressItem.Layer);
                    }
                }

                // First, unequip all conflicting layers
                if (layersToUnequip.Count > 0)
                    UnequipLayers(layersToUnequip, config);

                foreach (DressItem dressItem in config.Items)
                {
                    // Check if the item is already equipped on the player
                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial)
                    {
                        continue;
                    }

                    MoveItemQueue.Instance.EnqueueEquipSingle(dressItem.Serial, (Layer)dressItem.Layer);
                }
            }
        }

        private void Undress(DressConfig config)
        {
            if (config.UseKREquipPacket)
            {
                // Use KR Unequip Packets for faster operation
                var layersToUnequip = new List<Layer>();

                // Collect layers that are currently equipped with config items
                foreach (DressItem dressItem in config.Items)
                {
                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial)
                    {
                        var layer = (Layer)dressItem.Layer;
                        if (!layersToUnequip.Contains(layer))
                        {
                            layersToUnequip.Add(layer);
                        }
                    }
                }

                if (layersToUnequip.Count > 0)
                {
                    // Send KR unequip packet for all layers at once
                    AsyncNetClient.Socket.Send_UnequipMacroKR(layersToUnequip.ToArray().AsSpan());
                }
            }
            else
            {
                // Use traditional queue-based approach
                var itemsToUnequip = new List<uint>();

                // Collect items that are currently equipped
                foreach (DressItem dressItem in config.Items)
                {
                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial)
                    {
                        itemsToUnequip.Add(dressItem.Serial);
                    }
                }

                foreach (uint serial in itemsToUnequip)
                    UnequipItemAsync(serial, config);
            }
        }

        private void UnequipLayers(List<byte> layers, DressConfig config)
        {
            uint undressBag = GetUndressBag(config);

            foreach (byte layer in layers)
            {
                Item currentlyEquipped = World.Instance.Player?.FindItemByLayer((Layer)layer);
                if (currentlyEquipped != null && !config.Contains(currentlyEquipped.Serial))
                    MoveItemQueue.Instance.Enqueue(currentlyEquipped, undressBag);
            }
        }

        private void UnequipItemAsync(uint serial, DressConfig config)
        {
            Item item = World.Instance.Items.Get(serial);

            if (item != null && item.Container == World.Instance.Player?.Serial)
            {
                uint undressBag = GetUndressBag(config);
                MoveItemQueue.Instance.Enqueue(item, undressBag);
            }
        }

        public void CreateDressMacro(string configName)
        {
            var macroManager = MacroManager.TryGetMacroManager(World.Instance);

            if (macroManager == null) return;

            var macro = new Macro($"Dress: {configName}", SDL3.SDL.SDL_Keycode.SDLK_UNKNOWN, false, false, false) { Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, $"dressagent dress \"{configName}\"") };

            macroManager.PushToBack(macro);
            UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
        }

        public void CreateUndressMacro(string configName)
        {
            var macroManager = MacroManager.TryGetMacroManager(World.Instance);

            if (macroManager == null) return;

            var macro = new Macro($"Undress: {configName}", SDL3.SDL.SDL_Keycode.SDLK_UNKNOWN, false, false, false) { Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, $"dressagent undress \"{configName}\"") };

            macroManager.PushToBack(macro);
            UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
        }
    }

    public class DressConfig
    {
        public string Name { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public uint UndressBagSerial { get; set; } = 0;
        public List<DressItem> Items { get; set; } = new();
        public bool UseKREquipPacket { get; set; } = false;

        public bool Contains(uint serial) => Items.Any(i => i.Serial == serial);
    }

    public class DressItem
    {
        public uint Serial { get; set; }
        public string Name { get; set; } = "";
        public byte Layer { get; set; }
    }

    [JsonSerializable(typeof(DressConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(DressItem), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<DressConfig>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<DressItem>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class DressAgentJsonContext : JsonSerializerContext
    {
    }
}
