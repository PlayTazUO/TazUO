using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Network.PacketHandlers.Helpers;

public static class ObjectHelpers
{
    public static void UpdateGameObject(
        World world,
        uint serial,
        ushort graphic,
        byte graphic_inc,
        ushort count,
        ushort x,
        ushort y,
        sbyte z,
        Direction direction,
        ushort hue,
        Flags flagss,
        int UNK,
        byte type,
        ushort UNK_2
    )
    {
        Mobile mobile = null;
        Item item = null;
        Entity obj = world.Get(serial);

        if (
            Client.Game.UO.GameCursor.ItemHold.Enabled
            && Client.Game.UO.GameCursor.ItemHold.Serial == serial
        )
        {
            if (SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container))
            {
                if (Client.Game.UO.GameCursor.ItemHold.Layer == 0)
                    UIManager
                        .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                        ?.RequestUpdateContents();
                else
                {
                    UIManager.GetGump<PaperDollGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                        ?.RequestUpdateContents();
                    UIManager.GetGump<ModernPaperdoll>(Client.Game.UO.GameCursor.ItemHold.Container)
                        ?.RequestUpdateContents();
                }
            }

            Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld = true;
        }

        bool created = false;

        if (obj == null || obj.IsDestroyed)
        {
            created = true;

            if (SerialHelper.IsMobile(serial) && type != 3)
            {
                mobile = world.GetOrCreateMobile(serial);

                if (mobile == null)
                    return;

                obj = mobile;
                mobile.Graphic = (ushort)(graphic + graphic_inc);
                mobile.CheckGraphicChange();
                mobile.Direction = direction & Direction.Up;
                mobile.FixHue(hue);
                mobile.X = x;
                mobile.Y = y;
                mobile.Z = z;
                mobile.Flags = flagss;
            }
            else
            {
                item = world.GetOrCreateItem(serial);

                if (item == null)
                    return;

                obj = item;
            }
        }
        else
        {
            if (obj is Item item1)
            {
                item = item1;

                if (SerialHelper.IsValid(item.Container))
                    world.RemoveItemFromContainer(item);
            }
            else
                mobile = (Mobile)obj;
        }

        if (obj == null)
            return;

        if (item != null)
        {
            if (graphic != 0x2006)
                graphic += graphic_inc;

            if (type == 2)
            {
                item.IsMulti = true;
                item.WantUpdateMulti =
                    (graphic & 0x3FFF) != item.Graphic
                    || item.X != x
                    || item.Y != y
                    || item.Z != z
                    || item.Hue != hue;
                item.Graphic = (ushort)(graphic & 0x3FFF);
            }
            else
            {
                item.IsDamageable = type == 3;
                item.IsMulti = false;
                item.Graphic = graphic;
            }

            item.X = x;
            item.Y = y;
            item.Z = z;
            item.LightID = (byte)direction;

            if (graphic == 0x2006)
                item.Layer = (Layer)direction;

            item.FixHue(hue);

            // FixHue above overwrites the hue with the server-provided value, which clobbers
            // the looted-corpse hue applied via AddCorpse when the graphic was set. Re-apply it
            // so previously looted corpses keep their hue when removed and readded due to range.
            if (graphic == 0x2006)
                AutoLootManager.ApplyLootedHueIfNeeded(item);

            if (count == 0)
                count = 1;

            item.Amount = count;
            item.Flags = flagss;
            item.Direction = direction;
            item.CheckGraphicChange(item.AnimIndex);

            if (created)
                EventSink.InvokeOnItemCreated(item);
            else
                EventSink.InvokeOnItemUpdated(item);

            // Update item database
            ItemDatabaseManager.Instance.AddOrUpdateItem(item, world);
        }
        else
        {
            graphic += graphic_inc;

            if (serial != world.Player)
            {
                Direction cleaned_dir = direction & Direction.Up;
                bool isrun = (direction & Direction.Running) != 0;

                if (world.Get(mobile) == null || (mobile.X == 0xFFFF && mobile.Y == 0xFFFF))
                {
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Direction = cleaned_dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                }

                if (!mobile.EnqueueStep(x, y, z, cleaned_dir, isrun))
                {
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Direction = cleaned_dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                }
            }

            mobile.Graphic = (ushort)(graphic & 0x3FFF);
            mobile.FixHue(hue);
            mobile.Flags = flagss;
        }

        if (created && !obj.IsClicked)
        {
            if (mobile != null)
            {
                if (ProfileManager.CurrentProfile.ShowNewMobileNameIncoming)
                    GameActions.SingleClick(world, serial);
            }
            else if (graphic == 0x2006)
                if (ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming)
                    GameActions.SingleClick(world, serial);
        }

        if (mobile != null)
        {
            mobile.SetInWorldTile(mobile.X, mobile.Y, mobile.Z);

            if (created)
            {
                // This is actually a way to get all Hp from all new mobiles.
                // Real UO client does it only when LastAttack == serial.
                // We force to close suddenly.
                GameActions.RequestMobileStatus(world, serial);
                EventSink.InvokeMobileCreated(mobile);

                //if (TargetManager.LastAttack != serial)
                //{
                //    GameActions.SendCloseStatus(serial);
                //}
            }
        }
        else
        {
            if (
                Client.Game.UO.GameCursor.ItemHold.Serial == serial
                && Client.Game.UO.GameCursor.ItemHold.Dropped
            )
            {
                // we want maintain the item data due to the denymoveitem packet
                //ItemHold.Clear();
                Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                Client.Game.UO.GameCursor.ItemHold.Dropped = false;
            }

            if (item.OnGround)
            {
                item.SetInWorldTile(item.X, item.Y, item.Z);

                if (graphic == 0x2006)
                {
                    if (created)
                        EventSink.InvokeOnCorpseCreated(item);

                    if (ProfileManager.CurrentProfile.AutoOpenCorpses)
                        world.Player.TryOpenCorpses();
                }
            }
        }
    }
}
