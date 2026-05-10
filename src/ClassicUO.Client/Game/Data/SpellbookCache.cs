using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Data
{
    public class SpellbookInfoPage
    {
        public string Title { get; set; }
        public string Body { get; set; }
    }

    public static class SpellbookBookmarkAction
    {
        public const byte None           = 0;
        public const byte JumpToPage     = 1;
        public const byte ServerCallback = 2;
    }

    public class SpellbookBookmarkInfo
    {
        public ushort Graphic { get; set; }
        public ushort PressedGraphic { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public ushort Hue { get; set; }
        public byte DisplayPage { get; set; }
        public byte ActionType { get; set; }
        public uint Action { get; set; }
        public string Tooltip { get; set; }
    }

    public class DynamicSpellDefinition
    {
        public ushort SpellID { get; set; }
        public ushort IconGraphic { get; set; }
        public int NameCliloc { get; set; }
        public string Name { get; set; }
        public string PowerWords { get; set; }
        public string Description { get; set; }
        public byte ManaCost { get; set; }
        public byte MinSkill { get; set; }
        public byte TargetType { get; set; }
        public ushort Reagents { get; set; }
        public string[] CustomReagents { get; set; }
        public ushort Cooldown { get; set; }
        public byte Page { get; set; }
    }

    public class SpellbookCacheEntry
    {
        public byte SpellbookType { get; set; }
        public uint Version { get; set; }
        public DateTime CachedAt { get; set; }

        [JsonIgnore]
        public DateTime ExpiresAt { get; set; }

        public ulong SpellBitmask { get; set; }
        public ushort BookGraphic { get; set; }
        public ushort MinimizedGraphic { get; set; }
        public byte SpellsPerPageSide { get; set; } = 8;
        public byte MaxDictionaryPages { get; set; } = 2;
        public string[] PageNames { get; set; } = Array.Empty<string>();
        public bool DisplayManaCost { get; set; }
        public bool DisplayMinSkill { get; set; }
        public bool DisplayPowerWords { get; set; } = true;
        public string ManaCostLabel { get; set; }
        public string MinSkillLabel { get; set; }
        public string CustomPropertyTitle { get; set; }
        public string CustomPropertyLabel { get; set; }
        public string CustomPropertyName { get; set; }

        public ushort BookHue { get; set; }
        public ushort TextColor { get; set; }
        public ushort SpellNameColor { get; set; }
        public ushort TitleColor { get; set; }
        public short ContentOffsetX { get; set; }
        public short ContentOffsetY { get; set; }
        public ushort PageTurnLeftGraphic { get; set; }
        public ushort PageTurnRightGraphic { get; set; }
        public short PageTurnLeftX { get; set; }
        public short PageTurnLeftY { get; set; }
        public short PageTurnRightX { get; set; }
        public short PageTurnRightY { get; set; }
        public ushort[] OverlayGraphics { get; set; } = Array.Empty<ushort>();
        public List<SpellbookInfoPage> InfoPages { get; set; } = new();
        public SpellbookBookmarkInfo Bookmark { get; set; }

        private List<DynamicSpellDefinition> _spells = new();
        private Dictionary<ushort, DynamicSpellDefinition> _byId;

        public List<DynamicSpellDefinition> Spells
        {
            get => _spells;
            set
            {
                _spells = value ?? new List<DynamicSpellDefinition>();
                _spells.Sort((a, b) => a.SpellID.CompareTo(b.SpellID));
                _byId = null;
            }
        }

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

        public void RefreshTTL(uint ttlSeconds) => ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds);

        public DynamicSpellDefinition GetSpell(ushort spellID)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<ushort, DynamicSpellDefinition>(_spells.Count);
                foreach (var s in _spells)
                    _byId[s.SpellID] = s;
            }
            return _byId.TryGetValue(spellID, out var spell) ? spell : null;
        }

        public DynamicSpellDefinition GetSpellByIndex(int index)
        {
            if (index < 1 || index > _spells.Count)
                return null;
            return _spells[index - 1];
        }
    }
}
