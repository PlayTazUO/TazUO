// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Data
{
    public class SpellDefinition : IEquatable<SpellDefinition>
    {
        public static SpellDefinition EmptySpell = new SpellDefinition
        (
            "",
            0,
            0,
            0,
            "",
            0,
            0,
            0,
            TargetType.Neutral,
            0
        );

        internal static Dictionary<string, SpellDefinition> WordToTargettype = new Dictionary<string, SpellDefinition>();


        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            int gumpSmallIconID,
            string powerwords,
            int manacost,
            int minskill,
            int tithingcost,
            TargetType target,
            int clilocNumber = 0,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpSmallIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minskill;
            PowerWords = powerwords;
            TithingCost = tithingcost;
            TargetType = target;
            ClilocNumber = clilocNumber;
            AddToWatchedSpell();
        }

        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            string powerwords,
            int manacost,
            int minskill,
            TargetType target,
            int clilocNumber = 0,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID;
            Regs = regs;
            ManaCost = manacost;
            MinSkill = minskill;
            PowerWords = powerwords;
            TithingCost = 0;
            TargetType = target;
            ClilocNumber = clilocNumber;
            AddToWatchedSpell();
        }

        public SpellDefinition
        (
            string name,
            int index,
            int gumpIconID,
            string powerwords,
            TargetType target,
            int clilocNumber = 0,
            params Reagents[] regs
        )
        {
            Name = name;
            ID = index;
            GumpIconID = gumpIconID;
            GumpIconSmallID = gumpIconID - 0x1298;
            Regs = regs;
            ManaCost = 0;
            MinSkill = 0;
            TithingCost = 0;
            PowerWords = powerwords;
            TargetType = target;
            ClilocNumber = clilocNumber;
            AddToWatchedSpell();
        }

        public bool Equals(SpellDefinition other) => ID.Equals(other.ID);

        public readonly int GumpIconID;
        public readonly int GumpIconSmallID;
        public readonly int ID;
        public readonly int ManaCost;
        public readonly int MinSkill;

        public readonly string Name;
        public readonly string PowerWords;
        public readonly Reagents[] Regs;
        public readonly TargetType TargetType;
        public readonly int TithingCost;
        public readonly int ClilocNumber;

        public string GetLocalizedName()
        {
            if (ClilocNumber > 0)
            {
                string cliloc = Client.Game.UO.FileManager.Clilocs?.GetString(ClilocNumber);
                if (!string.IsNullOrEmpty(cliloc))
                    return cliloc;
            }
            return Name;
        }

        public static void LoadCustomSpells(World world)
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "spelldef.json");
            if (File.Exists(path))
            {
                LoadSpellsFromFile(world, path);
            }

            path = Path.Combine(Settings.GlobalSettings.UltimaOnlineDirectory, "spelldef.json");
            if (File.Exists(path))
            {
                LoadSpellsFromFile(world, path);
            }
        }

        private static void LoadSpellsFromFile(World world, string path)
        {
            try
            {
                Log.Debug($"Loading custom spells from {path}");

                if (!File.Exists(path))
                    return;

                List<SpellJson> spells = JsonSerializer.Deserialize(path, SpellJsonContext.Default.ListSpellJson);
                if(spells != null)
                {
                    foreach (SpellJson spell in spells)
                    {
                        var spellDef = new SpellDefinition(spell.SpellName, spell.SpellIndex, spell.GumpIcon, spell.SmallGumpIcon, spell.PowerWords, spell.ManaCost, spell.MinSkill, spell.TithingCost, spell.TargetType, 0, spell.AllReagents);

                        switch (spell.School)
                        {
                            case "Magery":
                                SpellsMagery.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Necromancy":
                                SpellsNecromancy.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Bushido":
                                SpellsBushido.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Chivalry":
                                SpellsChivalry.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Mastery":
                                SpellsMastery.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Mysticism":
                                SpellsMysticism.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Ninjitsu":
                                SpellsNinjitsu.SetSpell(spell.SpellID, spellDef);
                                break;
                            case "Spellweaving":
                                SpellsSpellweaving.SetSpell(spell.SpellID, spellDef);
                                break;
                            default:
                                GameActions.Print(world, $"Failed to load a spell, matching school not found for: [{spell.School}]. Spell was {spell.SpellName}({spell.SpellID})");
                                continue;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void AddToWatchedSpell()
        {
            if (!string.IsNullOrEmpty(PowerWords))
            {
                WordToTargettype[PowerWords] = this;
            }
            else if (!string.IsNullOrEmpty(Name))
            {
                WordToTargettype[Name] = this;
            }
        }

        public static bool TryGetSpellFromName(string spellName, out SpellDefinition spell, bool partialMatch = true)
        {
            var allSpells = GetAllSpells();

            if (partialMatch)
            {
                string lower = spellName.ToLower();
                foreach (SpellDefinition def in allSpells)
                {
                    if (def.Name.ToLower().Contains(lower) || def.GetLocalizedName().ToLower().Contains(lower))
                    {
                        spell = def;
                        return true;
                    }
                }
            }
            else
            {
                foreach (SpellDefinition def in allSpells)
                {
                    if (def.Name.Equals(spellName, StringComparison.InvariantCultureIgnoreCase) ||
                        def.GetLocalizedName().Equals(spellName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        spell = def;
                        return true;
                    }
                }
            }

            spell = null;
            return false;
        }

        public string CreateReagentListString(string separator)
        {
            var sb = new ValueStringBuilder();
            {
                for (int i = 0; i < Regs.Length; i++)
                {
                    switch (Regs[i])
                    {
                        case Reagents.BlackPearl:
                            AppendReagentName(ref sb, 1044353, TazLang.Get("black_pearl"));

                            break;

                        case Reagents.Bloodmoss:
                            AppendReagentName(ref sb, 1044354, TazLang.Get("bloodmoss"));

                            break;

                        case Reagents.Garlic:
                            AppendReagentName(ref sb, 1044355, TazLang.Get("garlic"));

                            break;

                        case Reagents.Ginseng:
                            AppendReagentName(ref sb, 1044356, TazLang.Get("ginseng"));

                            break;

                        case Reagents.MandrakeRoot:
                            AppendReagentName(ref sb, 1044357, TazLang.Get("mandrake_root"));

                            break;

                        case Reagents.Nightshade:
                            AppendReagentName(ref sb, 1044358, TazLang.Get("nightshade"));

                            break;

                        case Reagents.SulfurousAsh:
                            AppendReagentName(ref sb, 1044359, TazLang.Get("sulfurous_ash"));

                            break;

                        case Reagents.SpidersSilk:
                            AppendReagentName(ref sb, 1044360, TazLang.Get("spiders_silk"));

                            break;

                        case Reagents.BatWing:
                            AppendReagentName(ref sb, 1023960, TazLang.Get("bat_wing"));

                            break;

                        case Reagents.GraveDust:
                            AppendReagentName(ref sb, 1023983, TazLang.Get("grave_dust"));

                            break;

                        case Reagents.DaemonBlood:
                            AppendReagentName(ref sb, 1023965, TazLang.Get("daemon_blood"));

                            break;

                        case Reagents.NoxCrystal:
                            AppendReagentName(ref sb, 1023982, TazLang.Get("nox_crystal"));

                            break;

                        case Reagents.PigIron:
                            AppendReagentName(ref sb, 1023978, TazLang.Get("pig_iron"));

                            break;

                        case Reagents.Bone:
                            AppendReagentName(ref sb, 1023966, StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));

                            break;

                        case Reagents.DemonBone:
                            AppendReagentName(ref sb, 1023968, StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));

                            break;

                        case Reagents.FertileDirt:
                            AppendReagentName(ref sb, 1023969, StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));

                            break;

                        case Reagents.DragonsBlood:
                            AppendReagentName(ref sb, 1023970, StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));

                            break;

                        default:

                            if (Regs[i] < Reagents.None)
                            {
                                sb.Append(StringHelper.AddSpaceBeforeCapital(Regs[i].ToString()));
                            }

                            break;
                    }

                    if (i < Regs.Length - 1)
                    {
                        sb.Append(separator);
                    }
                }

                string ss = sb.ToString();
                sb.Dispose();
                return ss;
            }
        }

        private static void AppendReagentName(ref ValueStringBuilder sb, int clilocNumber, string fallback)
        {
            string cliloc = Client.Game.UO.FileManager.Clilocs?.GetString(clilocNumber);
            if (!string.IsNullOrEmpty(cliloc))
            {
                sb.Append(cliloc);
            }
            else
            {
                sb.Append(fallback);
            }
        }

        public static SpellDefinition FullIndexGetSpell(int fullidx)
        {
            if (fullidx < 1 || fullidx > 799) return EmptySpell;

            if (fullidx < 100) return SpellsMagery.GetSpell(fullidx);

            if (fullidx < 200) return SpellsNecromancy.GetSpell(fullidx % 100);

            if (fullidx < 300) return SpellsChivalry.GetSpell(fullidx % 100);

            #region Custom eventine spells
            if (Settings.GlobalSettings.CustomServer == Settings.CustomServers.Eventine)
            {
                if (fullidx < 340 ) return SpellsDruid.GetSpell((fullidx - 1) % 100);

                if (fullidx < 400) return SpellsCleric.GetSpell((fullidx - 41) % 100);
            }
            #endregion

            if (fullidx < 500) return SpellsBushido.GetSpell(fullidx % 100);

            if (fullidx < 600) return SpellsNinjitsu.GetSpell(fullidx % 100);

            if (fullidx < 678) return SpellsSpellweaving.GetSpell(fullidx % 100);

            if (fullidx < 700) return SpellsMysticism.GetSpell((fullidx - 77) % 100);

            return SpellsMastery.GetSpell(fullidx % 100);
        }

        public static SpellDefinition[] GetAllSpells() => [
                .. SpellsMagery.GetAllSpells.Values,
                .. SpellsNecromancy.GetAllSpells.Values,
                .. SpellsChivalry.GetAllSpells.Values,
                .. SpellsBushido.GetAllSpells.Values,
                .. SpellsNinjitsu.GetAllSpells.Values,
                .. SpellsSpellweaving.GetAllSpells.Values,
                .. SpellsMysticism.GetAllSpells.Values,
                .. SpellsMastery.GetAllSpells.Values,
            ];

        public static void SaveAllSpellsToJson(World world)
        {
            var list = new List<SpellJson>();

            foreach (SpellDefinition spell in GetAllSpells())
            {
                if (spell.ID < 1 || spell.ID > 799)
                {
                    continue;
                }

                var spellJson = new SpellJson()
                {
                    SpellName = spell.Name,
                    PowerWords = spell.PowerWords,
                    GumpIcon = spell.GumpIconID,
                    SmallGumpIcon = spell.GumpIconSmallID,
                    ManaCost = spell.ManaCost,
                    MinSkill = spell.MinSkill,
                    TithingCost = spell.TithingCost,
                    TargetType = spell.TargetType,
                    AllReagents = spell.Regs

                };

                if (spell.ID < 100)
                {
                    spellJson.School = "Magery";
                    spellJson.SpellID = spell.ID;
                }
                else if (spell.ID < 200)
                {
                    spellJson.School = "Necromancy";
                    spellJson.SpellID = spell.ID - 100;
                    spellJson.SpellOffset = 100;

                }
                else if (spell.ID < 300)
                {
                    spellJson.School = "Chivalry";
                    spellJson.SpellID = spell.ID - 200;
                    spellJson.SpellOffset = 200;
                }
                else if (spell.ID < 500)
                {
                    spellJson.School = "Bushido";
                    spellJson.SpellID = spell.ID - 400;
                    spellJson.SpellOffset = 400;
                }
                else if (spell.ID < 600)
                {
                    spellJson.School = "Ninjitsu";
                    spellJson.SpellID = spell.ID - 500;
                    spellJson.SpellOffset = 500;
                }
                else if (spell.ID < 678)
                {
                    spellJson.School = "Spellweaving";
                    spellJson.SpellID = spell.ID - 600;
                    spellJson.SpellOffset = 600;
                }
                else if (spell.ID < 700)
                {
                    spellJson.School = "Mysticism";
                    spellJson.SpellID = spell.ID - 600;
                    spellJson.SpellOffset = 600;
                }
                else if (spell.ID < 800)
                {
                    spellJson.School = "Mastery";
                    spellJson.SpellID = spell.ID - 700;
                    spellJson.SpellOffset = 700;
                }

                list.Add(spellJson);
            }

            FileSystemHelper.WriteAllTextSafe(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "spelldef.json"),JsonSerializer.Serialize(list, SpellJsonContext.Default.ListSpellJson));

            GameActions.Print(world, $"Saved all spells as a json file at {Path.Combine(CUOEnviroment.ExecutablePath, "Data", "spelldef.json")}");
        }

        public static void FullIndexSetModifySpell
        (
            int fullidx,
            int id,
            int iconid,
            int smalliconid,
            int minskill,
            int manacost,
            int tithing,
            string name,
            string words,
            TargetType target,
            params Reagents[] regs
        )
        {
            if (fullidx < 1 || fullidx > 799) return;

            SpellDefinition sd = FullIndexGetSpell(fullidx);

            if (sd.ID == fullidx) //we are not using an emptyspell spelldefinition
            {
                if (iconid == 0) iconid = sd.GumpIconID;

                if (smalliconid == 0) smalliconid = sd.GumpIconSmallID;

                if (tithing == 0) tithing = sd.TithingCost;

                if (manacost == 0) manacost = sd.ManaCost;

                if (minskill == 0) minskill = sd.MinSkill;

                if (!string.IsNullOrEmpty(sd.PowerWords) && sd.PowerWords != words) WordToTargettype.Remove(sd.PowerWords);

                if (!string.IsNullOrEmpty(sd.Name) && sd.Name != name) WordToTargettype.Remove(sd.Name);
            }

            sd = new SpellDefinition
            (
                name,
                fullidx,
                iconid,
                smalliconid,
                words,
                manacost,
                minskill,
                tithing,
                target,
                0,
                regs
            );

            switch (fullidx)
            {
                case < 100:
                    SpellsMagery.SetSpell(id, in sd);
                    break;
                case < 200:
                    SpellsNecromancy.SetSpell(id, in sd);
                    break;
                case < 300:
                    SpellsChivalry.SetSpell(id, in sd);
                    break;

                #region Custom eventine spells
                case < 340 when Settings.GlobalSettings.CustomServer == Settings.CustomServers.Eventine:
                    SpellsDruid.SetSpell(id - 1, in sd);
                    break;
                case < 400 when Settings.GlobalSettings.CustomServer == Settings.CustomServers.Eventine:
                    SpellsCleric.SetSpell(id - 41, in sd);
                    break;
                #endregion

                case < 500:
                    SpellsBushido.SetSpell(id, in sd);
                    break;
                case < 600:
                    SpellsNinjitsu.SetSpell(id, in sd);
                    break;
                case < 678:
                    SpellsSpellweaving.SetSpell(id, in sd);
                    break;
                case < 700:
                    SpellsMysticism.SetSpell(id - 77, in sd);
                    break;
                default:
                    SpellsMastery.SetSpell(id, in sd);
                    break;
            }
        }
    }

    [JsonSerializable(typeof(List<SpellJson>))]
    public partial class SpellJsonContext : JsonSerializerContext
    {
    }

    public class SpellJson
    {
        public string School { get; set; } = "Magery";

        public int SpellID { get; set; } = 0;
        public int SpellOffset { get; set; } = 0;
        public string SpellName { get; set; } = "";
        public string PowerWords { get; set; } = "";
        public int GumpIcon { get; set; } = 0x5000;
        public int SmallGumpIcon { get; set; } = 0x5000;
        public int ManaCost { get; set; } = 0;
        public int MinSkill { get; set; } = 0;
        public int TithingCost { get; set; } = 0;
        public TargetType TargetType { get; set; } = TargetType.Neutral;
        public Reagents[] AllReagents { get; set; } = { };

        [JsonIgnore]
        public int SpellIndex => SpellID + SpellOffset;
    }
}
