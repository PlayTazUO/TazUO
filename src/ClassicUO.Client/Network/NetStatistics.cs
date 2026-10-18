// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace ClassicUO.Network
{
    /// <summary>
    ///     Source currently producing the in-game latency figure. Packet ping (0x73) is preferred;
    ///     some servers never answer it, so a session falls back to ICMP and finally gives up.
    /// </summary>
    public enum PingSource : byte
    {
        Packet,
        Icmp,
        Disabled
    }

    public sealed class NetStatistics(AsyncNetClient socket)
    {
        // How long a source gets to produce a result before we advance (or give up). The packet
        // ping is sent once per second from GameScene, so 5s covers several unanswered attempts.
        private const int FallbackWindowMs = 5000;

        private static readonly byte[] _icmpBuffer = new byte[32];
        private static readonly PingOptions _icmpOptions = new PingOptions(64, true);

        private uint _lastTotalBytesReceived, _lastTotalBytesSent, _lastTotalPacketsReceived, _lastTotalPacketsSent;
        private byte _pingIdx;

        private readonly uint[] _pings = new uint[5];

        // Send time of the oldest probe still awaiting a reply, or 0 when none is outstanding. Any
        // reply clears it. While it stays set its age keeps climbing, which is what lets Ping rise
        // during a stall instead of freezing on the last good sample. Shared by both ping sources,
        // which never overlap: switching source clears it.
        private uint _pendingSinceTick;
        private uint _startTickValue, _statisticsTimer;

        private PingSource _pingSource = PingSource.Packet;
        private uint _pingSourceStartTick;
        private bool _pingWindowStarted;

        // Written by the ICMP completion on the threadpool, drained on the game thread in SendPing.
        // Interlocked/volatile keep the hand-off safe without the game thread ever blocking.
        private int _icmpResult = -1;
        private int _icmpSending;
        private Ping _pinger;


        public DateTime ConnectedFrom { get; set; }

        public uint TotalBytesReceived { get; set; }

        public uint TotalBytesSent { get; set; }

        public uint TotalPacketsReceived { get; set; }

        public uint TotalPacketsSent { get; set; }

        public uint DeltaBytesReceived { get; private set; }

        public uint DeltaBytesSent { get; private set; }

        public uint DeltaPacketsReceived { get; private set; }

        public uint DeltaPacketsSent { get; private set; }

        public uint LastPingReceived { get; private set; } = Time.Ticks;

        /// <summary>Most recent latency sample, in ms; 0 until the first reply arrives. Unlike <see cref="Ping"/> this is not averaged.</summary>
        public uint LastPing { get; private set; }

        /// <summary>Source currently backing <see cref="Ping"/>; <see cref="PingSource.Disabled"/> means N/A.</summary>
        public PingSource CurrentPingSource => _pingSource;

        public uint Ping
        {
            get
            {
                byte count = 0;
                uint sum = 0;

                for (byte i = 0; i < 5; i++)
                {
                    if (_pings[i] != 0)
                    {
                        count++;
                        sum += _pings[i];
                    }
                }

                uint average = count == 0 ? 0 : sum / count;

                // An unanswered probe means the average understates how long we have actually been
                // waiting, so report the outstanding probe's age once it passes the average.
                return Math.Max(average, _pendingSinceTick == 0 ? 0 : Time.Ticks - _pendingSinceTick);
            }
        }

        public void PingReceived(byte idx)
        {
            if (_pingSource == PingSource.Disabled)
            {
                return;
            }

            // A reply proves the packet path works, so it is (once again) the primary source, and
            // nothing is left outstanding.
            _pingSource = PingSource.Packet;
            _pendingSinceTick = 0;

            _pings[idx % _pings.Length] = Time.Ticks - _startTickValue;
            LastPing = _pings[idx % _pings.Length];
            _pingSourceStartTick = Time.Ticks;
            LastPingReceived = Time.Ticks;
        }

        /// <summary>
        ///     Drives the ping source state machine and emits the current source's probe. Called
        ///     roughly once per second while in game (see <c>GameScene</c>).
        /// </summary>
        public void SendPing()
        {
            if (_pingSource == PingSource.Disabled || socket == null || !socket.IsConnected)
            {
                return;
            }

            if (!_pingWindowStarted)
            {
                // Start the fallback clock on the first in-game tick rather than at TCP connect,
                // so character-select time is not charged against the packet-ping window.
                _pingWindowStarted = true;
                _pingSourceStartTick = Time.Ticks;
            }

            if (_pingSource == PingSource.Packet)
            {
                if (Time.Ticks - _pingSourceStartTick >= FallbackWindowMs)
                {
                    // Abandon any packet probe still outstanding so its stale age does not pin the
                    // displayed latency once ICMP takes over.
                    _pingSource = PingSource.Icmp;
                    _pingSourceStartTick = Time.Ticks;
                    _pendingSinceTick = 0;
                }
                else
                {
                    _startTickValue = Time.Ticks;

                    // Only the first unanswered probe is stamped: repeated sends must not reset the
                    // age, otherwise a stall would sawtooth instead of climbing.
                    if (_pendingSinceTick == 0)
                    {
                        _pendingSinceTick = Time.Ticks;
                    }

                    socket.Send_Ping(_pingIdx);
                    _pingIdx = (byte)((_pingIdx + 1) % _pings.Length);
                    return;
                }
            }

            if (_pingSource == PingSource.Icmp)
            {
                int icmpResult = Interlocked.Exchange(ref _icmpResult, -1);

                if (icmpResult >= 0)
                {
                    _pings[_pingIdx] = (uint)icmpResult;
                    LastPing = _pings[_pingIdx];
                    _pingIdx = (byte)((_pingIdx + 1) % _pings.Length);
                    LastPingReceived = Time.Ticks;
                    _pingSourceStartTick = Time.Ticks;
                    _pendingSinceTick = 0;
                }
                else if (Time.Ticks - _pingSourceStartTick >= FallbackWindowMs)
                {
                    SetPingDisabled();
                }

                if (_pingSource == PingSource.Icmp)
                {
                    SendIcmpPing();
                }
            }

            // The packet ping is the only hang signal. Once we've fallen back (or given up), keep
            // the resync-hang check from firing forever on a now-stale packet-ping timestamp.
            if (_pingSource != PingSource.Packet)
            {
                LastPingReceived = Time.Ticks;
            }
        }

        public void Reset()
        {
            _statisticsTimer = 0;
            _pingIdx = 0;
            _pingSource = PingSource.Packet;
            _pingSourceStartTick = 0;
            _pingWindowStarted = false;
            _icmpResult = -1;
            _icmpSending = 0;
            _pendingSinceTick = 0;
            LastPing = 0;
            Array.Clear(_pings);
            DisposePinger();

            LastPingReceived = Time.Ticks;
            ConnectedFrom = DateTime.MinValue;
            _lastTotalBytesReceived = _lastTotalBytesSent = _lastTotalPacketsReceived = _lastTotalPacketsSent = 0;
            TotalBytesReceived = TotalBytesSent = TotalPacketsReceived = TotalPacketsSent = 0;
            DeltaBytesReceived = DeltaBytesSent = DeltaPacketsReceived = DeltaPacketsSent = 0;
        }

        public void Update()
        {
            if (_statisticsTimer > Time.Ticks) return;

            _statisticsTimer = Time.Ticks + 500;

            DeltaBytesReceived = TotalBytesReceived - _lastTotalBytesReceived;
            DeltaBytesSent = TotalBytesSent - _lastTotalBytesSent;
            DeltaPacketsReceived = TotalPacketsReceived - _lastTotalPacketsReceived;
            DeltaPacketsSent = TotalPacketsSent - _lastTotalPacketsSent;
            _lastTotalBytesReceived = TotalBytesReceived;
            _lastTotalBytesSent = TotalBytesSent;
            _lastTotalPacketsReceived = TotalPacketsReceived;
            _lastTotalPacketsSent = TotalPacketsSent;
        }

        public override string ToString() => $"Packets:\n >> {DeltaPacketsReceived}\n << {DeltaPacketsSent}\nBytes:\n >> {GetSizeAdaptive(DeltaBytesReceived)}\n << {GetSizeAdaptive(DeltaBytesSent)}";

        public static string GetSizeAdaptive(long bytes)
        {
            decimal num = bytes;
            string arg = "B";

            if (!(num < 1024m))
            {
                arg = "KB";
                num /= 1024m;

                if (!(num < 1024m))
                {
                    arg = "MB";
                    num /= 1024m;

                    if (!(num < 1024m))
                    {
                        arg = "GB";
                        num /= 1024m;
                    }
                }
            }

            return $"{Math.Round(num, 2):0.##} {arg}";
        }

        private void SetPingDisabled()
        {
            _pingSource = PingSource.Disabled;
            _icmpSending = 0;
            _pendingSinceTick = 0;
            DisposePinger();
        }

        private void SendIcmpPing()
        {
            if (!TryGetRemoteAddress(out IPAddress address))
            {
                SetPingDisabled();

                return;
            }

            EnsurePinger();

            if (_pinger == null || Interlocked.CompareExchange(ref _icmpSending, 1, 0) != 0)
            {
                return;
            }

            try
            {
                _pinger.SendAsync(address, 1000, _icmpBuffer, _icmpOptions, null);

                // Only stamp the first unanswered probe: repeated attempts must not reset the age,
                // otherwise a failing ICMP path would sawtooth instead of climbing.
                if (_pendingSinceTick == 0)
                {
                    _pendingSinceTick = Time.Ticks;
                }
            }
            catch
            {
                // ICMP can fail to even queue (socket exhaustion, restricted address). Treat it as
                // one failed attempt; the 5s window decides when the whole method is abandoned.
                Interlocked.Exchange(ref _icmpSending, 0);
            }
        }

        private void EnsurePinger()
        {
            if (_pinger != null)
            {
                return;
            }

            try
            {
                _pinger = new Ping();
                _pinger.PingCompleted += PingerOnPingCompleted;
            }
            catch
            {
                _pinger = null;
                SetPingDisabled();
            }
        }

        private void PingerOnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            // Ignore completions from a pinger disposed by Reset/disable.
            if (!ReferenceEquals(sender, _pinger))
            {
                return;
            }

            Interlocked.Exchange(ref _icmpSending, 0);

            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                Interlocked.Exchange(ref _icmpResult, (int)e.Reply.RoundtripTime);
            }
        }

        private bool TryGetRemoteAddress(out IPAddress address)
        {
            address = null;

            try
            {
                // Prefer the live socket endpoint over LoginHandshake.IP so relays/proxies are
                // pinged at the address actually carrying the game traffic.
                if (socket?.RemoteEndPoint is IPEndPoint endPoint && endPoint.Address != null)
                {
                    address = endPoint.Address;

                    if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
                    {
                        address = address.MapToIPv4();
                    }

                    return true;
                }
            }
            catch
            {
                // Fall through and report failure.
            }

            return false;
        }

        private void DisposePinger()
        {
            Ping pinger = _pinger;
            _pinger = null;

            if (pinger != null)
            {
                pinger.PingCompleted -= PingerOnPingCompleted;
                pinger.Dispose();
            }
        }
    }
}
