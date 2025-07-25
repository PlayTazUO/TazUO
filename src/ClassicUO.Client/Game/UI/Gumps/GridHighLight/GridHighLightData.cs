using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightData
    {
        private static GridHighlightData[] allConfigs;
        private readonly GridHighlightSetupEntry _entry;

        private static readonly Queue<uint> _queue = new();
        private static bool hasQueuedItems;

        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);
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
            private set => allConfigs = value;
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

        public GridHighlightData()
        {
            _entry = new GridHighlightSetupEntry();
            ProfileManager.CurrentProfile.GridHighlightSetup.Add(_entry);
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

        public static void ProcessItemOpl(uint value)
        {
            _queue.Enqueue(value);
            hasQueuedItems = true;
        }

        public static void ProcessQueue()
        {
            if (!hasQueuedItems)
                return;

            List<ItemPropertiesData> itemData = new(3);

            for (int i = 0; i < 3 && _queue.Count > 0; i++)
            {
                uint ser = _queue.Dequeue();
                if (World.Items.TryGetValue(ser, out var item))
                    itemData.Add(new ItemPropertiesData(item));
            }

            foreach (var config in AllConfigs)
            {
                foreach (var data in itemData)
                {
                    if (config.IsMatch(data))
                    {
                        data.item.MatchesHighlightData = true;
                        data.item.HighlightHue = config.Hue;
                    }
                }
            }

            if (_queue.Count == 0)
                hasQueuedItems = false;
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

        public bool IsMatch(ItemPropertiesData itemData)
        {
            if (!itemData.HasData)
                return false;

            return AcceptExtraProperties
                ? IsMatchFromProperties(itemData)
                : IsMatchFromItemPropertiesData(itemData);
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

            var itemResistances = props.Where(p =>
                GridHighlightRules.Resistances.Any(rule =>
                    Normalize(rule).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase))).ToList();

            var itemRarities = props.Where(p =>
                GridHighlightRules.RarityProperties.Any(rule =>
                    Normalize(rule).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase))).ToList();

            if (!itemProperties.Any() && !itemNegatives.Any() && !itemResistances.Any() && !itemRarities.Any())
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

            var result = string.IsNullOrWhiteSpace(input)
                ? string.Empty
                : HtmlTagRegex.Replace(input, string.Empty).Trim();

            _normalizeCache[input] = result;
            return result;
        }

        private bool IsItemNameMatch(string itemName)
        {
            if (ItemNames.Count == 0)
                return true;

            string cleanedUpItemName = Regex.Replace(itemName, @"^\d+\s+", "", RegexOptions.Compiled | RegexOptions.CultureInvariant)
                                            .Trim()
                                            .ToLowerInvariant();

            return ItemNames.Any(name => cleanedUpItemName == name.Trim().ToLowerInvariant());
        }

        private bool MatchesSlot(byte layer)
        {
            if (EquipmentSlots.Other) {
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
