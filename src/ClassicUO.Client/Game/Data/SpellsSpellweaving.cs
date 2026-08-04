// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsSpellweaving
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsSpellweaving()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1,
                    new SpellDefinition
                    (
                        "Arcane Circle",
                        601,
                        0x59D8,
                        "Myrshalee",
                        20,
                        0,
                        TargetType.Neutral,
                        1071026,
                        Reagents.None
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Gift of Renewal",
                        602,
                        0x59D9,
                        "Olorisstra",
                        24,
                        0,
                        TargetType.Beneficial,
                        1071027,
                        Reagents.None
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Immolating Weapon",
                        603,
                        0x59DA,
                        "Thalshara",
                        32,
                        10,
                        TargetType.Neutral,
                        1071028,
                        Reagents.None
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Attunement",
                        604,
                        0x59DB,
                        "Haeldril",
                        24,
                        0,
                        TargetType.Harmful,
                        1071029,
                        Reagents.None
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Thunderstorm",
                        605,
                        0x59DC,
                        "Erelonia",
                        32,
                        10,
                        TargetType.Harmful,
                        1071030,
                        Reagents.None
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Nature's Fury",
                        606,
                        0x59DD,
                        "Rauvvrae",
                        24,
                        0,
                        TargetType.Neutral,
                        1071031,
                        Reagents.None
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Summon Fey",
                        607,
                        0x59DE,
                        "Alalithra",
                        10,
                        38,
                        TargetType.Neutral,
                        1071032,
                        Reagents.None
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Summon Fiend",
                        608,
                        0x59DF,
                        "Nylisstra",
                        10,
                        38,
                        TargetType.Neutral,
                        1071033,
                        Reagents.None
                    )
                },
                {
                    9,
                    new SpellDefinition
                    (
                        "Reaper Form",
                        609,
                        0x59E0,
                        "Tarisstree",
                        34,
                        24,
                        TargetType.Neutral,
                        1071034,
                        Reagents.None
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Wildfire",
                        610,
                        0x59E1,
                        "Haelyn",
                        50,
                        66,
                        TargetType.Harmful,
                        1071035,
                        Reagents.None
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Essence of Wind",
                        611,
                        0x59E2,
                        "Anathrae",
                        40,
                        52,
                        TargetType.Harmful,
                        1071036,
                        Reagents.None
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Dryad Allure",
                        612,
                        0x59E3,
                        "Rathril",
                        40,
                        52,
                        TargetType.Neutral,
                        1071037,
                        Reagents.None
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Ethereal Voyage",
                        613,
                        0x59E4,
                        "Orlavdra",
                        32,
                        24,
                        TargetType.Neutral,
                        1071038,
                        Reagents.None
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Word of Death",
                        614,
                        0x59E5,
                        "Nyraxle",
                        50,
                        23,
                        TargetType.Harmful,
                        1071039,
                        Reagents.None
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Gift of Life",
                        615,
                        0x59E6,
                        "Illorae",
                        70,
                        38,
                        TargetType.Beneficial,
                        1071040,
                        Reagents.None
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Arcane Empowerment",
                        616,
                        0x59E7,
                        "Aslavdra",
                        50,
                        24,
                        TargetType.Beneficial,
                        1071041,
                        Reagents.None
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Spellweaving.ToString();

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
