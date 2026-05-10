// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Resources;
using System;

namespace ClassicUO.Game.Data
{
    public enum Lock : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    public sealed class Skill
    {
        public Skill(string name, int index, bool click)
        {
            Name = name;
            Index = index;
            IsClickable = click;
        }

        public Lock Lock { get; internal set; }

        public ushort ValueFixed { get; internal set; }

        public ushort BaseFixed { get; internal set; }

        public ushort BaseFixedAtLogin { get; internal set; }

        public bool HasLoginBaseline { get; internal set; }

        public ushort CapFixed { get; internal set; }

        public float Value => ValueFixed / 10.0f;

        public float Base => BaseFixed / 10.0f;

        public float BaseAtLogin
        {
            get
            {
                return BaseFixedAtLogin / 10.0f;
            }
            set
            {
                BaseFixedAtLogin = (ushort)(value * 10.0f);
            }
        }

        public float Cap => CapFixed / 10.0f;

        public bool IsClickable { get; }

        public string Name { get; }

        public int Index { get; }

        public override string ToString() => string.Format(ResGeneral.Name0Val1, Name, Value);
    }
}
