// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsChivalry
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsChivalry()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1,
                    new SpellDefinition
                    (
                        "Cleanse by Fire",
                        201,
                        0x5100,
                        0x5100,
                        "Expor Flamus",
                        10,
                        5,
                        10,
                        TargetType.Beneficial,
                        1060493,
                        Reagents.None
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Close Wounds",
                        202,
                        0x5101,
                        0x5101,
                        "Obsu Vulni",
                        10,
                        0,
                        10,
                        TargetType.Beneficial,
                        1060494,
                        Reagents.None
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Consecrate Weapon",
                        203,
                        0x5102,
                        0x5102,
                        "Consecrus Arma",
                        10,
                        15,
                        10,
                        TargetType.Neutral,
                        1060495,
                        Reagents.None
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Dispel Evil",
                        204,
                        0x5103,
                        0x5103,
                        "Dispiro Malas",
                        10,
                        35,
                        10,
                        TargetType.Neutral,
                        1060496,
                        Reagents.None
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Divine Fury",
                        205,
                        0x5104,
                        0x5104,
                        "Divinum Furis",
                        10,
                        25,
                        10,
                        TargetType.Neutral,
                        1060497,
                        Reagents.None
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Enemy of One",
                        206,
                        0x5105,
                        0x5105,
                        "Forul Solum",
                        20,
                        45,
                        10,
                        TargetType.Neutral,
                        1060498,
                        Reagents.None
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Holy Light",
                        207,
                        0x5106,
                        0x5106,
                        "Augus Luminos",
                        20,
                        55,
                        10,
                        TargetType.Harmful,
                        1060499,
                        Reagents.None
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Noble Sacrifice",
                        208,
                        0x5107,
                        0x5107,
                        "Dium Prostra",
                        20,
                        65,
                        30,
                        TargetType.Beneficial,
                        1060500,
                        Reagents.None
                    )
                },
                {
                    9,
                    new SpellDefinition
                    (
                        "Remove Curse",
                        209,
                        0x5108,
                        0x5108,
                        "Extermo Vomica",
                        20,
                        5,
                        10,
                        TargetType.Beneficial,
                        1060501,
                        Reagents.None
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Sacred Journey",
                        210,
                        0x5109,
                        0x5109,
                        "Sanctum Viatas",
                        20,
                        5,
                        10,
                        TargetType.Neutral,
                        1060502,
                        Reagents.None
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Chivalry.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            if (_spellsDict.TryGetValue(spellIndex, out SpellDefinition spell))
            {
                return spell;
            }

            return SpellDefinition.EmptySpell;
        }

        public static void SetSpell(int id, in SpellDefinition newspell) => _spellsDict[id] = newspell;

        internal static void Clear() => _spellsDict.Clear();
    }
}
