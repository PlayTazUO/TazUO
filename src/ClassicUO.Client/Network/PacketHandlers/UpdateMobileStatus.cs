using ClassicUO.Game;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdateMobileStatus
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        byte status = p.ReadUInt8();

        //In testing, this packet seems to not be used?

        if (status == 1)
        {
            uint attackerSerial = p.ReadUInt32BE();
        }
    }
}
