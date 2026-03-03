using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class SpellbookTypeRegistry
    {
        private static readonly Dictionary<ushort, SpellBookType> _graphicToType = new();

        static SpellbookTypeRegistry()
        {
            RegisterStandardSpellbooks();
        }

        private static void RegisterStandardSpellbooks()
        {
            // Magery
            Register(0x0EFA, SpellBookType.Magery);

            // Necromancy
            Register(0x2253, SpellBookType.Necromancy);

            // Chivalry
            Register(0x2252, SpellBookType.Chivalry);

            // Bushido
            Register(0x238C, SpellBookType.Bushido);

            // Ninjitsu
            Register(0x23A0, SpellBookType.Ninjitsu);

            // Spellweaving
            Register(0x2D50, SpellBookType.Spellweaving);

            // Mysticism
            Register(0x2D9D, SpellBookType.Mysticism);

            // Mastery - ONLY register 0x225B, leave 0x225A for dynamic registration
            // (0x225A is commonly used for custom spellbooks)
            Register(0x225B, SpellBookType.Mastery);

            Log.Trace("[SPELLBOOK REGISTRY] Registered standard spellbook types");
        }

        public static void Register(ushort itemGraphic, SpellBookType type)
        {
            if (_graphicToType.ContainsKey(itemGraphic))
            {
                Log.Warn($"[SPELLBOOK REGISTRY DEBUG] Overwriting existing mapping: 0x{itemGraphic:X4} was {_graphicToType[itemGraphic]}, now {type}");
            }

            _graphicToType[itemGraphic] = type;
            Log.Info($"[SPELLBOOK REGISTRY DEBUG] Registered: ItemGraphic=0x{itemGraphic:X4} -> SpellBookType={type} (numeric value: {(byte)type})");
        }

        public static SpellBookType GetTypeForGraphic(ushort itemGraphic)
        {
            if (_graphicToType.TryGetValue(itemGraphic, out var type))
            {
                Log.Info($"[SPELLBOOK REGISTRY DEBUG] GetTypeForGraphic: 0x{itemGraphic:X4} -> {type} (numeric: {(byte)type})");
                return type;
            }

            // Default to Magery for unknown graphics
            Log.Warn($"[SPELLBOOK REGISTRY DEBUG] Unknown item graphic 0x{itemGraphic:X4}, defaulting to Magery");
            return SpellBookType.Magery;
        }

        public static bool IsSpellbook(ushort itemGraphic)
        {
            return _graphicToType.ContainsKey(itemGraphic);
        }
        public static void ClearDynamic()
        {
            _graphicToType.Clear();
            RegisterStandardSpellbooks();
            Log.Trace("[SPELLBOOK REGISTRY] Cleared dynamic registrations, re-registered standard types");
        }

        public static IEnumerable<ushort> GetAllRegisteredGraphics()
        {
            return _graphicToType.Keys;
        }
    }
}
