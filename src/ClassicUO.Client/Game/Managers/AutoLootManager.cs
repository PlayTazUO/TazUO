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
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Game.Managers.Structs;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(AutoLootManager.AutoLootConfigEntry))]
    [JsonSerializable(typeof(List<AutoLootManager.AutoLootConfigEntry>))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootPriority))]
    [JsonSerializable(typeof(AutoLootManager.AutoLootProfile))]
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
        public List<AutoLootConfigEntry> AutoLootList { get => _mergedEntries; }

        private const int SCAVENGER_TRACKING_RADIUS = 20;

        private readonly HashSet<uint> _quickContainsLookup = new ();
        private readonly HashSet<uint> _recentlyLooted = new();
        internal readonly HashSet<uint> _nearbyGroundItems = new();
        private uint[] _nearbyItemsSnapshot = Array.Empty<uint>();
        private readonly Dictionary<uint, long> _lootCooldowns = new();
        private static readonly PriorityQueue<(uint item, AutoLootConfigEntry entry), int> _lootItems = new ();
        internal volatile List<AutoLootConfigEntry> _mergedEntries = new ();
        private volatile int _activeProfileCount = 0;
        internal Dictionary<int, List<AutoLootConfigEntry>> _graphicIndex = new();
        internal List<AutoLootConfigEntry> _wildcardEntries = new();
        internal readonly Dictionary<uint, AutoLootConfigEntry> _matchCache = new();
        internal readonly HashSet<uint> _matchCacheHasOpl = new();
        internal volatile bool _loaded = false;
        public bool Loaded => _loaded;
        private readonly string _savePath;
        private readonly string _profilesDir;
        private readonly string _exclusionsPath;
        private string _migrationSourcePath;
        public bool NeedsMigration => _migrationSourcePath != null;
        public string MigrationSourcePath => _migrationSourcePath;

        // Global exclusion list — items matching these entries are never looted/scavenged
        public List<AutoLootConfigEntry> ExclusionList { get; private set; } = new();
        internal Dictionary<int, List<AutoLootConfigEntry>> _exclusionGraphicIndex = new();
        internal List<AutoLootConfigEntry> _exclusionWildcardEntries = new();

        public List<AutoLootProfile> Profiles { get; set; } = new();
        public AutoLootProfile SelectedProfile { get; set; }
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
            _profilesDir = Path.Combine(ProfileManager.ProfilePath, "AutoLootProfiles");
            _exclusionsPath = Path.Combine(ProfileManager.ProfilePath, "AutoLootExclusions.json");
        }

        /// <summary>
        /// Internal constructor for unit testing. Does not initialize world or save path.
        /// </summary>
        internal AutoLootManager(bool testMode)
        {
        }

        /// <summary>
        /// Internal constructor for unit testing with a World instance.
        /// </summary>
        internal AutoLootManager(World world)
        {
            _world = world;
        }

        public void NotifyEntryChanged() => RebuildMergedList();

        /// <summary>
        /// Lighter-weight notification for changes that don't affect graphic bucketing
        /// (e.g. hue or regex changes). Only clears the match cache so items are re-evaluated.
        /// </summary>
        public void NotifyMatchCriteriaChanged() => ClearMatchCache();

        public bool IsBeingLooted(uint serial) => _quickContainsLookup.Contains(serial);

        public void LootItem(uint serial)
        {
            Item item = _world.Items.Get(serial);
            if (item != null) LootItem(item, null);
        }

        public void LootItem(Item item, AutoLootConfigEntry entry = null)
        {
            if (item == null || !_recentlyLooted.Add(item.Serial) || !_quickContainsLookup.Add(item.Serial)) return;

            // Skip corpse/container items that are on cooldown from a previous loot attempt.
            // Ground items are excluded — their retry cadence is already gated by position changes.
            if (!item.OnGround && _lootCooldowns.TryGetValue(item.Serial, out long cooldownUntil) && Time.Ticks < cooldownUntil)
            {
                _recentlyLooted.Remove(item.Serial);
                _quickContainsLookup.Remove(item.Serial);
                return;
            }

            int pri = entry != null ? -(int)entry.Priority : -(int)AutoLootPriority.Normal;
            // Corpse items get a priority boost so they're dequeued before ground items
            // of the same AutoLootPriority. This is handled here (not in the ObjectActionQueue)
            // to avoid OAQ priority starvation where ground items would never be processed
            // while corpse items keep flowing in.
            if (!item.OnGround) pri -= 100;
            _lootItems.Enqueue((item.Serial, entry), pri);
            _currentLootTotalCount++;
        }

        public void ForceLootContainer(uint serial)
        {
            Item cont = _world.Items.Get(serial);

            if (cont == null) return;

            for (LinkedObject i = cont.Items; i != null; i = i.Next) CheckAndLoot((Item)i);
        }

        internal static bool IsTrackableGroundItem(Item item)
        {
            return item != null && item.OnGround && !item.IsCorpse && !item.IsLocked;
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
        /// Uses graphic index for O(1) lookup by graphic ID, with match cache for repeated checks.
        /// Multiple active profiles: returns the match with highest Priority.
        /// </summary>
        /// <param name="i">The item to check the loot list against</param>
        /// <returns>The matched AutoLootConfigEntry, or null if no match found</returns>
        internal AutoLootConfigEntry IsOnLootList(Item i)
        {
            if (!_loaded) return null;

            // Check global exclusion list before profile matching
            if (IsExcluded(i)) return null;

            bool oplChecked = false;
            bool oplAvailable = false;

            // Check cache first
            if (_matchCache.TryGetValue(i.Serial, out AutoLootConfigEntry cached))
            {
                if (cached != null)
                    return cached; // Positive cache hit — already matched

                // Negative cache hit — check for stale OPL state
                oplAvailable = _world.OPL.TryGetNameAndData(i.Serial, out _, out _);
                oplChecked = true;
                bool hadOplWhenCached = _matchCacheHasOpl.Contains(i.Serial);

                if (!oplAvailable || hadOplWhenCached)
                    return null; // Valid negative cache

                // OPL is now available but wasn't when we cached — stale, fall through to re-evaluate
            }

            // Cache miss or stale negative cache — run matching logic
            if (!oplChecked)
                oplAvailable = _world.OPL.TryGetNameAndData(i.Serial, out _, out _);
            AutoLootConfigEntry result = null;

            bool isGroundItem = i.OnGround;

            if (_graphicIndex.TryGetValue(i.Graphic, out List<AutoLootConfigEntry> entries))
                foreach (AutoLootConfigEntry entry in entries)
                {
                    if (isGroundItem && !entry.Scavenge) continue;
                    if (entry.Match(i))
                    {
                        if (result == null || entry.Priority > result.Priority)
                            result = entry;
                        if (result.Priority == AutoLootPriority.High)
                            break;
                    }
                }

            if (result == null || result.Priority != AutoLootPriority.High)
                foreach (AutoLootConfigEntry entry in _wildcardEntries)
                {
                    if (isGroundItem && !entry.Scavenge) continue;
                    if (entry.Match(i))
                    {
                        if (result == null || entry.Priority > result.Priority)
                            result = entry;
                        if (result.Priority == AutoLootPriority.High)
                            break;
                    }
                }

            // Store result in cache
            _matchCache[i.Serial] = result;

            if (oplAvailable)
                _matchCacheHasOpl.Add(i.Serial);
            else
                _matchCacheHasOpl.Remove(i.Serial);

            return result;
        }

        /// <summary>
        /// Add an entry for auto looting to match against when opening corpses.
        /// Routes to SelectedProfile's entry list.
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="hue"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AutoLootConfigEntry AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            AutoLootProfile profile = EnsureSelectedProfile();
            if (profile == null)
                return null;

            var item = new AutoLootConfigEntry() { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry entry in profile.Entries)
                if (entry.Equals(item))
                    return entry;

            profile.Entries.Add(item);
            RebuildMergedList();
            SaveProfile(profile);

            return item;
        }

        private AutoLootProfile EnsureSelectedProfile()
        {
            if (SelectedProfile != null)
                return SelectedProfile;

            if (!_loaded)
            {
                Log.Warn("AddAutoLootEntry called before profiles are loaded");
                return null;
            }

            if (Profiles.Count > 0)
            {
                SelectedProfile = Profiles[0];
                return SelectedProfile;
            }

            SelectedProfile = CreateProfile("Default");
            return SelectedProfile;
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

            for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                CheckAndLoot((Item)i);

            if(ProfileManager.CurrentProfile.HueCorpseAfterAutoloot)
                corpse.Hue = 73;
        }

        public void TryRemoveAutoLootEntry(string uid)
        {
            if (!_loaded)
                return;

            AutoLootProfile profile = SelectedProfile;
            if (profile == null)
                return;

            int removeAt = -1;

            for (int i = 0; i < profile.Entries.Count; i++)
            {
                if (profile.Entries[i].Uid == uid)
                {
                    removeAt = i;
                    break;
                }
            }

            if (removeAt > -1)
            {
                profile.Entries.RemoveAt(removeAt);
                RebuildMergedList();
                SaveProfile(profile);
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
            LoadProfiles();
            LoadExclusions();
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
            _nearbyGroundItems.Clear();
            _lootCooldowns.Clear();
            ClearMatchCache();
            SaveAll();
            SaveExclusions();
            Instance = null;
        }

        internal void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!_loaded) return;

            if (ProfileManager.CurrentProfile.EnableScavenger)
            {
                if (_nearbyGroundItems.Count == 0)
                {
                    BootstrapNearbyGroundItems();
                    return;
                }

                List<uint> toRemove = null;
                List<Item> toLoot = null;

                // Snapshot to avoid collection-modified-during-enumeration if CheckAndLoot
                // triggers events that call OnItemCreatedOrUpdated (which modifies _nearbyGroundItems).
                int count = _nearbyGroundItems.Count;
                if (_nearbyItemsSnapshot.Length < count)
                    _nearbyItemsSnapshot = new uint[count];
                _nearbyGroundItems.CopyTo(_nearbyItemsSnapshot);

                for (int idx = 0; idx < count; idx++)
                {
                    uint serial = _nearbyItemsSnapshot[idx];
                    Item item = _world.Items.Get(serial);

                    if (item == null || !item.OnGround || item.IsLocked || item.IsCorpse)
                    {
                        (toRemove ??= new List<uint>()).Add(serial);
                        continue;
                    }

                    if (item.Distance >= SCAVENGER_TRACKING_RADIUS)
                    {
                        (toRemove ??= new List<uint>()).Add(serial);
                        continue;
                    }

                    if (item.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                        (toLoot ??= new List<Item>()).Add(item);
                }

                if (toRemove != null)
                    foreach (uint serial in toRemove)
                        _nearbyGroundItems.Remove(serial);

                // Loot after iteration is complete to avoid re-entrancy issues
                if (toLoot != null)
                    foreach (Item item in toLoot)
                        CheckAndLoot(item);
            }

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
            if (!_loaded) return;

            if (sender is Item i)
            {
                _matchCache.Remove(i.Serial);
                _matchCacheHasOpl.Remove(i.Serial);

                if (IsEnabled)
                    CheckCorpse(i);

                // Maintain spatial tracking set for scavenger and attempt immediate pickup.
                if (ProfileManager.CurrentProfile.EnableScavenger)
                {
                    if (IsTrackableGroundItem(i) && i.Distance <= SCAVENGER_TRACKING_RADIUS)
                    {
                        _nearbyGroundItems.Add(i.Serial);

                        // Attempt immediate scavenge for items within pickup range
                        if (i.Distance <= ProfileManager.CurrentProfile.AutoOpenCorpseRange)
                            CheckAndLoot(i);
                    }
                    else
                    {
                        _nearbyGroundItems.Remove(i.Serial);
                    }
                }
            }
        }

        internal void BootstrapNearbyGroundItems()
        {
            _nearbyGroundItems.Clear();

            foreach (Item item in _world.Items.Values)
            {
                if (IsTrackableGroundItem(item) && item.Distance <= SCAVENGER_TRACKING_RADIUS)
                    _nearbyGroundItems.Add(item.Serial);
            }
        }

        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!_loaded || !IsEnabled) return;
            _matchCache.Remove(e.Serial);
            _matchCacheHasOpl.Remove(e.Serial);
            Item item = _world.Items.Get(e.Serial);
            if (item != null)
                CheckCorpse(item);
        }

        public void Update()
        {
            if (!_loaded || (!IsEnabled && !ProfileManager.CurrentProfile.EnableScavenger) || !_world.InGame) return;

            if (_nextLootTime > Time.Ticks) return;

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                return; //Prevent moving stuff while holding an item.

            // Periodically clear _recentlyLooted so ground items can be retried
            // even while the queue is active (e.g., corpse looting in progress).
            if (Time.Ticks > _nextClearRecents)
            {
                _recentlyLooted.Clear();
                ClearMatchCache();
                _nextClearRecents = Time.Ticks + 5000;

                // Clean up expired cooldowns and cooldowns for items that no longer exist
                List<uint> expiredCooldowns = null;
                foreach (var kvp in _lootCooldowns)
                {
                    if (Time.Ticks >= kvp.Value || _world.Items.Get(kvp.Key) == null)
                        (expiredCooldowns ??= new List<uint>()).Add(kvp.Key);
                }

                if (expiredCooldowns != null)
                    foreach (uint serial in expiredCooldowns)
                        _lootCooldowns.Remove(serial);
            }

            if (_lootItems.Count == 0)
            {
                _progressBarGump?.Dispose();
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

            //If the entry's profile has a default container, use it
            if (destinationSerial == 0 && entry != null && entry.ProfileDestinationContainer != 0)
            {
                Item profileDestContainer = _world.Items.Get(entry.ProfileDestinationContainer);
                if (profileDestContainer != null) destinationSerial = entry.ProfileDestinationContainer;
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

                // Set a 30-second cooldown for corpse/container items so if the move fails, we don't spam retry.
                // Ground items don't need this — their retry is already gated by position changes.
                if (!moveItem.OnGround)
                    _lootCooldowns[moveItem.Serial] = Time.Ticks + 30000;
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
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150,
                    ForegrouneColor = Color.DarkOrange
                };
                _progressBarGump.CenterXInViewPort();
                UIManager.Add(_progressBarGump);
            }
        }

        private void LoadProfiles()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Directory.CreateDirectory(_profilesDir);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create auto loot profiles directory: {e.Message}");
                    return;
                }

                // Migration state detection: check conditions in priority order
                string[] files;
                try
                {
                    files = Directory.GetFiles(_profilesDir, "*.json");
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to scan auto loot profiles directory: {e.Message}");
                    return;
                }

                if (files.Length > 0)
                {
                    // State 1: Directory has .json files — normal load, no migration
                    Log.Info("AutoLoot profiles directory found with existing profiles, loading normally.");
                }
                else
                {
                    // Directory is empty (or only has .backup files).
                    // Check for legacy AutoLoot.json files to migrate.
                    string ancientPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");

                    if (File.Exists(_savePath))
                    {
                        // State 3: AutoLoot.json at ProfileManager.ProfilePath — flag for migration
                        _migrationSourcePath = _savePath;
                        Log.Info($"Legacy AutoLoot.json found at profile path, flagged for migration: {_savePath}");

                        if (File.Exists(ancientPath))
                            Log.Warn($"Legacy AutoLoot.json also exists at ancient path ({ancientPath}), using profile path version.");
                    }
                    else if (File.Exists(ancientPath))
                    {
                        // State 4: AutoLoot.json at ancient executable path — READ only, flag for migration
                        _migrationSourcePath = ancientPath;
                        Log.Info($"Legacy AutoLoot.json found at ancient path, flagged for migration: {ancientPath}");
                    }
                    else
                    {
                        // State 2/5: No legacy files exist — create empty Default profile
                        Log.Info("No existing auto loot data found, creating Default profile.");
                        var defaultProfile = CreateDefaultProfile();
                        SaveProfile(defaultProfile);
                        files = new[] { Path.Combine(_profilesDir, defaultProfile.FileName) };
                    }
                }

                // Perform migration if a legacy file was detected
                if (_migrationSourcePath != null)
                {
                    files = MigrateToDefaultProfile(files);
                }
                var loadedProfiles = new List<AutoLootProfile>();

                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        AutoLootProfile profile = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootProfile);

                        if (profile == null)
                        {
                            Log.Error($"Failed to deserialize auto loot profile: {file}");
                            continue;
                        }

                        profile.FileName = Path.GetFileName(file);
                        loadedProfiles.Add(profile);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error loading auto loot profile '{file}': {e.Message}");
                    }
                }

                loadedProfiles.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
                Profiles = loadedProfiles;
                // Build merged list before marking as loaded so other threads
                // see a populated _mergedEntries when they check _loaded.
                var newList = new List<AutoLootConfigEntry>();
                int activeCount = 0;
                foreach (AutoLootProfile profile in loadedProfiles)
                {
                    if (profile.IsActive)
                    {
                        newList.AddRange(profile.Entries);
                        activeCount++;
                    }
                }
                _activeProfileCount = activeCount;
                _mergedEntries = newList;
                RebuildGraphicIndex();
                _loaded = true;
            });
        }

        private AutoLootProfile CreateDefaultProfile()
        {
            var profile = new AutoLootProfile
            {
                Name = "Default",
                IsActive = true,
                Entries = new List<AutoLootConfigEntry>(),
                FileName = "Default.json"
            };
            return profile;
        }

        /// <summary>
        /// Migrates entries from a legacy AutoLoot.json into a new Default profile.
        /// Reads the legacy file via JsonHelper.Load (which tries backup files too),
        /// creates a Default profile with those entries, and saves it to the profiles directory.
        /// The original legacy file is left untouched.
        /// </summary>
        /// <param name="currentFiles">The current array of .json files in the profiles directory</param>
        /// <returns>Updated files array including the newly created Default.json</returns>
        private string[] MigrateToDefaultProfile(string[] currentFiles)
        {
            List<AutoLootConfigEntry> legacyEntries = null;

            try
            {
                if (JsonHelper.Load(_migrationSourcePath, AutoLootJsonContext.Default.ListAutoLootConfigEntry, out legacyEntries))
                {
                    legacyEntries ??= new List<AutoLootConfigEntry>();
                }
                else
                {
                    Log.Error($"Failed to load legacy AutoLoot.json from '{_migrationSourcePath}', creating empty Default profile.");
                    legacyEntries = new List<AutoLootConfigEntry>();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error reading legacy AutoLoot.json from '{_migrationSourcePath}': {e.Message}");
                legacyEntries = new List<AutoLootConfigEntry>();
            }

            var defaultProfile = new AutoLootProfile
            {
                Name = "Default",
                IsActive = true,
                Entries = legacyEntries,
                FileName = "Default.json"
            };

            SaveProfile(defaultProfile);

            int entryCount = legacyEntries.Count;
            Log.Info($"Migrated {entryCount} entries from AutoLoot.json to Default profile.");
            GameActions.Print($"Migrated {entryCount} auto loot entries to Default profile.", 0x48);

            _migrationSourcePath = null;

            // currentFiles is always empty during migration (states 3/4 only trigger
            // when no .json files exist in the profiles dir), so just return the new file
            return new[] { Path.Combine(_profilesDir, defaultProfile.FileName) };
        }

        public void SaveProfile(AutoLootProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.FileName))
            {
                Log.Error($"Cannot save profile: {(profile == null ? "null profile" : "empty FileName")}");
                return;
            }

            try
            {
                if (!Directory.Exists(_profilesDir))
                    Directory.CreateDirectory(_profilesDir);

                string fullPath = Path.Combine(_profilesDir, profile.FileName);
                JsonHelper.SaveAndBackup(profile, fullPath, AutoLootJsonContext.Default.AutoLootProfile);
            }
            catch (Exception e)
            {
                Log.Error($"Error saving profile '{profile.Name}': {e.Message}");
            }
        }

        /// <summary>
        /// Rebuilds the merged entry list from all active profiles, then rebuilds
        /// the graphic index and clears the match cache.
        /// Uses atomic reference swap to prevent ConcurrentModificationException
        /// when IsOnLootList iterates on the network thread while the UI triggers a rebuild.
        /// </summary>
        public void RebuildMergedList()
        {
            if (!_loaded)
                return;

            var newList = new List<AutoLootConfigEntry>();
            int activeCount = 0;

            foreach (AutoLootProfile profile in Profiles)
            {
                if (profile.IsActive)
                {
                    foreach (var entry in profile.Entries)
                        entry.ProfileDestinationContainer = profile.DestinationContainer;
                    newList.AddRange(profile.Entries);
                    activeCount++;
                }
            }

            _activeProfileCount = activeCount;
            _mergedEntries = newList;
            RebuildGraphicIndex();
        }

        internal void ClearMatchCache()
        {
            _matchCache.Clear();
            _matchCacheHasOpl.Clear();
        }

        public void RebuildGraphicIndex()
        {
            _graphicIndex.Clear();
            _wildcardEntries.Clear();
            ClearMatchCache();

            foreach (AutoLootConfigEntry entry in _mergedEntries)
            {
                if (entry.Graphic == -1)
                {
                    _wildcardEntries.Add(entry);
                }
                else
                {
                    if (!_graphicIndex.TryGetValue(entry.Graphic, out List<AutoLootConfigEntry> bucket))
                    {
                        bucket = new List<AutoLootConfigEntry>();
                        _graphicIndex[entry.Graphic] = bucket;
                    }
                    bucket.Add(entry);
                }
            }
        }

        /// <summary>
        /// Check if an item is on the global exclusion list.
        /// Uses the exclusion graphic index for O(1) lookup.
        /// </summary>
        internal bool IsExcluded(Item i)
        {
            if (ExclusionList.Count == 0) return false;

            if (_exclusionGraphicIndex.TryGetValue(i.Graphic, out List<AutoLootConfigEntry> entries))
                foreach (AutoLootConfigEntry entry in entries)
                    if (entry.Match(i))
                        return true;

            foreach (AutoLootConfigEntry entry in _exclusionWildcardEntries)
                if (entry.Match(i))
                    return true;

            return false;
        }

        public AutoLootConfigEntry AddExclusionEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            var entry = new AutoLootConfigEntry { Graphic = graphic, Hue = hue, Name = name };

            foreach (AutoLootConfigEntry existing in ExclusionList)
                if (existing.Equals(entry))
                    return existing;

            ExclusionList.Add(entry);
            RebuildExclusionIndex();
            ClearMatchCache();
            SaveExclusions();
            return entry;
        }

        public void RemoveExclusionEntry(string uid)
        {
            for (int i = 0; i < ExclusionList.Count; i++)
            {
                if (ExclusionList[i].Uid == uid)
                {
                    ExclusionList.RemoveAt(i);
                    RebuildExclusionIndex();
                    ClearMatchCache();
                    SaveExclusions();
                    return;
                }
            }
        }

        internal void RebuildExclusionIndex()
        {
            _exclusionGraphicIndex.Clear();
            _exclusionWildcardEntries.Clear();

            foreach (AutoLootConfigEntry entry in ExclusionList)
            {
                if (entry.Graphic == -1)
                {
                    _exclusionWildcardEntries.Add(entry);
                }
                else
                {
                    if (!_exclusionGraphicIndex.TryGetValue(entry.Graphic, out List<AutoLootConfigEntry> bucket))
                    {
                        bucket = new List<AutoLootConfigEntry>();
                        _exclusionGraphicIndex[entry.Graphic] = bucket;
                    }
                    bucket.Add(entry);
                }
            }
        }

        private void LoadExclusions()
        {
            try
            {
                if (JsonHelper.Load(_exclusionsPath, AutoLootJsonContext.Default.ListAutoLootConfigEntry, out List<AutoLootConfigEntry> entries))
                {
                    ExclusionList = entries ?? new List<AutoLootConfigEntry>();
                }
                else
                {
                    ExclusionList = new List<AutoLootConfigEntry>();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error loading auto loot exclusions: {e.Message}");
                ExclusionList = new List<AutoLootConfigEntry>();
            }

            RebuildExclusionIndex();
        }

        private void SaveExclusions()
        {
            try
            {
                JsonHelper.SaveAndBackup(ExclusionList, _exclusionsPath, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
            }
            catch (Exception e)
            {
                Log.Error($"Error saving auto loot exclusions: {e.Message}");
            }
        }

        public void MoveEntryToProfile(AutoLootConfigEntry entry, AutoLootProfile fromProfile, AutoLootProfile toProfile)
        {
            if (entry == null || fromProfile == null || toProfile == null || fromProfile == toProfile)
                return;

            fromProfile.Entries.Remove(entry);
            toProfile.Entries.Add(entry);
            RebuildMergedList();
            SaveProfile(fromProfile);
            SaveProfile(toProfile);
        }

        public void CopyEntryToProfile(AutoLootConfigEntry entry, AutoLootProfile toProfile)
        {
            if (entry == null || toProfile == null)
                return;

            var copy = new AutoLootConfigEntry
            {
                Graphic = entry.Graphic,
                Hue = entry.Hue,
                Name = entry.Name,
                RegexSearch = entry.RegexSearch,
                Priority = entry.Priority,
                Scavenge = entry.Scavenge,
                DestinationContainer = entry.DestinationContainer
            };

            toProfile.Entries.Add(copy);
            RebuildMergedList();
            SaveProfile(toProfile);
        }

        public void ReorderProfile(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0
                || fromIndex >= Profiles.Count || toIndex >= Profiles.Count)
                return;

            AutoLootProfile profile = Profiles[fromIndex];
            Profiles.RemoveAt(fromIndex);
            Profiles.Insert(toIndex, profile);

            for (int i = 0; i < Profiles.Count; i++)
                Profiles[i].DisplayOrder = i;

            SaveAll();
        }

        public void SaveAll()
        {
            if (!_loaded)
                return;

            foreach (AutoLootProfile profile in Profiles)
            {
                SaveProfile(profile);
            }
        }

        public AutoLootProfile CreateProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "New Profile";

            string uniqueName = GetUniqueName(name);
            string fileName = SanitizeFileName(uniqueName);

            var profile = new AutoLootProfile
            {
                Name = uniqueName,
                IsActive = true,
                Entries = new List<AutoLootConfigEntry>(),
                FileName = fileName,
                DisplayOrder = GetNextDisplayOrder()
            };

            Profiles.Add(profile);
            SaveProfile(profile);

            return profile;
        }

        public void DeleteProfile(AutoLootProfile profile)
        {
            if (profile == null || Profiles.Count <= 1)
                return;

            if (!string.IsNullOrWhiteSpace(profile.FileName))
            {
                string basePath = Path.Combine(_profilesDir, profile.FileName);

                try
                {
                    if (File.Exists(basePath))
                        File.Delete(basePath);

                    for (int i = 1; i <= 3; i++)
                    {
                        string backupPath = basePath + ".backup" + i;
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error deleting profile files for '{profile.Name}': {e.Message}");
                }
            }

            Profiles.Remove(profile);

            if (SelectedProfile == profile)
                SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;

            RebuildMergedList();
        }

        public void RenameProfile(AutoLootProfile profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName))
                return;

            string uniqueName = GetUniqueName(newName, profile);
            string newFileName = SanitizeFileName(uniqueName, profile.FileName);

            // Delete old files from disk
            if (!string.IsNullOrWhiteSpace(profile.FileName))
            {
                string oldBasePath = Path.Combine(_profilesDir, profile.FileName);

                try
                {
                    if (File.Exists(oldBasePath))
                        File.Delete(oldBasePath);

                    for (int i = 1; i <= 3; i++)
                    {
                        string backupPath = oldBasePath + ".backup" + i;
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error deleting old profile files during rename of '{profile.Name}': {e.Message}");
                }
            }

            profile.Name = uniqueName;
            profile.FileName = newFileName;
            SaveProfile(profile);
        }


        public AutoLootProfile ImportFromOtherCharacter(string characterName, List<AutoLootConfigEntry> entries)
        {
            try
            {
                if (entries == null || entries.Count == 0)
                {
                    GameActions.Print($"No autoloot entries found for character: {characterName}", Constants.HUE_ERROR);
                    return null;
                }

                string name = GetUniqueName($"Import - {characterName}");
                var profile = new AutoLootProfile
                {
                    Name = name,
                    IsActive = false,
                    Entries = entries,
                    FileName = SanitizeFileName(name),
                    DisplayOrder = GetNextDisplayOrder()
                };

                Profiles.Add(profile);
                SaveProfile(profile);
                RebuildMergedList();

                GameActions.Print($"Imported {entries.Count} entries from {characterName} into new profile '{name}'.", 0x48);
                return profile;
            }
            catch (Exception e)
            {
                GameActions.Print($"Error importing from other character: {e.Message}", Constants.HUE_ERROR);
                return null;
            }
        }

        private void ImportEntries(List<AutoLootConfigEntry> entries, string source, AutoLootProfile targetProfile = null)
        {
            targetProfile ??= EnsureSelectedProfile();
            if (targetProfile == null)
            {
                GameActions.Print("No profile available to import into.", Constants.HUE_ERROR);
                return;
            }

            var newItems = new List<AutoLootConfigEntry>();
            int duplicateCount = 0;

            foreach (AutoLootConfigEntry importedItem in entries)
            {
                bool isDuplicate = false;
                foreach (AutoLootConfigEntry existingItem in targetProfile.Entries)
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
                targetProfile.Entries.AddRange(newItems);
                RebuildMergedList();
                SaveProfile(targetProfile);
            }

            string message = $"Imported {newItems.Count} new autoloot entries from {source}";
            if (duplicateCount > 0) message += $" ({duplicateCount} duplicates skipped)";
            GameActions.Print(message, 0x48);
        }

        public List<AutoLootConfigEntry> LoadOtherCharacterConfig(string characterPath)
        {
            // Try profile directory first (new format)
            string profilesDir = Path.Combine(characterPath, "AutoLootProfiles");
            if (Directory.Exists(profilesDir))
            {
                var allEntries = new List<AutoLootConfigEntry>();
                try
                {
                    string[] files = Directory.GetFiles(profilesDir, "*.json");
                    foreach (string file in files)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            AutoLootProfile profile = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootProfile);

                            if (profile == null)
                            {
                                Log.Error($"Failed to deserialize auto loot profile: {file}");
                                continue;
                            }

                            if (profile.Entries != null)
                            {
                                allEntries.AddRange(profile.Entries);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Error loading auto loot profile '{file}' from other character: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    GameActions.Print($"Error scanning autoloot profiles from {profilesDir}: {e.Message}", Constants.HUE_ERROR);
                }
                return allEntries;
            }

            // Fall back to legacy AutoLoot.json
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
        public string? GetProfileJsonExport(AutoLootProfile profile)
        {
            if (profile == null)
                return null;

            try
            {
                return JsonSerializer.Serialize(profile, AutoLootJsonContext.Default.AutoLootProfile);
            }
            catch (Exception e)
            {
                Log.Error($"Error exporting autoloot profile to JSON: {e}");
            }

            return null;
        }
        #nullable disable

        public AutoLootProfile ImportProfileFromClipboard(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !_loaded)
                return null;

            // Try new format: full AutoLootProfile
            try
            {
                AutoLootProfile profile = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.AutoLootProfile);
                if (profile != null && profile.Entries != null)
                    return FinalizeImportedProfile(profile);
            }
            catch (Exception e)
            {
                Log.Trace($"Clipboard is not an AutoLootProfile: {e.Message}");
            }

            // Try legacy format: List<AutoLootConfigEntry>
            try
            {
                List<AutoLootConfigEntry> entries = JsonSerializer.Deserialize(json, AutoLootJsonContext.Default.ListAutoLootConfigEntry);
                if (entries != null)
                {
                    var profile = new AutoLootProfile
                    {
                        Name = "Imported",
                        Entries = entries
                    };
                    return FinalizeImportedProfile(profile);
                }
            }
            catch (Exception e)
            {
                Log.Trace($"Clipboard is not a List<AutoLootConfigEntry>: {e.Message}");
            }

            return null;
        }

        private AutoLootProfile FinalizeImportedProfile(AutoLootProfile profile)
        {
            profile.Name = GetUniqueName(string.IsNullOrWhiteSpace(profile.Name) ? "Imported" : profile.Name);
            profile.IsActive = false;
            profile.FileName = SanitizeFileName(profile.Name);
            profile.DisplayOrder = GetNextDisplayOrder();
            Profiles.Add(profile);
            RebuildMergedList();
            SaveProfile(profile);
            return profile;
        }

        private int GetNextDisplayOrder()
        {
            int max = -1;
            foreach (AutoLootProfile p in Profiles)
                if (p.DisplayOrder > max)
                    max = p.DisplayOrder;
            return max + 1;
        }

        public string GetUniqueName(string baseName, AutoLootProfile excludeProfile = null)
        {
            string name = baseName;
            int i = 1;
            while (Profiles.Any(p => p != excludeProfile && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = $"{baseName} ({i++})";
            return name;
        }

        public string SanitizeFileName(string name, string existingFileName = null)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "profile";

            string fileName = sanitized + ".json";

            if (!Directory.Exists(_profilesDir))
                return fileName;

            int counter = 1;
            while (counter < 10000)
            {
                string fullPath = Path.Combine(_profilesDir, fileName);
                if (!File.Exists(fullPath))
                    return fileName;

                if (existingFileName != null && string.Equals(fileName, existingFileName, StringComparison.OrdinalIgnoreCase))
                    return fileName;

                fileName = $"{sanitized}_{counter}.json";
                counter++;
            }

            return $"{sanitized}_{DateTime.Now.Ticks}.json";
        }

        public enum AutoLootPriority { Low = 0, Normal = 1, High = 2 }

        public class AutoLootConfigEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            public uint DestinationContainer { get; set; } = 0;
            [JsonIgnore]
            public uint ProfileDestinationContainer { get; set; } = 0;
            public AutoLootPriority Priority { get; set; } = AutoLootPriority.Normal;
            public bool Scavenge { get; set; } = true;
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
                else if (!Client.UnitTestingActive)
                    search = StringHelper.GetPluralAdjustedString(compareTo.ItemData.Name);

                return RegexHelper.GetRegex(RegexSearch, RegexOptions.Multiline).IsMatch(search);
            }

            public bool Equals(AutoLootConfigEntry other) => other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
        }

        public class AutoLootProfile
        {
            public string Name { get; set; } = "";
            public bool IsActive { get; set; } = true;
            public int DisplayOrder { get; set; } = 0;
            public uint DestinationContainer { get; set; } = 0;
            public List<AutoLootConfigEntry> Entries { get; set; } = new();

            [JsonIgnore]
            public string FileName { get; set; } = "";
        }
    }
}
