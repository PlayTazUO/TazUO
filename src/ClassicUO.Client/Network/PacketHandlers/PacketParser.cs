// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal sealed class PacketParser
{
    #region Members

    private readonly PacketHandler[] _handlers = new PacketHandler[0x100];

    // Increased from 4096 to 65536 (64KB) to reduce Array.Resize frequency during packet processing
    private byte[] _readingBuffer = new byte[65536];

    private readonly CircularBuffer _buffer = new();
    private readonly CircularBuffer _pluginsBuffer = new();

    #endregion

    #region Singleton

    public static readonly PacketParser Instance = new();

    #endregion

    public PacketParser()
    {
        foreach ((uint id, PacketHandler handler) in PacketHandlerRegistry.GetHandlers())
            AddHandler(id, handler, false);
    }

    #region Publics

    public int ParsePackets(World world, Span<byte> data)
    {
        Profiler.EnterContext("APPEND");
        Append(data, false);
        Profiler.ExitContext("APPEND");

#if DEBUG
        string packet = _buffer == null || _buffer.Length == 0 ? "0xFF" : _buffer[0].ToString();

        Profiler.EnterContext(packet);
#endif

        int c = ParsePackets(world, _buffer, true) + ParsePackets(world, _pluginsBuffer, false);

#if DEBUG
        Profiler.ExitContext(packet);
#endif

        return c;
    }

    public void AddHandler(uint id, PacketHandler handler, bool allowOverride = true)
    {
        if (id >= _handlers.Length)
            throw new ArgumentOutOfRangeException($"A packet handler's ID must be between 0 and {_handlers.Length}");

        if (!allowOverride && _handlers[id] != null)
            throw new InvalidOperationException($"Handler {id} is already registered");

        _handlers[id] = handler;
    }

    /// <summary>
    /// Appends data to the internal reader buffer
    /// Can be used to 'inject' network traffic
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fromPlugins"></param>
    public void Append(Span<byte> data, bool fromPlugins)
    {
        if (data.IsEmpty)
            return;

        (fromPlugins ? _pluginsBuffer : _buffer).Enqueue(data);
    }

    #endregion

    #region Privates

    private int ParsePackets(World world, CircularBuffer stream, bool allowPlugins)
    {
        int packetsCount = 0;

        lock (stream)
        {
            ref byte[] packetBuffer = ref _readingBuffer;

            while (stream.Length > 0)
            {
                if (
                    !GetPacketInfo(
                        stream,
                        stream.Length,
                        out byte packetID,
                        out int offset,
                        out int packetlength
                    )
                )
                {
                    Log.Warn(
                        $"Invalid ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                    );

                    break;
                }

                if (stream.Length < packetlength)
                {
                    Log.Warn(
                        $"Need more data ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                    );

                    // need more data
                    break;
                }

                while (packetlength > packetBuffer.Length)
                {
                    Profiler.EnterContext("PACKET_BUFFER_RESIZE");
                    int oldSize = packetBuffer.Length;
                    int newSize = packetBuffer.Length * 2;

                    Log.Warn(
                        $"PacketHandler buffer resize from {oldSize} to {newSize} for packet length {packetlength} (may cause spike)");

                    Array.Resize(ref packetBuffer, newSize);
                    Profiler.ExitContext("PACKET_BUFFER_RESIZE");
                }

                _ = stream.Dequeue(packetBuffer, 0, packetlength);

                PacketLogger.Default?.Log(packetBuffer.AsSpan(0, packetlength), false);

                if (!allowPlugins || Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
                {
                    AnalyzePacket(world, packetBuffer.AsSpan(0, packetlength), offset);

                    ++packetsCount;
                }
            }
        }

        return packetsCount;
    }

    private void AnalyzePacket(World world, ReadOnlySpan<byte> data, int offset)
    {
        if (data.IsEmpty)
            return;

        PacketHandler handler = _handlers[data[0]];

        if (handler != null)
        {
            var buffer = new StackDataReader(data);
            buffer.Seek(offset);

            handler(world, ref buffer);
        }
    }

    private static bool GetPacketInfo(
        CircularBuffer buffer,
        int bufferLen,
        out byte packetID,
        out int packetOffset,
        out int packetLen
    )
    {
        if (buffer == null || bufferLen <= 0)
        {
            packetID = 0xFF;
            packetLen = 0;
            packetOffset = 0;

            return false;
        }

        packetLen = AsyncNetClient.PacketsTable.GetPacketLength(packetID = buffer[0]);
        packetOffset = 1;

        if (packetLen == -1)
        {
            if (bufferLen < 3)
                return false;

            byte b0 = buffer[1];
            byte b1 = buffer[2];

            packetLen = (b0 << 8) | b1;
            packetOffset = 3;
        }

        return true;
    }

    #endregion
}
