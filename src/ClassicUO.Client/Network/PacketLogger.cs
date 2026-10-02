using System;
using System.Collections.Generic;
using System.Globalization;
using ClassicUO.Network.PacketHandlers;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network;

internal sealed class PacketLogger
{
    public static PacketLogger Default { get; set; } = new();
    public readonly List<byte> LogPacketId = [];

    private LogFile _logFile;

    private readonly Lazy<Dictionary<uint, string>> _packetNames = new(() =>
    {
        var dict = new Dictionary<uint, string>();
        foreach ((uint id, PacketHandler _, string name) in PacketHandlerRegistry.GetHandlers())
            dict.Add(id, name);
        return dict;
    });

    public bool Enabled { get; set; }

    public LogFile CreateFile()
    {
        _logFile?.Dispose();
        return _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");
    }

    public void Log(Span<byte> message, bool toServer, long? processingTimeMs = null)
    {
        if (!Enabled) return;

        if (LogPacketId.Count != 0 && !LogPacketId.Contains(message[0]))
            return;

        Span<char> span = stackalloc char[256];
        var output = new ValueStringBuilder(span);
        {
            const int off = sizeof(ulong) + 2;

            output.Append(FormatHeader(message, toServer, processingTimeMs));

            if ((message[0] == 0x80 || message[0] == 0x91) &&
                (!LogPacketId.Contains(0x80) || !LogPacketId.Contains(0x91))) //Avoid logging account UNLESS requested specifically
            {
                output.Append(' ', off);
                output.Append("[ACCOUNT CREDENTIALS HIDDEN]\n");
            }
            else
            {
                output.Append(' ', off);
                output.Append("0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F\n");

                output.Append(' ', off);
                output.Append("-- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --\n");

                ulong address = 0;

                for (int i = 0; i < message.Length; i += 16, address += 16)
                {
                    output.Append($"{address:X8}");

                    for (int j = 0; j < 16; ++j)
                    {
                        if (j % 8 == 0)
                            output.Append(" ");

                        output.Append(i + j < message.Length ? $" {message[i + j]:X2}" : "   ");
                    }

                    output.Append("  ");

                    for (int j = 0; j < 16 && i + j < message.Length; ++j)
                    {
                        byte c = message[i + j];

                        if (c is >= 0x20 and < 0x80)
                            output.Append((char)c);
                        else
                            output.Append('.');
                    }

                    output.Append('\n');
                }
            }

            output.Append('\n');
            output.Append('\n');

            string s = output.ToString();

            if (_logFile != null)
                _logFile.Write(s);
            else
                Console.WriteLine(s);

            output.Dispose();
        }
    }

    private string FormatHeader(Span<byte> message, bool toServer, long? processingTimeMs)
    {
        string timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string packetDirection = toServer ? "Client -> Server" : "Server -> Client";

        string processingTimeFragment = "";
        if (!toServer && processingTimeMs.HasValue)
            processingTimeFragment = $"Processing time: {processingTimeMs.Value.ToString()}ms\n";

        string packetNameOrId = GetPacketName(message[0]);
        return $"Thread: {Environment.CurrentManagedThreadId}\nTime: {timestamp}\nDirection: {packetDirection}\nID: {packetNameOrId}\nLength: {message.Length}\n{processingTimeFragment}\n";
    }

    private string GetPacketName(byte id)
    {
        string packetId = $"0x{id:X2}";
        return _packetNames.Value.TryGetValue(id, out string name) ? $"{name} ({packetId})" : packetId;
    }
}
