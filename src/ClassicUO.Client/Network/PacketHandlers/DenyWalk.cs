using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class DenyWalk
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        byte seq = p.ReadUInt8();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        var direction = (Direction)p.ReadUInt8();
        direction &= Direction.Up;
        sbyte z = p.ReadInt8();

        world.Player.Walker.DenyWalk(seq, x, y, z);
        world.Player.Direction = direction;

        // The weather is kept: a denied step doesn't change it, and the server won't resend what it already sent.
    }
}
