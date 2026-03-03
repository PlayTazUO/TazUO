using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class DynamicSpells
    {
        private static SpellBookType _defaultType = SpellBookType.Magery;

        public static void SetDefaultType(SpellBookType type)
        {
            _defaultType = type;
        }

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells(SpellBookType type)
        {
            return DynamicSpellbookRegistry.GetSpellDictionary(type);
        }

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells()
        {
            return GetAllSpells(_defaultType);
        }
        internal static int MaxSpellCount(SpellBookType type)
        {
            return DynamicSpellbookRegistry.GetMaxSpellCount(type);
        }
        internal static int MaxSpellCount()
        {
            return MaxSpellCount(_defaultType);
        }
        public static SpellDefinition GetSpell(SpellBookType type, int spellIndex)
        {
            return DynamicSpellbookRegistry.GetSpell(type, spellIndex);
        }
        public static SpellDefinition GetSpell(int spellIndex)
        {
            return GetSpell(_defaultType, spellIndex);
        }
        public static void SetSpell(SpellBookType type, int id, in SpellDefinition newspell)
        {
            var dict = DynamicSpellbookRegistry.GetSpellDictionary(type);
            dict[id] = newspell;
        }
        public static void SetSpell(int id, in SpellDefinition newspell)
        {
            SetSpell(_defaultType, id, in newspell);
        }
        internal static void Clear(SpellBookType type)
        {
            DynamicSpellbookRegistry.ClearType(type);
        }
        internal static void Clear()
        {
            Clear(_defaultType);
        }
    }
}
