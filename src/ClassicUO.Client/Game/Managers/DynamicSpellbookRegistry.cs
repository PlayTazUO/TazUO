using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal static class DynamicSpellbookRegistry
    {
        private static readonly HashSet<SpellBookType> _dynamicSpellbooks = new();
        private static readonly Dictionary<SpellBookType, Dictionary<int, SpellDefinition>> _dynamicSpellDictionaries = new();

        public static void RegisterDynamic(SpellBookType type)
        {
            if (_dynamicSpellbooks.Add(type))
            {
                if (!_dynamicSpellDictionaries.ContainsKey(type))
                {
                    _dynamicSpellDictionaries[type] = new Dictionary<int, SpellDefinition>();
                }
            }
        }

        public static bool IsDynamic(SpellBookType type)
        {
            return _dynamicSpellbooks.Contains(type);
        }

        public static Dictionary<int, SpellDefinition> GetSpellDictionary(SpellBookType type)
        {
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict))
            {
                return dict;
            }

            var newDict = new Dictionary<int, SpellDefinition>();
            _dynamicSpellDictionaries[type] = newDict;
            return newDict;
        }

        public static SpellDefinition GetSpell(SpellBookType type, int spellIndex)
        {
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict) && dict.TryGetValue(spellIndex, out var spell))
            {
                return spell;
            }
            return SpellDefinition.EmptySpell;
        }

        public static void Clear()
        {
            _dynamicSpellbooks.Clear();
            _dynamicSpellDictionaries.Clear();
        }

        public static void ClearType(SpellBookType type)
        {
            _dynamicSpellbooks.Remove(type);
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict))
            {
                dict.Clear();
            }
        }

        public static int GetMaxSpellCount(SpellBookType type)
        {
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict))
            {
                return dict.Count;
            }
            return 0;
        }

        public static List<SpellBookType> GetAllRegisteredTypes()
        {
            return new List<SpellBookType>(_dynamicSpellbooks);
        }
    }
}
