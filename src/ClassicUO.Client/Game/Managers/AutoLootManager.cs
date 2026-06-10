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
using ClassicUO.Common;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(AutoLootManager.AutoLootConfigEntry))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootConfigEntry>))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootPriority))]
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
        private static readonly PriorityQueue<(uint item, AutoLootConfigEntry entry), AutoLootPriority> _lootItems = new ();
        private List<AutoLootConfigEntry> _autoLootItems = new ();
        private bool _loaded = false;
        private readonly string _savePath;
        private long _nextLootTime = Time.Ticks;
        private long _nextClearRecents = Time.Ticks + 5000;
        private ProgressBarGump _progressBarGump;
        private int _currentLootTotalCount = 0;
        private bool IsEnabled => ProfileManager.CurrentProfile.EnableAutoLoot;

        // Pre/post loot action state, all keyed by corpse serial. Per-corpse lifecycle:
        //   pre-action fires → [wait for target] → [delay] → loot items → post-action.
        // The two "fired" sets gate one-shot actions and are cleared on the periodic
        // cleanup (see Update) or by ClearActiveLootQueue. The pending/count maps drain
        // themselves as corpses advance through the stages.
        private readonly HashSet<uint> _preActionTriggeredCorpses = new();    // pre-action already fired
        private readonly HashSet<uint> _postActionFiredCorpses = new();       // post-action already fired
        private readonly Dictionary<uint, long> _corpsesPendingTarget = new();// corpse → tick action fired, awaiting target consumption
        private readonly Dictionary<uint, long> _corpsesPendingLoot = new();  // corpse → tick when looting may begin
        private readonly Dictionary<uint, int> _pendingLootByCorpse = new();  // corpse → items still queued
        private readonly Dictionary<uint, uint> _itemToCorpseSerial = new();  // queued item → its corpse
        private const long TARGET_WAIT_TIMEOUT_MS = 5000;

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

        public void LootItem(Item item, AutoLootConfigEntry entry = null, AutoLootPriority priority = AutoLootPriority.Normal)
        {
            if (item == null || !_recentlyLooted.Add(item.Serial) || !_quickContainsLookup.Add(item.Serial)) return;

            if (entry != null)
                priority = entry.Priority;
            _lootItems.Enqueue((item, entry), priority);
            _currentLootTotalCount++;
            _nextClearRecents = Time.Ticks + 5000;

            // Track which corpse this item belongs to (skip if it's a distance-retry re-queue)
            if (!_itemToCorpseSerial.ContainsKey(item.Serial) && _world.Items.Get(item.RootContainer) is { IsCorpse: true } root)
            {
                _pendingLootByCorpse.TryGetValue(root.Serial, out int c);
                _pendingLootByCorpse[root.Serial] = c + 1;
                _itemToCorpseSerial[item.Serial] = root.Serial;
            }
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
        /// Note: This method doesn't gurantee to process all itmes in the corpse,
        /// because `corpse.Items` is populated via `AddItemToContainer` packet, thus
        /// it may not have all items yet when the method is called.
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

            Profile p = ProfileManager.CurrentProfile;
            LootActionType preType = (LootActionType)p.AutoLootPreActionType;

            if (preType != LootActionType.None)
            {
                if (!_preActionTriggeredCorpses.Add(corpse.Serial)) return;

                // Register auto-target BEFORE executing the action so the target info is
                // set before the server's target cursor packet can arrive.
                if (p.AutoLootPreActionTargetCorpse)
                    TargetManager.SetAutoTarget(corpse.Serial, TargetType.Neutral);

                ExecuteLootAction(preType, p.AutoLootPreActionText);

                if (p.AutoLootPreActionTargetCorpse)
                    _corpsesPendingTarget[corpse.Serial] = Time.Ticks;
                else
                    _corpsesPendingLoot[corpse.Serial] = Time.Ticks + p.AutoLootPreActionDelayMs;

                return;
            }

            LootCorpseItems(corpse);
        }

        private void LootCorpseItems(Item corpse)
        {
            for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                CheckAndLoot((Item)i);

            if (ProfileManager.CurrentProfile.HueCorpseAfterAutoloot)
                corpse.Hue = 73;

            // If nothing was queued for this corpse, fire the post-action immediately
            if (!_pendingLootByCorpse.ContainsKey(corpse.Serial))
                FirePostActionForCorpse(corpse.Serial);
        }

        private void FirePostActionForCorpse(uint corpseSerial)
        {
            if (!_postActionFiredCorpses.Add(corpseSerial)) return;
            Profile p = ProfileManager.CurrentProfile;
            LootActionType postType = (LootActionType)p.AutoLootPostActionType;
            if (postType == LootActionType.None) return;
            if (p.AutoLootPostActionTargetCorpse && corpseSerial != 0)
                TargetManager.SetAutoTarget(corpseSerial, TargetType.Neutral);
            ExecuteLootAction(postType, p.AutoLootPostActionText);
        }

        private void OnItemLootCompleted(uint itemSerial)
        {
            if (!_itemToCorpseSerial.TryGetValue(itemSerial, out uint corpseSerial)) return;
            _itemToCorpseSerial.Remove(itemSerial);
            if (!_pendingLootByCorpse.TryGetValue(corpseSerial, out int remaining)) return;
            remaining--;
            if (remaining <= 0)
            {
                _pendingLootByCorpse.Remove(corpseSerial);
                FirePostActionForCorpse(corpseSerial);
            }
            else
                _pendingLootByCorpse[corpseSerial] = remaining;
        }

        private void ExecuteLootAction(LootActionType type, string text)
        {
            if (type == LootActionType.None || string.IsNullOrWhiteSpace(text)) return;
            switch (type)
            {
                case LootActionType.Say:
                    GameActions.Say(text);
                    break;
                case LootActionType.Macro:
                    Macro macro = _world.Macros.FindMacro(text);
                    if (macro?.Items is MacroObject macroObj)
                        _world.Macros.SetMacroToExecute(macroObj);
                    break;
            }
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

            // Advance corpses from "waiting for target" → "delay countdown".
            // NextAutoTarget is a single global slot, so once it clears every
            // corpse waiting on a target advances together.
            if (_corpsesPendingTarget.Count > 0)
            {
                long now = Time.Ticks;
                bool targetConsumed = !TargetManager.NextAutoTarget.IsSet;
                List<uint> targetReady = null;
                foreach (KeyValuePair<uint, long> kv in _corpsesPendingTarget)
                    if (targetConsumed || now - kv.Value >= TARGET_WAIT_TIMEOUT_MS)
                        (targetReady ??= new List<uint>()).Add(kv.Key);

                if (targetReady != null)
                    foreach (uint serial in targetReady)
                    {
                        _corpsesPendingTarget.Remove(serial);
                        _corpsesPendingLoot[serial] = now + ProfileManager.CurrentProfile.AutoLootPreActionDelayMs;
                    }
            }

            // Loot corpses whose pre-action delay has expired
            if (_corpsesPendingLoot.Count > 0)
            {
                long now = Time.Ticks;
                List<uint> lootReady = null;
                foreach (KeyValuePair<uint, long> kv in _corpsesPendingLoot)
                    if (now >= kv.Value)
                        (lootReady ??= new List<uint>()).Add(kv.Key);

                if (lootReady != null)
                    foreach (uint serial in lootReady)
                    {
                        _corpsesPendingLoot.Remove(serial);
                        Item corpse = _world.Items.Get(serial);
                        if (corpse != null) LootCorpseItems(corpse);
                    }
            }

            if (_lootItems.Count == 0)
            {
                _progressBarGump?.Dispose();
                if (Time.Ticks > _nextClearRecents)
                {
                    _recentlyLooted.Clear();
                    _preActionTriggeredCorpses.Clear();
                    _postActionFiredCorpses.Clear();
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
            {
                OnItemLootCompleted(item);
                return;
            }

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

            OnItemLootCompleted(item);
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

        public void ClearActiveLootQueue()
        {
            while (_lootItems.TryDequeue(out _, out _));
            _currentLootTotalCount = 0;
            _quickContainsLookup.Clear();
            _progressBarGump?.Dispose();
            _progressBarGump = null;
            _corpsesPendingTarget.Clear();
            _corpsesPendingLoot.Clear();
            _preActionTriggeredCorpses.Clear();
            _postActionFiredCorpses.Clear();
            _pendingLootByCorpse.Clear();
            _itemToCorpseSerial.Clear();
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
                return JsonSerializer.Serialize(_autoLootItems, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
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
        public enum LootActionType { None = 0, Say = 1, Macro = 2 }

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
