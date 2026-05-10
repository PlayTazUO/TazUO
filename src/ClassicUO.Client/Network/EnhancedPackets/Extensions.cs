using System.IO;
using ClassicUO.IO;
using ClassicUO.Network;

public static class Extensions
{
    public static StackDataWriter GetWriter(EnhancedPacketType type, bool noLength = false)
    {
        StackDataWriter writer = new(64);
        writer.WriteUInt8(EnhancedPacketHandler.EPID);
        if (!noLength)
            writer.WriteZero(2);
        writer.WriteUInt16BE((ushort)type);
        return writer;
    }

    /// <summary>
    /// Set the length byte after writing all the data.
    /// </summary>
    public static void FinalLength(this ref StackDataWriter writer, int? length = null, bool noLength = false)
    {
        if (length.HasValue) writer.WriteZero(length.Value - writer.BytesWritten);

        if (noLength) return;
        writer.Seek(1, SeekOrigin.Begin);
        writer.WriteUInt16BE((ushort)writer.BytesWritten);
    }
}
