using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
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

        public string GetDisplayName()
        {
            if (NameCliloc > 0 && Client.Game.UO.FileManager.Clilocs != null)
            {
                var clilocText = Client.Game.UO.FileManager.Clilocs.GetString(NameCliloc);
                if (!string.IsNullOrEmpty(clilocText))
                    return clilocText;
            }
            return Name ?? "Unknown Spell";
        }

        internal SpellDefinition ToSpellDefinition()
        {
            return new SpellDefinition(
                GetDisplayName(),
                (int)SpellID,
                IconGraphic,
                IconGraphic, // Use same graphic for both large and small
                PowerWords ?? "",
                ManaCost,
                MinSkill,
                0, // tithing cost
                (Managers.TargetType)TargetType
            );
        }
    }

    public class SpellbookCacheEntry
    {
        public byte SpellbookType { get; set; }
        public uint Version { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ulong SpellBitmask { get; set; }
        public ushort BookGraphic { get; set; }
        public ushort MinimizedGraphic { get; set; }
        public byte SpellsPerPageSide { get; set; } = 8;        // Default: 8 spells per page side
        public byte MaxDictionaryPages { get; set; } = 2;       // Default: 2 dictionary pages
        public string[] PageNames { get; set; } = Array.Empty<string>();  // Page names
        public bool DisplayManaCost { get; set; }               // Whether to display mana cost
        public bool DisplayMinSkill { get; set; }               // Whether to display minimum skill
        public string CustomPropertyTitle { get; set; }         // Custom property title (line 1)
        public string CustomPropertyLabel { get; set; }         // Custom property label (line 2 before value)
        public string CustomPropertyName { get; set; }          // Custom property to read from Mobile
        public Dictionary<ushort, DynamicSpellDefinition> Spells { get; set; }

        public SpellbookCacheEntry()
        {
            Spells = new Dictionary<ushort, DynamicSpellDefinition>();
        }
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
        public bool IsFresh()
        {
            return !IsExpired();
        }
        public void RefreshTTL(uint ttlSeconds)
        {
            ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds);
        }

        public DynamicSpellDefinition GetSpell(ushort spellID)
        {
            return Spells.TryGetValue(spellID, out var spell) ? spell : null;
        }

        public DynamicSpellDefinition GetSpellByIndex(int index)
        {
            if (index < 1 || index > Spells.Count)
                return null;

            var sortedSpells = new List<DynamicSpellDefinition>(Spells.Values);
            sortedSpells.Sort((a, b) => a.SpellID.CompareTo(b.SpellID));
            return sortedSpells[index - 1];
        }
    }

    [Serializable]
    public class PersistentCacheEntry
    {
        public byte SpellbookType { get; set; }
        public uint Version { get; set; }
        public DateTime CachedAt { get; set; }
        public ushort ItemGraphic { get; set; }  // Item graphic for this spellbook (e.g., 0x2259)
        public ushort BookGraphic { get; set; }
        public ushort MinimizedGraphic { get; set; }
        public byte SpellsPerPageSide { get; set; } = 8;
        public byte MaxDictionaryPages { get; set; } = 2;
        public string[] PageNames { get; set; } = Array.Empty<string>();
        public bool DisplayManaCost { get; set; }
        public bool DisplayMinSkill { get; set; }
        public string CustomPropertyTitle { get; set; }
        public string CustomPropertyLabel { get; set; }
        public string CustomPropertyName { get; set; }
        public List<DynamicSpellDefinition> Spells { get; set; }

        public PersistentCacheEntry()
        {
            Spells = new List<DynamicSpellDefinition>();
        }

        public SpellbookCacheEntry ToRuntimeCache(uint defaultTTL)
        {
            var entry = new SpellbookCacheEntry
            {
                SpellbookType = SpellbookType,
                Version = Version,
                CachedAt = CachedAt,
                ExpiresAt = DateTime.UtcNow.AddSeconds(defaultTTL),
                BookGraphic = BookGraphic,
                MinimizedGraphic = MinimizedGraphic,
                SpellsPerPageSide = SpellsPerPageSide,
                MaxDictionaryPages = MaxDictionaryPages,
                PageNames = PageNames ?? Array.Empty<string>(),
                DisplayManaCost = DisplayManaCost,
                DisplayMinSkill = DisplayMinSkill,
                CustomPropertyTitle = CustomPropertyTitle,
                CustomPropertyLabel = CustomPropertyLabel,
                CustomPropertyName = CustomPropertyName
            };

            foreach (var spell in Spells)
            {
                entry.Spells[spell.SpellID] = spell;
            }

            return entry;
        }
        public static PersistentCacheEntry FromRuntimeCache(SpellbookCacheEntry entry)
        {
            var sortedSpells = new List<DynamicSpellDefinition>(entry.Spells.Values);
            sortedSpells.Sort((a, b) => a.SpellID.CompareTo(b.SpellID));

            return new PersistentCacheEntry
            {
                SpellbookType = entry.SpellbookType,
                Version = entry.Version,
                CachedAt = entry.CachedAt,
                BookGraphic = entry.BookGraphic,
                MinimizedGraphic = entry.MinimizedGraphic,
                SpellsPerPageSide = entry.SpellsPerPageSide,
                MaxDictionaryPages = entry.MaxDictionaryPages,
                PageNames = entry.PageNames ?? Array.Empty<string>(),
                DisplayManaCost = entry.DisplayManaCost,
                DisplayMinSkill = entry.DisplayMinSkill,
                CustomPropertyTitle = entry.CustomPropertyTitle,
                CustomPropertyLabel = entry.CustomPropertyLabel,
                CustomPropertyName = entry.CustomPropertyName,
                Spells = sortedSpells
            };
        }
    }
}
