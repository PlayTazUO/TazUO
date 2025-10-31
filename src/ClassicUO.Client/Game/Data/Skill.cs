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
        public static event EventHandler<SkillChangeArgs> SkillValueChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillBaseChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillCapChangedEvent;

        public Skill(string name, int index, bool click)
        {
            Name = name;
            Index = index;
            IsClickable = click;
        }

        public Lock Lock { get; internal set; }

        public ushort ValueFixed { get; internal set; }

        public ushort BaseFixed { get; internal set; }

        public ushort CapFixed { get; internal set; }

        public float Value => ValueFixed / 10.0f;

        public float Base => BaseFixed / 10.0f;

        public float Cap => CapFixed / 10.0f;

        public bool IsClickable { get; }

        public string Name { get; }

        public int Index { get; }

        public static void InvokeSkillValueChanged(int index) => SkillValueChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        public static void InvokeSkillBaseChanged(int index) => SkillBaseChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        public static void InvokeSkillCapChanged(int index) => SkillCapChangedEvent?.Invoke(null, new SkillChangeArgs(index));

        public override string ToString() => string.Format(ResGeneral.Name0Val1, Name, Value);

        public class SkillChangeArgs : EventArgs
        {
            public int Index;
            public SkillChangeArgs(int index)
            {
                Index = index;
            }
        }
    }
}