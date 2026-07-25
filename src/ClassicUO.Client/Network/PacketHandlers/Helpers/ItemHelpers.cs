using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers.Helpers;

internal static class ItemHelpers
{
    public static void AddItemToContainer(
        World world,
        uint serial,
        ushort graphic,
        ushort amount,
        ushort x,
        ushort y,
        ushort hue,
        uint containerSerial
    )
    {
        if (Client.Game.UO.GameCursor.ItemHold.Serial == serial)
            if (Client.Game.UO.GameCursor.ItemHold.Dropped)
                Client.Game.UO.GameCursor.ItemHold.Clear();

        Entity container = world.Get(containerSerial);

        if (container == null)
        {
            Log.Warn($"No container ({containerSerial}) found");

            //container = world.GetOrCreateItem(containerSerial);
            return;
        }

        Item item = world.Items.Get(serial);

        if (SerialHelper.IsMobile(serial))
        {
            world.RemoveMobile(serial, true);
            Log.Warn("AddItemToContainer function adds mobile as Item");
        }

        //Added item.Container != containerSerial to prevent closing containers when changing facets
        if (item != null && item.Container != containerSerial &&
            (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            world.RemoveItem(item, true);

        // Track if item is newly created
        bool itemWasCreated = item == null || item.IsDestroyed;

        item = world.GetOrCreateItem(serial);
        item.Graphic = graphic;
        item.CheckGraphicChange();
        item.Amount = amount;
        item.FixHue(hue);
        item.X = x;
        item.Y = y;
        item.Z = 0;

        //Added item.Container != containerSerial to prevent closing containers when changing facets
        //Shouldn't need to remove it just to add it back in
        if (item.Container != containerSerial)
        {
            world.RemoveItemFromContainer(item);
            item.Container = containerSerial;
        }

        container.PushToBack(item);

        // Fire event after item is fully configured
        if (itemWasCreated)
            EventSink.InvokeOnItemCreated(item);
        else
            EventSink.InvokeOnItemUpdated(item);

        if (SerialHelper.IsMobile(containerSerial))
        {
            Mobile m = world.Mobiles.Get(containerSerial);
            Item secureBox = m?.GetSecureTradeBox();

            if (secureBox != null)
                UIManager.GetTradingGump(secureBox)?.RequestUpdateContents();
            else
            {
                UIManager.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                UIManager.GetGump<ModernPaperdoll>(containerSerial)?.RequestUpdateContents();
            }
        }
        else if (SerialHelper.IsItem(containerSerial))
        {
            ItemDatabaseManager.Instance.AddOrUpdateItem(item, world);

            Gump gump = UIManager.GetGump<BulletinBoardGump>(containerSerial);

            if (gump != null)
                AsyncNetClient.Socket.Send_BulletinBoardRequestMessageSummary(
                    containerSerial,
                    serial
                );
            else
            {
                gump = UIManager.GetGump<SpellbookGump>(containerSerial);

                if (gump == null)
                {
                    gump = UIManager.GetGump<ContainerGump>(containerSerial);

                    if (gump != null)
                        ((ContainerGump)gump).CheckItemControlPosition(item);

                    #region GridContainer

                    GridContainer gridGump = UIManager.GetGump<GridContainer>(containerSerial);
                    if (gridGump != null)
                        gridGump.RequestUpdateContents();

                    #endregion

                    if (ProfileManager.CurrentProfile.CorpseContainerStyle is CorpseContainerStyle.OldGridLoot or CorpseContainerStyle.OldGridLootAndContainer)
                    {
                        GridLootGump grid_gump = UIManager.GetGump<GridLootGump>(
                            containerSerial
                        );

                        if (
                            grid_gump == null
                            && SerialHelper.IsValid(SharedStore.RequestedGridLoot)
                            && SharedStore.RequestedGridLoot == containerSerial
                        )
                        {
                            grid_gump = new GridLootGump(world, SharedStore.RequestedGridLoot);
                            UIManager.Add(grid_gump);
                            SharedStore.RequestedGridLoot = 0;
                        }

                        grid_gump?.RequestUpdateContents();
                    }

                    UIManager.GetGump<NearbyLootGump>()?.RequestUpdateContents();
                }

                if (gump != null)
                {
                    if (SerialHelper.IsItem(containerSerial))
                        ((Item)container).Opened = true;

                    gump.RequestUpdateContents();
                }
            }
        }

        UIManager.GetTradingGump(containerSerial)?.RequestUpdateContents();
    }

    public static void ClearContainerAndRemoveItems(
        World world,
        Entity container,
        bool remove_unequipped = false
    )
    {
        if (container == null || container.IsEmpty)
            return;

        LinkedObject first = container.Items;
        LinkedObject new_first = null;

        while (first != null)
        {
            LinkedObject next = first.Next;
            var it = (Item)first;

            if (remove_unequipped && it.Layer != 0)
            {
                if (new_first == null)
                    new_first = first;
            }
            else
                world.RemoveItem(it, true);

            first = next;
        }

        container.Items = remove_unequipped ? new_first : null;
    }
}
