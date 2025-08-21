using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class OrganizerAgent
    {
        public static OrganizerAgent Instance { get; private set; }

        public List<OrganizerConfig> OrganizerConfigs { get; private set; } = new List<OrganizerConfig>();

        private static string GetDataPath()
        {
            var dataPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            return dataPath;
        }

        public static void Load()
        {
            Instance = new OrganizerAgent();

            try
            {
                string savePath = Path.Combine(GetDataPath(), "OrganizerConfig.json");
                if (File.Exists(savePath))
                {
                    var json = File.ReadAllText(savePath);
                    Instance.OrganizerConfigs = JsonSerializer.Deserialize<List<OrganizerConfig>>(json) ?? new List<OrganizerConfig>();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading OrganizerAgent config: {ex}");
                Instance.OrganizerConfigs = new List<OrganizerConfig>();
            }

            // Register organizer commands if they don't already exist
            RegisterCommands();
        }

        private static void RegisterCommands()
        {
            // Check if commands already exist before registering
            if (!CommandManager.Commands.ContainsKey("organize"))
            {
                CommandManager.Register("organize", (s) => 
                {
                    if (s.Length == 0)
                    {
                        // Run all organizers
                        Instance?.RunOrganizer();
                    }
                    else if (int.TryParse(s[0], out int index))
                    {
                        // Run organizer by index
                        Instance?.RunOrganizer(index);
                    }
                    else
                    {
                        // Run organizer by name
                        Instance?.RunOrganizer(s[0]);
                    }
                });
            }

            if (!CommandManager.Commands.ContainsKey("organizer"))
            {
                CommandManager.Register("organizer", (s) => 
                {
                    if (s.Length == 0)
                    {
                        // Run all organizers
                        Instance?.RunOrganizer();
                    }
                    else if (int.TryParse(s[0], out int index))
                    {
                        // Run organizer by index
                        Instance?.RunOrganizer(index);
                    }
                    else
                    {
                        // Run organizer by name
                        Instance?.RunOrganizer(s[0]);
                    }
                });
            }

            if (!CommandManager.Commands.ContainsKey("organizerlist"))
            {
                CommandManager.Register("organizerlist", (s) => 
                {
                    Instance?.ListOrganizers();
                });
            }
        }

        public static void Unload()
        {
            if (Instance != null)
            {
                try
                {
                    string savePath = Path.Combine(GetDataPath(), "OrganizerConfig.json");
                    var json = JsonSerializer.Serialize(Instance.OrganizerConfigs, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(savePath, json);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error saving OrganizerAgent config: {ex}");
                }
            }

            // Unregister commands
            UnregisterCommands();

            Instance = null;
        }

        private static void UnregisterCommands()
        {
            CommandManager.UnRegister("organize");
            CommandManager.UnRegister("organizer");
            CommandManager.UnRegister("organizerlist");
        }

        public OrganizerConfig NewOrganizerConfig()
        {
            var config = new OrganizerConfig();
            OrganizerConfigs.Add(config);
            return config;
        }

        public void DeleteConfig(OrganizerConfig config)
        {
            if (config != null)
            {
                OrganizerConfigs?.Remove(config);
            }
        }

        public void ListOrganizers()
        {
            if (OrganizerConfigs.Count == 0)
            {
                GameActions.Print("No organizers configured.");
                return;
            }

            GameActions.Print($"Available organizers ({OrganizerConfigs.Count}):");
            for (int i = 0; i < OrganizerConfigs.Count; i++)
            {
                var config = OrganizerConfigs[i];
                var status = config.Enabled ? "enabled" : "disabled";
                var itemCount = config.ItemConfigs.Count(ic => ic.Enabled);
                GameActions.Print($"  {i}: '{config.Name}' ({status}, {itemCount} item types, target: {config.TargetBagSerial:X})");
            }
        }

        public void RunOrganizer()
        {
            var backpack = World.Player?.FindItemByLayer(Data.Layer.Backpack);
            if (backpack == null)
            {
                GameActions.Print("Cannot find player backpack.");
                return;
            }

            int totalOrganized = 0;
            foreach (var config in OrganizerConfigs)
            {
                if (!config.Enabled) continue;

                var targetBag = World.Items.Get(config.TargetBagSerial);
                if (targetBag == null)
                {
                    GameActions.Print($"Cannot find target bag for organizer '{config.Name}' (Serial: {config.TargetBagSerial:X})");
                    continue;
                }

                totalOrganized += OrganizeItems(backpack, targetBag, config);
            }

            if (totalOrganized == 0)
            {
                GameActions.Print("No items were organized.");
            }
        }

        public void RunOrganizer(string name)
        {
            var config = OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (config == null)
            {
                GameActions.Print($"Organizer '{name}' not found.");
                return;
            }

            if (!config.Enabled)
            {
                GameActions.Print($"Organizer '{name}' is disabled.");
                return;
            }

            var backpack = World.Player?.FindItemByLayer(Data.Layer.Backpack);
            if (backpack == null)
            {
                GameActions.Print("Cannot find player backpack.");
                return;
            }

            var targetBag = World.Items.Get(config.TargetBagSerial);
            if (targetBag == null)
            {
                GameActions.Print($"Cannot find target bag for organizer '{config.Name}' (Serial: {config.TargetBagSerial:X})");
                return;
            }

            int organized = OrganizeItems(backpack, targetBag, config);
            if (organized == 0)
            {
                GameActions.Print($"No items were organized by '{config.Name}'.");
            }
        }

        public void RunOrganizer(int index)
        {
            if (index < 0 || index >= OrganizerConfigs.Count)
            {
                GameActions.Print($"Organizer index {index} is out of range. Available organizers: 0-{OrganizerConfigs.Count - 1}");
                return;
            }

            var config = OrganizerConfigs[index];
            if (!config.Enabled)
            {
                GameActions.Print($"Organizer {index} ('{config.Name}') is disabled.");
                return;
            }

            var backpack = World.Player?.FindItemByLayer(Data.Layer.Backpack);
            if (backpack == null)
            {
                GameActions.Print("Cannot find player backpack.");
                return;
            }

            var targetBag = World.Items.Get(config.TargetBagSerial);
            if (targetBag == null)
            {
                GameActions.Print($"Cannot find target bag for organizer '{config.Name}' (Serial: {config.TargetBagSerial:X})");
                return;
            }

            int organized = OrganizeItems(backpack, targetBag, config);
            if (organized == 0)
            {
                GameActions.Print($"No items were organized by '{config.Name}'.");
            }
        }

        private int OrganizeItems(Item backpack, Item targetBag, OrganizerConfig config)
        {
            var itemsToMove = new List<Item>();

            var item = (Item)backpack.Items;
            while (item != null)
            {
                // Skip the target bag itself if it's in the backpack
                if (item.Serial == targetBag.Serial)
                {
                    item = (Item)item.Next;
                    continue;
                }

                // Check if this item matches any of the configured graphics/hues
                foreach (var itemConfig in config.ItemConfigs)
                {
                    if (itemConfig.Enabled && itemConfig.IsMatch(item.Graphic, item.Hue))
                    {
                        itemsToMove.Add(item);
                        break;
                    }
                }

                item = (Item)item.Next;
            }

            // Move matching items to target bag using MoveItemQueue
            foreach (var itemToMove in itemsToMove)
            {
                MoveItemQueue.Instance?.Enqueue(itemToMove.Serial, targetBag.Serial, 0, 0xFFFF, 0xFFFF, 0);
            }

            if (itemsToMove.Count > 0)
            {
                GameActions.Print($"Organized {itemsToMove.Count} items from '{config.Name}' to target bag.");
            }

            return itemsToMove.Count;
        }
    }

    internal class OrganizerConfig
    {
        public string Name { get; set; } = "Organizer";
        public uint TargetBagSerial { get; set; }
        public bool Enabled { get; set; } = true;
        public List<OrganizerItemConfig> ItemConfigs { get; set; } = new List<OrganizerItemConfig>();

        public OrganizerItemConfig NewItemConfig()
        {
            var config = new OrganizerItemConfig();
            ItemConfigs.Add(config);
            return config;
        }

        public void DeleteItemConfig(OrganizerItemConfig config)
        {
            if (config != null)
            {
                ItemConfigs?.Remove(config);
            }
        }
    }

    internal class OrganizerItemConfig
    {
        public ushort Graphic { get; set; }
        public ushort Hue { get; set; } = ushort.MaxValue;
        public bool Enabled { get; set; } = true;

        public bool IsMatch(ushort graphic, ushort hue)
        {
            return graphic == Graphic && (hue == Hue || Hue == ushort.MaxValue);
        }
    }
}