using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
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
            if (JsonHelper.Load<List<OrganizerConfig>>(Path.Combine(GetDataPath(), "OrganizerConfig.json"), OrganizerAgentContext.Default.ListOrganizerConfig, out var configs))
                Instance.OrganizerConfigs = configs;

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

        public void Save()
        {
            JsonHelper.SaveAndBackup(OrganizerConfigs, Path.Combine(GetDataPath(), "OrganizerConfig.json"), OrganizerAgentContext.Default.ListOrganizerConfig);
        }

        public static void Unload()
        {
            Instance?.Save();

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

        public OrganizerConfig DupeConfig(OrganizerConfig config)
        {
            if (config == null) return null;

            var dupedConfig = new OrganizerConfig
            {
                Name = config.Name + " Copy",
                SourceContSerial = config.SourceContSerial,
                DestContSerial = config.DestContSerial,
                Enabled = config.Enabled,
                ItemConfigs = config.ItemConfigs.Select(c => new OrganizerItemConfig
                {
                    Graphic = c.Graphic,
                    Hue = c.Hue,
                    Amount = c.Amount,
                    Enabled = c.Enabled
                }).ToList()
            };
            OrganizerConfigs.Add(dupedConfig);
            return dupedConfig;
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
                GameActions.Print($"  {i}: '{config.Name}' ({status}, {itemCount} item types, target: {config.DestContSerial:X})");
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

                var destcontainer = World.Items.Get(config.DestContSerial);
                if (destcontainer == null)
                {
                    GameActions.Print($"Cannot find destination container '{config.Name}' (Serial: {config.DestContSerial:X})");
                    continue;
                }

                totalOrganized += OrganizeItems(backpack, destcontainer, config);
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

            var sourceBag = World.Items.Get(config.SourceContSerial) ?? World.Player?.FindItemByLayer(Data.Layer.Backpack);
            if (sourceBag == null)
            {
                GameActions.Print("Cannot find player backpack.");
                return;
            }

            var targetBag = World.Items.Get(config.DestContSerial);
            if (targetBag == null)
            {
                GameActions.Print($"Cannot find target bag for organizer '{config.Name}' (Serial: {config.DestContSerial:X})");
                return;
            }

            int organized = OrganizeItems(sourceBag, targetBag, config);
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

            var sourcecont = World.Items.Get(config.SourceContSerial);
            if (sourcecont == null)
            {
                GameActions.Print("Cannot find source bag.");
                return;
            }

            var destinationcont = World.Items.Get(config.DestContSerial);
            if (destinationcont == null)
            {
                GameActions.Print($"Cannot find destination Container '{config.Name}' (Serial: {config.DestContSerial:X})");
                return;
            }

            int organized = OrganizeItems(sourcecont, destinationcont, config);
            if (organized == 0)
            {
                GameActions.Print($"No items were organized by '{config.Name}'.");
            }
        }

        private int OrganizeItems(Item SourceCont, Item DestCont, OrganizerConfig config)
        {
            var itemsToMove = new List<(Item Item, ushort Amount)>();

            var srcitem = (Item)SourceCont.Items;

            while (srcitem != null)
            {
                // Skip the target bag itself if it's in the same container
                if (srcitem.Serial == DestCont.Serial && SourceCont.Serial == DestCont.Serial)
                {
                    srcitem = (Item)srcitem.Next;
                    continue;
                }

                // Check if this item matches any of the configured graphics/hues
                foreach (var itemConfig in config.ItemConfigs)
                {
                    if (itemConfig.Enabled && itemConfig.IsMatch(srcitem.Graphic, srcitem.Hue))
                    {
                        if (itemConfig.Amount == 0)
                        {
                            itemsToMove.Add((srcitem, ushort.MaxValue)); // Move all
                            break;
                        }

                        ushort amountAtDest = World.Items.Get(DestCont.Serial).FindItem(srcitem.Graphic, srcitem.Hue)?.Amount ?? 0;

                        int missing = itemConfig.Amount - amountAtDest;

                        if (missing > 0)
                        {
                            ushort amountToMove = (ushort)Math.Min(srcitem.Amount, missing);
                            if (amountToMove > 0)
                            {
                                itemsToMove.Add((srcitem, amountToMove));
                            }
                        }
                    }

                }
                srcitem = (Item)srcitem.Next;
            }

            // Move matching items to target bag using MoveItemQueue
            foreach (var itemToMove in itemsToMove)
            {
                MoveItemQueue.Instance?.Enqueue(itemToMove.Item.Serial, DestCont.Serial, itemToMove.Amount, 0xFFFF, 0xFFFF, 0);
            }

            if (itemsToMove.Count > 0)
            {
                GameActions.Print($"Organized {itemsToMove.Count} items from '{config.Name}' to target bag.");
            }

            return itemsToMove.Count;
        }
    }

    [JsonSerializable(typeof(List<OrganizerAgent>))]
    internal partial class OrganizerAgentContext : JsonSerializerContext
    { }

    internal class OrganizerConfig
    {
        public string Name { get; set; } = "Organizer";
        public uint SourceContSerial { get; set; }
        public uint DestContSerial { get; set; }
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
        public ushort Amount { get; set; } = 0; // default max amount
        public bool Enabled { get; set; } = true;

        public bool IsMatch(ushort graphic, ushort hue)
        {
            return graphic == Graphic && (hue == Hue || Hue == ushort.MaxValue);
        }
    }
}
