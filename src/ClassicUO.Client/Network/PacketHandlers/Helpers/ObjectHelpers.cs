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
        Entity obj;

        bool itemHeld =
            Client.Game.UO.GameCursor.ItemHold.Enabled
            && Client.Game.UO.GameCursor.ItemHold.Serial == serial;

        if (itemHeld)
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

        // Single dictionary lookup (GetOrCreate* does the lookup and the create), avoiding the
        // old world.Get() followed by a second GetOrCreate* lookup on the same serial.
        if (SerialHelper.IsMobile(serial) && type != 3)
        {
            mobile = world.GetOrCreateMobile(serial, out created);

            if (mobile == null)
                return;

            obj = mobile;

            if (created)
            {
                mobile.Graphic = (ushort)(graphic + graphic_inc);
                mobile.CheckGraphicChange();
                mobile.Direction = direction & Direction.Up;
                mobile.FixHue(hue);
                mobile.X = x;
                mobile.Y = y;
                mobile.Z = z;
                mobile.Flags = flagss;
            }

            // Servers re-send object updates (0x78/0x25) for in-range objects even when nothing
            // changed. Skip the step enqueue, tile re-insert and events below when the update is
            // identical to current state. Restricted to settled mobiles (no pending steps) that
            // are already placed in the world.
            if (
                !created
                && mobile.Steps.Count == 0
                && mobile.X != 0xFFFF
                && mobile.Y != 0xFFFF
                && mobile.X == x
                && mobile.Y == y
                && mobile.Z == z
                && (direction & Direction.Up) == mobile.Direction
                && ((direction & Direction.Running) != 0) == mobile.IsRunning
                && mobile.Graphic == ((graphic + graphic_inc) & 0x3FFF)
                && mobile.Hue == GetFixedHue(hue)
                && mobile.Flags == flagss
            )
                return;
        }
        else
        {
            item = world.GetOrCreateItem(serial, out created);

            if (item == null)
                return;

            obj = item;

            if (!created)
            {
                ushort gfx = graphic;

                if (gfx != 0x2006)
                    gfx += graphic_inc;

                if (type == 2)
                    gfx &= 0x3FFF;

                // Same dedup as the mobile branch above: unchanged ground items are re-sent
                // periodically, and re-processing them is pure overhead. Held items and items
                // still inside a container must keep taking the full path (cursor/container UI).
                if (
                    item.OnGround
                    && !itemHeld
                    && item.X == x
                    && item.Y == y
                    && item.Z == z
                    && item.Graphic == gfx
                    && item.IsMulti == (type == 2)
                    && item.IsDamageable == (type == 3)
                    && item.LightID == (byte)direction
                    && (graphic != 0x2006 || item.Layer == (Layer)direction)
                    && item.Hue == GetFixedHue(hue)
                    && item.Amount == (count == 0 ? 1 : count)
                    && item.Flags == flagss
                    && item.Direction == direction
                )
                    return;

                if (SerialHelper.IsValid(item.Container))
                    world.RemoveItemFromContainer(item);
            }
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

    /// <summary>
    /// Mirrors <see cref="Entity.FixHue"/> so the dedup checks can predict the post-processing hue
    /// without mutating state. Conservative for script-highlighted entities: an active highlight
    /// keeps its own hue, which never matches here, so those skip dedup.
    /// </summary>
    private static ushort GetFixedHue(ushort hue)
    {
        ushort fixedColor = (ushort)(hue & 0x3FFF);

        if (fixedColor != 0)
        {
            if (fixedColor >= 0x0BB8)
            {
                fixedColor = 1;
            }

            fixedColor |= (ushort)(hue & 0xC000);
        }
        else
        {
            fixedColor = (ushort)(hue & 0x8000);
        }

        return fixedColor;
    }
}
