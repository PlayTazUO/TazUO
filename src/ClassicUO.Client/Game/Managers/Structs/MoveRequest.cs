using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
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

    public static ObjectActionQueueItem ToObjectActionQueueItem(this MoveRequest moveRequest) => new(moveRequest.Execute);
}

public enum MoveType
{
    Move,
    Equip
}
