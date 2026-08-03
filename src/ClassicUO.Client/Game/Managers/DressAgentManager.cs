using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Common.Enums;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;

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

        private DressConfigSave _save = new();

        public List<DressConfig> CurrentPlayerConfigs => _save.Configs;
        public List<DressConfig> OtherCharacterConfigs { get; private set; } = new();
        public bool IsLoaded { get; private set; }

        private const string SaveFileName = DressConfigSave.DressConfigsFileName;

        private Layer[] _forbiddenLayers = [Layer.Backpack, Layer.Beard, Layer.Hair, Layer.ShopBuy, Layer.ShopBuyRestock, Layer.ShopSell, Layer.Bank];

        private DressAgentManager() { }

        public void Load()
        {
            if (ProfileManager.ProfilePath == null)
                return;

            string characterName = ProfileManager.CurrentProfile?.CharacterName ?? "";

            _save = DressConfigSave.LoadForCurrentProfile();

            // Ensure all configs have the correct character name and clean up null items
            foreach (DressConfig config in _save.Configs)
            {
                config.CharacterName = characterName;
                CleanupNullItems(config);
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
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            else
                rootpath = Settings.GlobalSettings.ProfilesPath;

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

                            string configFilePath = Path.Combine(characterPath, SaveFileName);
                            if (File.Exists(configFilePath))
                                try
                                {
                                    List<DressConfig> configs = DressConfigSave.ReadConfigsFromFile(configFilePath);

                                    foreach (DressConfig config in configs)
                                    {
                                        config.CharacterName = characterName; // Ensure character name is set
                                        CleanupNullItems(config);
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

        private void CleanupNullItems(DressConfig config)
        {
            if (config?.Items == null)
                return;

            int originalCount = config.Items.Count;
            config.Items.RemoveAll(item => item == null);
            int removedCount = originalCount - config.Items.Count;

            if (removedCount > 0)
            {
                Utility.Logging.Log.Warn($"Removed {removedCount} null item(s) from dress config '{config.Name}'");
            }
        }

        public void Save()
        {
            if (ProfileManager.ProfilePath == null || !IsLoaded)
                return;

            // Clean up null items before saving
            foreach (DressConfig config in _save.Configs)
            {
                CleanupNullItems(config);
            }

            _save.Save();
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
            if (CurrentPlayerConfigs.Remove(config)) Save();
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
                if (item != null) AddItemToConfig(config, item.Serial, item.Name);
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
            if (World.Instance == null || World.Instance.Player == null)
                return;

            if (config.UseKREquipPacket)
            {
                // Then collect items to equip
                var itemsToEquip = new List<uint>();

                foreach (DressItem dressItem in config.Items)
                {
                    if(dressItem == null) continue;

                    // Check if the item is already equipped on the player
                    Item item = World.Instance.Items.Get(dressItem.Serial);

                    if(item == null) continue;

                    if (item.Container == World.Instance.Player?.Serial  || _forbiddenLayers.Contains((Layer)item.ItemData.Layer)) continue;

                    itemsToEquip.Add(dressItem.Serial);
                }

                if (itemsToEquip.Count > 0)
                    // Send KR equip packet for all items at once
                    AsyncNetClient.Socket.Send_EquipMacroKR(itemsToEquip.ToArray().AsSpan());
            }
            else
            {
                // Use traditional queue-based approach
                // Collect layers that need to be unequipped
                var layersToUnequip = new List<byte>();
                foreach (DressItem dressItem in config.Items)
                {
                    if (dressItem == null) continue;
                    if(_forbiddenLayers.Contains((Layer)dressItem.Layer)) continue;

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
                    if (dressItem == null) continue;

                    // Check if the item is already equipped on the player
                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial) continue;

                    ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.EquipItem(dressItem.Serial, (Layer)dressItem.Layer), ActionPriority.EquipItem);
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
                    if (dressItem == null) continue;
                    if(_forbiddenLayers.Contains((Layer)dressItem.Layer)) continue;

                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial)
                    {
                        var layer = (Layer)dressItem.Layer;
                        if (!layersToUnequip.Contains(layer)) layersToUnequip.Add(layer);
                    }
                }

                if (layersToUnequip.Count > 0)
                    // Send KR unequip packet for all layers at once
                    AsyncNetClient.Socket.Send_UnequipMacroKR(layersToUnequip.ToArray().AsSpan());
            }
            else
            {
                // Use traditional queue-based approach
                var itemsToUnequip = new List<uint>();

                // Collect items that are currently equipped
                foreach (DressItem dressItem in config.Items)
                {
                    if (dressItem == null) continue;
                    if(_forbiddenLayers.Contains((Layer)dressItem.Layer)) continue;

                    Item item = World.Instance.Items.Get(dressItem.Serial);
                    if (item != null && item.Container == World.Instance.Player?.Serial) itemsToUnequip.Add(dressItem.Serial);
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
                if(_forbiddenLayers.Contains((Layer)layer)) continue;

                Item currentlyEquipped = World.Instance.Player?.FindItemByLayer((Layer)layer);
                if (currentlyEquipped != null && !config.Contains(currentlyEquipped.Serial))
                    ObjectActionQueue.Instance.Enqueue(new MoveRequest(currentlyEquipped, undressBag).ToObjectActionQueueItem(), ActionPriority.UnequipItem);
            }
        }

        private void UnequipItemAsync(uint serial, DressConfig config)
        {
            Item item = World.Instance.Items.Get(serial);

            if (item != null && item.Container == World.Instance.Player?.Serial)
            {
                uint undressBag = GetUndressBag(config);
                ObjectActionQueue.Instance.Enqueue(new MoveRequest(item, undressBag).ToObjectActionQueueItem(), ActionPriority.UnequipItem);
            }
        }

        public void UndressAll(bool kr)
        {
            uint undressBag = World.Instance.Player.Backpack;
            List<Layer> toUnequip = new();

            foreach (byte layer in Enum.GetValues<Layer>())
            {
                if(_forbiddenLayers.Contains((Layer)layer)) continue;

                Item currentlyEquipped = World.Instance.Player?.FindItemByLayer((Layer)layer);
                if (currentlyEquipped == null) continue;

                if(!kr)
                    ObjectActionQueue.Instance.Enqueue(new MoveRequest(currentlyEquipped, undressBag).ToObjectActionQueueItem(), ActionPriority.UnequipItem);
                else
                    toUnequip.Add((Layer)layer);
            }

            if (kr) AsyncNetClient.Socket.Send_UnequipMacroKR(toUnequip.ToArray().AsSpan());
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

        public bool Contains(uint serial) => Items?.Any(i => i != null && i.Serial == serial) ?? false;
    }

    public class DressItem
    {
        public uint Serial { get; set; }
        public string Name { get; set; } = "";
        public byte Layer { get; set; }
    }

    /// <summary>
    /// JSON-backed store for a character's dress configs. Persisted to <c>dress_configs.json</c> in the
    /// current profile folder. Saving/loading (with rotating backups) is handled by <see cref="JsonSave{T}"/>.
    /// </summary>
    public sealed class DressConfigSave : JsonSave<DressConfigSave>
    {
        public const string DressConfigsFileName = "dress_configs.json";

        public List<DressConfig> Configs { get; set; } = new();

        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => DressConfigsFileName;

        protected override JsonTypeInfo<DressConfigSave> TypeInfo => DressAgentJsonContext.Default.DressConfigSave;

        /// <summary>Migrates any legacy bare-array file to the wrapped format, then loads the current profile's save.</summary>
        public static DressConfigSave LoadForCurrentProfile()
        {
            MigrateLegacyFormatIfNeeded();
            return Load();
        }

        /// <summary>
        /// Reads dress configs from a file, accepting both the legacy bare-array format
        /// (<c>List&lt;DressConfig&gt;</c>) and the current wrapped <see cref="DressConfigSave"/> format.
        /// </summary>
        public static List<DressConfig> ReadConfigsFromFile(string path)
        {
            string json = File.ReadAllText(path);

            if (json.TrimStart().StartsWith('['))
                return JsonSerializer.Deserialize(json, DressAgentJsonContext.Default.ListDressConfig) ?? new List<DressConfig>();

            return JsonSerializer.Deserialize(json, DressAgentJsonContext.Default.DressConfigSave)?.Configs ?? new List<DressConfig>();
        }

        // Older versions stored a bare JSON array; rewrite it once into the wrapped format JsonSave expects.
        private static void MigrateLegacyFormatIfNeeded()
        {
            string path = Path.Combine(JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Char), DressConfigsFileName);

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);

                if (!json.TrimStart().StartsWith('['))
                    return;

                List<DressConfig> legacy = JsonSerializer.Deserialize(json, DressAgentJsonContext.Default.ListDressConfig) ?? new List<DressConfig>();
                new DressConfigSave { Configs = legacy }.Save();
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Error migrating legacy dress configs: {ex.Message}");
            }
        }
    }

    [JsonSerializable(typeof(DressConfig), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(DressItem), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<DressConfig>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<DressItem>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(DressConfigSave), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class DressAgentJsonContext : JsonSerializerContext
    {
    }
}
