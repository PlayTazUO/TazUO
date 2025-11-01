using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public List<AutoLootConfigEntry> AutoLootList { get => _autoLootItems; set => _autoLootItems = value; }

        private readonly HashSet<uint> _quickContainsLookup = new ();
        private readonly HashSet<uint> _recentlyLooted = new();
        private static readonly Queue<(uint item, AutoLootConfigEntry entry)> _lootItems = new ();
        private List<AutoLootConfigEntry> _autoLootItems = new ();
        private bool _loaded = false;
        private readonly string _savePath;
        private long _nextLootTime = Time.Ticks;
        private long _nextClearRecents = Time.Ticks + 5000;
        private ProgressBarGump _progressBarGump;
        private int _currentLootTotalCount = 0;
        private bool IsEnabled => ProfileManager.CurrentProfile.EnableAutoLoot;

        private readonly World _world;

        private AutoLootManager()
        {
            _world = Client.Game.UO.World;
            _savePath = Path.Combine(ProfileManager.ProfilePath, "AutoLoot.json");
        }

        public bool IsBeingLooted(uint serial) => _quickContainsLookup.Contains(serial);

        public void LootItem(uint serial)
        {
            Item item = _world.Items.Get(serial);
            if (item != null) LootItem(item, null);
        }

        public void LootItem(Item item, AutoLootConfigEntry entry = null)
        {
            if (item == null || !_recentlyLooted.Add(item.Serial) || !_quickContainsLookup.Add(item.Serial)) return;

            _lootItems.Enqueue((item, entry));
            _currentLootTotalCount++;
            _nextClearRecents = Time.Ticks + 5000;
        }

        public void ForceLootContainer(uint serial)
        {
            Item cont = _world.Items.Get(serial);

            if (cont == null) return;

            for (LinkedObject i = cont.Items; i != null; i = i.Next) CheckAndLoot((Item)i);
        }

        /// <summary>
        /// Check an item against the loot list, if it needs to be auto looted it will be.
        /// </summary>
        private void CheckAndLoot(Item i)
        {
            if (!_loaded || i == null || _quickContainsLookup.Contains(i.Serial)) return;

            if(i.IsCorpse)
            {
                HandleCorpse(i);

                return;
            }

            AutoLootConfigEntry entry = IsOnLootList(i);
            if (entry != null) LootItem(i, entry);
        }

        /// <summary>
        /// Check if an item is on the auto loot list.
        /// </summary>
        /// <param name="i">The item to check the loot list against</param>
        /// <returns>The matched AutoLootConfigEntry, or null if no match found</returns>
        private AutoLootConfigEntry IsOnLootList(Item i)
        {
            if (!_loaded) return null;

            foreach (AutoLootConfigEntry entry in _autoLootItems)
                if (entry.Match(i))
                    return entry;

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
            var item = new AutoLootConfigEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry entry in _autoLootItems)
                if (entry.Equals(item))
                    return entry;

            _autoLootItems.Add(item);

            return item;
        }

        /// <summary>
        /// Search through a corpse and check items that need to be looted.
        /// Only call this after checking that autoloot IsEnabled
        /// </summary>
        /// <param name="corpse"></param>
        private void HandleCorpse(Item corpse)
        {
            if (corpse is not { IsCorpse: true }) return;

            if (corpse.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
            {
                World.Instance?.Player?.AutoOpenedCorpses.Remove(corpse); //Retry if the distance was too great to loot
                return;
            }

            if (corpse.IsHumanCorpse && !ProfileManager.CurrentProfile.AutoLootHumanCorpses) return;

            for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                CheckAndLoot((Item)i);
        }

        public void TryRemoveAutoLootEntry(string uid)
        {
            int removeAt = -1;

            for (int i = 0; i < _autoLootItems.Count; i++)
                if (_autoLootItems[i].Uid == uid)
                    removeAt = i;

            if (removeAt > -1) _autoLootItems.RemoveAt(removeAt);
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

            Item root = _world.Items.Get(i.RootContainer);
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
            if (!_loaded) return;

            if(ProfileManager.CurrentProfile.EnableScavenger)
                foreach (Item item in _world.Items.Values)
                {
                    if (item == null || !item.OnGround || item.IsCorpse || item.IsLocked) continue;
                    if (item.Distance >= 3) continue;
                    CheckAndLoot(item);
                }
        }

        private void OnOpenContainer(object sender, uint e)
        {
            if (!_loaded || !IsEnabled) return;

            CheckCorpse((Item)sender);
        }

        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!_loaded || !IsEnabled) return;

            if (sender is Item i)
            {
                CheckCorpse(i);

                // Check for ground items to auto-loot (scavenger functionality)
                if (ProfileManager.CurrentProfile.EnableScavenger && i.OnGround && !i.IsCorpse && !i.IsLocked && i.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange) CheckAndLoot(i);
            }
        }

        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!_loaded || !IsEnabled) return;
            Item item = _world.Items.Get(e.Serial);
            if (item != null)
                CheckCorpse(item);
        }

        public void Update()
        {
            if (!_loaded || !IsEnabled || !_world.InGame) return;

            if (_nextLootTime > Time.Ticks) return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return; //Prevent moving stuff while holding an item.

            if (_lootItems.Count == 0)
            {
                _progressBarGump?.Dispose();
                if (Time.Ticks > _nextClearRecents)
                {
                    _recentlyLooted.Clear();
                    _nextClearRecents = Time.Ticks + 5000;
                }
                return;
            }

            (uint item, AutoLootConfigEntry entry) = _lootItems.Dequeue();
            if (item == 0) return;

            if (_lootItems.Count == 0) //Que emptied out
                _currentLootTotalCount = 0;

            _quickContainsLookup.Remove(item);

            Item moveItem = _world.Items.Get(item);

            if (moveItem == null)
                return;

            CreateProgressBar();

            if (_progressBarGump is { IsDisposed: false }) _progressBarGump.CurrentPercentage = 1 - ((double)_lootItems.Count / (double)_currentLootTotalCount);

            if (moveItem.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
            {
                Item rc = _world.Items.Get(moveItem.RootContainer);
                if (rc != null && rc.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                {
                    if (rc.IsCorpse)
                        World.Instance?.Player?.AutoOpenedCorpses.Remove(rc); //Allow reopening this corpse, we got too far away to finish looting..
                    _recentlyLooted.Remove(item);
                    return;
                }
            }

            uint destinationSerial = 0;

            //If this entry has a specific container, use it
            if (entry != null && entry.DestinationContainer != 0)
            {
                Item itemDestContainer = _world.Items.Get(entry.DestinationContainer);
                if (itemDestContainer != null) destinationSerial = entry.DestinationContainer;
            }

            if (destinationSerial == 0 && ProfileManager.CurrentProfile.GrabBagSerial != 0)
            {
                Item grabBag = _world.Items.Get(ProfileManager.CurrentProfile.GrabBagSerial);
                if (grabBag != null) destinationSerial = ProfileManager.CurrentProfile.GrabBagSerial;
            }

            if (destinationSerial == 0)
            {
                Item backpack = _world.Player.Backpack;
                if (backpack != null) destinationSerial = backpack.Serial;
            }

            if (destinationSerial != 0)
                MoveItemQueue.Instance?.Enqueue(moveItem.Serial, destinationSerial, moveItem.Amount, 0xFFFF, 0xFFFF);
            else
                GameActions.Print("Could not find a container to loot into. Try setting a grab bag.");

            _nextLootTime = Time.Ticks + ProfileManager.CurrentProfile.MoveMultiObjectDelay;
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (_progressBarGump == null || _progressBarGump.IsDisposed))
            {
                _progressBarGump = new ProgressBarGump(_world, "Auto looting...", 0)
                {
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150,
                    ForegrouneColor = Color.DarkOrange
                };
                _progressBarGump.CenterXInViewPort();
                UIManager.Add(_progressBarGump);
            }
        }

        private void Load()
        {
            if (_loaded) return;

            Task.Factory.StartNew(() =>
            {
                string oldPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");
                if(File.Exists(oldPath))
                    File.Move(oldPath, _savePath);

                if (!File.Exists(_savePath))
                {
                    _autoLootItems = new List<AutoLootConfigEntry>();
                    Log.Error("Auto loot save path not found, creating new..");
                    _loaded = true;
                }
                else
                {
                    Log.Info($"Loading: {_savePath}");
                    try
                    {
                        JsonHelper.Load(_savePath, AutoLootJsonContext.Default.ListAutoLootConfigEntry, out _autoLootItems);

                        if (_autoLootItems == null)
                        {
                            Log.Error("There was an error loading your auto loot config file, defaulted to no configs.");
                            _autoLootItems = new();
                        }

                        _loaded = true;
                    }
                    catch
                    {
                        Log.Error("There was an error loading your auto loot config file, please check it with a json validator.");
                        _loaded = false;
                    }

                }
            });
        }

        public void Save()
        {
            if (_loaded)
                try
                {
                    JsonHelper.SaveAndBackup(_autoLootItems, _savePath, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public void ExportToFile(string filePath)
        {
            try
            {
                string fileData = JsonSerializer.Serialize(_autoLootItems, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
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

                if (importedItems != null) ImportEntries(importedItems, $"file: {filePath}");
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
                    ImportEntries(entries, $"character: {characterName}");
                else
                    GameActions.Print($"No autoloot entries found for character: {characterName}", 32);
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

            foreach (AutoLootConfigEntry importedItem in entries)
            {
                bool isDuplicate = false;
                foreach (AutoLootConfigEntry existingItem in _autoLootItems)
                    if (existingItem.Equals(importedItem))
                    {
                        isDuplicate = true;
                        duplicateCount++;
                        break;
                    }

                if (!isDuplicate) newItems.Add(importedItem);
            }

            if (newItems.Count > 0)
            {
                _autoLootItems.AddRange(newItems);
                Save();
            }

            string message = $"Imported {newItems.Count} new autoloot entries from {source}";
            if (duplicateCount > 0) message += $" ({duplicateCount} duplicates skipped)";
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
                rootpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            else
                rootpath = Settings.GlobalSettings.ProfilesPath;

            string currentCharacterName = ProfileManager.CurrentProfile?.CharacterName ?? "";
            Dictionary<string, string> characterPaths = Exstentions.GetAllCharacterPaths(rootpath);

            foreach (KeyValuePair<string, string> kvp in characterPaths)
            {
                string characterName = kvp.Key;
                string characterPath = kvp.Value;

                if (characterPath == ProfileManager.ProfilePath)
                    continue;

                List<AutoLootConfigEntry> configs = LoadOtherCharacterConfig(characterPath);
                if (configs.Count > 0) otherConfigs[characterName] = configs;
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
            public string Uid { get; set; } = Guid.NewGuid().ToString();

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
                    return true;
                else if (Hue == value) //Hue must match, and it does
                    return true;
                else //Hue is not ignored, and does not match
                    return false;
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

            public bool Equals(AutoLootConfigEntry other) => other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
        }
    }
}
