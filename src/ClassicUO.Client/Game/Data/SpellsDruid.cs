using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsDruid
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        private static string[] _spRegsChars;

        static SpellsDruid()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // first circle
                {
                    1,
                    new SpellDefinition
                    (
                        "Shield of Earth",
                        302,
                        0x5A06,
                        "Kes En Sepa Ohm",
                        15,
                        20,
                        TargetType.Neutral,
                        Reagents.FertileDirt,
                        Reagents.SpringWater
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Hollow Reed",
                        303,
                        0x59FF,
                        "En Crur Aeta Sec En Ess",
                        30,
                        30,
                        TargetType.Beneficial,
                        Reagents.SpringWater,
                        Reagents.FenMoss
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Pack of Beast",
                        304,
                        0x5A04,
                        "En Sec Ohm Ess Sepa",
                        45,
                        40,
                        TargetType.Neutral,
                        Reagents.Pumice,
                        Reagents.DestroyingAngel
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Spring of Life",
                        305,
                        0x5A07,
                        "En Sepa Aete",
                        30,
                        40,
                        TargetType.Neutral,
                        Reagents.SpringWater,
                        Reagents.FenMoss
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Grasping Roots",
                        306,
                        0x59FD,
                        "En Ohm Sepa Tia Kes",
                        40,
                        40,
                        TargetType.Harmful,
                        Reagents.SpringWater,
                        Reagents.PetrifiedWood,
                        Reagents.Pumice
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Circle of Thorns",
                        307,
                        0x59F9,
                        "En Ess Ohm",
                        45,
                        60,
                        TargetType.Neutral,
                        Reagents.FertileDirt,
                        Reagents.Pumice,
                        Reagents.SpringWater
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Swarm of Insects",
                        308,
                        0x5A09,
                        "Ess Ohm En Sec Tia",
                        30,
                        20,
                        TargetType.Harmful,
                        Reagents.FenMoss,
                        Reagents.Pumice,
                        Reagents.DestroyingAngel
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Volcanic Eruption",
                        309,
                        0x5A0B,
                        "Vauk Ohm En Tia Crur",
                        75,
                        94,
                        TargetType.Harmful,
                        Reagents.FertileDirt,
                        Reagents.Pumice
                    )
                },
                {
                    9,
                    new SpellDefinition
                    (
                        "Summon Treefellow",
                        310,
                        0x5A0A,
                        "Kes En Sepa Ohm",
                        50,
                        80,
                        TargetType.Neutral,
                        Reagents.FenMoss,
                        Reagents.PetrifiedWood
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Deadly Spores",
                        311,
                        0x59FA,
                        "Telwa Nox",
                        20,
                        65,
                        TargetType.Harmful,
                        Reagents.FenMoss,
                        Reagents.Pumice
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Enchanted Grove",
                        312,
                        0x59FB,
                        "En Ante Ohm Sepa",
                        60,
                        75,
                        TargetType.Neutral,
                        Reagents.DestroyingAngel,
                        Reagents.PetrifiedWood,
                        Reagents.SpringWater
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Lure Stone",
                        313,
                        0x5A01,
                        "En Kes Ohm Crur",
                        15,
                        20,
                        TargetType.Neutral,
                        Reagents.Pumice,
                        Reagents.SpringWater
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Hurricane",
                        314,
                        0x5A00,
                        "In Xen Hur",
                        32,
                        105,
                        TargetType.Neutral,
                        Reagents.FenMoss,
                        Reagents.FertileDirt,
                        Reagents.Pumice
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Mushroom Gateway",
                        315,
                        0x5A03,
                        "Vauk Sepa Ohm",
                        40,
                        70,
                        TargetType.Neutral,
                        Reagents.FenMoss,
                        Reagents.Pumice,
                        Reagents.FertileDirt
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Restorative Soil",
                        316,
                        0x5A05,
                        "Ohm Sepa Ante",
                        55,
                        89,
                        TargetType.Neutral,
                        Reagents.PetrifiedWood,
                        Reagents.FenMoss,
                        Reagents.SpringWater
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Summon Firefly",
                        317,
                        0x5A08,
                        "Kes En Crur",
                        10,
                        1,
                        TargetType.Neutral,
                        Reagents.DestroyingAngel,
                        Reagents.Pumice
                    )
                },
                // third circle
                {
                    17,
                    new SpellDefinition
                    (
                        "Forest Kin",
                        318,
                        0x59FC,
                        "Lore Sec En Sepa Ohm",
                        13,
                        20,
                        TargetType.Neutral,
                        Reagents.FertileDirt,
                        Reagents.PetrifiedWood
                    )
                },
                {
                    18, new SpellDefinition
                    (
                        "Bark Skin",
                        319,
                        0x59F8,
                        "Porm Helma",
                        10,
                        30,
                        TargetType.Beneficial,
                        Reagents.PetrifiedWood,
                        Reagents.FertileDirt
                    )
                },
                {
                    19,
                    new SpellDefinition
                    (
                        "Mana Spring",
                        320,
                        0x5A02,
                        "En Mani Uus",
                        60,
                        85,
                        TargetType.Neutral,
                        Reagents.FertileDirt,
                        Reagents.SpringWater,
                        Reagents.FenMoss
                    )
                },
                {
                    20, new SpellDefinition
                    (
                        "Hibernate",
                        321,
                        0x59FE,
                        "En Xen Zu",
                        23,
                        70,
                        TargetType.Harmful,
                        Reagents.PetrifiedWood,
                        Reagents.Pumice
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Druidic.ToString();

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
