using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

/// <summary>
///     Reason byte the server sends with a rejected item move (packet 0x27).
///     <see cref="EmptyMessageOnClient" /> and anything above it produce no client-side message.
/// </summary>
internal enum RejectMoveItemReason : byte
{
    /// <summary>The item cannot be lifted at all.</summary>
    CannotLiftItem = 0,

    /// <summary>The item is too far away to reach.</summary>
    OutOfRange = 1,

    /// <summary>Line of sight to the item is blocked.</summary>
    OutOfSight = 2,

    /// <summary>The item belongs to someone else.</summary>
    BelongsToAnother = 3,

    /// <summary>Something is already being held on the cursor.</summary>
    AlreadyHoldingSomething = 4,

    /// <summary>Rejected without a reason to display; the client shows nothing.</summary>
    EmptyMessageOnClient = 5
}

internal static class DenyMoveItem
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        Item firstItem = world.Items.Get(Client.Game.UO.GameCursor.ItemHold.Serial);

        if (
            Client.Game.UO.GameCursor.ItemHold.Enabled
            || (Client.Game.UO.GameCursor.ItemHold.Dropped
                && (firstItem == null || !firstItem.AllowedToDraw))
        )
        {
            if (world.ObjectToRemove == Client.Game.UO.GameCursor.ItemHold.Serial)
                world.ObjectToRemove = 0;

            if (
                SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Serial)
                && Client.Game.UO.GameCursor.ItemHold.Graphic != 0xFFFF
            )
            {
                if (!Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld)
                {
                    if (
                        Client.Game.UO.GameCursor.ItemHold.Layer == Layer.Invalid
                        && SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container)
                    )
                    {
                        // Server should send an UpdateContainedItem after this packet.
                        Console.WriteLine("=== DENY === ADD TO CONTAINER");

                        Helpers.ItemHelpers.AddItemToContainer(
                            world,
                            Client.Game.UO.GameCursor.ItemHold.Serial,
                            Client.Game.UO.GameCursor.ItemHold.Graphic,
                            Client.Game.UO.GameCursor.ItemHold.TotalAmount,
                            Client.Game.UO.GameCursor.ItemHold.X,
                            Client.Game.UO.GameCursor.ItemHold.Y,
                            Client.Game.UO.GameCursor.ItemHold.Hue,
                            Client.Game.UO.GameCursor.ItemHold.Container
                        );

                        UIManager
                            .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                    else
                    {
                        Item item = world.GetOrCreateItem(
                            Client.Game.UO.GameCursor.ItemHold.Serial
                        );

                        item.Graphic = Client.Game.UO.GameCursor.ItemHold.Graphic;
                        item.Hue = Client.Game.UO.GameCursor.ItemHold.Hue;
                        item.Amount = Client.Game.UO.GameCursor.ItemHold.TotalAmount;
                        item.Flags = Client.Game.UO.GameCursor.ItemHold.Flags;
                        item.Layer = Client.Game.UO.GameCursor.ItemHold.Layer;
                        item.X = Client.Game.UO.GameCursor.ItemHold.X;
                        item.Y = Client.Game.UO.GameCursor.ItemHold.Y;
                        item.Z = Client.Game.UO.GameCursor.ItemHold.Z;
                        item.CheckGraphicChange();

                        Entity container = world.Get(Client.Game.UO.GameCursor.ItemHold.Container);

                        if (container != null)
                        {
                            if (SerialHelper.IsMobile(container.Serial))
                            {
                                Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                world.RemoveItemFromContainer(item);
                                container.PushToBack(item);
                                item.Container = container.Serial;

                                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
                                UIManager.GetGump<ModernPaperdoll>(item.Container)?.RequestUpdateContents();
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === SOMETHING WRONG");

                                world.RemoveItem(item, true);
                            }
                        }
                        else
                        {
                            Console.WriteLine("=== DENY === ADD TO TERRAIN");

                            world.RemoveItemFromContainer(item);

                            item.SetInWorldTile(item.X, item.Y, item.Z);
                        }
                    }
                }
            }
            else
                Log.Error(
                    $"Wrong data: serial = {Client.Game.UO.GameCursor.ItemHold.Serial:X8}  -  graphic = {Client.Game.UO.GameCursor.ItemHold.Graphic:X4}"
                );

            UIManager.GetGump<SplitMenuGump>(Client.Game.UO.GameCursor.ItemHold.Serial)?.Dispose();

            Client.Game.UO.GameCursor.ItemHold.Clear();
        }
        else
            Log.Warn("There was a problem with ItemHold object. It was cleared before :|");

        // Actually RejectMoveItemReason
        byte rejectReason = p.ReadUInt8();

        if (rejectReason < 5)
            world.MessageManager.HandleMessage(
                null,
                ServerErrorMessages.GetError(p[0], rejectReason),
                string.Empty,
                0x03b2,
                MessageType.System,
                3,
                TextType.SYSTEM
            );
    }
}
