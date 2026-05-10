using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Gump = ClassicUO.Renderer.Gumps.Gump;

namespace ClassicUO.Network.PacketHandlers;

internal static class OpenContainer
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        if (Settings.GlobalSettings.CustomServer == Settings.CustomServers.Eventine)
        {
            ReceiveEventine(world, ref p);
            return;
        }

        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();

        if (graphic == 0xFFFF)
        {
            Item spellBookItem = world.Items.Get(serial);

            if (spellBookItem == null)
                return;

            UIManager.GetGump<SpellbookGump>(serial)?.Dispose();

            var spellbookGump = new SpellbookGump(world, spellBookItem);

            if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location))
                location = new Point(64, 64);

            spellbookGump.Location = location;
            UIManager.Add(spellbookGump);

            Client.Game.Audio.PlaySound(0x0055);
        }
        else if (graphic == 0x0030)
        {
            Mobile vendor = world.Mobiles.Get(serial);

            if (vendor == null)
                return;

            UIManager.GetGump<ShopGump>(serial)?.Dispose();
            UIManager.GetGump<ModernShopGump>(serial)?.Dispose();

            ModernShopGump modernShopGump = null;
            ShopGump gump = null;

            if (ProfileManager.CurrentProfile.UseModernShopGump)
                UIManager.Add(modernShopGump = new ModernShopGump(world, vendor, true));
            else
                UIManager.Add(gump = new ShopGump(world, serial, true, 150, 5));


            for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
            {
                Item item = vendor.FindItemByLayer(layer);

                LinkedObject first = item.Items;

                if (first == null)
                    //Log.Warn("buy item not found");
                    continue;

                bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                if (reverse)
                    while (first?.Next != null)
                        first = first.Next;

                var buyList = new List<Item>();

                while (first != null)
                {
                    var it = (Item)first;
                    buyList.Add(it);
                    if (ProfileManager.CurrentProfile.UseModernShopGump)
                        modernShopGump.AddItem
                        (
                            world,
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );
                    else
                        gump.AddItem
                        (
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );

                    if (reverse)
                        first = first.Previous;
                    else
                        first = first.Next;
                }

                BuySellAgent.Instance?.HandleBuyPacket(buyList, serial);
            }
        }
        else
        {
            Item item = world.Items.Get(serial);

            if (item != null)
            {
                if (!NearbyLootGump.IsCorpseRequested(serial))
                {
                    if (
                        item.IsCorpse
                        && (
                            ProfileManager.CurrentProfile.GridLootType == 1
                            || ProfileManager.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        UIManager.Add(new GridLootGump(world, serial));
                        Helpers.SharedStore.RequestedGridLoot = serial;

                        if (ProfileManager.CurrentProfile.GridLootType == 1)
                            return;
                    }

                    if (
                        Client.Game.UO.Version >= Utility.ClientVersion.CV_706000
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.UseLargeContainerGumps
                    )
                        UpdateLargeContainerGraphics(ref graphic);


                    if (ProfileManager.CurrentProfile.UseGridLayoutContainerGumps && graphic != 0x091A)
                        GridContainer.OpenOrUpdate(serial, graphic);
                    else
                    {
                        ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
                        bool playsound = false;
                        int x, y;


                        if (container != null)
                        {
                            x = container.ScreenCoordinateX;
                            y = container.ScreenCoordinateY;
                            container.Dispose();
                        }
                        else
                        {
                            world.ContainerManager.CalculateContainerPosition(serial, graphic);
                            x = world.ContainerManager.X;
                            y = world.ContainerManager.Y;
                            playsound = true;
                        }

                        UIManager.Add
                        (
                            new ContainerGump(world, item, graphic, playsound)
                            {
                                X = x, Y = y, InvalidateContents = true
                            }
                        );
                    }
                }

                EventSink.InvokeOnOpenContainer(item, serial);

                UIManager.RemovePosition(serial);
            }
            else
                Log.Error("[OpenContainer]: item not found");
        }

        if (graphic != 0x0030)
        {
            Item it = world.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;

                if (!it.IsCorpse && graphic != 0xFFFF)
                    Helpers.ItemHelpers.ClearContainerAndRemoveItems(world, it);
            }
        }
    }

    private static void ReceiveEventine(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();

        if (graphic == 0xFFFF)
        {
            Item spellBookItem = world.Items.Get(serial);

            if (spellBookItem == null)
                return;

            UIManager.GetGump<SpellbookGump>(serial)?.Dispose();

            var spellbookGump = new SpellbookGump(world, spellBookItem);

            if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location))
                location = new Point(64, 64);

            spellbookGump.Location = location;
            UIManager.Add(spellbookGump);

            Client.Game.Audio.PlaySound(0x0055);
        }
        else if (graphic == 0x0030)
        {
            Mobile vendor = world.Mobiles.Get(serial);

            if (vendor == null)
                return;

            UIManager.GetGump<ShopGump>(serial)?.Dispose();
            UIManager.GetGump<ModernShopGump>(serial)?.Dispose();

            ModernShopGump modernShopGump = null;
            ShopGump gump = null;

            if (ProfileManager.CurrentProfile.UseModernShopGump)
                UIManager.Add(modernShopGump = new ModernShopGump(world, vendor, true));
            else
                UIManager.Add(gump = new ShopGump(world, serial, true, 150, 5));


            for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
            {
                Item item = vendor.FindItemByLayer(layer);

                if (item == null) continue;

                LinkedObject first = item.Items;

                if (first == null) continue;

                bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                if (reverse)
                    while (first?.Next != null)
                        first = first.Next;

                var buyList = new List<Item>();

                while (first != null)
                {
                    var it = (Item)first;
                    buyList.Add(it);
                    if (ProfileManager.CurrentProfile.UseModernShopGump)
                        modernShopGump.AddItem
                        (
                            world,
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );
                    else
                        gump.AddItem
                        (
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );

                    if (reverse)
                        first = first.Previous;
                    else
                        first = first.Next;
                }

                BuySellAgent.Instance?.HandleBuyPacket(buyList, serial);
            }
        }
        else
        {
            Item item = world.Items.Get(serial);

            if (item != null)
            {
                if (!NearbyLootGump.IsCorpseRequested(serial))
                {
                    if (
                        item.IsCorpse
                        && (
                            ProfileManager.CurrentProfile.GridLootType == 1
                            || ProfileManager.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        UIManager.Add(new GridLootGump(world, serial));
                        Helpers.SharedStore.RequestedGridLoot = serial;

                        if (ProfileManager.CurrentProfile.GridLootType == 1)
                            return;
                    }
                    bool canuse = graphic == 1009 || graphic == 1081 || graphic == 1278 || graphic == 2417 || (graphic >= 1060 && graphic <= 1068) || (graphic >= 1071 && graphic <= 1079) || (graphic >= 1258 && graphic <= 1270) || (graphic >= 1282 && graphic <= 1291) || (graphic >= 1071 && graphic <= 1079);

                    bool isvendor = graphic == 10009 || graphic == 2330 || (graphic >= 10060 && graphic <= 10081) || (graphic >= 10258 && graphic <= 10291) || graphic == 11000 || graphic == 11156 || graphic == 11415 || graphic == 11417 || graphic == 11422 || (graphic >= 11747 && graphic <= 11750) || (graphic >= 11765 && graphic <= 11770) || (graphic >= 19800 && graphic <= 19835) || graphic == 29724 || graphic == 40558 || graphic == 40560 || graphic == 40562 || graphic == 40586 || graphic == 49922 || graphic == 49934 || graphic == 50138 || (graphic >= 50153 && graphic <= 50167) || (graphic >= 50246 && graphic == 50250) || (graphic >= 50298 && graphic == 50300);

                    // TODO: check client version ?
                    if (
                        Client.Game.UO.Version >= Utility.ClientVersion.CV_706000
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.UseLargeContainerGumps
                    )
                    {
                        if (graphic == 10009 || (graphic >= 10060 && graphic <= 10081) || (graphic >= 10258 && graphic <= 10291) || graphic == 11000 || graphic == 11156 || graphic == 11415 || graphic == 11417 || graphic == 11422 || (graphic >= 11747 && graphic <= 11750) || (graphic >= 11765 && graphic <= 11770) || (graphic >= 19800 && graphic <= 19835) || graphic == 29724 || graphic == 40558 || graphic == 40560 || graphic == 40562 || graphic == 40586 || graphic == 49922 || graphic == 49934 || graphic == 50138 || (graphic >= 50153 && graphic <= 50167) || (graphic >= 50246 && graphic == 50250) || (graphic >= 50298 && graphic == 50300))
                            graphic -= 10000;

                        EventineUpdateLargeContainerGraphics(ref graphic);

                        if (graphic == 1009 || graphic == 20724 || graphic == 1081 || graphic == 1278 || graphic == 2417 || (graphic >= 1060 && graphic <= 1068) || (graphic >= 1071 && graphic <= 1079) || (graphic >= 1258 && graphic <= 1270) || (graphic >= 1282 && graphic <= 1291) || (graphic >= 1071 && graphic <= 1079))
                            graphic -= 1000;

                    }

                    if (ProfileManager.CurrentProfile.UseGridLayoutContainerGumps && !canuse && !isvendor && graphic != 0x091A)
                    {
                        GridContainer.OpenOrUpdate(serial, graphic);
                    }
                    else
                    {
                        if (graphic == 10009 || (graphic >= 10060 && graphic <= 10081) || (graphic >= 10258 && graphic <= 10291) || graphic == 11000 || graphic == 11156 || graphic == 11415 || graphic == 11417 || graphic == 11422 || (graphic >= 11747 && graphic <= 11750) || (graphic >= 11765 && graphic <= 11770) || (graphic >= 19800 && graphic <= 19835) || graphic == 29724 || graphic == 40558 || graphic == 40560 || graphic == 40562 || graphic == 40586 || graphic == 49922 || graphic == 49934 || graphic == 50138 || (graphic >= 50153 && graphic <= 50167) || (graphic >= 50246 && graphic == 50250) || (graphic >= 50298 && graphic == 50300))
                            graphic -= 10000;

                        if (graphic == 1009 || graphic == 20724 || graphic == 1081 || graphic == 1278 || graphic == 2417 || (graphic >= 1060 && graphic <= 1068) || (graphic >= 1071 && graphic <= 1079) || (graphic >= 1258 && graphic <= 1270) || (graphic >= 1282 && graphic <= 1291) || (graphic >= 1071 && graphic <= 1079))
                            graphic -= 1000;

                        ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
                        bool playsound = false;
                        int x, y;


                        if (container != null)
                        {
                            x = container.ScreenCoordinateX;
                            y = container.ScreenCoordinateY;
                            container.Dispose();
                        }
                        else
                        {
                            world.ContainerManager.CalculateContainerPosition(serial, graphic);
                            x = world.ContainerManager.X;
                            y = world.ContainerManager.Y;
                            playsound = true;
                        }

                        UIManager.Add
                        (
                            new ContainerGump(world, item, graphic, playsound)
                            {
                                X = x,
                                Y = y,
                                InvalidateContents = true
                            }
                        );
                    }
                }

                EventSink.InvokeOnOpenContainer(item, serial);

                UIManager.RemovePosition(serial);
            }
            else
                Log.Error("[OpenContainer]: item not found");
        }

        if (graphic != 0x0030)
        {
            Item it = world.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;

                if (!it.IsCorpse && graphic != 0xFFFF)
                    Helpers.ItemHelpers.ClearContainerAndRemoveItems(world, it);
            }
        }
    }

    private static void UpdateLargeContainerGraphics(ref ushort graphic)
    {
        Gump gumps = Client.Game.UO.Gumps;

        switch (graphic)
        {
            case 0x0048:
                if (gumps.GetGump(0x06E8).Texture != null)
                    graphic = 0x06E8;

                break;

            case 0x0049:
                if (gumps.GetGump(0x9CDF).Texture != null)
                    graphic = 0x9CDF;

                break;

            case 0x0051:
                if (gumps.GetGump(0x06E7).Texture != null)
                    graphic = 0x06E7;

                break;

            case 0x003E:
                if (gumps.GetGump(0x06E9).Texture != null)
                    graphic = 0x06E9;

                break;

            case 0x004D:
                if (gumps.GetGump(0x06EA).Texture != null)
                    graphic = 0x06EA;

                break;

            case 0x004E:
                if (gumps.GetGump(0x06E6).Texture != null)
                    graphic = 0x06E6;

                break;

            case 0x004F:
                if (gumps.GetGump(0x06E5).Texture != null)
                    graphic = 0x06E5;

                break;

            case 0x004A:
                if (gumps.GetGump(0x9CDD).Texture != null)
                    graphic = 0x9CDD;

                break;

            case 0x0044:
                if (gumps.GetGump(0x9CE3).Texture != null)
                    graphic = 0x9CE3;

                break;
        }
    }

    private static void EventineUpdateLargeContainerGraphics(ref ushort graphic)
    {
        Gump gumps = Client.Game.UO.Gumps;

        switch (graphic)
        {
            case 0x0048:
                if (gumps.GetGump(0x06E8).Texture != null)
                    graphic = 0x06E8;

                break;

            case 0x0049:
                if (gumps.GetGump(0x9CDF).Texture != null)
                    graphic = 0x9CDF;

                break;

            case 0x0051:
                if (gumps.GetGump(0x06E7).Texture != null)
                    graphic = 0x06E7;

                break;

            case 0x003E:
                if (gumps.GetGump(0x06E9).Texture != null)
                    graphic = 0x06E9;

                break;

            case 0x004D:
                if (gumps.GetGump(0x06EA).Texture != null)
                    graphic = 0x06EA;

                break;

            case 0x004E:
                if (gumps.GetGump(0x06E6).Texture != null)
                    graphic = 0x06E6;

                break;

            case 0x004F:
                if (gumps.GetGump(0x06E5).Texture != null)
                    graphic = 0x06E5;

                break;

            case 0x004A:
                if (gumps.GetGump(0x9CDD).Texture != null)
                    graphic = 0x9CDD;

                break;

            case 0x0044:
                if (gumps.GetGump(0x9CE3).Texture != null)
                    graphic = 0x9CE3;

                break;

            case 0x0042:
                if (gumps.GetGump(0x9D6C).Texture != null)
                    graphic = 0x9D6C;

                break;

            case 19724:
                if (gumps.GetGump(0x9D6C).Texture != null)
                    graphic = 0x9D6C;

                break;

            case 75:
                if (gumps.GetGump(0x9D6B).Texture != null)
                    graphic = 0x9D6B;

                break;

            case 67:
                if (gumps.GetGump(0x9D6A).Texture != null)
                    graphic = 0x9D6A;

                break;
        }
    }
}
