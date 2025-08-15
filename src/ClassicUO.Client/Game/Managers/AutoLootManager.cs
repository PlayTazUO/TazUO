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
        public static AutoLootManager Instance { get; private set; } = new();
        public bool IsLoaded => loaded;
        public List<AutoLootConfigEntry> AutoLootList { get => autoLootItems; set => autoLootItems = value; }

        private const bool DEBUG = false;
        private const ushort DBG_COLOR = 0x0044;
        private void Log(string msg)
        {
            if (!DEBUG) return;
            try { GameActions.Print($"[AutoLoot] {msg}", DBG_COLOR); } catch { }
            try { System.Diagnostics.Debug.WriteLine($"[AutoLoot] {msg}"); } catch { }
        }
        private static string Info(Item i)
        {
            if (i == null) return "null";
            return $"ser={i.Serial:X8} gfx={i.Graphic:X4} amt={i.Amount} xy=({i.X},{i.Y}) dist={i.Distance} corpse={(i.IsCorpse ? "Y" : "N")} inside={(i.FindRootCorpse() != null ? "Y" : "N")}";
        }

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

        private AutoLootManager() { }

        private Item GetCorpseAnchor(Item maybeCorpseOrChild)
        {
            if (maybeCorpseOrChild == null) return null;
            if (maybeCorpseOrChild.OnGround && maybeCorpseOrChild.IsCorpse) return maybeCorpseOrChild;
            var rootCorpse = maybeCorpseOrChild.FindRootCorpse();
            if (rootCorpse != null) return rootCorpse;
            return maybeCorpseOrChild;
        }

        private bool InRangeChebyshev(Item target, int range, string tag)
        {
            var p = World.Player;
            target = GetCorpseAnchor(target);
            if (p == null || target == null) return false;
            int dx = Math.Abs((int)p.X - (int)target.X);
            int dy = Math.Abs((int)p.Y - (int)target.Y);
            int chev = Math.Max(dx, dy);
            Log($"Range[{tag}]: player=({p.X},{p.Y}) target=({target.X},{target.Y}) chev={chev} item.dist={target.Distance} range={range}");
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
            else Log($"LootItem(serial): missing {serial:X8}");
        }

        public void LootItem(Item item)
        {
            if (item == null) return;
            if (!quickContainsLookup.Add(item.Serial)) return;

            Log($"Queued {item.Serial:X8} {item.Graphic:X4} x{item.Amount}");
            lootItems.Enqueue(item.Serial);
            currentLootTotalCount++;

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        public void ForceLootContainer(uint serial)
        {
            var cont = World.Items.Get(serial);
            Log($"ForceLootContainer({serial:X8}) cont={Info(cont)}");
            if (cont == null) return;

            var anchor = GetCorpseAnchor(cont);
            Log($"ForceLootContainer: anchor={Info(anchor)}");

            if (!InRangeChebyshev(anchor, ProfileManager.CurrentProfile.AutoOpenCorpseRange, "ForceLootContainer"))
            {
                Log("ForceLootContainer: out of range");
                return;
            }

            if (anchor.IsCorpse && !_openedCorpses.Contains(anchor.Serial))
            {
                Log($"ForceLootContainer: opening corpse {anchor.Serial:X8}");
                GameActions.DoubleClick(anchor);
                _openedCorpses.Add(anchor.Serial);
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
                Log($"ForceLootContainer: child {Info(child)}");
                if (child == null) continue;

                if (child.IsCorpse) HandleCorpse(child);
                else if (IsOnLootList(child)) LootItem(child);
            }

            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void CheckAndLoot(Item i)
        {
            Log($"CheckAndLoot({Info(i)})");
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
            foreach (var entry in autoLootItems)
                if (entry.Match(i)) return true;
            return false;
        }

        public AutoLootConfigEntry AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            var item = new AutoLootConfigEntry { Graphic = graphic, Hue = hue, Name = name };
            foreach (var entry in autoLootItems)
                if (entry.Equals(item)) return entry;

            autoLootItems.Add(item);
            return item;
        }

        private void HandleCorpse(Item corpse)
        {
            var anchorCorpse = GetCorpseAnchor(corpse);
            Log($"HandleCorpse ENTER corpse={Info(corpse)}  anchor={Info(anchorCorpse)}");

            if (anchorCorpse == null || !anchorCorpse.IsCorpse) return;
            if (!InRangeChebyshev(anchorCorpse, ProfileManager.CurrentProfile.AutoOpenCorpseRange, "HandleCorpse")) return;
            if (anchorCorpse.IsHumanCorpse && !ProfileManager.CurrentProfile.AutoLootHumanCorpses) return;
            if (!_scanningCorpses.Add(anchorCorpse.Serial)) return;

            try
            {
                if (!_openedCorpses.Contains(anchorCorpse.Serial))
                {
                    Log($"HandleCorpse: opening corpse {anchorCorpse.Serial:X8}");
                    GameActions.DoubleClick(anchorCorpse);
                    _openedCorpses.Add(anchorCorpse.Serial);
                    return;
                }

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
                                Log($"HandleCorpse: opening subcontainer {Info(child)}");
                                GameActions.DoubleClick(child);
                                _openedContainers.Add(child.Serial);
                                return; // open one per pass
                            }

                            if (child.Items != null) stack.Push(child);
                            continue;
                        }

                        if (IsOnLootList(child)) LootItem(child);
                    }
                }
            }
            finally
            {
                _scanningCorpses.Remove(anchorCorpse.Serial);
            }
        }

        public void TryRemoveAutoLootEntry(string UID)
        {
            int ix = autoLootItems.FindIndex(e => e.UID == UID);
            if (ix >= 0) autoLootItems.RemoveAt(ix);
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
        }

        private void OnPositionChanged(object sender, PositionChangedArgs e)
        {
            if (!loaded) return;

            if (ProfileManager.CurrentProfile.EnableScavenger)
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || !item.OnGround || item.IsCorpse) continue;
                    if (!InRangeChebyshev(item, 3, "Scavenger")) continue;
                    CheckAndLoot(item);
                }

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void OnOpenContainer(object sender, uint e)
        {
            if (!loaded || !IsEnabled) return;

            var cont = World.Items.Get(e);
            if (cont == null) return;

            var anchorCorpse = GetCorpseAnchor(cont);

            if (anchorCorpse != null && anchorCorpse.IsCorpse)
            {
                if (anchorCorpse.OnGround) _openedCorpses.Add(anchorCorpse.Serial);
                _openedContainers.Add(cont.Serial);
                HandleCorpse(anchorCorpse);
                PumpMoveOnce();
                MoveItemQueue.Instance?.ProcessQueue();
                return;
            }

            _openedContainers.Add(cont.Serial);

            for (LinkedObject n = cont.Items; n != null; n = n.Next)
            {
                var child = (Item)n;
                if (child == null) continue;
                if (IsOnLootList(child)) LootItem(child);
            }

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void OnItemCreatedOrUpdated(object sender, EventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            if (sender is Item i)
            {
                CheckCorpse(i);
                PumpMoveOnce();
                MoveItemQueue.Instance?.ProcessQueue();
            }
        }

        private void OnOPLReceived(object sender, OPLEventArgs e)
        {
            if (!loaded || !IsEnabled) return;
            var item = World.Items.Get(e.Serial);
            if (item != null) CheckCorpse(item);

            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        public void Update()
        {
            PumpMoveOnce();
            MoveItemQueue.Instance?.ProcessQueue();
        }

        private void PumpMoveOnce()
        {
            if (!loaded || !World.InGame) return;
            if (lootItems.Count == 0) { progressBarGump?.Dispose(); return; }
            if (Client.Game.GameCursor.ItemHold.Enabled) return;

            var moveSerial = lootItems.Dequeue();
            if (moveSerial == 0) return;

            if (lootItems.Count == 0) currentLootTotalCount = 0;

            quickContainsLookup.Remove(moveSerial);
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
                GameActions.DoubleClick(corpse);
                _openedCorpses.Add(corpse.Serial);
                lootItems.Enqueue(moveSerial);
                return;
            }

            var pack = World.Player?.FindItemByLayer(Layer.Backpack);
            if (pack == null) { lootItems.Enqueue(moveSerial); return; }

            if (m.Container == pack.Serial) return;

            MoveItemQueue.Instance?.EnqueueQuick(m);
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (progressBarGump == null || progressBarGump.IsDisposed))
            {
                progressBarGump = new ProgressBarGump("Auto looting...", 0)
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
            }
            catch
            {
                GameActions.Print("Error loading AutoLoot.json (check JSON).", 32);
                autoLootItems = new List<AutoLootConfigEntry>();
                loaded = true;
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
            catch { }
        }

        public class AutoLootConfigEntry
        {
            public string Name { get; set; } = "";
            public int Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            private bool RegexMatch => !string.IsNullOrEmpty(RegexSearch);
            public string UID { get; set; } = Guid.NewGuid().ToString();

            public bool Match(Item compareTo)
            {
                if (Graphic != -1 && Graphic != compareTo.Graphic) return false;
                if (!HueCheck(compareTo.Hue)) return false;
                if (RegexMatch && !RegexCheck(compareTo)) return false;
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

            public bool Equals(AutoLootConfigEntry other)
            {
                return other.Graphic == Graphic && other.Hue == Hue && RegexSearch == other.RegexSearch;
            }
        }
    }
}
