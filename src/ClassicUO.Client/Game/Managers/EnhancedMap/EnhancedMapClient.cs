using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>
/// Owns the TCP socket that speaks the EnhancedMap protocol. Runs a single background thread that
/// performs the handshake, reassembles frames, and raises <see cref="OnFrame"/> for every received
/// packet. Sending is guarded by a lock so the game thread can push player data while the receive
/// thread is running.
/// </summary>
internal sealed class EnhancedMapClient : IDisposable
{
    private const int ReceiveTimeoutMs = 500;

    private readonly object _sendLock = new();

    private Connection _connection;
    private Thread _thread;

    /// <summary>True while a socket object exists, regardless of login state.</summary>
    public bool IsAlive => _connection?.Socket != null;

    /// <summary>True once the server has accepted our login.</summary>
    public bool IsAccepted => _connection?.Accepted == true;

    /// <summary>Raised on the receive thread for each reassembled frame (id, payload).</summary>
    public Action<byte, byte[]> OnFrame;

    /// <summary>Raised on the receive thread when the connection ends; argument is false on failure.</summary>
    public Action<bool> OnDisconnected;

    /// <summary>Starts a connection attempt on a background thread, replacing any existing one.</summary>
    /// <param name="host">Server host name or IP.</param>
    /// <param name="port">Server TCP port.</param>
    /// <param name="user">Login account name.</param>
    /// <param name="pass">Login password.</param>
    public void Connect(string host, int port, string user, string pass)
    {
        Disconnect();

        var connection = new Connection(host, port, user ?? string.Empty, pass ?? string.Empty);
        _connection = connection;

        _thread = new Thread(() => Run(connection))
        {
            IsBackground = true,
            Name = "EnhancedMap"
        };

        _thread.Start();
    }

    private void Run(Connection connection)
    {
        bool graceful = true;
        Socket socket = null;

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(connection.Host);
            IPAddress address = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                                ?? (addresses.Length > 0 ? addresses[0] : null);

            if (address == null)
            {
                graceful = false;
                return;
            }

            socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveTimeout = ReceiveTimeoutMs
            };

            socket.Connect(new IPEndPoint(address, connection.Port));

            // Disconnect() may have run while Connect was in flight; don't publish a dead socket.
            if (!connection.Running)
                return;

            connection.Socket = socket;

            SendRaw(connection, new EmPacketWriter(EnhancedMapProtocol.ProtocolRequest).U8(0).Build());

            var buffer = new byte[8192];

            while (connection.Running)
            {
                int received;

                try
                {
                    received = socket.Receive(buffer);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    continue;
                }
                catch (SocketException)
                {
                    graceful = false;
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (received <= 0)
                {
                    graceful = false;
                    break;
                }

                Reassemble(connection, buffer.AsSpan(0, received));
            }
        }
        catch (Exception ex)
        {
            graceful = false;
            Log.Info($"EnhancedMap: connection to {connection.Host}:{connection.Port} failed ({ex.Message})");
        }
        finally
        {
            connection.Accepted = false;
            connection.Running = false;
            connection.Socket = null;

            try
            {
                socket?.Dispose();
            }
            catch (Exception)
            {
                // Socket teardown failures are not actionable.
            }

            // Only the still-current connection reports its own disconnect; a superseded run
            // must not tear down or notify for the newer connection.
            if (Interlocked.CompareExchange(ref _connection, null, connection) == connection)
                OnDisconnected?.Invoke(graceful);
        }
    }

    private void Reassemble(Connection connection, ReadOnlySpan<byte> data)
    {
        List<byte> received = connection.Received;

        foreach (byte b in data)
            received.Add(b);

        while (true)
        {
            if (received.Count < EnhancedMapProtocol.FrameHeaderSize)
                return;

            int id = received[0];
            int size = EnhancedMapProtocol.GetFrameLength(received[1], received[2]);

            if (size < EnhancedMapProtocol.FrameHeaderSize)
            {
                received.Clear();
                return;
            }

            if (received.Count < size)
                return;

            var payload = new byte[size - EnhancedMapProtocol.FrameHeaderSize];
            received.CopyTo(EnhancedMapProtocol.FrameHeaderSize, payload, 0, payload.Length);
            received.RemoveRange(0, size);

            HandleFrame(connection, (byte)id, payload);
        }
    }

    private void HandleFrame(Connection connection, byte id, byte[] payload)
    {
        switch (id)
        {
            case EnhancedMapProtocol.ProtocolRequest:
                SendLogin(connection);
                break;

            case EnhancedMapProtocol.Login:
                connection.Accepted = payload.Length > 0 && payload[0] == 1;
                if (!connection.Accepted)
                    connection.Running = false;
                break;
        }

        OnFrame?.Invoke(id, payload);
    }

    private void SendLogin(Connection connection)
    {
        byte[] frame = new EmPacketWriter(EnhancedMapProtocol.Login)
            .U8((byte)EnhancedMapProtocol.Wire.GetByteCount(connection.User))
            .U8((byte)EnhancedMapProtocol.Wire.GetByteCount(connection.Pass))
            .Ascii(connection.User)
            .Ascii(connection.Pass)
            .Build();

        SendRaw(connection, frame);
    }

    /// <summary>Sends this client's player state (packet 0x25), if connected and accepted.</summary>
    public void SendPlayerData(
        ushort x, ushort y, byte map,
        ushort hits, ushort stamina, ushort mana,
        ushort maxHits, ushort maxStamina, ushort maxMana,
        byte flag, bool panic, uint color,
        string font, float fontSize, byte fontStyle)
    {
        Connection connection = _connection;

        if (connection == null || !connection.Accepted)
            return;

        byte[] frame = new EmPacketWriter(EnhancedMapProtocol.PlayerData)
            .U16(x).U16(y).U8(map)
            .U16(hits).U16(stamina).U16(mana)
            .U16(maxHits).U16(maxStamina).U16(maxMana)
            .U8(flag)
            .U8(0)
            .U8((byte)(panic ? 1 : 0))
            .U32(color)
            .U8((byte)EnhancedMapProtocol.Wire.GetByteCount(font))
            .Ascii(font)
            .F32(fontSize)
            .U8(fontStyle)
            .Build();

        SendRaw(connection, frame);
    }

    /// <summary>Sends a one-shot alert marker at a world coordinate (packet 0x27).</summary>
    /// <param name="x">World X, clamped to the protocol's ushort range by the caller.</param>
    /// <param name="y">World Y, clamped to the protocol's ushort range by the caller.</param>
    public void SendAlert(ushort x, ushort y)
    {
        Connection connection = _connection;

        if (connection == null || !connection.Accepted)
            return;

        SendRaw(connection, new EmPacketWriter(EnhancedMapProtocol.AlertAdvice).U16(x).U16(y).Build());
    }

    private void SendRaw(Connection connection, byte[] data)
    {
        Socket socket = connection.Socket;

        if (socket == null)
            return;

        lock (_sendLock)
        {
            try
            {
                int sent = 0;

                while (sent < data.Length)
                {
                    int n = socket.Send(data, sent, data.Length - sent, SocketFlags.None);

                    if (n <= 0)
                        break;

                    sent += n;
                }
            }
            catch (Exception)
            {
                connection.Running = false;
            }
        }
    }

    /// <summary>Stops the receive loop and tears the socket down. Safe to call repeatedly.</summary>
    public void Disconnect()
    {
        Connection connection = _connection;
        _connection = null;

        if (connection == null)
            return;

        connection.Running = false;
        connection.Accepted = false;

        Socket socket = connection.Socket;
        connection.Socket = null;

        try
        {
            socket?.Shutdown(SocketShutdown.Both);
        }
        catch (Exception)
        {
            // The peer may already be gone.
        }

        try
        {
            socket?.Close();
        }
        catch (Exception)
        {
            // Ignore.
        }
    }

    public void Dispose() => Disconnect();

    /// <summary>
    /// State for a single connection attempt. Kept separate from the owning client so a receive
    /// thread that has been superseded by a newer <see cref="Connect"/> cannot read or mutate the
    /// newer connection, and cannot report its disconnect as the current one.
    /// </summary>
    private sealed class Connection
    {
        public Connection(string host, int port, string user, string pass)
        {
            Host = host;
            Port = port;
            User = user;
            Pass = pass;
        }

        public string Host { get; }
        public int Port { get; }
        public string User { get; }
        public string Pass { get; }

        public volatile Socket Socket;
        public volatile bool Running = true;
        public volatile bool Accepted;

        public readonly List<byte> Received = new();
    }
}
