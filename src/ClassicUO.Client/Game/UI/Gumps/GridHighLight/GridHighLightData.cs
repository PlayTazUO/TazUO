using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightData
    {
        private static GridHighlightData[] allConfigs;
        private readonly GridHighlightSetupEntry _entry;

        private static readonly Queue<uint> _queue = new();
        private static readonly HashSet<uint> _queuedItems = new();
        private static bool hasQueuedItems;

        private readonly Dictionary<string, string> _normalizeCache = new();

        public static GridHighlightData[] AllConfigs
        {
            get
            {
                if (allConfigs != null)
                    return allConfigs;

                var setup = ProfileManager.CurrentProfile.GridHighlightSetup;
                allConfigs = setup.Select(entry => new GridHighlightData(entry)).ToArray();
                return allConfigs;
            }
            set => allConfigs = value;
        }

        public string Name
        {
            get => _entry.Name;
            set => _entry.Name = value;
        }

        public List<string> ItemNames
        {
            get => _entry.ItemNames;
            set => _entry.ItemNames = value;
        }

        public ushort Hue
        {
            get => _entry.Hue;
            set => _entry.Hue = value;
        }

        public Color HighlightColor
        {
            get => _entry.GetHighlightColor();
            set => _entry.SetHighlightColor(value);
        }

        public List<GridHighlightProperty> Properties => _entry.Properties;

        public bool AcceptExtraProperties
        {
            get => _entry.AcceptExtraProperties;
            set => _entry.AcceptExtraProperties = value;
        }

        public int MinimumProperty
        {
            get => _entry.MinimumProperty;
            set => _entry.MinimumProperty = value;
        }

        public int MaximumProperty
        {
            get => _entry.MaximumProperty;
            set => _entry.MaximumProperty = value;
        }

        public List<string> ExcludeNegatives
        {
            get => _entry.ExcludeNegatives;
            set => _entry.ExcludeNegatives = value;
        }

        public bool Overweight
        {
            get => _entry.Overweight;
            set => _entry.Overweight = value;
        }

        public List<string> RequiredRarities
        {
            get => _entry.RequiredRarities;
            set => _entry.RequiredRarities = value;
        }

        public GridHighlightSlot EquipmentSlots
        {
            get => _entry.GridHighlightSlot;
            set => _entry.GridHighlightSlot = value;
        }

        public bool LootOnMatch
        {
            get => _entry.LootOnMatch;
            set => _entry.LootOnMatch = value;
        }

        private GridHighlightData(GridHighlightSetupEntry entry)
        {
            _entry = entry;
        }

        public void Delete()
        {
            ProfileManager.CurrentProfile.GridHighlightSetup.Remove(_entry);
            allConfigs = null;
        }

        public void Move(bool up)
        {
            var list = ProfileManager.CurrentProfile.GridHighlightSetup;
            int index = list.IndexOf(_entry);
            if (index == -1) return; // Not found

            // Prevent moving out of bounds
            if (up && index == 0) return;
            if (!up && index == list.Count - 1) return;

            list.RemoveAt(index);
            list.Insert(up ? index - 1 : index + 1, _entry);
        }

        public static void ProcessItemOpl(World world, uint serial)
        {
            // Only queue items if the server supports tooltips
            if (!world.ClientFeatures.TooltipsEnabled)
                return;

            // Check if already queued to avoid duplicates
            if (!_queuedItems.Add(serial))
                return;

            // Enqueue for processing - validation happens in ProcessQueue
            _queue.Enqueue(serial);
            hasQueuedItems = true;
        }

        public static void ProcessQueue(World World)
        {
            if (!hasQueuedItems)
                return;

            List<ItemPropertiesData> itemData = new(3);
            List<uint> requeueItems = new();

            for (int i = 0; i < 3 && _queue.Count > 0; i++)
            {
                uint ser = _queue.Dequeue();

                // Check if item still exists
                if (!World.Items.TryGetValue(ser, out var item))
                {
                    // Item was removed, remove from hashset and skip
                    _queuedItems.Remove(ser);
                    continue;
                }

                // Check if item is still valid for highlighting
                if (item.OnGround || item.IsMulti)
                {
                    // Item moved to ground or is multi, remove from hashset and skip
                    _queuedItems.Remove(ser);
                    continue;
                }

                // Check if OPL data exists
                if (!World.OPL.TryGetNameAndData(ser, out _, out _))
                {
                    // OPL data not available yet, requeue for later processing
                    requeueItems.Add(ser);
                    continue;
                }

                // OPL data exists, remove from hashset and create ItemPropertiesData
                _queuedItems.Remove(ser);
                itemData.Add(new ItemPropertiesData(World, item));
            }

            // Process items with OPL data
            foreach (var data in itemData)
            {
                var bestMatch = GetBestMatch(data);
                if (bestMatch != null)
                {
                    data.item.MatchesHighlightData = true;
                    data.item.HighlightColor = bestMatch.HighlightColor;
                    data.item.HighlightName = bestMatch.Name;

                    if (bestMatch.LootOnMatch)
                    {
                        var root = World.Items.Get(data.item.RootContainer);
                        if (root != null && root.IsCorpse)
                            AutoLootManager.Instance.LootItem(data.item);
                    }
                }
            }

            // Requeue items that don't have OPL data yet
            foreach (var ser in requeueItems)
            {
                _queue.Enqueue(ser);
            }

            if (_queue.Count == 0)
            {
                hasQueuedItems = false;
                _queuedItems.Clear(); // Clear hashset when queue is empty
            }
        }

        public static GridHighlightData GetGridHighlightData(int index)
        {
            var list = ProfileManager.CurrentProfile.GridHighlightSetup;
            var data = index >= 0 && index < list.Count ? new GridHighlightData(list[index]) : null;

            if (data == null)
            {
                list.Add(new GridHighlightSetupEntry());
                data = new GridHighlightData(list[index]);
            }

            return data;
        }

        public static void RecheckMatchStatus()
        {
            AllConfigs = null; //Reset configs
            foreach (var kvp in World.Instance.Items)
            {
                if (kvp.Value.OnGround || kvp.Value.IsMulti) continue;
                ProcessItemOpl(World.Instance, kvp.Key);
            }
        }

        public bool IsMatch(ItemPropertiesData itemData)
        {
            return AcceptExtraProperties
                ? IsMatchFromProperties(itemData)
                : IsMatchFromItemPropertiesData(itemData);
        }

        public bool DoesPropertyMatch(ItemPropertiesData.SinglePropertyData property)
        {
            foreach (var rule in Properties)
            {
                string nProp = Normalize(property.Name);
                string nRule = Normalize(rule.Name);

                bool nameMatch = nProp.Equals(nRule, StringComparison.OrdinalIgnoreCase) ||
                                 nProp.Contains(nRule, StringComparison.OrdinalIgnoreCase) ||
                                 Normalize(property.OriginalString).Contains(nRule, StringComparison.OrdinalIgnoreCase);

                bool valueMatch = rule.MinValue == -1 || property.FirstValue >= rule.MinValue;

                if (nameMatch && valueMatch)
                    return true;
            }

            // rarities
            if (RequiredRarities.Any(r => Normalize(property.Name).Equals(Normalize(r), StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private bool IsMatchFromProperties(ItemPropertiesData itemData)
        {
            if (!IsItemNameMatch(itemData.item.Name))
                return false;

            if (!MatchesSlot(itemData.item.ItemData.Layer))
                return false;

            if (Overweight && itemData.singlePropertyData.Any(prop =>
                    Normalize(prop.OriginalString).IndexOf("Weight: 50 Stones", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }

            foreach (string pattern in ExcludeNegatives.Select(Normalize))
            {
                if (itemData.singlePropertyData.Any(prop =>
                        Normalize(prop.Name).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        Normalize(prop.OriginalString).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }
            }

            if (RequiredRarities.Count > 0)
            {
                bool hasRequired = itemData.singlePropertyData.Any(prop =>
                {
                    return GridHighlightRules.RarityProperties.Any(rule =>
                        Normalize(rule).Equals(Normalize(prop.Name), StringComparison.OrdinalIgnoreCase)) &&
                    RequiredRarities.Any(r =>
                        Normalize(r).Equals(Normalize(prop.Name), StringComparison.OrdinalIgnoreCase));
                });

                if (!hasRequired)
                    return false;
            }

            int matchingPropertiesCount = 0;

            foreach (var prop in Properties)
            {
                bool matched = itemData.singlePropertyData.Any(p =>
                    (Normalize(p.Name).IndexOf(Normalize(prop.Name), StringComparison.OrdinalIgnoreCase) >= 0 ||
                     Normalize(p.OriginalString).IndexOf(Normalize(prop.Name), StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (prop.MinValue == -1 || p.FirstValue >= prop.MinValue));

                if (matched)
                {
                    matchingPropertiesCount++;
                }

                if (!matched && !prop.IsOptional)
                    return false;
            }

            var isMatchingPropertyCount = IsMatchingPropertyCount(matchingPropertiesCount);
            return isMatchingPropertyCount;
        }

        private bool IsMatchFromItemPropertiesData(ItemPropertiesData itemData)
        {
            if (!IsItemNameMatch(itemData.item.Name))
                return false;

            if (!MatchesSlot(itemData.item.ItemData.Layer))
                return false;

            var props = itemData.singlePropertyData;

            var itemProperties = props.Where(p =>
                GridHighlightRules.Properties.Any(rule =>
                    Normalize(rule).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase))).ToList();

            var itemNegatives = props.Where(p =>
                GridHighlightRules.NegativeProperties.Any(rule =>
                    Normalize(rule).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase))).ToList();

            var itemRarities = props.Where(p =>
                GridHighlightRules.RarityProperties.Any(rule =>
                    Normalize(rule).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase))).ToList();

            if (!itemProperties.Any() && !itemNegatives.Any() && !itemRarities.Any())
                return false;

            if (Overweight && props.Any(prop =>
                    Normalize(prop.OriginalString).IndexOf("Weight: 50 Stones", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }

            foreach (var pattern in ExcludeNegatives.Select(Normalize))
            {
                if (itemProperties.Any(p => Normalize(p.Name).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    itemNegatives.Any(p => Normalize(p.Name).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;
            }

            if (RequiredRarities.Count > 0)
            {
                bool hasRequired = itemRarities.Any(r =>
                    RequiredRarities.Any(req =>
                        Normalize(r.Name).Equals(Normalize(req), StringComparison.OrdinalIgnoreCase)));

                if (!hasRequired)
                    return false;
            }

            int matchingPropertiesCount = 0;

            foreach (var prop in Properties)
            {
                var match = itemProperties.FirstOrDefault(p =>
                    Normalize(p.Name).Equals(Normalize(prop.Name), StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    if (!prop.IsOptional)
                        return false;
                }
                else if (prop.MinValue != -1 && match.FirstValue < prop.MinValue)
                {
                    return false;
                }

                matchingPropertiesCount++;
            }

            var isMatchingPropertyCount = IsMatchingPropertyCount(matchingPropertiesCount);
            return isMatchingPropertyCount;
        }

        public static GridHighlightData GetBestMatch(ItemPropertiesData itemData)
        {
            GridHighlightData best = null;
            int bestScore = -1;
            bool bestHasExact = false;

            foreach (var config in AllConfigs)
            {
                if (!config.IsMatch(itemData))
                    continue;

                int score = 0;
                bool hasExact = false;

                foreach (var prop in itemData.singlePropertyData)
                {
                    foreach (var rule in config.Properties)
                    {
                        string nProp = config.Normalize(prop.Name);
                        string nRule = config.Normalize(rule.Name);

                        if (nProp.Equals(nRule, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 2; // exact name match = stronger
                            hasExact = true;
                        }
                        else if (nProp.Contains(nRule, StringComparison.OrdinalIgnoreCase) ||
                                 config.Normalize(prop.OriginalString).Contains(nRule, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 1;
                        }
                    }
                }

                // tie-breaker: prefer exact name matches, then higher score
                if (best == null || (hasExact && !bestHasExact) ||
                    (hasExact == bestHasExact && score > bestScore))
                {
                    best = config;
                    bestScore = score;
                    bestHasExact = hasExact;
                }
            }

            return best;
        }

        private bool IsMatchingPropertyCount(int matchingPropertiesCount)
        {
            if (MinimumProperty > 0 && matchingPropertiesCount < MinimumProperty)
            {
                return false;
            }
            if (MaximumProperty > 0 && matchingPropertiesCount > MaximumProperty)
            {
                return false;
            }

            return true;
        }

        private string Normalize(string input)
        {
            input ??= string.Empty;

            if (_normalizeCache.TryGetValue(input, out var cached))
                return cached;

            var result = StripHtmlTags(input).Trim();
            _normalizeCache[input] = result;
            return result;
        }

        private string CleanItemName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;

            int index = 0;
            // Skip leading digits
            while (index < name.Length && char.IsDigit(name[index])) index++;

            // Skip following whitespace
            while (index < name.Length && char.IsWhiteSpace(name[index])) index++;

            return name.Substring(index).Trim().ToLowerInvariant();
        }

        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var output = new char[input.Length];
            int outputIndex = 0;
            bool insideTag = false;

            foreach (char c in input)
            {
                if (c == '<') { insideTag = true; continue; }
                if (c == '>') { insideTag = false; continue; }
                if (!insideTag) output[outputIndex++] = c;
            }

            return new string(output, 0, outputIndex);
        }

        private bool IsItemNameMatch(string itemName)
        {
            if (ItemNames.Count == 0)
                return true;

            string cleanedUpItemName = CleanItemName(itemName);
            return ItemNames.Any(name => string.Equals(cleanedUpItemName, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesSlot(byte layer)
        {
            if (EquipmentSlots.Other)
            {
                return true;
            }

            return layer switch
            {
                (byte)Layer.Talisman => EquipmentSlots.Talisman,
                (byte)Layer.OneHanded => EquipmentSlots.RightHand,
                (byte)Layer.TwoHanded => EquipmentSlots.LeftHand,
                (byte)Layer.Helmet => EquipmentSlots.Head,
                (byte)Layer.Earrings => EquipmentSlots.Earring,
                (byte)Layer.Necklace => EquipmentSlots.Neck,
                (byte)Layer.Torso or (byte)Layer.Tunic => EquipmentSlots.Chest,
                (byte)Layer.Shirt => EquipmentSlots.Shirt,
                (byte)Layer.Cloak => EquipmentSlots.Back,
                (byte)Layer.Robe => EquipmentSlots.Robe,
                (byte)Layer.Arms => EquipmentSlots.Arms,
                (byte)Layer.Gloves => EquipmentSlots.Hands,
                (byte)Layer.Bracelet => EquipmentSlots.Bracelet,
                (byte)Layer.Ring => EquipmentSlots.Ring,
                (byte)Layer.Waist => EquipmentSlots.Belt,
                (byte)Layer.Skirt => EquipmentSlots.Skirt,
                (byte)Layer.Legs => EquipmentSlots.Legs,
                (byte)Layer.Pants => EquipmentSlots.Legs,
                (byte)Layer.Shoes => EquipmentSlots.Footwear,

                (byte)Layer.Hair or
                (byte)Layer.Beard or
                (byte)Layer.Face or
                (byte)Layer.Mount or
                (byte)Layer.Backpack or
                (byte)Layer.ShopBuy or
                (byte)Layer.ShopBuyRestock or
                (byte)Layer.ShopSell or
                (byte)Layer.Bank or
                (byte)Layer.Invalid => false,

                _ => true
            };
        }
    }
}
