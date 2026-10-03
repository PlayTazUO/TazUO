using ClassicUO.Game;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class OpenGump
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        uint sender = p.ReadUInt32BE();
        uint gumpID = p.ReadUInt32BE();
        int x = (int)p.ReadUInt32BE();
        int y = (int)p.ReadUInt32BE();

        ushort cmdLen = p.ReadUInt16BE();
        string cmd = p.ReadASCII(cmdLen);

        ushort textLinesCount = p.ReadUInt16BE();

        string[] lines = new string[textLinesCount];

        for (int i = 0; i < textLinesCount; ++i)
        {
            int length = p.ReadUInt16BE();

            if (length > 0)
                lines[i] = p.ReadUnicodeBEFixed(length);
            else
                lines[i] = string.Empty;
        }

        Helpers.GumpHelpers.CreateGump(world, sender, gumpID, x, y, cmd, lines);
    }
}
