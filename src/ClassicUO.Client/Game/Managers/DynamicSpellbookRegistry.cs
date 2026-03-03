using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

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

                Log.Trace($"[DYNAMIC SPELLBOOK] Registered {type} as dynamic spellbook");
            }
        }

        public static bool IsDynamic(SpellBookType type)
        {
            bool isDynamic = _dynamicSpellbooks.Contains(type);
            Log.Trace($"[DYNAMIC REGISTRY] IsDynamic({type}): {isDynamic} (total registered: {_dynamicSpellbooks.Count})");
            return isDynamic;
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
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict))
            {
                if (dict.TryGetValue(spellIndex, out var spell))
                {
                    Log.Trace($"[DYNAMIC REGISTRY] GetSpell({type}, {spellIndex}): Found spell ID={spell.ID}, Name='{spell.Name}'");
                    return spell;
                }
                else
                {
                    Log.Warn($"[DYNAMIC REGISTRY] GetSpell({type}, {spellIndex}): Dictionary exists but index not found. Available indices: {string.Join(", ", dict.Keys.OrderBy(k => k))}");
                }
            }
            else
            {
                Log.Warn($"[DYNAMIC REGISTRY] GetSpell({type}, {spellIndex}): No dictionary found for spellbook type {type}. IsDynamic={IsDynamic(type)}");
            }

            Log.Warn($"[DYNAMIC REGISTRY] GetSpell({type}, {spellIndex}): Returning EmptySpell (ID={SpellDefinition.EmptySpell.ID})");
            return SpellDefinition.EmptySpell;
        }

        public static void Clear()
        {
            _dynamicSpellbooks.Clear();
            _dynamicSpellDictionaries.Clear();
            Log.Trace("[DYNAMIC SPELLBOOK] Cleared all dynamic spellbook registrations");
        }
        public static void ClearType(SpellBookType type)
        {
            _dynamicSpellbooks.Remove(type);
            if (_dynamicSpellDictionaries.TryGetValue(type, out var dict))
            {
                dict.Clear();
            }
            Log.Trace($"[DYNAMIC SPELLBOOK] Cleared dynamic spellbook {type}");
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
            var types = new List<SpellBookType>(_dynamicSpellbooks);
            Log.Trace($"[DYNAMIC REGISTRY] GetAllRegisteredTypes(): Returning {types.Count} registered types: {string.Join(", ", types)}");
            return types;
        }
    }
}
