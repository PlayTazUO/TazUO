using System;
using System.Text;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal static class OpenCompressedGump
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint sender = p.ReadUInt32BE();
        uint gumpID = p.ReadUInt32BE();
        uint x = p.ReadUInt32BE();
        uint y = p.ReadUInt32BE();

        uint layoutCompressedRaw = p.ReadUInt32BE();

        // The wire value includes the 4 bytes of the decompressed-length field that follows.
        // A value below 4 would underflow to a huge length, so treat it as a bad packet.
        if (layoutCompressedRaw < 4)
        {
            Log.Error("[Initial]A bad compressed gump packet was received. Unable to process.");
            return;
        }

        uint layoutCompressedLen = layoutCompressedRaw - 4;
        int layoutDecompressedLen = (int)p.ReadUInt32BE();

        if (layoutDecompressedLen < 1)
        {
            Log.Error("[Initial]A bad compressed gump packet was received. Unable to process.");
            return;
        }

        byte[]
            layoutBuffer =
                new byte[layoutDecompressedLen]; //System.Buffers.ArrayPool<byte>.Shared.Rent(layoutDecompressedLen);
        string layout = null;

        try
        {
            ZLib.Decompress(p.Buffer.Slice(p.Position, (int)layoutCompressedLen),
                layoutBuffer.AsSpan(0, layoutDecompressedLen));
            layout = Encoding.UTF8.GetString(layoutBuffer.AsSpan(0, layoutDecompressedLen));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to decompress or decode gump layout: {ex.Message}");
            return;
        }
        // finally
        // {
        //     System.Buffers.ArrayPool<byte>.Shared.Return(layoutBuffer);
        // }

        p.Skip((int)layoutCompressedLen);

        string[] lines = Array.Empty<string>();

        try
        {
            uint linesNum = p.ReadUInt32BE();

            if (linesNum != 0)
            {
                uint linesCompressedRaw = p.ReadUInt32BE();

                if (linesCompressedRaw < 4)
                {
                    Log.Error("A bad compressed gump packet was received. Unable to process.");
                    return;
                }

                uint linesCompressedLen = linesCompressedRaw - 4;
                int linesDecompressedLen = (int)p.ReadUInt32BE();

                if (linesDecompressedLen < 1)
                {
                    Log.Error("A bad compressed gump packet was received. Unable to process.");
                    return;
                }

                // Each line occupies at least a 2-byte length prefix in the decompressed buffer,
                // so the reported line count can never legitimately exceed half the decompressed
                // size. Without this guard a corrupt/garbage line count would throw an
                // OverflowException (or OutOfMemoryException) on the array allocation below and
                // crash the client.
                if (linesNum > (uint)(linesDecompressedLen / 2))
                {
                    Log.Error("A bad compressed gump packet was received (invalid line count). Unable to process.");
                    return;
                }

                lines = new string[linesNum];

                byte[]
                    linesBuffer =
                        new byte[linesDecompressedLen]; //System.Buffers.ArrayPool<byte>.Shared.Rent(linesDecompressedLen);

                ZLib.Decompress(p.Buffer.Slice(p.Position, (int)linesCompressedLen),
                    linesBuffer.AsSpan(0, linesDecompressedLen));
                p.Skip((int)linesCompressedLen);

                var reader = new StackDataReader(linesBuffer.AsSpan(0, linesDecompressedLen));

                for (int i = 0; i < linesNum; ++i)
                {
                    int remaining = reader.Remaining;

                    if (remaining >= 2)
                    {
                        int length = reader.ReadUInt16BE();

                        if (length > 0)
                            lines[i] = reader.ReadUnicodeBE(length);
                        else
                            lines[i] = string.Empty;
                    }
                    else
                        lines[i] = string.Empty;
                }

                reader.Release();

                // finally
                // {
                //     System.Buffers.ArrayPool<byte>.Shared.Return(linesBuffer);
                // }
            }

            if (string.IsNullOrEmpty(layout))
            {
                Log.Error("Gump layout is null or empty. Unable to create gump.");
                return;
            }

            Helpers.GumpHelpers.CreateGump(world, sender, gumpID, (int)x, (int)y, layout, lines);
        }
        catch (Exception e)
        {
            HtmlCrashLogGen.Generate(e.ToString(),
                description:
                "TazUO almost crashed, it was prevented but this was put in place for debugging, please post this on our discord.");
        }
    }
}
