using System;
using System.Net;
using System.Net.NetworkInformation;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network;

public class ServerListEntry
{
    private IPAddress _ipAddress;
    private IPAddress _ipAddressLittleEndian;
    private Ping _pinger = new Ping();
    private bool _sending;
    private readonly bool[] _last10Results = new bool[10];
    private int _resultIndex;

    private ServerListEntry()
    {
    }

    public static ServerListEntry Create(ref StackDataReader p)
    {
        var entry = new ServerListEntry()
        {
            Index = p.ReadUInt16BE(),
            Name = p.ReadASCII(32, true),
            PercentFull = p.ReadUInt8(),
            Timezone = p.ReadUInt8(),
            Address = p.ReadUInt32BE()
        };

        // some server sends invalid ip.
        try
        {
            entry._ipAddress = new IPAddress
            (
                new byte[]
                {
                    (byte) ((entry.Address >> 24) & 0xFF),
                    (byte) ((entry.Address >> 16) & 0xFF),
                    (byte) ((entry.Address >> 8) & 0xFF),
                    (byte) (entry.Address & 0xFF)
                }
            );

            // IP address in little-endian format, required for server ping
            entry._ipAddressLittleEndian = new IPAddress
            (
                new byte[]
                {
                    (byte) (entry.Address & 0xFF),
                    (byte) ((entry.Address >> 8) & 0xFF),
                    (byte) ((entry.Address >> 16) & 0xFF),
                    (byte) ((entry.Address >> 24) & 0xFF)
                }
            );

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        entry._pinger.PingCompleted += entry.PingerOnPingCompleted;

        return entry;
    }


    public uint Address;
    public ushort Index;
    public string Name;
    public byte PercentFull;
    public byte Timezone;
    public int Ping = -1;
    public int PacketLoss;
    public IPStatus PingStatus;
    public long Sent;

    private static byte[] _buffData = new byte[32];
    private static PingOptions _pingOptions = new PingOptions(64, true);

    public void DoPing()
    {
        if (_ipAddress != null && !_sending && _pinger != null)
        {
            if (_resultIndex >= _last10Results.Length)
            {
                _resultIndex = 0;
            }

            try
            {
                _pinger.SendAsync
                (
                    _ipAddressLittleEndian,
                    1000,
                    _buffData,
                    _pingOptions,
                    _resultIndex++
                );
                Sent = Time.Ticks;

                _sending = true;
            }
            catch
            {
                _ipAddress = null;
                Dispose();
            }
        }
    }

    private void PingerOnPingCompleted(object sender, PingCompletedEventArgs e)
    {
        int index = (int)e.UserState;

        if (e.Reply != null)
        {
            Ping = (int)e.Reply.RoundtripTime;
            PingStatus = e.Reply.Status;

            _last10Results[index] = e.Reply.Status == IPStatus.Success;
        }

        if (index >= _last10Results.Length - 1)
        {
            PacketLoss = 0;

            for (int i = 0; i < _resultIndex; i++)
            {
                if (!_last10Results[i])
                {
                    ++PacketLoss;
                }
            }

            if (_resultIndex > 0)
            {
                PacketLoss = (int)Math.Round((PacketLoss * 100.0) / _resultIndex, MidpointRounding.AwayFromZero);
            }
            else
            {
                PacketLoss = 0;
            }

            _resultIndex = 0;
        }

        if (Ping == -1)
        {
            Ping = (int)(Time.Ticks - Sent);
        }

        _sending = false;
    }

    public void Dispose()
    {
        if (_pinger != null)
        {
            _pinger.PingCompleted -= PingerOnPingCompleted;

            if (_sending)
            {
                try
                {
                    _pinger.SendAsyncCancel();
                }
                catch { }

            }

            _pinger.Dispose();
            _pinger = null;
        }
    }
}
