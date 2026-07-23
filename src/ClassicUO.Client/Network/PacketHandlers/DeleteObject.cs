using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class DeleteObject
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        uint serial = p.ReadUInt32BE();

        if (world.Player == serial)
            return;

        Entity entity = world.Get(serial);

        if (entity == null)
            return;

        bool updateAbilities = false;

        if (entity is Item it)
        {
            uint cont = it.Container & 0x7FFFFFFF;

            if (SerialHelper.IsValid(it.Container))
            {
                Entity top = world.Get(it.RootContainer);

                if (top != null)
                {
                    if (it.Layer == Layer.Mount && top is Mobile mob)
                        mob.Mount = null;

                    if (top == world.Player)
                    {
                        updateAbilities =
                            it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                        Item tradeBoxItem = world.Player.GetSecureTradeBox();

                        if (tradeBoxItem != null)
                            UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                    }
                }

                if (cont == world.Player && it.Layer == Layer.Invalid)
                    Client.Game.UO.GameCursor.ItemHold.Enabled = false;

                if (it.Layer != Layer.Invalid)
                {
                    UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    UIManager.GetGump<ModernPaperdoll>(cont)?.RequestUpdateContents();
                }

                UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                #region GridContainer

                UIManager.GetGump<GridContainer>(cont)?.RequestUpdateContents();

                #endregion

                if (top != null && top.Graphic == 0x2006)
                {
                    UIManager.GetGump<NearbyLootGump>()?.RequestUpdateContents();
                    if (ProfileManager.CurrentProfile.CorpseContainerStyle is CorpseContainerStyle.OldGridLoot or CorpseContainerStyle.OldGridLootAndContainer)
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                }

                if (it.Graphic == 0x0EB0)
                {
                    UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();

                    BulletinBoardGump bbgump = UIManager.GetGump<BulletinBoardGump>();

                    if (bbgump != null)
                        bbgump.RemoveBulletinObject(serial);
                }
            }
        }

        if (world.CorpseManager.Exists(0, serial))
            return;

        if (entity is Mobile m)
        {
            if (world.Party.Contains(serial))
            {
                // m.RemoveFromTile();
            }

            // else
            {
                //BaseHealthBarGump bar = UIManager.GetGump<BaseHealthBarGump>(serial);

                //if (bar == null)
                //{
                //    NetClient.Socket.Send(new PCloseStatusBarGump(serial));
                //}

                world.RemoveMobile(serial, true);
            }
        }
        else
        {
            var item = (Item)entity;

            if (item.IsMulti)
                world.HouseManager.Remove(serial);

            Entity cont = world.Get(item.Container);

            if (cont != null)
            {
                cont.Remove(item);

                if (item.Layer != Layer.Invalid)
                {
                    UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    UIManager.GetGump<ModernPaperdoll>(cont)?.RequestUpdateContents();
                }
            }
            else if (item.IsMulti)
                UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            world.RemoveItem(serial, true);

            if (updateAbilities)
                world.Player.UpdateAbilities();
        }
    }
}
