using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal static class SpellbookTypeRegistry
    {
        private static readonly Dictionary<ushort, SpellBookType> _graphicToType = new();
        private static readonly Dictionary<uint, SpellBookType> _serialToType = new();

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

        }

        public static void Register(ushort itemGraphic, SpellBookType type)
        {
            _graphicToType[itemGraphic] = type;
        }

        public static void RegisterSerial(uint serial, SpellBookType type)
        {
            _serialToType[serial] = type;
        }

        public static SpellBookType? GetTypeForSerial(uint serial)
        {
            if (_serialToType.TryGetValue(serial, out var type))
            {
                return type;
            }
            return null;
        }

        public static SpellBookType GetTypeForGraphic(ushort itemGraphic)
        {
            if (_graphicToType.TryGetValue(itemGraphic, out var type))
            {
                return type;
            }

            return SpellBookType.Magery;
        }

        public static bool IsSpellbook(ushort itemGraphic)
        {
            return _graphicToType.ContainsKey(itemGraphic);
        }
        public static void ClearDynamic()
        {
            _graphicToType.Clear();
            _serialToType.Clear();
            RegisterStandardSpellbooks();
        }

        public static IEnumerable<ushort> GetAllRegisteredGraphics()
        {
            return _graphicToType.Keys;
        }
    }
}
