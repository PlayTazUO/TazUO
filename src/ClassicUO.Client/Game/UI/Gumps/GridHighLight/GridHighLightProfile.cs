using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightSetupEntry
    {
        public string Name { get; set; }
        public List<string> ItemNames { get; set; } = new();
        public ushort Hue { get; set; }
        public string HighlightColor { get; set; } = "#FF0000";
        public List<GridHighlightProperty> Properties { get; set; } = new();
        public bool AcceptExtraProperties { get; set; } = true;
        public bool Overweight { get; set; } = true;
        public int MinimumProperty { get; set; } = 0;
        public int MaximumProperty { get; set; } = 0;
        public List<string> ExcludeNegatives { get; set; } = new();
        public List<string> RequiredRarities { get; set; } = new();
        public GridHighlightSlot GridHighlightSlot { get; set; } = new();
        public bool LootOnMatch { get; set; } = false;
        public bool IsHighlightProperties { get; set; } = true;

        public Color GetHighlightColor() => HighlightColor.FromHtmlHex();

        public void SetHighlightColor(Color color) => HighlightColor = color.ToHtmlHex();
    }

    public class GridHighlightSlot
    {
        public bool Talisman { get; set; } = true;
        public bool RightHand { get; set; } = true;
        public bool LeftHand { get; set; } = true;
        public bool Head { get; set; } = true;
        public bool Earring { get; set; } = true;
        public bool Neck { get; set; } = true;
        public bool Chest { get; set; } = true;
        public bool Shirt { get; set; } = true;
        public bool Back { get; set; } = true;
        public bool Robe { get; set; } = true;
        public bool Arms { get; set; } = true;
        public bool Hands { get; set; } = true;
        public bool Bracelet { get; set; } = true;
        public bool Ring { get; set; } = true;
        public bool Belt { get; set; } = true;
        public bool Skirt { get; set; } = true;
        public bool Legs { get; set; } = true;
        public bool Footwear { get; set; } = true;
        public bool Other { get; set; } = false;
    }

    public class GridHighlightProperty
    {
        public string Name { get; set; }
        public int MinValue { get; set; } = -1;
        public bool IsOptional { get; set; } = false;
    }
}
