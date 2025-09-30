using System;

namespace ClassicUO.Game.Data
{
    public class ItemInfo
    {
        public uint Serial { get; set; }
        public ushort Graphic { get; set; }
        public ushort Hue { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Properties { get; set; } = string.Empty;
        public uint Container { get; set; }
        public Layer Layer { get; set; }
        public DateTime UpdatedTime { get; set; }
        public uint Character { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public bool OnGround { get; set; }
    }
}