using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal static class SpellbookTypeRegistry
    {
        private static readonly Dictionary<uint, SpellBookType> _serialToType = new();

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
    }
}
