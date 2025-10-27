using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    using System.Text.Json.Serialization;

    [JsonSerializable(typeof(List<BuySellItemConfig>))]
    [JsonSerializable(typeof(BuySellItemConfig))]
    public partial class BuySellAgentJsonContext : JsonSerializerContext
    {
    }
    public class BuySellAgent
    {
        public static BuySellAgent Instance
        {
            get
            {
                if (field == null)
                    field = new();
                return field;
            }
            private set => field = value;
        }

        public List<BuySellItemConfig> SellConfigs => sellItems;
        public List<BuySellItemConfig> BuyConfigs => buyItems;

        private List<BuySellItemConfig> sellItems;
        private List<BuySellItemConfig> buyItems;

        private readonly Dictionary<uint, VendorSellInfo> sellPackets = new Dictionary<uint, VendorSellInfo>();
        public static void Load()
        {
            Instance = new BuySellAgent();

            try
            {
                string savePath = Path.Combine(ProfileManager.ProfilePath, "SellAgentConfig.json");
                if (File.Exists(savePath))
                {
                    Instance.sellItems = JsonSerializer.Deserialize(File.ReadAllText(savePath), BuySellAgentJsonContext.Default.ListBuySellItemConfig);
                }

                savePath = Path.Combine(ProfileManager.ProfilePath, "BuyAgentConfig.json");
                if (File.Exists(savePath))
                {
                    Instance.buyItems = JsonSerializer.Deserialize(File.ReadAllText(savePath), BuySellAgentJsonContext.Default.ListBuySellItemConfig);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading BuySellAgent config: {ex}");
            }
        }

        public void DeleteConfig(BuySellItemConfig config)
        {
            if (config == null) return;

            SellConfigs?.Remove(config);
            BuyConfigs?.Remove(config);
        }

        public BuySellItemConfig NewSellConfig()
        {
            var r = new BuySellItemConfig();

            sellItems ??= new List<BuySellItemConfig>();

            sellItems.Add(r);
            return r;
        }
        public BuySellItemConfig NewBuyConfig()
        {
            var r = new BuySellItemConfig();

            buyItems ??= new List<BuySellItemConfig>();

            buyItems.Add(r);
            return r;
        }

        public static void Unload()
        {
            if (Instance != null)
            {
                if (Instance.sellItems != null)
                {
                    string savePath = Path.Combine(ProfileManager.ProfilePath, "SellAgentConfig.json");
                    File.WriteAllText(savePath, JsonSerializer.Serialize(Instance.sellItems, BuySellAgentJsonContext.Default.ListBuySellItemConfig));
                }

                if (Instance.buyItems != null)
                {
                    string savePath = Path.Combine(ProfileManager.ProfilePath, "BuyAgentConfig.json");
                    File.WriteAllText(savePath, JsonSerializer.Serialize(Instance.buyItems, BuySellAgentJsonContext.Default.ListBuySellItemConfig));
                }
            }

            Instance = null;
        }

        public void HandleBuyPacket(List<Item> items, uint shopSerial)
        {
            if (!ProfileManager.CurrentProfile.BuyAgentEnabled) return;

            if (buyItems == null) return;

            var buyList = new List<Tuple<uint, ushort>>();
            long val = 0;
            ushort total_count = 0;
            ushort unique_items = 0;
            int max_total_items = ProfileManager.CurrentProfile.BuyAgentMaxItems;
            bool limit_total_items = max_total_items > 0;
            int max_unique_items = ProfileManager.CurrentProfile.BuyAgentMaxUniques;
            bool limit_unique_items = max_unique_items > 0;

            foreach (BuySellItemConfig buyConfigEntry in buyItems)
            {
                if (!buyConfigEntry.Enabled) continue;

                ushort current_count = 0;
                ushort maxToBuy = buyConfigEntry.MaxAmount;

                // Check restock functionality
                if (buyConfigEntry.RestockUpTo > 0)
                {
                    ushort currentBackpackAmount = GetBackpackItemCount(buyConfigEntry.Graphic, buyConfigEntry.Hue);
                    if (currentBackpackAmount >= buyConfigEntry.RestockUpTo)
                    {
                        continue; // Already have enough, skip this item
                    }
                    maxToBuy = (ushort)(buyConfigEntry.RestockUpTo - currentBackpackAmount);
                    maxToBuy = Math.Min(maxToBuy, buyConfigEntry.MaxAmount);
                }

                foreach (Item item in items)
                {
                    if (!buyConfigEntry.IsMatch(item.Graphic, item.Hue)) continue;

                    if (current_count >= maxToBuy) continue;

                    if (limit_unique_items && unique_items >= max_unique_items) break;

                    if (limit_total_items && current_count + total_count >= max_total_items) break;

                    int amount_buyable = Math.Min(item.Amount, maxToBuy - current_count);
                    if (limit_total_items)
                    {
                        amount_buyable = Math.Min(amount_buyable, max_total_items - total_count - current_count);
                    }

                    if (amount_buyable > 0)
                    {
                        buyList.Add(new Tuple<uint, ushort>(item.Serial, (ushort)amount_buyable));
                        current_count += (ushort)amount_buyable;
                        val += item.Price * (ushort)amount_buyable;
                    }
                }

                if (current_count > 0)
                    unique_items++;

                total_count += current_count;
            }

            if (buyList.Count == 0) return;

            AsyncNetClient.Socket.Send_BuyRequest(shopSerial, buyList.ToArray());
            GameActions.Print(Client.Game.UO.World, $"Purchased {total_count} items for {val} gold.");
            UIManager.GetGump(shopSerial)?.Dispose();
        }

        private ushort GetBackpackItemCount(ushort graphic, ushort hue)
        {
            Item backpack = World.Instance.Player?.Backpack;
            if (backpack == null) return 0;

            ushort count = 0;
            var item = (Item)backpack.Items;
            while (item != null)
            {
                if (item.Graphic == graphic && (hue == ushort.MaxValue || item.Hue == hue))
                {
                    count += item.Amount;
                }
                item = (Item)item.Next;
            }
            return count;
        }

        public void HandleSellPacket(uint vendorSerial, uint serial, ushort graphic, ushort hue, ushort amount, uint price)
        {
            if (!ProfileManager.CurrentProfile.SellAgentEnabled) return;

            if (!sellPackets.ContainsKey(vendorSerial))
                sellPackets.Add(vendorSerial, new VendorSellInfo());

            sellPackets[vendorSerial].HandleSellPacketItem(serial, graphic, hue, amount, price);
        }

        public void HandleSellPacketFinished(uint vendorSerial)
        {
            if (!ProfileManager.CurrentProfile.SellAgentEnabled) return;

            if (sellItems == null)
            {
                sellPackets.Remove(vendorSerial);
                return;
            }

            var sellList = new List<Tuple<uint, ushort>>();
            long val = 0;
            ushort total_count = 0;
            ushort unique_items = 0;
            int max_total_items = ProfileManager.CurrentProfile.SellAgentMaxItems;
            bool limit_total_items = max_total_items > 0;
            int max_unique_items = ProfileManager.CurrentProfile.SellAgentMaxUniques;
            bool limit_unique_items = max_unique_items > 0;

            foreach (BuySellItemConfig sellConfig in sellItems)
            {
                if (!sellConfig.Enabled) continue;

                ushort current_count = 0;

                // Check minimum on hand logic
                ushort backpackTotal = 0;
                if (sellConfig.RestockUpTo > 0)
                {
                    backpackTotal = GetBackpackItemCount(sellConfig.Graphic, sellConfig.Hue);
                    if (backpackTotal <= sellConfig.RestockUpTo)
                    {
                        continue; // Skip selling this item type - already at or below minimum
                    }
                }

                foreach (VendorSellItemData item in sellPackets[vendorSerial].AvailableItems)
                {
                    if (!sellConfig.IsMatch(item.Graphic, item.Hue)) continue;

                    if (current_count >= sellConfig.MaxAmount) continue;

                    if (limit_unique_items && unique_items >= max_unique_items) break;

                    if (limit_total_items && current_count + total_count >= max_total_items) break;

                    int amount_sellable = Math.Min(item.Amount, sellConfig.MaxAmount - current_count);
                    if (limit_total_items)
                    {
                        amount_sellable = Math.Min(amount_sellable, max_total_items - total_count - current_count);
                    }

                    // Apply minimum on hand restriction
                    if (sellConfig.RestockUpTo > 0 && backpackTotal > 0)
                    {
                        int maxSellableBeforeMin = backpackTotal - sellConfig.RestockUpTo;
                        amount_sellable = Math.Min(amount_sellable, maxSellableBeforeMin - current_count);

                        if (amount_sellable <= 0)
                            break; // Can't sell more without going below minimum
                    }

                    if (amount_sellable > 0)
                    {
                        sellList.Add(new Tuple<uint, ushort>(item.Serial, (ushort)amount_sellable));
                        current_count += (ushort)amount_sellable;
                        val += item.Price * (ushort)amount_sellable;
                    }
                }

                if (current_count > 0)
                    unique_items++;

                total_count += current_count;
            }
            sellPackets.Remove(vendorSerial);

            if (sellList.Count == 0) return;

            AsyncNetClient.Socket.Send_SellRequest(vendorSerial, sellList.ToArray());
            GameActions.Print(Client.Game.UO.World, $"Sold {total_count} items for {val} gold.");
            UIManager.GetGump(vendorSerial)?.Dispose();
        }
    }

    public class BuySellItemConfig
    {
        public ushort Graphic { get; set; }
        public ushort Hue { get; set; } = ushort.MaxValue;
        public ushort MaxAmount { get; set; } = ushort.MaxValue;
        public ushort RestockUpTo { get; set; } = 0;
        public bool Enabled { get; set; } = true;

        public bool IsMatch(ushort graphic, ushort hue) => graphic == Graphic && (hue == Hue || Hue == ushort.MaxValue);
    }

    public class VendorSellInfo
    {
        public List<VendorSellItemData> AvailableItems { get; set; } = new List<VendorSellItemData>();
        public void HandleSellPacketItem(uint serial, ushort graphic, ushort hue, ushort amount, uint price) => AvailableItems.Add(new VendorSellItemData(serial, graphic, hue, amount, price));
    }

    public class VendorSellItemData
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Hue;
        public ushort Amount;
        public uint Price;

        public VendorSellItemData(uint serial, ushort graphic, ushort hue, ushort amount, uint price)
        {
            Serial = serial;
            Graphic = graphic;
            Hue = hue;
            Amount = amount;
            Price = price;
        }
    }
}
