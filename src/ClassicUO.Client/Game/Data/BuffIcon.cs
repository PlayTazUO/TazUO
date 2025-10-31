// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Data
{
    public class BuffIcon : IEquatable<BuffIcon>
    {
        public BuffIcon(BuffIconType type, ushort graphic, long timer, string text, string title = "")
        {
            Type = type;
            Graphic = graphic;
            Timer = (timer <= 0 ? 0xFFFF_FFFF : Time.Ticks + timer * 1000);
            Text = text;
            Title = title;
        }

        public bool Equals(BuffIcon other) => other != null && Type == other.Type;

        public readonly ushort Graphic;

        public readonly string Text;

        public readonly long Timer;

        public readonly BuffIconType Type;

        public readonly string Title;
    }
}