using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(AutoLootManager.AutoLootConfigEntry))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootConfigEntry>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    public partial class AutoLootJsonContext : JsonSerializerContext
    {
    }

    public class AutoLootManager
    {
        public static AutoLootManager Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        public bool IsLoaded { get { return loaded; } }
        public List<AutoLootConfigEntry> AutoLootList { get => autoLootItems; set => autoLootItems = value; }
        public bool IsLooting => lootItems.Count > 0;

        private HashSet<uint> quickContainsLookup = new ();
        private HashSet<uint> recentlyLooted = new();
        private static Queue<(Item item, AutoLootConfigEntry entry)> lootItems = new ();
        private List<AutoLootConfigEntry> autoLootItems = new ();
        private bool loaded = false;
        private readonly string savePath;
        private long nextLootTime = Time.Ticks;
        private long nextClearRecents = Time.Ticks + 5000;
        private ProgressBarGump progressBarGump;
        private int currentLootTotalCount = 0;
        private bool IsEnabled { get { return ProfileManager.CurrentProfile.EnableAutoLoot; } }

        private World World;

        private AutoLootManager()
        {
            World = Client.Game.UO.World;
            savePath = Path.Combine(ProfileManager.ProfilePath, "AutoLoot.json");
        }


        public bool IsBeingLooted(uint serial) => quickContainsLookup.Contains(serial);

        public void LootItem(uint serial)
        {
            var item = World.Items.Get(serial);
            if (item != null)
            {
                LootItem(item, null);
            }
        }

        public void LootItem(Item item, AutoLootConfigEntry entry = null)
        {
            if (item == null || !recentlyLooted.Add(item.Serial) || !quickContainsLookup.Add(item.Serial)) return;

            lootItems.Enqueue((item, entry));
            currentLootTotalCount++;
            nextClearRecents = Time.Ticks + 5000;
        }

        public void ForceLootContainer(uint serial)
        {
            Item cont = World.Items.Get(serial);

            if (cont == null) return;

            for (LinkedObject i = cont.Items; i != null; i = i.Next)
            {
                CheckAndLoot((Item)i);
            }
        }

        /// <summary>
        /// Check an item against the loot list, if it needs to be auto looted it will be.
        /// </summary>
        private void CheckAndLoot(Item i)
        {
            if (!loaded || i == null || quickContainsLookup.Contains(i.Serial)) return;

            if(i.IsCorpse)
            {
                HandleCorpse(i);

                return;
            }

            var entry = IsOnLootList(i);
            if (entry != null)
            {
                LootItem(i, entry);
            }
        }

        /// <summary>
        /// Check if an item is on the auto loot list.
        /// </summary>
        /// <param name="i">The item to check the loot list against</param>
        /// <returns>The matched AutoLootConfigEntry, or null if no match found</returns>
        private AutoLootConfigEntry IsOnLootList(Item i)
        {
            if (!loaded) return null;

            foreach (var entry in autoLootItems)
            {
                if (entry.Match(i))
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Add an entry for auto looting to match against when opening corpses.
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="hue"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AutoLootConfigEntry AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            AutoLootConfigEntry item = new AutoLootConfigEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry entry in autoLootItems)
            {
                if (entry.Equals(item))
                {
                    return entry;
                }
            }

            autoLootItems.Add(item);

            return item;
        }

        /// <summary>
        /// Search through a corpse and check items that need to be looted.
        /// Only call this after checking that autoloot IsEnabled
        /// </summary>
        /// <param name="corpse"></param>
        private void HandleCorpse(Item corpse)
        {
            if (corpse != null && corpse.IsCorpse && corpse.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange && (!corpse.IsHumanCorpse || ProfileManager.CurrentProfile.AutoLootHumanCorpses))
            {
                for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                {
                    CheckAndLoot((Item)i);
                }
            }
        }

        public void TryRemoveAutoLootEntry(string UID)
        {
            int removeAt = -1;

            for (int i = 0; i < autoLootItems.Count; i++)
            {
                if (autoLootItems[i].UID == UID)
                {
                    removeAt = i;
                }
            }

            if (removeAt > -1)
            {
                autoLootItems.RemoveAt(removeAt);
            }
        }

        /// <summary>
        /// Checks if item is a corpse, or if its root container is corpse and handles them appropriately.
        /// </summary>
        /// <param name="i"></param>
        private void CheckCorpse(Item i)
        {
            if (i == null) return;

            if (i.IsCorpse)
            {
                HandleCorpse(i);
                return;
            }

            var root = World.Items.Get(i.RootContainer);
            if (root != null && root.IsCorpse)
            {
                HandleCorpse(root);
                return;
            }
        }

        public void OnSceneLoad()
        {
            Load();
            EventSink.OPLOnReceive += OnOPLReceived;
            EventSink.OnItemCreated += OnItemCreatedOrUpdated;
            EventSink.OnItemUpdated += OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer += OnOpenContainer;
            EventSink.OnPositionChanged += OnPositionChanged;
        }

        public void OnSceneUnload()
        {
            EventSink.OPLOnReceive -= OnOPLReceived;
            EventSink.OnItemCreated -= OnItemCreatedOrUpdated;
            EventSink.OnItemUpdated -= OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer -= OnOpenContainer;
            EventSink.OnPositionChanged -= OnPositionChanged;
            Save();
            Instance = null;
        }

        private void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!loaded) return;

            if(ProfileManager.CurrentProfile.EnableScavenger)
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || !item.OnGround || item.IsCorpse || item.IsLocked) continue;
                    if (item.Distance >= 3) continue;
                    CheckAndLoot(item);
                }
        }

        private void OnOpenContainer(object sender, uint e)
        {
            if (!loaded || !IsEnabled) return;

            CheckCorpse((Item)sender);
        }

        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!loaded || !IsEnabled) return;

            if (sender is Item i)
            {
                CheckCorpse(i);

                // Check for ground items to auto-loot (scavenger functionality)
                if (ProfileManager.CurrentProfile.EnableScavenger && i.OnGround && !i.IsCorpse && !i.IsLocked && i.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    CheckAndLoot(i);
                }
            }
        }

        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            var item = World.Items.Get(e.Serial);
            if (item != null)
                CheckCorpse(item);
        }

        public void Update()
        {
            if (!loaded || !IsEnabled || !World.InGame) return;

            if (nextLootTime > Time.Ticks) return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return; //Prevent moving stuff while holding an item.

            if (lootItems.Count == 0)
            {
                progressBarGump?.Dispose();
                if (Time.Ticks > nextClearRecents)
                {
                    recentlyLooted.Clear();
                    nextClearRecents = Time.Ticks + 5000;
                }
                return;
            }

            var (moveItem, entry) = lootItems.Dequeue();
            if (moveItem != null)
            {
                if (lootItems.Count == 0) //Que emptied out
                    currentLootTotalCount = 0;

                quickContainsLookup.Remove(moveItem.Serial);

                CreateProgressBar();

                if (progressBarGump != null && !progressBarGump.IsDisposed)
                {
                    progressBarGump.CurrentPercentage = 1 - ((double)lootItems.Count / (double)currentLootTotalCount);
                }

                if (moveItem.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    Item rc = World.Items.Get(moveItem.RootContainer);
                    if (rc != null && rc.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                        return;
                }

                // Determine destination container with fallback logic:
                // 1. Use per-item destination if set and container exists
                // 2. Fall back to global grab bag if set and exists
                // 3. Fall back to player's backpack
                uint destinationSerial = 0;

                if (entry != null && entry.DestinationContainer != 0)
                {
                    Item itemDestContainer = World.Items.Get(entry.DestinationContainer);
                    if (itemDestContainer != null)
                    {
                        destinationSerial = entry.DestinationContainer;
                    }
                }

                if (destinationSerial == 0 && ProfileManager.CurrentProfile.GrabBagSerial != 0)
                {
                    Item grabBag = World.Items.Get(ProfileManager.CurrentProfile.GrabBagSerial);
                    if (grabBag != null)
                    {
                        destinationSerial = ProfileManager.CurrentProfile.GrabBagSerial;
                    }
                }

                if (destinationSerial == 0)
                {
                    Item backpack = World.Player.Backpack;
                    if (backpack != null)
                    {
                        destinationSerial = backpack.Serial;
                    }
                }

                if (destinationSerial != 0)
                {
                    MoveItemQueue.Instance?.Enqueue(moveItem.Serial, destinationSerial, moveItem.Amount, 0xFFFF, 0xFFFF);
                }

                nextLootTime = Time.Ticks + ProfileManager.CurrentProfile.MoveMultiObjectDelay;
            }
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (progressBarGump == null || progressBarGump.IsDisposed))
            {
                progressBarGump = new ProgressBarGump(World, "Auto looting...", 0)
                {
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150,
                    ForegrouneColor = Color.DarkOrange
                };
                progressBarGump.CenterXInViewPort();
                UIManager.Add(progressBarGump);
            }
        }

        private void Load()
        {
            if (loaded) return;

            Task.Factory.StartNew(() =>
            {
                var oldPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");
                if(File.Exists(oldPath))
                    File.Move(oldPath, savePath);

                if (!File.Exists(savePath))
                {
                    autoLootItems = new List<AutoLootConfigEntry>();
                    Log.Error("Auto loot save path not found, creating new..");
                    loaded = true;
                }
                else
                {
                    Log.Info($"Loading: {savePath}");
                    try
                    {
                        JsonHelper.Load(savePath, AutoLootJsonContext.Default.ListAutoLootConfigEntry, out autoLootItems);

                        if (autoLootItems == null)
                        {
                            Log.Error("There was an error loading your auto loot config file, defaulted to no configs.");
                            autoLootItems = new();
                        }

                        loaded = true;
                    }
                    catch
                    {
                        Log.Error("There was an error loading your auto loot config file, please check it with a json validator.");
                        loaded = false;
                    }

                }
            });
        }

        public void Save()
        {
            if (loaded)
            {
                try
                {
                    JsonHelper.SaveAndBackup(autoLootItems, savePath, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }
        }

        public void ExportToFile(string filePath)
        {
            try
            {
                string fileData = JsonSerializer.Serialize(autoLootItems, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                File.WriteAllText(filePath, fileData);
                GameActions.Print($"Autoloot configuration exported to: {filePath}", 0x48);
            }
            catch (Exception e)
            {
                GameActions.Print($"Error exporting autoloot configuration: {e.Message}", 32);
            }
        }

        public void ImportFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    GameActions.Print($"File not found: {filePath}", 32);
                    return;
                }

                string data = File.ReadAllText(filePath);
                List<AutoLootConfigEntry> importedItems = JsonSerializer.Deserialize(data, AutoLootJsonContext.Default.ListAutoLootConfigEntry);

                if (importedItems != null)
                {
                    ImportEntries(importedItems, $"file: {filePath}");
                }
            }
            catch (Exception e)
            {
                GameActions.Print($"Error importing autoloot configuration: {e.Message}", 32);
            }
        }

        public void ImportFromOtherCharacter(string characterName, List<AutoLootConfigEntry> entries)
        {
            try
            {
                if (entries != null && entries.Count > 0)
                {
                    ImportEntries(entries, $"character: {characterName}");
                }
                else
                {
                    GameActions.Print($"No autoloot entries found for character: {characterName}", 32);
                }
            }
            catch (Exception e)
            {
                GameActions.Print($"Error importing from other character: {e.Message}", 32);
            }
        }

        private void ImportEntries(List<AutoLootConfigEntry> entries, string source)
        {
            var newItems = new List<AutoLootConfigEntry>();
            int duplicateCount = 0;

            foreach (var importedItem in entries)
            {
                bool isDuplicate = false;
                foreach (var existingItem in autoLootItems)
                {
                    if (existingItem.Equals(importedItem))
                    {
                        isDuplicate = true;
                        duplicateCount++;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    newItems.Add(importedItem);
                }
            }

            if (newItems.Count > 0)
            {
                autoLootItems.AddRange(newItems);
                Save();
            }

            string message = $"Imported {newItems.Count} new autoloot entries from {source}";
            if (duplicateCount > 0)
            {
                message += $" ({duplicateCount} duplicates skipped)";
            }
            GameActions.Print(message, 0x48);
        }

        public List<AutoLootConfigEntry> LoadOtherCharacterConfig(string characterPath)
        {
            try
            {
                string configPath = Path.Combine(characterPath, "AutoLoot.json");
                if (File.Exists(configPath))
                {
                    string data = File.ReadAllText(configPath);
                    List<AutoLootConfigEntry> items = JsonSerializer.Deserialize(data, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                    return items ?? new List<AutoLootConfigEntry>();
                }
            }
            catch (Exception e)
            {
                GameActions.Print($"Error loading autoloot config from {characterPath}: {e.Message}", 32);
            }
            return new List<AutoLootConfigEntry>();
        }

        public Dictionary<string, List<AutoLootConfigEntry>> GetOtherCharacterConfigs()
        {
            var otherConfigs = new Dictionary<string, List<AutoLootConfigEntry>>();

            string rootpath;
            if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
            {
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            }
            else
            {
                rootpath = Settings.GlobalSettings.ProfilesPath;
            }

            string currentCharacterName = ProfileManager.CurrentProfile?.CharacterName ?? "";
            var characterPaths = Exstentions.GetAllCharacterPaths(rootpath);

            foreach (var kvp in characterPaths)
            {
                string characterName = kvp.Key;
                string characterPath = kvp.Value;

                if (characterPath == ProfileManager.ProfilePath)
                    continue;

                var configs = LoadOtherCharacterConfig(characterPath);
                if (configs.Count > 0)
                {
                    otherConfigs[characterName] = configs;
                }
            }

            return otherConfigs;
        }

        public class AutoLootConfigEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            public uint DestinationContainer { get; set; } = 0;
            private bool RegexMatch => !string.IsNullOrEmpty(RegexSearch);
            /// <summary>
            /// Do not set this manually.
            /// </summary>
            public string UID { get; set; } = Guid.NewGuid().ToString();

            public bool Match(Item compareTo)
            {
                if (Graphic != -1 && Graphic != compareTo.Graphic) return false;

                if (!HueCheck(compareTo.Hue)) return false;

                if (RegexMatch && !RegexCheck(compareTo.World, compareTo)) return false;

                return true;
            }

            private bool HueCheck(ushort value)
            {
                if (Hue == ushort.MaxValue) //Ignore hue.
                {
                    return true;
                }
                else if (Hue == value) //Hue must match, and it does
                {
                    return true;
                }
                else //Hue is not ignored, and does not match
                {
                    return false;
                }
            }

            private bool RegexCheck(World world, Item compareTo)
            {
                string search = "";
                if (world.OPL.TryGetNameAndData(compareTo, out string name, out string data))
                    search += name + data;
                else
                    search = StringHelper.GetPluralAdjustedString(compareTo.ItemData.Name);

                return RegexHelper.GetRegex(RegexSearch, RegexOptions.Multiline).IsMatch(search);
            }

            public bool Equals(AutoLootConfigEntry other)
            {
                return other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
            }
        }
    }
}
