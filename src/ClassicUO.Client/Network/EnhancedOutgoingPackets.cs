using System.Collections.Generic;
using System.IO;
using ClassicUO.IO;

namespace ClassicUO.Network;

internal static class EnhancedOutgoingPackets
{
    public static HashSet<EnhancedPacketType> EnabledPackets = new();
    public static void SendEnhancedPacket(this AsyncNetClient socket)
    {
        EnhancedPacketType id = EnhancedPacketType.EnableEnhancedPacket;

        if (!EnabledPackets.Contains(id))
            return;
        
        StackDataWriter writer = new(3);
        writer.SetHeader(id);
        writer.FinalLength();
        socket.Send(writer.BufferWritten);
    }

    private static void SetHeader(this StackDataWriter writer, EnhancedPacketType type)
    {
        writer.WriteUInt8(EnhancedPacketHandler.EPID); //Enhanced Packet ID
        writer.WriteZero(2); //Length - will update later
        writer.WriteUInt16BE((ushort)type); //Packet ID;
    }

    private static void FinalLength(this StackDataWriter writer)
    {
        writer.Seek(1, SeekOrigin.Begin);
        writer.WriteUInt16BE((ushort)writer.BytesWritten);
    }
}