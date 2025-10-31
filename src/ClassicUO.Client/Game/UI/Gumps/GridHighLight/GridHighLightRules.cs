using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public static class GridHighlightRules
    {
        private const string CONFIG_FILE_NAME = "GridHighlightSettings.json";
        private static bool _loaded = false;

        private static HashSet<string> GetConfigurableOrDefault(
            Func<List<string>> getList,
            HashSet<string> defaultSet)
        {
            if (!_loaded)
            {
                LoadGridHighlightConfiguration();
                _loaded = true;
            }

            List<string> list = getList();
            return list != null && list.Count > 0
                ? new HashSet<string>(list, StringComparer.OrdinalIgnoreCase)
                : defaultSet;
        }
        public static void SaveGridHighlightConfiguration()
        {
            if (ProfileManager.CurrentProfile == null)
                return;
            var config = new GridHighlightSettings
            {
                Properties = ProfileManager.CurrentProfile.ConfigurableProperties?.ToList(),
                Resistances = ProfileManager.CurrentProfile.ConfigurableResistances?.ToList(),
                Negatives = ProfileManager.CurrentProfile.ConfigurableNegatives?.ToList(),
                SuperSlayers = ProfileManager.CurrentProfile.ConfigurableSuperSlayers?.ToList(),
                Slayers = ProfileManager.CurrentProfile.ConfigurableSlayers?.ToList(),
                Rarities = ProfileManager.CurrentProfile.ConfigurableRarities?.ToList()
            };

            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", CONFIG_FILE_NAME);
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save GridHighlightConfiguration: " + ex.Message);
            }
        }

        public static void LoadGridHighlightConfiguration()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", CONFIG_FILE_NAME);

            if (!File.Exists(path))
            {
                // Ensure directory exists
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Initialize with default values and save to file
                if (ProfileManager.CurrentProfile != null)
                {
                    ProfileManager.CurrentProfile.ConfigurableProperties = DefaultProperties.ToList();
                    ProfileManager.CurrentProfile.ConfigurableResistances = DefaultResistances.ToList();
                    ProfileManager.CurrentProfile.ConfigurableNegatives = DefaultNegative.ToList();
                    ProfileManager.CurrentProfile.ConfigurableSuperSlayers = DefaultSuperSlayer.ToList();
                    ProfileManager.CurrentProfile.ConfigurableSlayers = DefaultSlayer.ToList();
                    ProfileManager.CurrentProfile.ConfigurableRarities = DefaultRarity.ToList();
                    SaveGridHighlightConfiguration();
                }

                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                GridHighlightSettings config = JsonSerializer.Deserialize<GridHighlightSettings>(json);

                if (config != null && ProfileManager.CurrentProfile != null)
                {
                    ProfileManager.CurrentProfile.ConfigurableProperties = config.Properties ?? new List<string>();
                    ProfileManager.CurrentProfile.ConfigurableResistances = config.Resistances ?? new List<string>();
                    ProfileManager.CurrentProfile.ConfigurableNegatives = config.Negatives ?? new List<string>();
                    ProfileManager.CurrentProfile.ConfigurableSuperSlayers = config.SuperSlayers ?? new List<string>();
                    ProfileManager.CurrentProfile.ConfigurableSlayers = config.Slayers ?? new List<string>();
                    ProfileManager.CurrentProfile.ConfigurableRarities = config.Rarities ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load GridHighlightConfiguration: " + ex.Message);
            }
        }

        private class GridHighlightSettings
        {
            public List<string> Properties { get; set; }
            public List<string> Resistances { get; set; }
            public List<string> Negatives { get; set; }
            public List<string> SuperSlayers { get; set; }
            public List<string> Slayers { get; set; }
            public List<string> Rarities { get; set; }
        }

        public static HashSet<string> Properties =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableProperties, DefaultProperties);
        private static readonly HashSet<string> DefaultProperties = new(StringComparer.OrdinalIgnoreCase)
            {
                "Damage Increase",
                "Defense Chance Increase",
                "Dexterity Bonus",
                "Enhance Potions",
                "Faster Cast Recovery",
                "Faster Casting",
                "Hit Cold Area",
                "Hit Energy Area",
                "Hit Fire Area",
                "Hit Physical Area",
                "Hit Poison Area",
                "Hit Chance Increase",
                "Hit Curse",
                "Hit Dispel",
                "Hit Fatigue",
                "Hit Fireball",
                "Hit Harm",
                "Hit Life Leech",
                "Hit Lightning",
                "Hit Lower Attack",
                "Hit Lower Defense",
                "Hit Mana Drain",
                "Hit Mana Leech",
                "Hit Point Increase",
                "Hit Point Regeneration",
                "Hit Stamina Leech",
                "Intelligence Bonus",
                "Lower Mana Cost",
                "Lower Reagent Cost",
                "Luck",
                "Mage Armor",
                "Mage Weapon",
                "Mana Increase",
                "Mana Regeneration",
                "Night Sight",
                "Reflect Physical Damage",
                "Self Repair",
                "Spell Channeling",
                "Spell Damage Increase",
                "Stamina Increase",
                "Stamina Regeneration",
                "Strength Bonus",
                "Swing Speed Increase",
                "Use Best Weapon Skill",
                "Blood Drinker",
                "Battle Lust",
                "Casting Focus",
                "Damage Eater",
                "Reactive Paralyze",
                "Resonance",
                "Soul Charge",
                "Splintering Weapon",
                "Fire Eater",
                "Cold Eater",
                "Poison Eater",
                "Energy Eater",
                "Kinetic Eater",
            };

        public static HashSet<string> Resistances =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableResistances, DefaultResistances);
        private static readonly HashSet<string> DefaultResistances = new(StringComparer.OrdinalIgnoreCase)
            {
                "Poison Resist", "Physical Resist", "Fire Resist", "Cold Resist", "Energy Resist"
            };

        public static HashSet<string> NegativeProperties =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableNegatives, DefaultNegative);
        private static readonly HashSet<string> DefaultNegative = new(StringComparer.OrdinalIgnoreCase)
            {
                "Antique", "Brittle", "Prized", "Massive", "Unwieldy", "Cursed", "Unlucky",
            };

        public static HashSet<string> SuperSlayerProperties =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableSuperSlayers, DefaultSuperSlayer);
        private static readonly HashSet<string> DefaultSuperSlayer = new(StringComparer.OrdinalIgnoreCase)
            {
                "Demon Slayer",
                "Arachnid Slayer",
                "Elemental Slayer",
                "Fey Slayer",
                "Repond Slayer",
                "Reptile Slayer",
                "Undead Slayer",
            };

        public static HashSet<string> SlayerProperties =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableSlayers, DefaultSlayer);
        private static readonly HashSet<string> DefaultSlayer = new(StringComparer.OrdinalIgnoreCase)
            {
                "Daemon Slayer",
                "Gargoyle Slayer",
                "Scorpion Slayer",
                "Spider Slayer",
                "Terathan Slayer",
                "Air Elemental Slayer",
                "Blood Elemental Slayer",
                "Earth Elemental Slayer",
                "Fire Elemental Slayer",
                "Poison Elemental Slayer",
                "Snow Elemental Slayer",
                "Water Elemental Slayer",
                "Dryad Slayer",
                "Satyr Slayer",
                "Ogre Slayer",
                "Orc Slayer",
                "Troll Slayer",
                "Goblin Slayer",
                "Dragon Slayer",
                "Lizardman Slayer",
                "Ophidian Slayer",
                "Snake Slayer",
                "Mummy Slayer",
                "Skeleton Slayer",
                "Zombie Slayer",
                "Vampire Slayer",
            };

        public static HashSet<string> RarityProperties =>
            GetConfigurableOrDefault(() => ProfileManager.CurrentProfile?.ConfigurableRarities, DefaultRarity);
        private static readonly HashSet<string> DefaultRarity = new(StringComparer.OrdinalIgnoreCase)
            {
                "Minor Magic Item",
                "Lesser Magic Item",
                "Greater Magic Item",
                "Major Magic Item",
                "Minor Artifact",
                "Lesser Artifact",
                "Greater Artifact",
                "Major Artifact",
                "Legendary Artifact"
            };

        public static string[] FlattenAndDistinctParameters(params HashSet<string>[] propertySets) => propertySets
                .SelectMany(set => set)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
