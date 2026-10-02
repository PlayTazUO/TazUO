using System;
using System.Collections.Generic;
using System.Globalization;
using ClassicUO.Network.PacketHandlers;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network;

/// <summary>
///     Hex-dumps network traffic to <c>Logs/Network/packets.log</c>, or to stdout when no file was created.
///     Inactive until <see cref="Enabled"/> is set; the instance is not thread-safe, so a single logger must
///     not be shared across threads that write concurrently.
/// </summary>
internal sealed class PacketLogger
{
    /// <summary>Process-wide logger used by the network layer</summary>
    public static PacketLogger Default { get; set; } = new();

    /// <summary>
    ///     Packet IDs to log, empty meaning "log everything". Credential packets (0x80, 0x91) are dumped only
    ///     when listed here.
    /// </summary>
    public readonly List<byte> LogPacketId = [];

    private LogFile _logFile;

    /// <summary>
    ///     Packet ID to handler name, built on first use. Deferred because the registry pulls in every handler
    ///     type and logging is normally off.
    /// </summary>
    private readonly Lazy<Dictionary<uint, string>> _packetNames = new(() =>
    {
        var dict = new Dictionary<uint, string>();
        foreach ((uint id, PacketHandler _, string name) in PacketHandlerRegistry.GetHandlers())
            dict.TryAdd(id, name);
        return dict;
    });

    /// <summary>Whether <see cref="Log"/> writes anything; false discards every call</summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Opens a fresh <c>packets.log</c>, disposing any file this logger already held, and routes subsequent
    ///     output to it instead of stdout.
    /// </summary>
    /// <returns>The newly opened log file, owned by this logger</returns>
    public LogFile CreateFile()
    {
        _logFile?.Dispose();
        return _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");
    }

    /// <summary>
    ///     Writes a header plus hex/ASCII dump for one packet, skipping it when logging is off or when
    ///     <see cref="LogPacketId"/> filters its ID out.
    /// </summary>
    /// <param name="message">The complete packet, ID byte first; must not be empty</param>
    /// <param name="toServer">True for an outgoing packet, false for one received from the server</param>
    /// <param name="processingTimeMs">
    ///     How long the handler took, for received packets. Ignored when <paramref name="toServer"/> is true,
    ///     since outgoing packets have no handler pass to measure.
    /// </param>
    public void Log(Span<byte> message, bool toServer, double? processingTimeMs = null)
    {
        if (!Enabled) return;

        if (LogPacketId.Count != 0 && !LogPacketId.Contains(message[0]))
            return;

        const int off = sizeof(ulong) + 2;

        Span<char> span = stackalloc char[256];
        using var output = new ValueStringBuilder(span);

        output.Append(FormatHeader(message, toServer, processingTimeMs));

        if ((message[0] == 0x80 || message[0] == 0x91) &&
            (!LogPacketId.Contains(0x80) && !LogPacketId.Contains(0x91))) //Avoid logging account UNLESS requested specifically
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
    }

    /// <summary>
    ///     Builds the per-packet header block: thread, UTC timestamp, direction, ID and length, plus the
    ///     handler duration when one was measured.
    /// </summary>
    /// <param name="message">The complete packet, ID byte first</param>
    /// <param name="toServer">True for an outgoing packet, false for one received from the server</param>
    /// <param name="processingTimeMs">Handler duration, or null when not measured</param>
    /// <returns>A newline-terminated header, ending with a blank line before the dump</returns>
    private string FormatHeader(Span<byte> message, bool toServer, double? processingTimeMs)
    {
        string timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
        string packetDirection = toServer ? "Client -> Server" : "Server -> Client";

        string processingTimeFragment = "";
        if (!toServer && processingTimeMs.HasValue)
            processingTimeFragment = $"Processing time: {processingTimeMs.Value.ToString("F3", CultureInfo.InvariantCulture)}ms\n";

        // Only received packets can be named: the registry that supplies the names is the receive table, and
        // several client-to-server IDs reuse a server ID for an unrelated purpose.
        string packetNameOrId = toServer ? FormatPacketId(message[0]) : GetReceivedPacketName(message[0]);
        return $"Thread: {Environment.CurrentManagedThreadId}\nTime (UTC): {timestamp}\nDirection: {packetDirection}\nID: {packetNameOrId}\nLength: {message.Length}\n{processingTimeFragment}\n";
    }

    /// <summary>
    ///     Resolves a received packet's ID to its handler name for display.
    /// </summary>
    /// <param name="id">The packet's leading ID byte</param>
    /// <returns><c>Name (0xID)</c> when the ID is registered, otherwise the bare <c>0xID</c></returns>
    private string GetReceivedPacketName(byte id) =>
        _packetNames.Value.TryGetValue(id, out string name) ? $"{name} ({FormatPacketId(id)})" : FormatPacketId(id);

    /// <summary>Formats a packet ID as a two-digit hex literal</summary>
    /// <param name="id">The packet's leading ID byte</param>
    /// <returns>The ID as <c>0xID</c></returns>
    private static string FormatPacketId(byte id) => $"0x{id:X2}";
}
