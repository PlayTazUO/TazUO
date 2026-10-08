// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Text;
using System.Xml;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class NetworkStatsGump : Gump
    {
        private static Point _last_position = new Point(-1, -1);

        private uint _ping, _deltaBytesReceived, _deltaBytesSent, _totalPacketsReceived, _totalPacketsSent;
        private bool _pingDisabled;
        private uint _time_to_update;
        private readonly AlphaBlendControl _trans;
        private string _cacheText = string.Empty;


        public NetworkStatsGump(World world, int x, int y) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _ping = _deltaBytesReceived = _deltaBytesSent = 0;

            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;
            Width = 100;
            Height = 30;

            Add
            (
                _trans = new AlphaBlendControl(.7f)
                {
                    Width = Width,
                    Height = Height
                }
            );


            LayerOrder = UILayer.Over;

            WantUpdateSize = false;
        }

        public override GumpType GumpType => GumpType.NetStats;

        public bool IsMinimized { get; set; }

        public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks > _time_to_update)
            {
                _time_to_update = Time.Ticks + 100;

                if (AsyncNetClient.Socket.IsConnected)
                {
                    _ping = AsyncNetClient.Socket.Statistics.Ping;
                    _pingDisabled = AsyncNetClient.Socket.Statistics.CurrentPingSource == PingSource.Disabled;
                    _deltaBytesReceived = AsyncNetClient.Socket.Statistics.DeltaBytesReceived;
                    _deltaBytesSent = AsyncNetClient.Socket.Statistics.DeltaBytesSent;
                    _totalPacketsReceived = AsyncNetClient.Socket.Statistics.TotalPacketsReceived;
                    _totalPacketsSent = AsyncNetClient.Socket.Statistics.TotalPacketsSent;
                }

                Span<char> span = stackalloc char[128];
                var sb = new ValueStringBuilder(span);

                string pingText = _pingDisabled ? "Ping: N/A" : $"Ping: {_ping} ms";

                if (IsMinimized)
                {
                    sb.Append(pingText);
                }
                else
                {
                    sb.Append($"{pingText}\n{"In:"} {NetStatistics.GetSizeAdaptive(_deltaBytesReceived),-6} {"Out:"} {NetStatistics.GetSizeAdaptive(_deltaBytesSent),-6}\nPkts In: {AbbreviateCount(_totalPacketsReceived)} Out: {AbbreviateCount(_totalPacketsSent)}");
                }

                _cacheText = sb.ToString();

                sb.Dispose();

                Vector2 size = Fonts.Bold.MeasureString(_cacheText);

                _trans.Width = Width = (int) (size.X + 20);
                _trans.Height = Height = (int) (size.Y + 20);
                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            if (_ping < 150)
            {
                hueVector.X = 0x44; // green
            }
            else if (_ping < 200)
            {
                hueVector.X = 0x34; // yellow
            }
            else if (_ping < 300)
            {
                hueVector.X = 0x31; // orange
            }
            else
            {
                hueVector.X = 0x20; // red
            }

            hueVector.Y = 1;

            batcher.DrawString
            (
                Fonts.Bold,
                _cacheText,
                x + 10,
                y + 10,
                hueVector
            );

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        /// <summary>Formats a packet count with a k/m/b suffix once it reaches the matching thousand.</summary>
        private static string AbbreviateCount(uint count)
        {
            if (count >= 1_000_000_000)
            {
                return $"{count / 1_000_000_000.0:0.##}b";
            }

            if (count >= 1_000_000)
            {
                return $"{count / 1_000_000.0:0.##}m";
            }

            if (count >= 1_000)
            {
                return $"{count / 1_000.0:0.##}k";
            }

            return count.ToString();
        }
    }
}
