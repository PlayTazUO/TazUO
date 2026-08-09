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
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ClassicUO.Common;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility.Logging;
using System.ComponentModel;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(AutoLootManager.AutoLootConfigEntry))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootConfigEntry>))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootPriority))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootList))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootList>))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootData))]
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
        /// <summary>
        /// Entries of the currently selected loot list. Matching, adding and removing all operate
        /// against this list.
        /// </summary>
        public List<AutoLootConfigEntry> AutoLootEntries => _currentList?.Entries ?? _fallbackEntries;

        /// <summary>
        /// All configured loot lists. There is always at least one.
        /// </summary>
        public IReadOnlyList<AutoLootList> Lists => _data.Lists;

        /// <summary>
        /// The loot list currently selected/active.
        /// </summary>
        public AutoLootList CurrentList => _currentList;

        /// <summary>
        /// True once the config file has finished loading. Mutating operations are ignored until
        /// this is true so the background load cannot overwrite user changes.
        /// </summary>
        public bool IsLoaded => _loaded;

        /// <summary>
        /// The hue applied to corpses after they have been auto looted.
        /// </summary>
        public const ushort LootedCorpseHue = 73;

        /// <summary>
        /// Name of the list that legacy (flat) configs are migrated into.
        /// </summary>
        public const string DefaultListName = "Default";

        /// <summary>
        /// Max number of looted corpse serials to remember before evicting the oldest.
        /// </summary>
        private const int MaxLootedCorpseHistory = 10000;

        /// <summary>
        /// Serials of corpses that have been looted and hued. Persists across corpse
        /// recreation (e.g. walking out of and back into view) so the looted hue is reapplied.
        /// </summary>
        private static readonly HashSet<uint> _huedCorpses = new();
        private static readonly Queue<uint> _huedCorpseOrder = new();

        private readonly HashSet<uint> _quickContainsLookup = new ();
        private readonly HashSet<uint> _recentlyLooted = new();
        private static readonly PriorityQueue<(uint item, AutoLootConfigEntry entry), AutoLootPriority> _lootItems = new ();
        private readonly List<AutoLootConfigEntry> _fallbackEntries = new ();
        private AutoLootData _data = new ();
        private AutoLootList _currentList;
        private bool _loaded = false;
        private long _nextLootTime = Time.Ticks;
        private long _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
        private ProgressBarGump _progressBarGump;
        private int _currentLootTotalCount = 0;
        private bool IsEnabled => ProfileManager.CurrentProfile.EnableAutoLoot;

        private readonly World _world;

        private AutoLootManager()
        {
            _world = Client.Game.UO.World;
            EnsureAtLeastOneList();
        }

        public bool IsBeingLooted(uint serial) => _quickContainsLookup.Contains(serial);

        public void LootItem(uint serial)
        {
            Item item = _world.Items.Get(serial);
            if (item != null) LootItem(item, null);
        }

        public void LootItem(Item item, AutoLootConfigEntry entry = null, AutoLootPriority priority = AutoLootPriority.Normal)
        {
            if (item == null || !_recentlyLooted.Add(item.Serial) || !_quickContainsLookup.Add(item.Serial)) return;

            if (entry != null)
                priority = entry.Priority;
            _lootItems.Enqueue((item, entry), priority);
            _currentLootTotalCount++;
            _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
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

            if (i.ShouldAutoLoot)
            {
                LootItem(i, null);
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

            foreach (AutoLootConfigEntry entry in AutoLootEntries)
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
            if (!_loaded) return null;

            var item = new AutoLootConfigEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry entry in AutoLootEntries)
                if (entry.Equals(item))
                    return entry;

            AutoLootEntries.Add(item);

            return item;
        }

        /// <summary>
        /// Search through a corpse and check items that need to be looted.
        /// Only call this after checking that autoloot IsEnabled
        /// Note: This method doesn't gurantee to process all itmes in the corpse,
        /// because `corpse.Items` is populated via `AddItemToContainer` packet, thus
        /// it may not have all items yet when the method is called.
        /// </summary>
        /// <param name="corpse"></param>
        private void HandleCorpse(Item corpse)
        {
            if (corpse is not { IsCorpse: true }) return;

            if (corpse.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange && !ProfileManager.CurrentProfile.DisableAutolootCorpseRetry)
            {
                World.Instance?.Player?.AutoOpenedCorpses.Remove(corpse); //Retry if the distance was too great to loot
                return;
            }

            if (corpse.IsHumanCorpse && !ProfileManager.CurrentProfile.AutoLootHumanCorpses) return;

            for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                CheckAndLoot((Item)i);

            if(ProfileManager.CurrentProfile.HueCorpseAfterAutoloot)
            {
                corpse.Hue = LootedCorpseHue;
                MarkCorpseHued(corpse.Serial);
            }
        }

        /// <summary>
        /// Records a corpse serial as having been looted and hued, so the looted hue can be
        /// reapplied if the corpse is recreated. Evicts the oldest entry once the history
        /// reaches <see cref="MaxLootedCorpseHistory"/>.
        /// </summary>
        public static void MarkCorpseHued(uint serial)
        {
            if (serial == 0) return;

            if (!_huedCorpses.Add(serial)) return;

            _huedCorpseOrder.Enqueue(serial);

            while (_huedCorpseOrder.Count > MaxLootedCorpseHistory && _huedCorpseOrder.TryDequeue(out uint oldest))
                _huedCorpses.Remove(oldest);
        }

        /// <summary>
        /// Applies the looted hue to a corpse if it was previously looted and hued.
        /// Intended to be called when a corpse is (re)added to the world.
        /// </summary>
        public static void ApplyLootedHueIfNeeded(Item corpse)
        {
            if (corpse == null || !_huedCorpses.Contains(corpse.Serial)) return;

            corpse.Hue = LootedCorpseHue;
        }

        public void TryRemoveAutoLootEntry(string uid)
        {
            if (!_loaded) return;

            List<AutoLootConfigEntry> entries = AutoLootEntries;
            int removeAt = -1;

            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Uid == uid)
                    removeAt = i;

            if (removeAt > -1) entries.RemoveAt(removeAt);
        }

        /// <summary>
        /// Ensures there is always at least one loot list and that a list is selected.
        /// </summary>
        private void EnsureAtLeastOneList()
        {
            _data ??= new AutoLootData();
            _data.Lists ??= new List<AutoLootList>();

            if (_data.Lists.Count == 0)
                _data.Lists.Add(new AutoLootList { Name = DefaultListName });

            if (_currentList == null || !_data.Lists.Contains(_currentList))
            {
                AutoLootList found = null;

                if (!string.IsNullOrEmpty(_data.SelectedUid))
                    found = _data.Lists.Find(l => l.Uid == _data.SelectedUid);

                _currentList = found ?? _data.Lists[0];
                _data.SelectedUid = _currentList.Uid;
            }
        }

        /// <summary>
        /// Selects the given loot list as the active one. All matching/adding will use it.
        /// </summary>
        public void SelectList(AutoLootList list)
        {
            if (!_loaded || list == null || !_data.Lists.Contains(list) || _currentList == list) return;

            _currentList = list;
            _data.SelectedUid = list.Uid;
            Save();
        }

        /// <summary>
        /// Creates a new loot list and selects it.
        /// </summary>
        public AutoLootList AddList(string name)
        {
            if (!_loaded) return null;

            var list = new AutoLootList { Name = string.IsNullOrWhiteSpace(name) ? "New List" : name.Trim() };

            _data.Lists.Add(list);
            _currentList = list;
            _data.SelectedUid = list.Uid;
            Save();

            return list;
        }

        /// <summary>
        /// Deletes a loot list. Refuses to delete the last remaining list so there is always at
        /// least one. If the deleted list was selected, the first remaining list becomes active.
        /// </summary>
        /// <returns>True if the list was deleted.</returns>
        public bool DeleteList(AutoLootList list)
        {
            if (!_loaded || list == null || _data.Lists.Count <= 1 || !_data.Lists.Remove(list)) return false;

            if (_currentList == list)
            {
                _currentList = _data.Lists[0];
                _data.SelectedUid = _currentList.Uid;
            }

            Save();
            return true;
        }

        /// <summary>
        /// Renames a loot list.
        /// </summary>
        public void RenameList(AutoLootList list, string newName)
        {
            if (!_loaded || list == null || string.IsNullOrWhiteSpace(newName)) return;

            list.Name = newName.Trim();
            Save();
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
                // Check the item that triggered this call directly
                CheckAndLoot(i);
                // A defensive safety net to ensure all items in the corpse are processed
                HandleCorpse(root);
                return;
            }
        }

        public void OnSceneLoad()
        {
            Load();
            EventSink.OPLOnReceive += OnOPLReceived;
            EventSink.OnItemCreatedInternal += OnItemCreatedOrUpdated;
            EventSink.OnItemUpdatedInternal += OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer += OnOpenContainer;
            EventSink.OnPositionChanged += OnPositionChanged;
        }

        public void OnSceneUnload()
        {
            EventSink.OPLOnReceive -= OnOPLReceived;
            EventSink.OnItemCreatedInternal -= OnItemCreatedOrUpdated;
            EventSink.OnItemUpdatedInternal -= OnItemCreatedOrUpdated;
            EventSink.OnOpenContainer -= OnOpenContainer;
            EventSink.OnPositionChanged -= OnPositionChanged;
            Save();
            Instance = null;
        }

        /// <summary>
        /// Invoked whenever the player changes position.
        ///
        /// The other looter entry points are item update events but those are not enough;
        /// If the player opens a corpse and walks away a few steps, there wouldn't be any new events firing.
        ///
        /// This handler effectively allows re-triggering as soon as the corpses are back in range.
        ///
        /// Note that this venue kicks in only when distance is less than 3.
        /// </summary>
        /// <param name="sender">The source event sink</param>
        /// <param name="e">The position change event arguments</param>
        private void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!_loaded) return;

            if (ProfileManager.CurrentProfile.EnableScavenger)
                foreach (Item item in _world.Items.Values)
                    if (item != null && item.OnGround && !item.IsLocked && !item.IsCorpse && item.Distance < 3)
                        CheckAndLoot(item);

            if (IsEnabled)
                foreach (Item corpse in _world.GetCorpseSnapshot())
                    CheckCorpse(corpse);
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
                    _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
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
                    if (rc.IsCorpse && !ProfileManager.CurrentProfile.DisableAutolootCorpseRetry)
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
            {
                ActionPriority lootPriority = entry?.Priority switch
                {
                    AutoLootPriority.High => ActionPriority.LootItemHigh,
                    AutoLootPriority.Low => ActionPriority.LootItem,
                    _ => ActionPriority.LootItemMedium,
                };
                ObjectActionQueue.Instance.Enqueue(new MoveRequest(moveItem.Serial, destinationSerial, moveItem.Amount).ToObjectActionQueueItem(), lootPriority);
            }
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
                    Y = ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y - 150,
                    ForegrouneColor = Color.DarkOrange
                };
                _progressBarGump.CenterXInViewPort();
                UIManager.Add(_progressBarGump);
            }
        }

        private void Load()
        {
            if (_loaded) return;

            try
            {
                MigrateOldLocationIfNeeded();
                MigrateLegacyFormatIfNeeded();

                _data = AutoLootData.Load();
                _currentList = null;
                EnsureAtLeastOneList();
                _loaded = true;
            }
            catch
            {
                Log.Error("There was an error loading your auto loot config file, please check it with a json validator.");
                _loaded = false;
            }
        }

        /// <summary>The full path to the current profile's auto loot config file.</summary>
        private static string SavePath => Path.Combine(JsonSaveLocationHelper.GetScopeDirectory(SettingsScope.Char), AutoLootData.AutoLootFileName);

        /// <summary>
        /// Moves the very old <c>Data/Profiles/AutoLoot.json</c> into the current profile folder, if present.
        /// </summary>
        private static void MigrateOldLocationIfNeeded()
        {
            string oldPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", AutoLootData.AutoLootFileName);
            string newPath = SavePath;

            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Move(oldPath, newPath);
            }
        }

        /// <summary>
        /// Rewrites a legacy flat-list config (a JSON array of entries) into the wrapped
        /// <see cref="AutoLootData"/> format so <see cref="JsonSave{T}"/> can load it. The legacy file is
        /// rotated into the backups folder as part of the save.
        /// </summary>
        private static void MigrateLegacyFormatIfNeeded()
        {
            string path = SavePath;

            if (!File.Exists(path) || !IsLegacyFormat(path)) return;

            JsonHelper.Load(path, AutoLootJsonContext.Default.ListAutoLootConfigEntry, out List<AutoLootConfigEntry> legacy);

            var data = new AutoLootData();
            data.Lists.Add(new AutoLootList { Name = DefaultListName, Entries = legacy ?? new List<AutoLootConfigEntry>() });
            data.Save();
        }

        /// <summary>
        /// Determines whether the file at <paramref name="path"/> is a legacy flat-list config
        /// (a JSON array) rather than the newer <see cref="AutoLootData"/> object format.
        /// </summary>
        private static bool IsLegacyFormat(string path)
        {
            try
            {
                foreach (char c in File.ReadAllText(path))
                {
                    if (char.IsWhiteSpace(c)) continue;
                    return c == '[';
                }
            }
            catch { }

            return false;
        }

        public void Save()
        {
            if (!_loaded) return;

            try
            {
                if (_currentList != null)
                    _data.SelectedUid = _currentList.Uid;

                _data.Save();
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public void ClearActiveLootQueue()
        {
            while (_lootItems.TryDequeue(out _, out _));
            _currentLootTotalCount = 0;
            _quickContainsLookup.Clear();
            _progressBarGump?.Dispose();
            _progressBarGump = null;
        }

        public void ImportFromOtherCharacter(string characterName, List<AutoLootConfigEntry> entries)
        {
            try
            {
                if (entries != null && entries.Count > 0)
                    ImportEntries(entries, $"character: {characterName}");
                else
                    GameActions.Print($"No autoloot entries found for character: {characterName}", Constants.HUE_ERROR);
            }
            catch (Exception e)
            {
                GameActions.Print($"Error importing from other character: {e.Message}", Constants.HUE_ERROR);
            }
        }

        private void ImportEntries(List<AutoLootConfigEntry> entries, string source)
        {
            var newItems = new List<AutoLootConfigEntry>();
            int duplicateCount = 0;

            List<AutoLootConfigEntry> currentEntries = AutoLootEntries;

            foreach (AutoLootConfigEntry importedItem in entries)
            {
                bool isDuplicate = false;
                foreach (AutoLootConfigEntry existingItem in currentEntries)
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
                currentEntries.AddRange(newItems);
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
                string configPath = Path.Combine(characterPath, AutoLootData.AutoLootFileName);
                if (File.Exists(configPath))
                {
                    if (IsLegacyFormat(configPath))
                    {
                        string data = File.ReadAllText(configPath);
                        List<AutoLootConfigEntry> items = JsonSerializer.Deserialize(data, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                        return items ?? new List<AutoLootConfigEntry>();
                    }

                    // New format: flatten the entries across all of the character's lists.
                    string json = File.ReadAllText(configPath);
                    AutoLootData other = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootData);
                    var flattened = new List<AutoLootConfigEntry>();
                    if (other?.Lists != null)
                        foreach (AutoLootList list in other.Lists)
                            if (list?.Entries != null)
                                flattened.AddRange(list.Entries);

                    return flattened;
                }
            }
            catch (Exception e)
            {
                GameActions.Print($"Error loading autoloot config from {characterPath}: {e.Message}", Constants.HUE_ERROR);
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
            Dictionary<string, string> characterPaths = Utility.Extensions.GetAllCharacterPaths(rootpath);

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

        #nullable enable
        public string? GetJsonExport()
        {
            try
            {
                return JsonSerializer.Serialize(AutoLootEntries, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
            }
            catch (Exception e)
            {
                Log.Error($"Error exporting autoloot to JSON: {e}");
            }

            return null;
        }
        #nullable disable

        public bool ImportFromJson(string json)
        {
            try
            {
                List<AutoLootConfigEntry> importedItems = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.ListAutoLootConfigEntry);

                if (importedItems != null)
                {
                    ImportEntries(importedItems, "clipboard");
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error importing autoloot from JSON: {e}");
            }

            return false;
        }

        public enum AutoLootPriority { Low = 0, Normal = 1, High = 2 }

        /// <summary>
        /// A named collection of auto loot entries. Users can create multiple lists and quickly
        /// swap between them.
        /// </summary>
        public class AutoLootList
        {
            public string Name { get; set; } = "";
            public List<AutoLootConfigEntry> Entries { get; set; } = new();
            /// <summary>
            /// Do not set this manually.
            /// </summary>
            public string Uid { get; set; } = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Root persisted object holding every loot list and the currently selected one.
        /// Saving/loading (with rotating backups) is handled by <see cref="JsonSave{T}"/>.
        /// </summary>
        public class AutoLootData : JsonSave<AutoLootData>, INotifyPropertyChanged
        {
            public const string AutoLootFileName = "AutoLoot.json";

            public List<AutoLootList> Lists { get; set; } = new();
            public string SelectedUid { get; set; } = "";

            /// <summary>Lives in the profile folder alongside the other per-character configs.</summary>
            protected override SettingsScope Scope => SettingsScope.Char;

            protected override string FileName => AutoLootFileName;

            protected override JsonTypeInfo<AutoLootData> TypeInfo => AutoLootJsonContext.Default.AutoLootData;
        }

        public class AutoLootConfigEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            [JsonConverter(typeof(RawStringConverter))]
            public string RegexSearch { get; set; } = string.Empty;
            public uint DestinationContainer { get; set; } = 0;
            public AutoLootPriority Priority { get; set; } = AutoLootPriority.Normal;
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
                string search;

                if (compareTo.OPLName.NotNullNotEmpty())
                    search = compareTo.OPLName;
                else
                    search = compareTo.GetNormalizedName(false);

                if (compareTo.OPLData.NotNullNotEmpty())
                    search += compareTo.OPLData;

                return RegexHelper.GetRegex(RegexSearch, RegexOptions.Multiline).IsMatch(search);
            }

            public bool Equals(AutoLootConfigEntry other) => other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
        }
    }
}
