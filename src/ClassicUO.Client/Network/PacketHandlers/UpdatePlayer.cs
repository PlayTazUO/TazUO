using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdatePlayer
{
    public static void Receive(World world, ref StackDataReader p)
    {
        WalkDiagnostics.OnUpdatePlayerReceived();

        if (world.Player == null)
            return;

        uint serial = p.ReadUInt32BE();
        ushort graphic = p.ReadUInt16BE();
        byte graphic_inc = p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        var flags = (Flags)p.ReadUInt8();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        ushort serverID = p.ReadUInt16BE();
        var direction = (Direction)p.ReadUInt8();
        sbyte z = p.ReadInt8();

        Helpers.PlayerHelpers.UpdatePlayer(world, serial, graphic, graphic_inc, hue, flags, x, y, z, serverID, direction);
    }
}
