using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Tracks the durability of equipped items, parsed from the object property list (OPL) data the
    /// server sends for those items. Each entry records the mobile it is equipped on, so callers can
    /// list the durabilities of a single mobile or look one up by item serial. Data is captured
    /// lazily: an item is tracked only after its OPL arrives, and entries are dropped when the item
    /// or its mobile leaves the world. Bound to a single <see cref="World"/>; OPL events arrive on
    /// the main game thread, so no synchronization is performed.
    /// </summary>
    public sealed class DurabilityManager : IDisposable
    {
        private readonly World _world;

        // The same entries are indexed two ways: by item serial for O(1) single-item lookups, and by
        // owner serial for O(1) per-mobile iteration. Item serials and mobile serials are both
        // globally unique, so neither index needs to worry about collisions.
        private readonly Dictionary<uint, DurabilityInfo> _byItem = new();
        private readonly Dictionary<uint, Dictionary<uint, DurabilityInfo>> _byOwner = new();

        private static readonly Layer[] _equipLayers =
        {
            Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Arms, Layer.Torso, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Gloves, Layer.Skirt,
            Layer.Robe, Layer.Waist, Layer.Neck, Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };

        // Fallback pattern for the "durability_regex" language key. The key lets a locale that labels
        // durability differently customize how current/max are pulled from an item's property list.
        private const string DefaultDurabilityPattern = @"(?<=Durability )(\d*) / (\d*)";

        private static string _resolvedDurabilityPattern;
        private static Regex _resolvedDurabilityRegex;

        /// <summary>Every tracked durability across all mobiles, keyed by item serial.</summary>
        public IReadOnlyDictionary<uint, DurabilityInfo> Durabilities => _byItem;

        public DurabilityManager(World world)
        {
            _world = world;
            EventSink.OPLOnReceive += OnOPLReceive;
        }

        /// <summary>
        /// Returns the tracked durabilities of a single mobile, ordered by remaining durability
        /// percentage (lowest first). Empty when the mobile has no tracked equipment.
        /// </summary>
        /// <param name="mobileSerial">Serial of the mobile whose equipment to list.</param>
        public List<DurabilityInfo> GetDurabilities(uint mobileSerial)
        {
            if (!_byOwner.TryGetValue(mobileSerial, out Dictionary<uint, DurabilityInfo> items))
                return new List<DurabilityInfo>();

            var list = new List<DurabilityInfo>(items.Count);

            foreach (DurabilityInfo durability in items.Values)
            {
                if (IsStillEquipped(durability))
                    list.Add(durability);
            }

            list.Sort((a, b) => a.Percentage.CompareTo(b.Percentage));

            return list;
        }

        /// <summary>
        /// Returns the local player's tracked durabilities, ordered by remaining durability
        /// percentage (lowest first).
        /// </summary>
        public List<DurabilityInfo> GetPlayerDurabilities() =>
            _world.Player != null ? GetDurabilities(_world.Player.Serial) : new List<DurabilityInfo>();

        /// <summary>Looks up a single item's durability by its serial.</summary>
        /// <param name="itemSerial">Serial of the item to look up.</param>
        /// <param name="durability">Receives the durability when found, otherwise <see langword="null" />.</param>
        /// <returns><see langword="true" /> when the item is tracked.</returns>
        public bool TryGetDurability(uint itemSerial, out DurabilityInfo durability) => _byItem.TryGetValue(itemSerial, out durability);

        /// <summary>
        /// True when the given mobile has at least one tracked, still-equipped durability. Callers
        /// use this to decide whether a durability tracker is worth showing for that mobile.
        /// </summary>
        /// <param name="mobileSerial">Serial of the mobile to check.</param>
        public bool HasDurabilityData(uint mobileSerial)
        {
            if (!_byOwner.TryGetValue(mobileSerial, out Dictionary<uint, DurabilityInfo> items))
                return false;

            foreach (DurabilityInfo durability in items.Values)
            {
                if (IsStillEquipped(durability))
                    return true;
            }

            return false;
        }

        /// <summary>Drops every tracked durability belonging to a mobile. Safe for an untracked serial.</summary>
        /// <param name="mobileSerial">Serial of the mobile whose data should be dropped.</param>
        public void RemoveMobile(uint mobileSerial)
        {
            if (!_byOwner.Remove(mobileSerial, out Dictionary<uint, DurabilityInfo> items))
                return;

            foreach (uint itemSerial in items.Keys)
                _byItem.Remove(itemSerial);
        }

        /// <summary>Drops the tracked durability of a single item, if present.</summary>
        /// <param name="itemSerial">Serial of the item whose durability should be dropped.</param>
        public void RemoveItem(uint itemSerial)
        {
            if (!_byItem.Remove(itemSerial, out DurabilityInfo durability))
                return;

            if (_byOwner.TryGetValue(durability.Owner, out Dictionary<uint, DurabilityInfo> items) && items.Remove(itemSerial) && items.Count == 0)
                _byOwner.Remove(durability.Owner);
        }

        /// <summary>Clears all tracked durabilities.</summary>
        public void Clear()
        {
            _byItem.Clear();
            _byOwner.Clear();
        }

        private void OnOPLReceive(object s, OPLEventArgs e)
        {
            if (!SerialHelper.IsItem(e.Serial))
                return;

            if (!_world.Items.TryGetValue(e.Serial, out Item item) || item.IsDestroyed)
                return;

            // An equipped item points its container at the mobile wearing it. Only those items, on a
            // known equipment layer, expose durability we care about.
            if (SerialHelper.IsMobile(item.Container) && _equipLayers.Contains(item.Layer))
            {
                DurabilityInfo durability = ParseDurability(item.Serial, item.Container, e.Data);

                if (durability.Serial != 0)
                    SetDurability(durability);
                else
                    RemoveItem(item.Serial);
            }
            else
            {
                RemoveItem(item.Serial);
            }

            UIManager.ForEach<DurabilitysGump>(g => g.RequestUpdateContents());
            UIManager.ForEach<ModernPaperdoll>(g => g.RequestUpdateContents());
        }

        /// <summary>
        /// Stores an entry in both indexes, replacing any prior entry for the item. Replacing through
        /// <see cref="RemoveItem(uint)" /> keeps the owner index correct if the item changed owner.
        /// </summary>
        private void SetDurability(DurabilityInfo durability)
        {
            RemoveItem(durability.Serial);

            _byItem[durability.Serial] = durability;

            if (!_byOwner.TryGetValue(durability.Owner, out Dictionary<uint, DurabilityInfo> items))
                _byOwner[durability.Owner] = items = new Dictionary<uint, DurabilityInfo>();

            items[durability.Serial] = durability;
        }

        /// <summary>
        /// An entry can outlive the equipment state that produced it, for example when an item is
        /// unequipped without a fresh OPL arriving. Confirm against the live item so stale entries
        /// are never listed.
        /// </summary>
        private bool IsStillEquipped(DurabilityInfo durability) =>
            _world.Items.TryGetValue(durability.Serial, out Item item)
            && !item.IsDestroyed
            && item.Container == durability.Owner
            && _equipLayers.Contains(item.Layer);

        private static DurabilityInfo ParseDurability(uint itemSerial, uint owner, string data)
        {
            MatchCollection matches = ResolveDurabilityRegex().Matches(data); // Matches e.g. "Durability 45 / 255"

            if (matches.Count == 0)
                return new DurabilityInfo();

            string[] parts = data.Substring(matches[0].Index, matches[0].Length).Split('/');

            return int.TryParse(parts[0].Trim(), out int min) && int.TryParse(parts[1].Trim(), out int max) ? new DurabilityInfo(itemSerial, owner, min, max) : new DurabilityInfo();
        }

        /// <summary>
        /// Resolves the pattern used to pull the current/max durability out of an item's property
        /// list. The pattern is user-customizable through the <c>durability_regex</c> language key so
        /// a locale that labels durability differently can adjust it; an empty or uncompilable value
        /// falls back to <see cref="DefaultDurabilityPattern"/>.
        /// </summary>
        private static Regex ResolveDurabilityRegex()
        {
            string pattern = TazLang.Get("durability_regex", DefaultDurabilityPattern);

            if (string.IsNullOrWhiteSpace(pattern))
                pattern = DefaultDurabilityPattern;

            if (pattern == _resolvedDurabilityPattern)
                return _resolvedDurabilityRegex;

            try
            {
                _resolvedDurabilityRegex = RegexHelper.GetRegex(pattern);
            }
            catch (ArgumentException ex)
            {
                Log.Warn($"DurabilityManager: invalid 'durability_regex' value, using the default pattern. {ex.Message}");
                _resolvedDurabilityRegex = RegexHelper.GetRegex(DefaultDurabilityPattern);
            }

            _resolvedDurabilityPattern = pattern;

            return _resolvedDurabilityRegex;
        }

        public void Dispose() => EventSink.OPLOnReceive -= OnOPLReceive;
    }

    /// <summary>
    /// A single equipped item's durability. <see cref="Owner" /> is the serial of the mobile the item
    /// is equipped on, which lets consumers group entries by mobile.
    /// </summary>
    public class DurabilityInfo
    {
        /// <summary>Serial of the item this durability belongs to.</summary>
        public uint Serial { get; set; }

        /// <summary>Serial of the mobile the item is equipped on, or 0 if unknown.</summary>
        public uint Owner { get; set; }

        /// <summary>Current durability.</summary>
        public int Current { get; set; }

        /// <summary>Maximum durability.</summary>
        public int Max { get; set; }

        public float Percentage => Max > 0 ? ((float)Current / (float)Max) : 0;

        public DurabilityInfo(uint serial, uint owner, int current, int max)
        {
            Serial = serial;
            Owner = owner;
            Current = current;
            Max = max;
        }

        public DurabilityInfo() : this(0, 0, 0, 0)
        {
        }
    }
}
