using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Timers;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.Structs;
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

        private OrganizerConfigSave _save = new();

        public List<OrganizerConfig> OrganizerConfigs => _save.Configs;

        public static void Load()
        {
            Instance = new OrganizerAgent();
            Instance._save = OrganizerConfigSave.LoadForCurrentProfile();
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

        public void Save() => _save.Save();

        public OrganizerConfig FindConfig(string nameOrIndex)
        {
            if (string.IsNullOrWhiteSpace(nameOrIndex))
                return null;

            nameOrIndex = nameOrIndex.Trim();

            if (int.TryParse(nameOrIndex, out int index))
            {
                if (index >= 0 && index < OrganizerConfigs.Count)
                    return OrganizerConfigs[index];

                return null;
            }

            return OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(nameOrIndex, StringComparison.OrdinalIgnoreCase));
        }

        public void SetSourceContainerViaTarget(string nameOrIndex)
        {
            OrganizerConfig config = FindConfig(nameOrIndex);
            if (config == null)
            {
                GameActions.Print(World.Instance, $"Organizer '{nameOrIndex}' not found.", Constants.HUE_ERROR);
                return;
            }

            GameActions.Print(World.Instance, $"Target the source container for organizer '{config.Name}'.");
            World.Instance.TargetManager.SetTargeting((o) =>
            {
                if (o is Item item && item.ItemData.IsContainer)
                {
                    config.SourceContSerial = item.Serial;
                    Save();
                    GameActions.Print(World.Instance, $"Source container for organizer '{config.Name}' set.", Constants.HUE_SUCCESS);
                }
                else
                {
                    GameActions.Print(World.Instance, "That doesn't appear to be a valid container.", Constants.HUE_ERROR);
                }
            });
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
                    Enabled = c.Enabled,
                    DestContSerial = c.DestContSerial
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

            OrganizerConfig config = OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
                OrganizerConfig config = OrganizerConfigs[i];
                string status = config.Enabled ? "enabled" : "disabled";
                int itemCount = config.ItemConfigs.Count(ic => ic.Enabled);
                GameActions.Print(World.Instance, $"  {i}: '{config.Name}' ({status}, {itemCount} item types, destination: {config.DestContSerial:X})");
            }
        }

        public void RunOrganizer()
        {
            Item backpack = World.Instance.Player?.Backpack;
            if (backpack == null)
            {
                GameActions.Print(World.Instance, "Cannot find player backpack.");
                return;
            }

            int totalOrganized = 0;
            foreach (OrganizerConfig config in OrganizerConfigs)
            {
                if (!config.Enabled) continue;

                Item sourceCont = config.SourceContSerial != 0
                    ? World.Instance.Items.Get(config.SourceContSerial)
                    : backpack;

                if (sourceCont == null)
                {
                    GameActions.Print(World.Instance, $"Cannot find source container for organizer '{config.Name}'.");
                    continue;
                }

                Item destCont = config.DestContSerial != 0
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
            OrganizerConfig config = OrganizerConfigs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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

            OrganizerConfig config = OrganizerConfigs[index];
            RunSingleOrganizer(config);
        }

        private int OrganizeItems(Item sourceCont, Item destCont, OrganizerConfig config)
        {
            Item backpack = World.Instance.Player?.Backpack;

            // Group items by destination (either per-item destination or config destination)
            var itemsToMoveByDestination = new Dictionary<uint, List<(Item Item, ushort Amount, OrganizerItemConfig Config)>>();

            var sourceItems = (Item)sourceCont.Items;

            // First pass: identify items to move and group by destination
            for (Item item = sourceItems; item != null; item = (Item)item.Next)
            {
                foreach (OrganizerItemConfig itemConfig in config.ItemConfigs)
                {
                    if (itemConfig.Enabled && itemConfig.IsMatch(item.Graphic, item.Hue))
                    {
                        // Determine the destination for this item
                        uint itemDestSerial = itemConfig.DestContSerial != 0 ? itemConfig.DestContSerial : destCont.Serial;

                        if (!itemsToMoveByDestination.ContainsKey(itemDestSerial))
                            itemsToMoveByDestination[itemDestSerial] = new List<(Item, ushort, OrganizerItemConfig)>();

                        itemsToMoveByDestination[itemDestSerial].Add((item, 0, itemConfig)); // Amount will be calculated in second pass
                        break; // Avoid processing the same item multiple times
                    }
                }
            }

            int totalItemsMoved = 0;

            // Second pass: process each destination group
            foreach (KeyValuePair<uint, List<(Item Item, ushort Amount, OrganizerItemConfig Config)>> kvp in itemsToMoveByDestination)
            {
                uint destinationSerial = kvp.Key;
                List<(Item Item, ushort Amount, OrganizerItemConfig Config)> itemsForThisDest = kvp.Value;

                Item thisDestCont = World.Instance.Items.Get(destinationSerial);
                if (thisDestCont == null)
                {
                    GameActions.Print($"Cannot find destination container {destinationSerial:X}. Using backpack as default.");
                    thisDestCont = backpack;
                    if (thisDestCont == null) continue;
                }

                var destItems = (Item)thisDestCont.Items;
                bool sameContainer = sourceCont.Serial == thisDestCont.Serial;

                if (sameContainer)
                {
                    // Same container logic
                    foreach ((Item item, ushort _, OrganizerItemConfig itemConfig) in itemsForThisDest)
                    {
                        if (!item.ItemData.IsStackable) continue; // non-stackable items can't be organized in the same container

                        ushort amountToMove = itemConfig.Amount > 0 ? itemConfig.Amount : ushort.MaxValue;
                        ObjectActionQueue.Instance.Enqueue(new MoveRequest(item.Serial, thisDestCont.Serial, amountToMove).ToObjectActionQueueItem(), ActionPriority.MoveItem);
                        totalItemsMoved++;
                    }
                }
                else
                {
                    // Build a lookup of existing item counts in the destination container
                    var destItemCounts = new Dictionary<(ushort Graphic, ushort Hue), int>();
                    for (Item item = destItems; item != null; item = (Item)item.Next)
                    {
                        (ushort Graphic, ushort Hue) key = (item.Graphic, item.Hue);
                        if (destItemCounts.ContainsKey(key))
                            destItemCounts[key] += item.Amount;
                        else
                            destItemCounts[key] = item.Amount;
                    }

                    // Determine which items to move based on config and existing counts in destination
                    foreach ((Item item, ushort _, OrganizerItemConfig itemConfig) in itemsForThisDest)
                    {
                        if (itemConfig.Amount == 0)
                        {
                            // Move all items of this type
                            ObjectActionQueue.Instance.Enqueue(new MoveRequest(item.Serial, thisDestCont.Serial, ushort.MaxValue).ToObjectActionQueueItem(), ActionPriority.MoveItem);
                            totalItemsMoved++;
                        }
                        else
                        {
                            // Move up to the configured amount, considering existing items in destination
                            destItemCounts.TryGetValue((item.Graphic, item.Hue), out int existingCount);
                            int toMove = itemConfig.Amount - existingCount;
                            if (toMove > 0)
                            {
                                ushort actualAmount = (ushort)Math.Min(toMove, item.Amount);
                                ObjectActionQueue.Instance.Enqueue(new MoveRequest(item.Serial, thisDestCont.Serial, actualAmount).ToObjectActionQueueItem(), ActionPriority.MoveItem);
                                // Update the count to avoid over-moving if multiple stacks exist in source
                                destItemCounts[(item.Graphic, item.Hue)] = existingCount + actualAmount;
                                totalItemsMoved++;
                            }
                        }
                    }
                }
            }

            if (totalItemsMoved > 0)
            {
                GameActions.Print($"Organizing {totalItemsMoved} items from '{config.Name}'...", Constants.HUE_SUCCESS);
            }

            return totalItemsMoved;
        }

        private void RunSingleOrganizer(OrganizerConfig config, uint source = 0, uint dest = 0)
        {
            if (!config.Enabled)
            {
                GameActions.Print(World.Instance, $"Organizer '{config.Name}' is disabled.", Constants.HUE_ERROR);
                return;
            }

            Item backpack = World.Instance.Player?.Backpack;
            if (backpack == null)
            {
                GameActions.Print(World.Instance, "Cannot find player backpack.");
                return;
            }

            Item sourceCont = source != 0
                ? World.Instance.Items.Get(source)
                : config.SourceContSerial != 0
                    ? World.Instance.Items.Get(config.SourceContSerial)
                    : backpack;

            if (sourceCont == null)
            {
                GameActions.Print($"Cannot find source container for organizer '{config.Name}'.");
                return;
            }

            Item destCont = dest != 0 ? World.Instance.Items.Get(dest) :
                config.DestContSerial != 0
                    ? World.Instance.Items.Get(config.DestContSerial)
                    : backpack;

            if (destCont == null)
            {
                GameActions.Print(World.Instance, $"Cannot find destination container for organizer '{config.Name}' (Serial: {config.DestContSerial:X})", Constants.HUE_ERROR);
                return;
            }

            int organized = OrganizeItems(sourceCont, destCont, config);
            if (organized == 0)
            {
                GameActions.Print(World.Instance, $"No items were organized by '{config.Name}'.", Constants.HUE_ERROR);
            }
        }

        #nullable enable
        public string? GetJsonExport(OrganizerConfig config)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(config, OrganizerAgentContext.Default.OrganizerConfig);
            }
            catch (Exception e)
            {
                Utility.Logging.Log.Error($"Error exporting organizer to JSON: {e}");
            }

            return null;
        }
        #nullable disable

        public bool ImportFromJson(string json)
        {
            try
            {
                OrganizerConfig importedConfig = System.Text.Json.JsonSerializer.Deserialize(json, OrganizerAgentContext.Default.OrganizerConfig);

                if (importedConfig != null)
                {
                    importedConfig.Name = GetUniqueName(importedConfig.Name);
                    importedConfig.Enabled = false;
                    OrganizerConfigs.Add(importedConfig);
                    GameActions.Print($"Imported organizer '{importedConfig.Name}' with {importedConfig.ItemConfigs.Count} items!", Constants.HUE_SUCCESS);
                    return true;
                }
            }
            catch (Exception e)
            {
                Utility.Logging.Log.Error($"Error importing organizer from JSON: {e}");
            }

            return false;
        }

    }

    [JsonSerializable(typeof(List<OrganizerConfig>))]
    [JsonSerializable(typeof(OrganizerConfig))]
    [JsonSerializable(typeof(OrganizerConfigSave))]
    internal partial class OrganizerAgentContext : JsonSerializerContext
    { }

    /// <summary>
    /// JSON-backed store for a character's organizer configs. Persisted to <c>OrganizerConfig.json</c> in the
    /// current profile folder. Saving/loading (with rotating backups) is handled by <see cref="JsonSave{T}"/>.
    /// </summary>
    internal sealed class OrganizerConfigSave : JsonSave<OrganizerConfigSave>, INotifyPropertyChanged
    {
        public const string OrganizerConfigFileName = "OrganizerConfig.json";

        public List<OrganizerConfig> Configs { get; set; } = new();

        protected override SettingsScope Scope => SettingsScope.Char;

        protected override string FileName => OrganizerConfigFileName;

        protected override JsonTypeInfo<OrganizerConfigSave> TypeInfo => OrganizerAgentContext.Default.OrganizerConfigSave;

        /// <summary>Migrates any legacy bare-array file to the wrapped format, then loads the current profile's save.</summary>
        public static OrganizerConfigSave LoadForCurrentProfile()
        {
            MigrateLegacyFormatIfNeeded();
            return Load();
        }

        // Older versions stored a bare JSON array; rewrite it once into the wrapped format JsonSave expects.
        private static void MigrateLegacyFormatIfNeeded()
        {
            string path = Path.Combine(JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Char), OrganizerConfigFileName);

            if (!File.Exists(path))
                return;

            try
            {
                string json = File.ReadAllText(path);

                if (!json.TrimStart().StartsWith('['))
                    return;

                List<OrganizerConfig> legacy = JsonSerializer.Deserialize(json, OrganizerAgentContext.Default.ListOrganizerConfig) ?? new List<OrganizerConfig>();
                new OrganizerConfigSave { Configs = legacy }.Save();
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Error migrating legacy organizer configs: {ex.Message}");
            }
        }
    }

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
        public uint DestContSerial { get; set; } = 0; // 0 = use configuration's destination

        public bool IsMatch(ushort graphic, ushort hue) => graphic == Graphic && (hue == Hue || Hue == ushort.MaxValue);
    }
}
