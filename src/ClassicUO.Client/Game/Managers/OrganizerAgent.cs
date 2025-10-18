using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Timers;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal class OrganizerAgent
    {
        public static OrganizerAgent Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        public List<OrganizerConfig> OrganizerConfigs { get; private set; } = new();

        private static string GetDataPath()
        {
            var dataPath = ProfileManager.ProfilePath;
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            return dataPath;
        }

        public static void Load()
        {
            Instance = new OrganizerAgent();
            var newPath = Path.Combine(GetDataPath(), "OrganizerConfig.json");
            var oldPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data");
            if(File.Exists(oldPath))
                File.Move(oldPath, newPath);

            if (JsonHelper.Load<List<OrganizerConfig>>(newPath, OrganizerAgentContext.Default.ListOrganizerConfig, out var configs))
                Instance.OrganizerConfigs = configs;
        }

        public void OrganizerCommand(string[] args)
        {
            if (args is not { Length: > 1 })
            {
                // Run all organizers
                Instance?.RunOrganizer();
                return;
            }

            if (int.TryParse(args[1], out int index))
            {
                // Run organizer by index
                Instance?.RunOrganizer(index);
            }
            else
            {
                // Run organizer by name - join all args after command
                string name = string.Join(" ", args.Skip(1));
                Instance?.RunOrganizer(name);
            }
        }

        public void Save()
        {
            JsonHelper.SaveAndBackup(OrganizerConfigs, Path.Combine(GetDataPath(), "OrganizerConfig.json"), OrganizerAgentContext.Default.ListOrganizerConfig);
        }

        public static void Unload()
        {
            Instance?.Save();
            Instance = null;
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
                Name = GetUniqueName(config.Name + " Copy"),
                SourceContSerial = config.SourceContSerial,
                DestContSerial = config.DestContSerial,
                Enabled = false,
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

        private string GetUniqueName(string baseName)
        {
            string name = baseName;
            int i = 2;
            while (OrganizerConfigs.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = $"{baseName} ({i++})";
            return name;
        }

        public void CreateOrganizerMacroButton(string name)
        {
            var macroManager = MacroManager.TryGetMacroManager(World.Instance);

            if (macroManager == null) return;

            var config = OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (config == null) return;
            int index = OrganizerConfigs.IndexOf(config);
            var macro = new Macro($"Organizer: {config.Name}", SDL3.SDL.SDL_Keycode.SDLK_UNKNOWN, false, false, false) { Items = new MacroObjectString(MacroType.ClientCommand, MacroSubType.MSC_NONE, $"organize {index}") };

            macroManager.PushToBack(macro);
            UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
        }
        public void ListOrganizers()
        {
            if (OrganizerConfigs.Count == 0)
            {
                GameActions.Print(World.Instance, "No organizers configured.");
                return;
            }

            GameActions.Print(World.Instance, $"Available organizers ({OrganizerConfigs.Count}):");
            for (int i = 0; i < OrganizerConfigs.Count; i++)
            {
                var config = OrganizerConfigs[i];
                var status = config.Enabled ? "enabled" : "disabled";
                var itemCount = config.ItemConfigs.Count(ic => ic.Enabled);
                GameActions.Print(World.Instance, $"  {i}: '{config.Name}' ({status}, {itemCount} item types, destination: {config.DestContSerial:X})");
            }
        }

        public void RunOrganizer()
        {
            var backpack = World.Instance.Player?.Backpack;
            if (backpack == null)
            {
                GameActions.Print(World.Instance, "Cannot find player backpack.");
                return;
            }

            int totalOrganized = 0;
            foreach (var config in OrganizerConfigs)
            {
                if (!config.Enabled) continue;

                var sourceCont = config.SourceContSerial != 0
                    ? World.Instance.Items.Get(config.SourceContSerial)
                    : backpack;

                if (sourceCont == null)
                {
                    GameActions.Print(World.Instance, $"Cannot find source container for organizer '{config.Name}'.");
                    continue;
                }

                var destCont = config.DestContSerial != 0
                    ? World.Instance.Items.Get(config.DestContSerial)
                    : backpack;

                if (destCont == null)
                {
                    GameActions.Print($"Cannot find destination container for organizer '{config.Name}'. Using backpack as default.");
                    destCont = backpack;
                }

                totalOrganized += OrganizeItems(sourceCont, destCont, config);
            }

            if (totalOrganized == 0)
            {
                GameActions.Print(World.Instance, "No items were organized.", 33);
            }
        }

        public void RunOrganizer(string name, uint source = 0, uint dest = 0)
        {
            var config = OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (config == null)
            {
                GameActions.Print(World.Instance, $"Organizer '{name}' not found.", 33);
                return;
            }

            RunSingleOrganizer(config, source, dest);
        }

        public void RunOrganizer(int index)
        {
            if (index < 0 || index >= OrganizerConfigs.Count)
            {
                GameActions.Print(World.Instance, $"Organizer index {index} is out of range. Available organizers: 0-{OrganizerConfigs.Count - 1}", 33);
                return;
            }

            var config = OrganizerConfigs[index];
            RunSingleOrganizer(config);
        }

        private int OrganizeItems(Item sourceCont, Item destCont, OrganizerConfig config)
        {
            var itemsToMove = new List<(Item Item, ushort Amount)>();

            var sourceItems = (Item)sourceCont.Items;

            var destItems = (Item)destCont.Items;

            if (sourceCont.Serial == destCont.Serial)
            {
                for (var item = sourceItems; item != null; item = (Item)item.Next)
                {
                    foreach (var itemConfig in config.ItemConfigs)
                    {
                        if (itemConfig.Enabled && itemConfig.IsMatch(item.Graphic, item.Hue))
                        {
                            if (!item.ItemData.IsStackable) break; // non-stackable items can't be organized in the same container
                            if (itemConfig.Amount > 0)
                                itemsToMove.Add((item, itemConfig.Amount));
                            else
                            {
                                itemsToMove.Add((item, ushort.MaxValue));
                            }
                            break; // Avoid processing the same item multiple times
                        }
                    }
                }
            }
            else
            {
                // Build a lookup of existing item counts in the destination container
                var destItemCounts = new Dictionary<(ushort Graphic, ushort Hue), int>();
                for (var item = destItems; item != null; item = (Item)item.Next)
                {
                    var key = (item.Graphic, item.Hue);
                    if (destItemCounts.ContainsKey(key))
                        destItemCounts[key] += item.Amount;
                    else
                        destItemCounts[key] = item.Amount;
                }

                // Determine which items to move based on config and existing counts in destination
                for (var item = sourceItems; item != null; item = (Item)item.Next)
                {
                    foreach (var itemConfig in config.ItemConfigs)
                    {
                        if (itemConfig.Enabled && itemConfig.IsMatch(item.Graphic, item.Hue))
                        {
                            if (itemConfig.Amount == 0)
                            {
                                // Move all items of this type
                                itemsToMove.Add((item, ushort.MaxValue));
                            }
                            else
                            {
                                // Move up to the configured amount, considering existing items in destination
                                destItemCounts.TryGetValue((item.Graphic, item.Hue), out int existingCount);
                                int toMove = itemConfig.Amount - existingCount;
                                if (toMove > 0)
                                {
                                    itemsToMove.Add((item, (ushort)Math.Min(toMove, item.Amount)));
                                    // Update the count to avoid over-moving if multiple stacks exist in source
                                    destItemCounts[(item.Graphic, item.Hue)] = (ushort)(existingCount + Math.Min(toMove, item.Amount));
                                }
                            }
                        }
                    }
                }
            }


            // Move matching items to target bag using MoveItemQueue
            foreach (var itemToMove in itemsToMove)
            {
                MoveItemQueue.Instance?.Enqueue(itemToMove.Item.Serial, destCont.Serial, itemToMove.Amount, 0xFFFF, 0xFFFF, 0);
            }

            if (itemsToMove.Count > 0)
            {
                GameActions.Print($"Organizing {itemsToMove.Count} items from '{config.Name}' to destination container...", 63);
            }

            return itemsToMove.Count;
        }

        private void RunSingleOrganizer(OrganizerConfig config, uint source = 0, uint dest = 0)
        {
            if (!config.Enabled)
            {
                GameActions.Print(World.Instance, $"Organizer '{config.Name}' is disabled.", 33);
                return;
            }

            var backpack = World.Instance.Player?.Backpack;
            if (backpack == null)
            {
                GameActions.Print(World.Instance, "Cannot find player backpack.");
                return;
            }

            var sourceCont = source != 0
                ? World.Instance.Items.Get(source)
                : config.SourceContSerial != 0
                    ? World.Instance.Items.Get(config.SourceContSerial)
                    : backpack;

            if (sourceCont == null)
            {
                GameActions.Print($"Cannot find source container for organizer '{config.Name}'.");
                return;
            }

            var destCont = dest != 0 ? World.Instance.Items.Get(dest) :
                config.DestContSerial != 0
                    ? World.Instance.Items.Get(config.DestContSerial)
                    : backpack;

            if (destCont == null)
            {
                GameActions.Print(World.Instance, $"Cannot find destination container for organizer '{config.Name}' (Serial: {config.DestContSerial:X})", 33);
                return;
            }

            int organized = OrganizeItems(sourceCont, destCont, config);
            if (organized == 0)
            {
                GameActions.Print(World.Instance, $"No items were organized by '{config.Name}'.", 33);
            }
        }

    }

    [JsonSerializable(typeof(List<OrganizerConfig>))]
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
        public ushort Amount { get; set; } = 0; // 0 = move all; otherwise move up to this amount
        public bool Enabled { get; set; } = true;

        public bool IsMatch(ushort graphic, ushort hue)
        {
            return graphic == Graphic && (hue == Hue || Hue == ushort.MaxValue);
        }
    }
}
