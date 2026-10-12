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
    private readonly List<byte> _received = new();

    private volatile Socket _socket;
    private Thread _thread;
    private volatile bool _running;
    private volatile bool _accepted;

    private string _host = string.Empty;
    private int _port;
    private string _user = string.Empty;
    private string _pass = string.Empty;

    /// <summary>True while a socket object exists, regardless of login state.</summary>
    public bool IsAlive => _socket != null;

    /// <summary>True once the server has accepted our login.</summary>
    public bool IsAccepted => _accepted;

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

        _host = host;
        _port = port;
        _user = user ?? string.Empty;
        _pass = pass ?? string.Empty;
        _received.Clear();
        _accepted = false;
        _running = true;

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "EnhancedMap"
        };

        _thread.Start();
    }

    private void Run()
    {
        bool graceful = true;
        Socket socket = null;

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(_host);
            IPAddress address = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                                ?? (addresses.Length > 0 ? addresses[0] : null);

            if (address == null)
            {
                graceful = false;
                return;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveTimeout = ReceiveTimeoutMs
            };

            socket.Connect(new IPEndPoint(address, _port));

            // Disconnect() may have run while Connect was in flight; don't publish a dead socket.
            if (!_running)
                return;

            _socket = socket;

            SendRaw(new EmPacketWriter(EnhancedMapProtocol.ProtocolRequest).U8(0).Build());

            var buffer = new byte[8192];

            while (_running)
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

                Reassemble(buffer.AsSpan(0, received));
            }
        }
        catch (Exception ex)
        {
            graceful = false;
            Log.Info($"EnhancedMap: connection to {_host}:{_port} failed ({ex.Message})");
        }
        finally
        {
            _accepted = false;
            _running = false;
            _socket = null;

            try
            {
                socket?.Dispose();
            }
            catch (Exception)
            {
                // Socket teardown failures are not actionable.
            }

            OnDisconnected?.Invoke(graceful);
        }
    }

    private void Reassemble(ReadOnlySpan<byte> data)
    {
        foreach (byte b in data)
            _received.Add(b);

        while (true)
        {
            if (_received.Count < EnhancedMapProtocol.FrameHeaderSize)
                return;

            int id = _received[0];
            int size = EnhancedMapProtocol.GetFrameLength(_received[1], _received[2]);

            if (size < EnhancedMapProtocol.FrameHeaderSize)
            {
                _received.Clear();
                return;
            }

            if (_received.Count < size)
                return;

            var payload = new byte[size - EnhancedMapProtocol.FrameHeaderSize];
            _received.CopyTo(EnhancedMapProtocol.FrameHeaderSize, payload, 0, payload.Length);
            _received.RemoveRange(0, size);

            HandleFrame((byte)id, payload);
        }
    }

    private void HandleFrame(byte id, byte[] payload)
    {
        switch (id)
        {
            case EnhancedMapProtocol.ProtocolRequest:
                SendLogin();
                break;

            case EnhancedMapProtocol.Login:
                _accepted = payload.Length > 0 && payload[0] == 1;
                if (!_accepted)
                    _running = false;
                break;
        }

        OnFrame?.Invoke(id, payload);
    }

    private void SendLogin()
    {
        byte[] frame = new EmPacketWriter(EnhancedMapProtocol.Login)
            .U8((byte)EnhancedMapProtocol.Wire.GetByteCount(_user))
            .U8((byte)EnhancedMapProtocol.Wire.GetByteCount(_pass))
            .Ascii(_user)
            .Ascii(_pass)
            .Build();

        SendRaw(frame);
    }

    /// <summary>Sends this client's player state (packet 0x25), if connected and accepted.</summary>
    public void SendPlayerData(
        ushort x, ushort y, byte map,
        ushort hits, ushort stamina, ushort mana,
        ushort maxHits, ushort maxStamina, ushort maxMana,
        byte flag, bool panic, uint color,
        string font, float fontSize, byte fontStyle)
    {
        if (!IsAccepted)
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

        SendRaw(frame);
    }

    /// <summary>Sends a one-shot alert marker at a world coordinate (packet 0x27).</summary>
    /// <param name="x">World X, clamped to the protocol's ushort range by the caller.</param>
    /// <param name="y">World Y, clamped to the protocol's ushort range by the caller.</param>
    public void SendAlert(ushort x, ushort y)
    {
        if (!IsAccepted)
            return;

        SendRaw(new EmPacketWriter(EnhancedMapProtocol.AlertAdvice).U16(x).U16(y).Build());
    }

    private void SendRaw(byte[] data)
    {
        Socket socket = _socket;

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
                _running = false;
            }
        }
    }

    /// <summary>Stops the receive loop and tears the socket down. Safe to call repeatedly.</summary>
    public void Disconnect()
    {
        _running = false;
        _accepted = false;

        Socket socket = _socket;
        _socket = null;

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
}
