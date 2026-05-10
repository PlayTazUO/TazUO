using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Network;

internal static class EnhancedOutgoingPackets
{
    public static HashSet<EnhancedPacketType> EnabledPackets = new();

    //Example server side packet handler:
    // private static void TazUOEnhancedPacket(NetState state, SpanReader reader)
    // {
    //     //Register packet elsewhere
    //     //IncomingPackets.Register(new PacketHandler(0xCE, &TazUOEnhancedPacket));
    //
    //     uint ID = reader.ReadUInt16();
    //
    //     Console.WriteLine($"------Got TUO Packet with ID: {ID}");
    //
    //
    //     switch (ID)
    //     {
    //         case 2:
    //             string v = reader.ReadAscii();
    //             Console.WriteLine($"-------TazUO Connected! V{v}");
    //             //state.IsTazUO = true; <-- Can use elsewhere to use different code if it's a TUO client
    //             break;
    //
    //         default:
    //             Console.WriteLine($"Got an unknown packet from TazUO: {ID}(0x{ID:X2})");
    //             break;
    //     }
    // }

    public static void Send_TazUO(this AsyncNetClient socket)
    {
        //Send [0xCE] [2] [VERSION STRING ASCII]
        EnhancedPacketType id = EnhancedPacketType.TazUO_Identifier;

        StackDataWriter f = Extensions.GetWriter(id);
        ref StackDataWriter writer = ref f;

        writer.WriteASCII(CUOEnviroment.Version);

        writer.FinalLength();

        socket.Send(writer.BufferWritten, true);
        writer.Dispose();
    }

    public static void SendEnhancedPacket(this AsyncNetClient socket)
    {
        EnhancedPacketType id = EnhancedPacketType.EnableEnhancedPacket;

        if (!EnabledPackets.Contains(id))
            return;

        StackDataWriter f = Extensions.GetWriter(id);
        ref StackDataWriter writer = ref f;
        writer.FinalLength();
        socket.Send(writer.BufferWritten, true);
        writer.Dispose();
    }
}
