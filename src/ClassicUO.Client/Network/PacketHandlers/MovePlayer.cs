using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class MovePlayer
{
    public static void Receive(World world, ref StackDataReader p)
    {
        WalkDiagnostics.OnMovePlayerReceived();

        if (!world.InGame)
            return;

        var direction = (Direction)p.ReadUInt8();
        world.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
    }
}
