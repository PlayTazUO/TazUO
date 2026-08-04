// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMagery
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        private static string[] _spRegsChars;

        private static string _spellBookName = SpellBookType.Magery.ToString();

        static SpellsMagery()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // first circle
                {
                    1,
                    new SpellDefinition
                    (
                        "Clumsy",
                        1,
                        0x1B58,
                        "Uus Jux",
                        TargetType.Harmful,
                        3002011,
                        Reagents.Bloodmoss,
                        Reagents.Nightshade
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Create Food",
                        2,
                        0x1B59,
                        "In Mani Ylem",
                        TargetType.Neutral,
                        3002012,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Feeblemind",
                        3,
                        0x1B5A,
                        "Rel Wis",
                        TargetType.Harmful,
                        3002013,
                        Reagents.Nightshade,
                        Reagents.Ginseng
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Heal",
                        4,
                        0x1B5B,
                        "In Mani",
                        TargetType.Beneficial,
                        3002014,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.SpidersSilk
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Magic Arrow",
                        5,
                        0x1B5C,
                        "In Por Ylem",
                        TargetType.Harmful,
                        3002015,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Night Sight",
                        6,
                        0x1B5D,
                        "In Lor",
                        TargetType.Beneficial,
                        3002016,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Reactive Armor",
                        7,
                        0x1B5E,
                        "Flam Sanct",
                        TargetType.Beneficial,
                        3002017,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Weaken",
                        8,
                        0x1B5F,
                        "Des Mani",
                        TargetType.Harmful,
                        3002018,
                        Reagents.Garlic,
                        Reagents.Nightshade
                    )
                },
                // second circle
                {
                    9,
                    new SpellDefinition
                    (
                        "Agility",
                        9,
                        0x1B60,
                        "Ex Uus",
                        TargetType.Beneficial,
                        3002019,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Cunning",
                        10,
                        0x1B61,
                        "Uus Wis",
                        TargetType.Beneficial,
                        3002020,
                        Reagents.Nightshade,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Cure",
                        11,
                        0x1B62,
                        "An Nox",
                        TargetType.Beneficial,
                        3002021,
                        Reagents.Garlic,
                        Reagents.Ginseng
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Harm",
                        12,
                        0x1B63,
                        "An Mani",
                        TargetType.Harmful,
                        3002022,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Magic Trap",
                        13,
                        0x1B64,
                        "In Jux",
                        TargetType.Neutral,
                        3002023,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Magic Untrap",
                        14,
                        0x1B65,
                        "An Jux",
                        TargetType.Neutral,
                        3002024,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Protection",
                        15,
                        0x1B66,
                        "Uus Sanct",
                        TargetType.Beneficial,
                        3002025,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Strength",
                        16,
                        0x1B67,
                        "Uus Mani",
                        TargetType.Beneficial,
                        3002026,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                // third circle
                {
                    17,
                    new SpellDefinition
                    (
                        "Bless",
                        17,
                        0x1B68,
                        "Rel Sanct",
                        TargetType.Beneficial,
                        3002027,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    18, new SpellDefinition
                    (
                        "Fireball",
                        18,
                        0x1B69,
                        "Vas Flam",
                        TargetType.Harmful,
                        3002028,
                        Reagents.BlackPearl
                    )
                },
                {
                    19,
                    new SpellDefinition
                    (
                        "Magic Lock",
                        19,
                        0x1B6a,
                        "An Por",
                        TargetType.Neutral,
                        3002029,
                        Reagents.Bloodmoss,
                        Reagents.Garlic,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    20, new SpellDefinition
                    (
                        "Poison",
                        20,
                        0x1B6b,
                        "In Nox",
                        TargetType.Harmful,
                        3002030,
                        Reagents.Nightshade
                    )
                },
                {
                    21,
                    new SpellDefinition
                    (
                        "Telekinesis",
                        21,
                        0x1B6c,
                        "Ort Por Ylem",
                        TargetType.Neutral,
                        3002031,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    22,
                    new SpellDefinition
                    (
                        "Teleport",
                        22,
                        0x1B6d,
                        "Rel Por",
                        TargetType.Neutral,
                        3002032,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    23,
                    new SpellDefinition
                    (
                        "Unlock",
                        23,
                        0x1B6e,
                        "Ex Por",
                        TargetType.Neutral,
                        3002033,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    24,
                    new SpellDefinition
                    (
                        "Wall of Stone",
                        24,
                        0x1B6f,
                        "In Sanct Ylem",
                        TargetType.Neutral,
                        3002034,
                        Reagents.Bloodmoss,
                        Reagents.Garlic
                    )
                },
                // fourth circle
                {
                    25,
                    new SpellDefinition
                    (
                        "Arch Cure",
                        25,
                        0x1B70,
                        "Vas An Nox",
                        TargetType.Beneficial,
                        3002035,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    26,
                    new SpellDefinition
                    (
                        "Arch Protection",
                        26,
                        0x1B71,
                        "Vas Uus Sanct",
                        TargetType.Beneficial,
                        3002036,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    27,
                    new SpellDefinition
                    (
                        "Curse",
                        27,
                        0x1B72,
                        "Des Sanct",
                        TargetType.Harmful,
                        3002037,
                        Reagents.Garlic,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    28,
                    new SpellDefinition
                    (
                        "Fire Field",
                        28,
                        0x1B73,
                        "In Flam Grav",
                        TargetType.Neutral,
                        3002038,
                        Reagents.BlackPearl,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    29,
                    new SpellDefinition
                    (
                        "Greater Heal",
                        29,
                        0x1B74,
                        "In Vas Mani",
                        TargetType.Beneficial,
                        3002039,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    30,
                    new SpellDefinition
                    (
                        "Lightning",
                        30,
                        0x1B75,
                        "Por Ort Grav",
                        TargetType.Harmful,
                        3002040,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    31,
                    new SpellDefinition
                    (
                        "Mana Drain",
                        31,
                        0x1B76,
                        "Ort Rel",
                        TargetType.Harmful,
                        3002041,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    32,
                    new SpellDefinition
                    (
                        "Recall",
                        32,
                        0x1B77,
                        "Kal Ort Por",
                        TargetType.Neutral,
                        3002042,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                // fifth circle
                {
                    33,
                    new SpellDefinition
                    (
                        "Blade Spirits",
                        33,
                        0x1B78,
                        "In Jux Hur Ylem",
                        TargetType.Neutral,
                        3002043,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                {
                    34,
                    new SpellDefinition
                    (
                        "Dispel Field",
                        34,
                        0x1B79,
                        "An Grav",
                        TargetType.Neutral,
                        3002044,
                        Reagents.BlackPearl,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    35,
                    new SpellDefinition
                    (
                        "Incognito",
                        35,
                        0x1B7a,
                        "Kal In Ex",
                        TargetType.Neutral,
                        3002045,
                        Reagents.Bloodmoss,
                        Reagents.Garlic,
                        Reagents.Nightshade
                    )
                },
                {
                    36,
                    new SpellDefinition
                    (
                        "Magic Reflection",
                        36,
                        0x1B7b,
                        "In Jux Sanct",
                        TargetType.Beneficial,
                        3002046,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    37,
                    new SpellDefinition
                    (
                        "Mind Blast",
                        37,
                        0x1B7c,
                        "Por Corp Wis",
                        TargetType.Harmful,
                        3002047,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    38,
                    new SpellDefinition
                    (
                        "Paralyze",
                        38,
                        0x1B7d,
                        "An Ex Por",
                        TargetType.Harmful,
                        3002048,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    39,
                    new SpellDefinition
                    (
                        "Poison Field",
                        39,
                        0x1B7e,
                        "In Nox Grav",
                        TargetType.Neutral,
                        3002049,
                        Reagents.BlackPearl,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk
                    )
                },
                {
                    40,
                    new SpellDefinition
                    (
                        "Summon Creature",
                        40,
                        0x1B7f,
                        "Kal Xen",
                        TargetType.Neutral,
                        3002050,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                // sixth circle
                {
                    41,
                    new SpellDefinition
                    (
                        "Dispel",
                        41,
                        0x1B80,
                        "An Ort",
                        TargetType.Neutral,
                        3002051,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    42,
                    new SpellDefinition
                    (
                        "Energy Bolt",
                        42,
                        0x1B81,
                        "Corp Por",
                        TargetType.Harmful,
                        3002052,
                        Reagents.BlackPearl,
                        Reagents.Nightshade
                    )
                },
                {
                    43,
                    new SpellDefinition
                    (
                        "Explosion",
                        43,
                        0x1B82,
                        "Vas Ort Flam",
                        TargetType.Harmful,
                        3002053,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    44,
                    new SpellDefinition
                    (
                        "Invisibility",
                        44,
                        0x1B83,
                        "An Lor Xen",
                        TargetType.Beneficial,
                        3002054,
                        Reagents.Bloodmoss,
                        Reagents.Nightshade
                    )
                },
                {
                    45,
                    new SpellDefinition
                    (
                        "Mark",
                        45,
                        0x1B84,
                        "Kal Por Ylem",
                        TargetType.Neutral,
                        3002055,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    46,
                    new SpellDefinition
                    (
                        "Mass Curse",
                        46,
                        0x1B85,
                        "Vas Des Sanct",
                        TargetType.Harmful,
                        3002056,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    47,
                    new SpellDefinition
                    (
                        "Paralyze Field",
                        47,
                        0x1B86,
                        "In Ex Grav",
                        TargetType.Neutral,
                        3002057,
                        Reagents.BlackPearl,
                        Reagents.Ginseng,
                        Reagents.SpidersSilk
                    )
                },
                {
                    48,
                    new SpellDefinition
                    (
                        "Reveal",
                        48,
                        0x1B87,
                        "Wis Quas",
                        TargetType.Neutral,
                        3002058,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                // seventh circle
                {
                    49,
                    new SpellDefinition
                    (
                        "Chain Lightning",
                        49,
                        0x1B88,
                        "Vas Ort Grav",
                        TargetType.Harmful,
                        3002059,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    50,
                    new SpellDefinition
                    (
                        "Energy Field",
                        50,
                        0x1B89,
                        "In Sanct Grav",
                        TargetType.Neutral,
                        3002060,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    51,
                    new SpellDefinition
                    (
                        "Flamestrike",
                        51,
                        0x1B8a,
                        "Kal Vas Flam",
                        TargetType.Harmful,
                        3002061,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    52,
                    new SpellDefinition
                    (
                        "Gate Travel",
                        52,
                        0x1B8b,
                        "Vas Rel Por",
                        TargetType.Neutral,
                        3002062,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    53,
                    new SpellDefinition
                    (
                        "Mana Vampire",
                        53,
                        0x1B8c,
                        "Ort Sanct",
                        TargetType.Harmful,
                        3002063,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    54,
                    new SpellDefinition
                    (
                        "Mass Dispel",
                        54,
                        0x1B8d,
                        "Vas An Ort",
                        TargetType.Neutral,
                        3002064,
                        Reagents.BlackPearl,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    55,
                    new SpellDefinition
                    (
                        "Meteor Swarm",
                        55,
                        0x1B8e,
                        "Flam Kal Des Ylem",
                        TargetType.Harmful,
                        3002065,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    56,
                    new SpellDefinition
                    (
                        "Polymorph",
                        56,
                        0x1B8f,
                        "Vas Ylem Rel",
                        TargetType.Neutral,
                        3002066,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                // eighth circle
                {
                    57,
                    new SpellDefinition
                    (
                        "Earthquake",
                        57,
                        0x1B90,
                        "In Vas Por",
                        TargetType.Harmful,
                        3002067,
                        Reagents.Bloodmoss,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    58,
                    new SpellDefinition
                    (
                        "Energy Vortex",
                        58,
                        0x1B91,
                        "Vas Corp Por",
                        TargetType.Neutral,
                        3002068,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                {
                    59,
                    new SpellDefinition
                    (
                        "Resurrection",
                        59,
                        0x1B92,
                        "An Corp",
                        TargetType.Beneficial,
                        3002069,
                        Reagents.Bloodmoss,
                        Reagents.Ginseng,
                        Reagents.Garlic
                    )
                },
                {
                    60,
                    new SpellDefinition
                    (
                        "Air Elemental",
                        60,
                        0x1B93,
                        "Kal Vas Xen Hur",
                        TargetType.Neutral,
                        3002070,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    61,
                    new SpellDefinition
                    (
                        "Summon Daemon",
                        61,
                        0x1B94,
                        "Kal Vas Xen Corp",
                        TargetType.Neutral,
                        3002071,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    62,
                    new SpellDefinition
                    (
                        "Earth Elemental",
                        62,
                        0x1B95,
                        "Kal Vas Xen Ylem",
                        TargetType.Neutral,
                        3002072,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    63,
                    new SpellDefinition
                    (
                        "Fire Elemental",
                        63,
                        0x1B96,
                        "Kal Vas Xen Flam",
                        TargetType.Neutral,
                        3002073,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    64,
                    new SpellDefinition
                    (
                        "Water Elemental",
                        64,
                        0x1B97,
                        "Kal Vas Xen An Flam",
                        TargetType.Neutral,
                        3002074,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Magery.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        private static string[] _circleNames;
        public static string[] CircleNames => _circleNames ??= GetCircleNames();

        private static string[] GetCircleNames()
        {
            return new string[]
            {
                ClilocOrFallback(1044369, "First Circle"),
                ClilocOrFallback(1044370, "Second Circle"),
                ClilocOrFallback(1044371, "Third Circle"),
                ClilocOrFallback(1044372, "Fourth Circle"),
                ClilocOrFallback(1044373, "Fifth Circle"),
                ClilocOrFallback(1044374, "Sixth Circle"),
                ClilocOrFallback(1044375, "Seventh Circle"),
                ClilocOrFallback(1044376, "Eighth Circle")
            };
        }

        private static string ClilocOrFallback(int clilocNumber, string fallback)
        {
            string cliloc = Client.Game.UO.FileManager.Clilocs?.GetString(clilocNumber);
            return !string.IsNullOrEmpty(cliloc) ? cliloc : fallback;
        }

        public static string[] SpecialReagentsChars
        {
            get
            {
                if (_spRegsChars == null)
                {
                    _spRegsChars = new string[_spellsDict.Max(o => o.Key)];

                    for (int i = _spRegsChars.Length; i > 0; --i)
                    {
                        if (_spellsDict.TryGetValue(i, out SpellDefinition sd))
                        {
                            _spRegsChars[i - 1] = StringHelper.RemoveUpperLowerChars(sd.PowerWords);
                        }
                        else
                        {
                            _spRegsChars[i - 1] = string.Empty;
                        }
                    }
                }

                return _spRegsChars;
            }
        }

        public static SpellDefinition GetSpell(int index) => _spellsDict.TryGetValue(index, out SpellDefinition spell) ? spell : SpellDefinition.EmptySpell;

        public static void SetSpell(int id, in SpellDefinition newspell)
        {
            _spRegsChars = null;
            _spellsDict[id] = newspell;
        }

        internal static void Clear() => _spellsDict.Clear();
    }
}
