using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Managers
{
    internal class AutoLootManager
    {
        public static readonly AutoLootManager Instance = new();
        public bool IsLoaded => loaded;

        public List<AutoLootConfigEntry> AutoLootList
        {
            get => autoLootItems;
            set
            {
                autoLootItems = value ?? new List<AutoLootConfigEntry>();
                _quickMatchCache.Clear();
            }
        }

        private const ushort DBG_COLOR = 0x0044;

        private readonly HashSet<uint> quickContainsLookup = new();
        private static readonly Queue<uint> lootItems = new();
        private List<AutoLootConfigEntry> autoLootItems = new();
        private bool loaded;
        private readonly string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");

        private ProgressBarGump progressBarGump;
        private int currentLootTotalCount;
        private bool IsEnabled => ProfileManager.CurrentProfile.EnableAutoLoot;

        private readonly HashSet<uint> _scanningCorpses = new();
        private readonly HashSet<uint> _openedCorpses = new();
        private readonly HashSet<uint> _openedContainers = new();
        private readonly HashSet<uint> _pendingContainers = new();

        private readonly Dictionary<uint, (int distance, long ts)> _distanceCache = new();

        private readonly Dictionary<uint, int> _openingAttempts = new();
        private readonly Dictionary<(ushort graphic, ushort hue), bool> _quickMatchCache = new();

        private long _lastPruneTicks;

        private AutoLootManager() { }

        private Item GetCorpseAnchor(Item maybeCorpseOrChild)
        {
            if (maybeCorpseOrChild == null) return null;
            if (maybeCorpseOrChild.OnGround && maybeCorpseOrChild.IsCorpse) return maybeCorpseOrChild;
            var rootCorpse = maybeCorpseOrChild.FindRootCorpse();
            if (rootCorpse != null) return rootCorpse;
            return maybeCorpseOrChild;
        }

        private bool InRangeChebyshev(Item target, int range, string _)
        {
            var p = World.Player;
            target = GetCorpseAnchor(target);
            if (p == null || target == null) return false;

            if (target.OnGround && _distanceCache.TryGetValue(target.Serial, out var cached))
            {
                if (Time.Ticks - cached.ts < 500)
                {
                    return cached.distance <= range;
                }
            }

            int dx = Math.Abs((int)p.X - (int)target.X);
            int dy = Math.Abs((int)p.Y - (int)target.Y);
            int chev = Math.Max(dx, dy);

            if (target.OnGround)
                _distanceCache[target.Serial] = (chev, Time.Ticks);

            return chev <= range;
        }

        private bool InRangeAnchor(Item itemOrCorpse, int range, out Item anchor)
        {
            anchor = GetCorpseAnchor(itemOrCorpse);
            return InRangeChebyshev(anchor, range, "anchor");
        }

        public bool IsBeingLooted(uint serial) => quickContainsLookup.Contains(serial);

        public void LootItem(uint serial)
        {
            var item = World.Items.Get(serial);
            if (item != null) LootItem(item);
        }

        public void LootItem(Item item)
        {
            if (item == null) return;
            if (!quickContainsLookup.Add(item.Serial)) return;

            lootItems.Enqueue(item.Serial);
            currentLootTotalCount++;

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        public void ForceLootContainer(uint serial)
        {
            var cont = World.Items.Get(serial);
            if (cont == null) return;

            var anchor = GetCorpseAnchor(cont);

            if (!InRangeChebyshev(anchor, ProfileManager.CurrentProfile.AutoOpenCorpseRange, "ForceLootContainer"))
            {
                return;
            }

            if (anchor.IsCorpse && !_openedCorpses.Contains(anchor.Serial))
            {
                GameActions.DoubleClick(anchor);
                _openedCorpses.Add(anchor.Serial);
                _openingAttempts.Remove(anchor.Serial); // reset attempts on manual open
                return;
            }

            if (anchor.IsCorpse)
            {
                HandleCorpse(anchor);
                return;
            }

            for (LinkedObject n = cont.Items; n != null; n = n.Next)
            {
                var child = (Item)n;
                if (child == null) continue;

                if (child.IsCorpse) HandleCorpse(child);
                else if (IsOnLootList(child)) LootItem(child);
            }

            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void CheckAndLoot(Item i)
        {
            if (!loaded || i == null || quickContainsLookup.Contains(i.Serial)) return;

            if (i.IsCorpse) { HandleCorpse(i); return; }

            var corpse = i.FindRootCorpse();
            if (corpse != null)
            {
                if (_scanningCorpses.Contains(corpse.Serial))
                {
                    if (IsOnLootList(i)) LootItem(i);
                }
                else HandleCorpse(corpse);
                return;
            }

            if (IsOnLootList(i)) LootItem(i);
        }

        private bool IsOnLootList(Item i)
        {
            if (!loaded) return false;

            // Quick check for exact graphic/hue matches
            var key = (i.Graphic, i.Hue);
            if (_quickMatchCache.TryGetValue(key, out bool cached))
                return cached;

            foreach (var entry in autoLootItems)
                if (entry.Match(i))
                {
                    // Cache only non-regex matches
                    if (string.IsNullOrEmpty(entry.RegexSearch))
                        _quickMatchCache[key] = true;
                    return true;
                }

            if (autoLootItems.All(e => string.IsNullOrEmpty(e.RegexSearch)))
                _quickMatchCache[key] = false;

            return false;
        }

        public AutoLootConfigEntry AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            var candidate = new AutoLootConfigEntry { Graphic = graphic, Hue = hue, Name = name };
            var existing = autoLootItems.FirstOrDefault(e => e.Equals(candidate));
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(name) && !string.Equals(existing.Name, name, StringComparison.Ordinal))
                    existing.Name = name;
                return existing;
            }

            autoLootItems.Add(candidate);
            _quickMatchCache.Clear();
            return candidate;
        }

        private void HandleCorpse(Item corpse)
        {
            var anchorCorpse = GetCorpseAnchor(corpse);

            if (anchorCorpse == null || !anchorCorpse.IsCorpse) return;
            if (!InRangeChebyshev(anchorCorpse, ProfileManager.CurrentProfile.AutoOpenCorpseRange, "HandleCorpse")) return;
            if (anchorCorpse.IsHumanCorpse && !ProfileManager.CurrentProfile.AutoLootHumanCorpses) return;
            if (!_scanningCorpses.Add(anchorCorpse.Serial)) return;

            try
            {
                if (!_openedCorpses.Contains(anchorCorpse.Serial))
                {
                    GameActions.DoubleClick(anchorCorpse);
                    _openedCorpses.Add(anchorCorpse.Serial);
                    _openingAttempts.Remove(anchorCorpse.Serial); // reset attempts when we open here
                    return;
                }

                ProcessCorpseContents(anchorCorpse);
            }
            finally
            {
                _scanningCorpses.Remove(anchorCorpse.Serial);
            }
        }

        private void ProcessCorpseContents(Item anchorCorpse)
        {
            var visited = new HashSet<uint>();
            var stack = new Stack<Item>();
            stack.Push(anchorCorpse);

            while (stack.Count > 0)
            {
                var container = stack.Pop();
                if (container == null || !visited.Add(container.Serial)) continue;

                for (LinkedObject node = container.Items; node != null; node = node.Next)
                {
                    var child = (Item)node;
                    if (child == null) continue;

                    if (child.ItemData.IsContainer)
                    {
                        if (!_openedContainers.Contains(child.Serial))
                        {
                            // Queue for opening but continue processing
                            if (!_pendingContainers.Contains(child.Serial))
                            {
                                GameActions.DoubleClick(child);
                                _pendingContainers.Add(child.Serial);
                            }
                            continue;
                        }

                        if (child.Items != null) stack.Push(child);
                        continue;
                    }

                    if (IsOnLootList(child)) LootItem(child);
                }
            }
        }

        public void TryRemoveAutoLootEntry(string UID)
        {
            int ix = autoLootItems.FindIndex(e => e.UID == UID);
            if (ix >= 0)
            {
                autoLootItems.RemoveAt(ix);
                _quickMatchCache.Clear();
            }
        }

        private void CheckCorpse(Item i)
        {
            if (i == null) return;
            if (i.IsCorpse) { HandleCorpse(i); return; }
            var corpse = i.FindRootCorpse();
            if (corpse != null) { HandleCorpse(corpse); return; }
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

            if (progressBarGump != null && !progressBarGump.IsDisposed)
                progressBarGump.Dispose();
            progressBarGump = null;

            _distanceCache.Clear();
            _openingAttempts.Clear();
            _scanningCorpses.Clear();
            _openedCorpses.Clear();
            _openedContainers.Clear();
            _pendingContainers.Clear();
            _quickMatchCache.Clear();
        }

        private void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!loaded) return;
            _distanceCache.Clear();

            if (ProfileManager.CurrentProfile.EnableScavenger)
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || !item.OnGround || item.IsCorpse) continue;
                    if (!InRangeChebyshev(item, 3, "Scavenger")) continue;
                    CheckAndLoot(item);
                }

            PruneContainerSetsIfNeeded();

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void OnOpenContainer(object sender, uint e)
        {
            if (!loaded) return;
            if (!IsEnabled && lootItems.Count == 0)
            {
                if (progressBarGump != null && !progressBarGump.IsDisposed)
                    progressBarGump.Dispose();
                progressBarGump = null;
                return;
            }

            var cont = World.Items.Get(e);
            if (cont == null) return;

            var anchorCorpse = GetCorpseAnchor(cont);

            if (anchorCorpse != null && anchorCorpse.IsCorpse)
            {
                if (anchorCorpse.OnGround) _openedCorpses.Add(anchorCorpse.Serial);
                _openedContainers.Add(cont.Serial);
                _pendingContainers.Remove(cont.Serial);
                _openingAttempts.Remove(anchorCorpse.Serial);
                HandleCorpse(anchorCorpse);
                PruneContainerSetsIfNeeded();
                PumpMoveOnce();
                MoveItemQueue.Instance?.ProcessQueue();
                return;
            }

            _openedContainers.Add(cont.Serial);
            _pendingContainers.Remove(cont.Serial);

            for (LinkedObject n = cont.Items; n != null; n = n.Next)
            {
                var child = (Item)n;
                if (child == null) continue;
                if (IsOnLootList(child)) LootItem(child);
            }

            PruneContainerSetsIfNeeded();
            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            if (sender is Item i)
            {
                CheckCorpse(i);
                PruneContainerSetsIfNeeded();
                PumpMoveOnce();
                MoveItemQueue.Instance?.ProcessQueue();
            }
        }

        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            var item = World.Items.Get(e.Serial);
            if (item != null) CheckCorpse(item);

            PruneContainerSetsIfNeeded();
            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        public void Update()
        {
            PruneContainerSetsIfNeeded();
            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void PumpMoveOnce()
        {
            // Always clean up the bar if the queue is empty, before any early returns.
            if (lootItems.Count == 0)
            {
                if (progressBarGump != null && !progressBarGump.IsDisposed)
                    progressBarGump.Dispose();
                progressBarGump = null;
            }

            if (!loaded || !World.InGame) return;
            if (!IsEnabled && lootItems.Count == 0) return;     // allow queued moves when disabled
            if (lootItems.Count == 0) return;                   // already disposed above
            if (Client.Game.GameCursor.ItemHold.Enabled) return;

            var moveSerial = lootItems.Dequeue();
            if (moveSerial == 0) return;

            if (lootItems.Count == 0) currentLootTotalCount = 0;

            CreateProgressBar();

            if (progressBarGump != null && !progressBarGump.IsDisposed)
                progressBarGump.CurrentPercentage = 1 - ((double)lootItems.Count / Math.Max(1, currentLootTotalCount));

            var m = World.Items.Get(moveSerial);
            if (m == null) return;

            if (!InRangeAnchor(m, ProfileManager.CurrentProfile.AutoOpenCorpseRange, out var anchor))
            {
                lootItems.Enqueue(moveSerial);
                return;
            }

            var corpse = m.FindRootCorpse();
            if (corpse != null && !_openedCorpses.Contains(corpse.Serial))
            {
                if (!_openingAttempts.TryGetValue(corpse.Serial, out var attempts))
                    attempts = 0;

                if (attempts >= 3)
                {
                    _openingAttempts.Remove(corpse.Serial);
                    return;
                }

                GameActions.DoubleClick(corpse);
                _openedCorpses.Add(corpse.Serial);
                _openingAttempts[corpse.Serial] = attempts + 1;
                lootItems.Enqueue(moveSerial);
                return;
            }

            var pack = World.Player?.FindItemByLayer(Layer.Backpack);
            if (pack == null) { lootItems.Enqueue(moveSerial); return; }

            if (m.Container == pack.Serial) { quickContainsLookup.Remove(moveSerial); return; }

            MoveItemQueue.Instance?.EnqueueQuick(m);
            quickContainsLookup.Remove(moveSerial);
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (progressBarGump == null || progressBarGump.IsDisposed))
            {
                progressBarGump = new ProgressBarGump("Auto looting...", 0)
                {
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150,
                    ForegroundColor = Color.DarkOrange
                };
                progressBarGump.CenterXInViewPort();
                UIManager.Add(progressBarGump);
            }
        }

        private void PruneContainerSetsIfNeeded()
        {
            if (Time.Ticks - _lastPruneTicks < 5000) return; // ~5s
            _lastPruneTicks = Time.Ticks;

            bool Missing(uint s) => World.Items.Get(s) == null;

            _openedContainers.RemoveWhere(Missing);
            _pendingContainers.RemoveWhere(Missing);
            _openedCorpses.RemoveWhere(Missing);
        }

        private void Load()
        {
            if (loaded) return;

            try
            {
                if (!File.Exists(savePath))
                {
                    autoLootItems = new List<AutoLootConfigEntry>();
                }
                else
                {
                    string data = File.ReadAllText(savePath);
                    autoLootItems = JsonSerializer.Deserialize<AutoLootConfigEntry[]>(data)?.ToList()
                                    ?? new List<AutoLootConfigEntry>();
                }
                loaded = true;

                _quickMatchCache.Clear();
                _distanceCache.Clear();
            }
            catch
            {
                GameActions.Print("Error loading AutoLoot.json (check JSON).", 32);
                autoLootItems = new List<AutoLootConfigEntry>();
                loaded = true;
                _quickMatchCache.Clear();
                _distanceCache.Clear();
            }
        }

        public void Save()
        {
            if (!loaded) return;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string fileData = JsonSerializer.Serialize(autoLootItems, options);
                File.WriteAllText(savePath, fileData);
            }
            catch
            {
                try { GameActions.Print("AutoLoot: failed to save AutoLoot.json.", 32); } catch { }
            }
        }

        public class AutoLootConfigEntry : IEquatable<AutoLootConfigEntry>
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = -1; // wildcard by default
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            public string UID { get; set; } = Guid.NewGuid().ToString();

            public bool Match(Item compareTo)
            {
                if (Graphic != -1 && Graphic != compareTo.Graphic) return false;
                if (!HueCheck(compareTo.Hue)) return false;
                if (!string.IsNullOrEmpty(RegexSearch) && !RegexCheck(compareTo)) return false;
                return true;
            }

            private bool HueCheck(ushort value)
            {
                if (Hue == ushort.MaxValue) return true;
                if (Hue == value) return true;
                return false;
            }

            private bool RegexCheck(Item compareTo)
            {
                string search;
                if (World.OPL.TryGetNameAndData(compareTo, out string name, out string data)) search = name + data;
                else search = StringHelper.GetPluralAdjustedString(compareTo.ItemData.Name);
                return RegexHelper.GetRegex(RegexSearch, RegexOptions.Multiline).IsMatch(search);
            }

            public override bool Equals(object obj) => Equals(obj as AutoLootConfigEntry);

            public bool Equals(AutoLootConfigEntry other)
                => !(other is null)
                && Graphic == other.Graphic
                && Hue == other.Hue
                && string.Equals(RegexSearch, other.RegexSearch, StringComparison.Ordinal);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + Graphic;
                    hash = hash * 31 + Hue.GetHashCode();
                    hash = hash * 31 + (RegexSearch != null ? StringComparer.Ordinal.GetHashCode(RegexSearch) : 0);
                    return hash;
                }
            }

            public static bool operator ==(AutoLootConfigEntry left, AutoLootConfigEntry right)
                => Equals(left, right);

            public static bool operator !=(AutoLootConfigEntry left, AutoLootConfigEntry right)
                => !Equals(left, right);
        }
    }
}
