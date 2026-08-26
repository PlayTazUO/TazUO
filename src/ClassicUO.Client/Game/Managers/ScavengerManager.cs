using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(ScavengerManager.ScavengerEntry))]
    [JsonSerializable(typeof(List<ScavengerManager.ScavengerEntry>))]
    [JsonSerializable(typeof(ScavengerManager.ScavengerPriority))]
    [JsonSerializable(typeof(ScavengerManager.ScavengerList))]
    [JsonSerializable(typeof(List<ScavengerManager.ScavengerList>))]
    [JsonSerializable(typeof(ScavengerManager.ScavengerData))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    public partial class ScavengerJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Scavenger support - automatically picks up items on the ground that match the configured
    /// scavenger lists. Independent of <see cref="AutoLootManager"/>: it has its own lists, config
    /// file and queue, sharing only the grab bag and move infrastructure.
    /// </summary>
    public class ScavengerManager
    {
        public static ScavengerManager Instance
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
        /// Entries of the currently selected scavenger list. Matching, adding and removing all
        /// operate against this list.
        /// </summary>
        public List<ScavengerEntry> ScavengerEntries => _currentList?.Entries ?? _fallbackEntries;

        /// <summary>
        /// All configured scavenger lists. There is always at least one.
        /// </summary>
        public IReadOnlyList<ScavengerList> Lists => _data.Lists;

        /// <summary>
        /// The scavenger list currently selected/active.
        /// </summary>
        public ScavengerList CurrentList => _currentList;

        /// <summary>
        /// True once the config file has finished loading. Mutating operations are ignored until
        /// this is true so the background load cannot overwrite user changes.
        /// </summary>
        public bool IsLoaded => _loaded;

        /// <summary>
        /// Name of the list that first-run configs are seeded into.
        /// </summary>
        public const string DefaultListName = "Default";

        private readonly HashSet<uint> _quickContainsLookup = new();
        private readonly HashSet<uint> _recentlyLooted = new();
        private static readonly PriorityQueue<(uint item, ScavengerEntry entry), ScavengerPriority> _lootItems = new();
        private readonly List<ScavengerEntry> _fallbackEntries = new();
        private ScavengerData _data = new();
        private ScavengerList _currentList;
        private bool _loaded = false;
        private long _nextLootTime = Time.Ticks;
        private long _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
        private bool IsEnabled => ProfileManager.CurrentProfile.EnableScavenger;

        private readonly World _world;

        /// <summary>
        /// The UID of the currently selected scavenger list. Stored per character on the profile so
        /// each character can have a different active list while the lists themselves are shared
        /// across the server.
        /// </summary>
        private string SelectedListUid
        {
            get => ProfileManager.CurrentProfile?.ScavengerSelectedListUid ?? string.Empty;
            set
            {
                if (ProfileManager.CurrentProfile != null)
                    ProfileManager.CurrentProfile.ScavengerSelectedListUid = value;
            }
        }

        private ScavengerManager()
        {
            _world = Client.Game.UO.World;
        }

        public void LootItem(uint serial)
        {
            Item item = _world.Items.Get(serial);
            if (item != null) LootItem(item, null);
        }

        public void LootItem(Item item, ScavengerEntry entry = null, ScavengerPriority priority = ScavengerPriority.Normal)
        {
            if (item == null || !_recentlyLooted.Add(item.Serial) || !_quickContainsLookup.Add(item.Serial)) return;

            if (entry != null)
                priority = entry.Priority;
            _lootItems.Enqueue((item, entry), priority);
            _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
        }

        /// <summary>
        /// Check a ground item against the scavenger list, if it needs to be picked up it will be.
        /// </summary>
        private void CheckAndLoot(Item i)
        {
            if (!_loaded || i == null || _quickContainsLookup.Contains(i.Serial)) return;

            ScavengerEntry entry = IsOnLootList(i);
            if (entry != null) LootItem(i, entry);
        }

        /// <summary>
        /// Check if an item is on the scavenger list.
        /// </summary>
        /// <param name="i">The item to check the scavenger list against</param>
        /// <returns>The matched ScavengerEntry, or null if no match found</returns>
        private ScavengerEntry IsOnLootList(Item i)
        {
            if (!_loaded) return null;

            foreach (ScavengerEntry entry in ScavengerEntries)
                if (entry.Match(i))
                    return entry;

            return null;
        }

        /// <summary>
        /// Add an entry for scavenging to match against items on the ground.
        /// </summary>
        public ScavengerEntry AddScavengerEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            if (!_loaded) return null;

            var item = new ScavengerEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (ScavengerEntry entry in ScavengerEntries)
                if (entry.Equals(item))
                    return entry;

            ScavengerEntries.Add(item);

            return item;
        }

        public void TryRemoveScavengerEntry(string uid)
        {
            if (!_loaded) return;

            List<ScavengerEntry> entries = ScavengerEntries;
            int removeAt = -1;

            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Uid == uid)
                    removeAt = i;

            if (removeAt > -1) entries.RemoveAt(removeAt);
        }

        /// <summary>
        /// Ensures there is always at least one scavenger list and that a list is selected.
        /// </summary>
        private void EnsureAtLeastOneList()
        {
            _data ??= new ScavengerData();
            _data.Lists ??= new List<ScavengerList>();

            if (_data.Lists.Count == 0)
                _data.Lists.Add(new ScavengerList { Name = DefaultListName });

            if (_currentList == null || !_data.Lists.Contains(_currentList))
            {
                ScavengerList found = null;

                if (!string.IsNullOrEmpty(SelectedListUid))
                    found = _data.Lists.Find(l => l.Uid == SelectedListUid);

                _currentList = found ?? _data.Lists[0];
                SelectedListUid = _currentList.Uid;
            }
        }

        /// <summary>
        /// Selects the given scavenger list as the active one. All matching/adding will use it.
        /// </summary>
        public void SelectList(ScavengerList list)
        {
            if (!_loaded || list == null || !_data.Lists.Contains(list) || _currentList == list) return;

            _currentList = list;
            SelectedListUid = list.Uid;
            Save();
        }

        /// <summary>
        /// Creates a new scavenger list and selects it.
        /// </summary>
        public ScavengerList AddList(string name)
        {
            if (!_loaded) return null;

            var list = new ScavengerList { Name = string.IsNullOrWhiteSpace(name) ? "New List" : name.Trim() };

            _data.Lists.Add(list);
            _currentList = list;
            SelectedListUid = list.Uid;
            Save();

            return list;
        }

        /// <summary>
        /// Deletes a scavenger list. Refuses to delete the last remaining list so there is always at
        /// least one. If the deleted list was selected, the first remaining list becomes active.
        /// </summary>
        /// <returns>True if the list was deleted.</returns>
        public bool DeleteList(ScavengerList list)
        {
            if (!_loaded || list == null || _data.Lists.Count <= 1 || !_data.Lists.Remove(list)) return false;

            if (_currentList == list)
            {
                _currentList = _data.Lists[0];
                SelectedListUid = _currentList.Uid;
            }

            Save();
            return true;
        }

        /// <summary>
        /// Renames a scavenger list.
        /// </summary>
        public void RenameList(ScavengerList list, string newName)
        {
            if (!_loaded || list == null || string.IsNullOrWhiteSpace(newName)) return;

            list.Name = newName.Trim();
            Save();
        }

        public void OnSceneLoad()
        {
            Load();
            EventSink.OnItemCreatedInternal += OnItemCreatedOrUpdated;
            EventSink.OnItemUpdatedInternal += OnItemCreatedOrUpdated;
            EventSink.OnPositionChanged += OnPositionChanged;
        }

        public void OnSceneUnload()
        {
            EventSink.OnItemCreatedInternal -= OnItemCreatedOrUpdated;
            EventSink.OnItemUpdatedInternal -= OnItemCreatedOrUpdated;
            EventSink.OnPositionChanged -= OnPositionChanged;
            Save();
            Instance = null;
        }

        /// <summary>
        /// Invoked whenever the player changes position. Scans ground items within reach so freshly
        /// dropped items (or items stepped over) get picked up without a new item event firing.
        /// </summary>
        private void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!_loaded || !IsEnabled) return;

            foreach (Item item in _world.Items.Values)
                if (item != null && item.OnGround && !item.IsLocked && !item.IsCorpse && item.Distance < 3)
                    CheckAndLoot(item);
        }

        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!_loaded || !IsEnabled) return;

            if (sender is Item i && i.OnGround && !i.IsCorpse && !i.IsLocked && i.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                CheckAndLoot(i);
        }

        public void Update()
        {
            if (!_loaded || !IsEnabled || !_world.InGame) return;

            if (_nextLootTime > Time.Ticks) return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return; //Prevent moving stuff while holding an item.

            if (_lootItems.Count == 0)
            {
                if (Time.Ticks > _nextClearRecents)
                {
                    _recentlyLooted.Clear();
                    _nextClearRecents = Time.Ticks + (ProfileManager.CurrentProfile?.AutoLootRetryDelay ?? 5000);
                }
                return;
            }

            (uint item, ScavengerEntry entry) = _lootItems.Dequeue();
            if (item == 0) return;

            _quickContainsLookup.Remove(item);

            Item moveItem = _world.Items.Get(item);

            if (moveItem == null)
                return;

            if (moveItem.Distance > ProfileManager.CurrentProfile.AutoOpenCorpseRange)
            {
                _recentlyLooted.Remove(item);
                return;
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
                    ScavengerPriority.High => ActionPriority.LootItemHigh,
                    ScavengerPriority.Low => ActionPriority.LootItem,
                    _ => ActionPriority.LootItemMedium,
                };

                ushort amount = GetAmountToMove(moveItem, entry, destinationSerial);
                if (amount > 0)
                    ObjectActionQueue.Instance.Enqueue(new MoveRequest(moveItem.Serial, destinationSerial, amount).ToObjectActionQueueItem(), lootPriority);
            }
            else
                GameActions.Print("Could not find a container to loot into. Try setting a grab bag.");

            _nextLootTime = Time.Ticks + ProfileManager.CurrentProfile.MoveMultiObjectDelay;
        }

        /// <summary>
        /// Computes how much of <paramref name="item"/> to move so the destination container ends
        /// up with at most <see cref="ScavengerEntry.MaxAmount"/> matching items. Returns the item's
        /// full amount when no limit is configured.
        /// </summary>
        private ushort GetAmountToMove(Item item, ScavengerEntry entry, uint destinationSerial)
        {
            if (entry == null || entry.MaxAmount <= 0)
                return item.Amount;

            Item destCont = _world.Items.Get(destinationSerial);
            if (destCont == null)
                return item.Amount;

            int existing = 0;
            for (LinkedObject i = destCont.Items; i != null; i = i.Next)
            {
                if (i is Item destItem && destItem.Serial != item.Serial && entry.Match(destItem))
                    existing += destItem.Amount;
            }

            int toMove = entry.MaxAmount - existing;
            if (toMove <= 0)
                return 0;

            return (ushort)Math.Min(toMove, item.Amount);
        }

        private void Load()
        {
            if (_loaded) return;

            try
            {
                _data = ScavengerData.Load();
                _currentList = null;
                EnsureAtLeastOneList();
                _loaded = true;
            }
            catch
            {
                Log.Error("There was an error loading your scavenger config file, please check it with a json validator.");
                _loaded = false;
            }
        }

        public void Save()
        {
            if (!_loaded) return;

            try
            {
                if (_currentList != null)
                    SelectedListUid = _currentList.Uid;

                _data.Save();
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public void ClearActiveLootQueue()
        {
            while (_lootItems.TryDequeue(out _, out _));
            _quickContainsLookup.Clear();
        }

        private void ImportEntries(List<ScavengerEntry> entries, string source)
        {
            var newItems = new List<ScavengerEntry>();
            int duplicateCount = 0;

            List<ScavengerEntry> currentEntries = ScavengerEntries;

            foreach (ScavengerEntry importedItem in entries)
            {
                bool isDuplicate = false;
                foreach (ScavengerEntry existingItem in currentEntries)
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

            string message = $"Imported {newItems.Count} new scavenger entries from {source}";
            if (duplicateCount > 0) message += $" ({duplicateCount} duplicates skipped)";
            GameActions.Print(message, 0x48);
        }

        #nullable enable
        public string? GetJsonExport()
        {
            try
            {
                return JsonSerializer.Serialize(ScavengerEntries, ScavengerJsonContext.Default.ListScavengerEntry);
            }
            catch (Exception e)
            {
                Log.Error($"Error exporting scavenger to JSON: {e}");
            }

            return null;
        }
        #nullable disable

        public bool ImportFromJson(string json)
        {
            try
            {
                List<ScavengerEntry> importedItems = JsonSerializer.Deserialize(json, ScavengerJsonContext.Default.ListScavengerEntry);

                if (importedItems != null)
                {
                    ImportEntries(importedItems, "clipboard");
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error importing scavenger from JSON: {e}");
            }

            return false;
        }

        public enum ScavengerPriority { Low = 0, Normal = 1, High = 2 }

        /// <summary>
        /// A named collection of scavenger entries. Users can create multiple lists and quickly
        /// swap between them.
        /// </summary>
        public class ScavengerList
        {
            public string Name { get; set; } = "";
            public List<ScavengerEntry> Entries { get; set; } = new();
            /// <summary>
            /// Do not set this manually.
            /// </summary>
            public string Uid { get; set; } = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Root persisted object holding every scavenger list. Saving/loading (with rotating
        /// backups) is handled by <see cref="JsonSave{T}"/>. The selected list UID lives on the
        /// profile (per character) rather than here.
        /// </summary>
        public class ScavengerData : JsonSave<ScavengerData>, INotifyPropertyChanged
        {
            public const string ScavengerFileName = "Scavenger.json";

            public List<ScavengerList> Lists { get; set; } = new();

            /// <summary>Lives in the server folder so it is shared across all characters on a server.</summary>
            protected override SettingsScope Scope => SettingsScope.Server;

            protected override string FileName => ScavengerFileName;

            protected override JsonTypeInfo<ScavengerData> TypeInfo => ScavengerJsonContext.Default.ScavengerData;
        }

        public class ScavengerEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            [JsonConverter(typeof(RawStringConverter))]
            public string RegexSearch { get; set; } = string.Empty;
            public uint DestinationContainer { get; set; } = 0;
            public ScavengerPriority Priority { get; set; } = ScavengerPriority.Normal;
            /// <summary>
            /// Maximum number of matching items to keep in the destination container when scavenging
            /// this entry. Counts items already in the destination. 0 = no limit (pick up all).
            /// </summary>
            public int MaxAmount { get; set; } = 0;
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

            public bool Equals(ScavengerEntry other) => other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
        }
    }
}
