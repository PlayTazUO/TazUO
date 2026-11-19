using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers.Structs;

/// <summary>
/// Create a move item request. If <param name="layer"></param> is not Layer.Invalid, it will be an equip request instead of a move item.
/// </summary>
public readonly struct MoveRequest
{
    private readonly MoveType _moveType;

    /// <summary>
    /// Create a move item request. If <param name="layer"></param> is not Layer.Invalid, it will be an equip request instead of a move item.
    /// </summary>
    /// <param name="serial"></param>
    /// <param name="destination"></param>
    /// <param name="amount">0 = Grab full stack</param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="layer"></param>
    public MoveRequest(uint serial, uint destination, ushort amount = 0, int x = 0xFFFF, int y = 0xFFFF, int z = 0, Layer layer = Layer.Invalid, MoveType moveType = MoveType.Move)
    {
        _moveType = moveType;
        Serial = serial;
        Destination = destination;
        Amount = amount;
        X = x;
        Y = y;
        Z = z;
        Layer = layer;

        if (Amount == 0)
        {
            Item item = World.Instance.Items.Get(Serial);
            if (item != null)
                Amount = item.Amount;
        }

        if (Amount == 0)
            Amount = 1;
    }

    public uint Serial { get; }

    /// <summary>
    /// Set to uint.MaxValue to try to equip this instead of moving it.
    /// </summary>
    public uint Destination { get; }
    public ushort Amount { get; }
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public Layer Layer { get; }

    public void Execute()
    {
        ObjectActionQueue.Instance.OnMoveStarted(this);

        AsyncNetClient.Socket.Send_PickUpRequest(Serial, Amount);

        if(_moveType == MoveType.Move)
            GameActions.DropItem(Serial, X, Y, Z, Destination, true);
        else
            AsyncNetClient.Socket.Send_EquipRequest(Serial, Layer, Destination);
    }

    public static MoveRequest? ToLootBag(uint serial)
    {
        if (World.Instance.Items.TryGetValue(serial, out Item item))
        {
            Item backpack = World.Instance.Player.Backpack;

            if (backpack == null) return null;

            uint bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;

            return new(item.Serial, bag, item.Amount);
        }

        return null;
    }

    public static MoveRequest? EquipItem(uint serial, Layer layer)
    {
        Item i = World.Instance.Items.Get(serial);

        if (i == null) return null;

        return new MoveRequest(serial, World.Instance.Player, 1, 0xFFFF, 0xFFFF, 0, layer, MoveType.Equip);
    }
}

public static class MoveRequestExtensions
{
    public static MoveRequest? ToLootBag(this Item item) => MoveRequest.ToLootBag(item.Serial);

    public static ObjectActionQueueItem ToObjectActionQueueItem(this MoveRequest moveRequest) =>
        new(moveRequest.Execute)
        {
            Serial = moveRequest.Serial
        };
}

/// <summary>
/// Snapshot of the item state just before a queued move is sent to the server. If the server
/// rejects the move, this is used to put the item back where it came from because queued moves
/// do not populate <see cref="ItemHold"/> the way a manual drag does.
/// </summary>
public readonly struct ItemMoveSource
{
    public uint Serial { get; init; }
    public uint Container { get; init; }
    public ushort Graphic { get; init; }
    public ushort Hue { get; init; }
    public ushort Amount { get; init; }
    public Flags Flags { get; init; }
    public Layer Layer { get; init; }
    public ushort X { get; init; }
    public ushort Y { get; init; }
    public sbyte Z { get; init; }

    public static ItemMoveSource Capture(Item item) =>
        new()
        {
            Serial = item.Serial,
            Container = item.Container,
            Graphic = item.Graphic,
            Hue = item.Hue,
            Amount = item.Amount,
            Flags = item.Flags,
            Layer = item.Layer,
            X = item.X,
            Y = item.Y,
            Z = item.Z
        };

    /// <summary>
    /// Restore the item from this snapshot, moving it back into its original container or tile.
    /// </summary>
    public void Restore(World world)
    {
        Item item = world.Items.Get(Serial);

        // Pickup was rejected before the item left its place, so the client's model is still correct.
        if (item != null && !item.IsDestroyed && item.Container == Container && item.X == X && item.Y == Y && item.Z == Z)
            return;

        item = world.GetOrCreateItem(Serial);
        item.Graphic = Graphic;
        item.Hue = Hue;
        item.Amount = Amount;
        item.Flags = Flags;
        item.Layer = Layer;
        item.X = X;
        item.Y = Y;
        item.Z = Z;
        item.CheckGraphicChange();

        if (SerialHelper.IsValid(Container))
        {
            Entity container = world.Get(Container);

            if (container == null)
            {
                world.RemoveItem(item, true);
                return;
            }

            world.RemoveItemFromContainer(item);
            item.Container = Container;
            container.PushToBack(item);

            if (SerialHelper.IsMobile(container.Serial))
            {
                UIManager.GetGump<PaperDollGump>(Container)?.RequestUpdateContents();
                UIManager.GetGump<ModernPaperdoll>(Container)?.RequestUpdateContents();
            }
            else
            {
                UIManager.GetGump<ContainerGump>(Container)?.RequestUpdateContents();
                UIManager.GetGump<GridContainer>(Container)?.RequestUpdateContents();
            }
        }
        else
        {
            world.RemoveItemFromContainer(item);
            item.SetInWorldTile(X, Y, Z);
        }
    }
}

public enum MoveType
{
    Move,
    Equip
}
