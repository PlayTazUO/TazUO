using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.LegionScripting;

namespace ClassicUO.Game.Managers;

/// <summary>The kind of action stored in a single counter bar slot.</summary>
public enum CounterBarSlotType { Empty = 0, Spell = 1, Macro = 2, Ability = 3, Script = 4, Skill = 5, DressAgent = 6 }

/// <summary>
/// A single counter bar slot. Holds one of: nothing, a spell, a macro, a weapon
/// (primary/secondary) ability, a script, a skill, or a dress-agent action, and
/// centralizes activation, icon, and tooltip resolution.
/// </summary>
public class CounterBarSlot
{
    /// <summary>What this slot holds (spell, macro, ability, script, skill, dress agent, or empty).</summary>
    public CounterBarSlotType Type { get; set; } = CounterBarSlotType.Empty;

    /// <summary>Full spell index when <see cref="Type"/> is <see cref="CounterBarSlotType.Spell"/>.</summary>
    public int SpellId { get; set; } = -2;          // Type == Spell

    /// <summary>Macro name when <see cref="Type"/> is <see cref="CounterBarSlotType.Macro"/>.</summary>
    public string MacroName { get; set; }           // Type == Macro

    /// <summary>Stable relative id (ScriptFile.RelativePath) when <see cref="Type"/> is <see cref="CounterBarSlotType.Script"/>.</summary>
    public string ScriptId { get; set; }            // Type == Script

    /// <summary>Skill index (matches <see cref="GameActions.UseSkill"/>) when <see cref="Type"/> is <see cref="CounterBarSlotType.Skill"/>.</summary>
    public int SkillIndex { get; set; } = -1;       // Type == Skill

    /// <summary>True for the primary ability, false for the secondary, when <see cref="Type"/> is <see cref="CounterBarSlotType.Ability"/>.</summary>
    public bool AbilityPrimary { get; set; }        // Type == Ability (true = primary, false = secondary)

    /// <summary>Dress configuration name when <see cref="Type"/> is <see cref="CounterBarSlotType.DressAgent"/>.</summary>
    public string DressConfigName { get; set; }     // Type == DressAgent

    /// <summary>True to undress, false to dress, when <see cref="Type"/> is <see cref="CounterBarSlotType.DressAgent"/>.</summary>
    public bool DressAgentUndress { get; set; }     // Type == DressAgent

    /// <summary>True when the slot holds nothing.</summary>
    [JsonIgnore]
    public bool IsEmpty => Type == CounterBarSlotType.Empty;

    /// <summary>The resolved spell for spell slots, otherwise <see cref="SpellDefinition.EmptySpell"/>.</summary>
    [JsonIgnore]
    public SpellDefinition Spell => Type == CounterBarSlotType.Spell ? SpellDefinition.FullIndexGetSpell(SpellId) : SpellDefinition.EmptySpell;

    /// <summary>The spell id for spell slots, or -1 for any other type (used to match cast events).</summary>
    [JsonIgnore]
    public int CurrentSpellID => Type == CounterBarSlotType.Spell ? SpellId : -1;

    /// <summary>The loaded script this slot points at (by RelativePath), or null.</summary>
    [JsonIgnore]
    public ScriptFile ResolvedScript =>
        ClassicUO.LegionScripting.LegionScripting.LoadedScripts.FirstOrDefault(f => f.RelativePath == ScriptId);

    /// <summary>True when this is a script slot whose script is currently running.</summary>
    [JsonIgnore]
    public bool IsScriptRunning => Type == CounterBarSlotType.Script && (ResolvedScript?.IsPlaying ?? false);

    /// <summary>Friendly name for a script slot: the resolved file name, else the id's basename.</summary>
    [JsonIgnore]
    public string ScriptDisplayName =>
        ResolvedScript?.FileName ??
        (string.IsNullOrEmpty(ScriptId) ? string.Empty : System.IO.Path.GetFileName(ScriptId));

    /// <summary>Friendly name for a skill slot, resolved from the skill data files, or empty.</summary>
    [JsonIgnore]
    public string SkillDisplayName
    {
        get
        {
            List<SkillEntry> skills = Client.Game?.UO?.FileManager?.Skills?.Skills;
            if (skills == null)
                return string.Empty;

            foreach (SkillEntry s in skills)
                if (s.Index == SkillIndex)
                    return s.Name;

            return string.Empty;
        }
    }

    /// <summary>Friendly action name for a dress-agent slot.</summary>
    [JsonIgnore]
    public string DressAgentDisplayName
    {
        get
        {
            string action = DressAgentUndress
                ? TazLang.Get("dressagent_undress", "Undress")
                : TazLang.Get("dressagent_dress", "Dress");
            return string.IsNullOrEmpty(DressConfigName) ? action : $"{action}: {DressConfigName}";
        }
    }

    /// <summary>The short in-slot label text for icon-less slots, or null for other types.</summary>
    [JsonIgnore]
    public string SlotLabel => Type switch
    {
        CounterBarSlotType.Macro => MacroName,
        CounterBarSlotType.Script => ScriptDisplayName,
        CounterBarSlotType.Skill => SkillDisplayName,
        CounterBarSlotType.DressAgent => DressAgentDisplayName,
        _ => null
    };

    /// <summary>Red highlight hue the spell/counter bar uses for an active ability or toggle-move spell.</summary>
    public const ushort ActiveHue = 38;

    /// <summary>
    /// The highlight hue for a slot whose action is currently "active": a primary/secondary weapon
    /// ability that is toggled on, or a toggle-move spell (e.g. Ninjitsu moves) reported active via
    /// <see cref="World.ActiveSpellIcons"/>. Returns 0 for everything else.
    /// </summary>
    public ushort GetActiveHue(World world)
    {
        switch (Type)
        {
            case CounterBarSlotType.Ability:
                if (world?.Player == null)
                    return 0;
                return ((byte)world.Player.Abilities[AbilityPrimary ? 0 : 1] & 0x80) != 0 ? ActiveHue : (ushort)0;

            case CounterBarSlotType.Spell:
                return world != null && world.ActiveSpellIcons.IsActive((ushort)CurrentSpellID) ? ActiveHue : (ushort)0;
        }

        return 0;
    }

    /// <summary>Toggle decision for a script slot: play when not already running.</summary>
    public static bool ShouldPlay(bool isRunning) => !isRunning;

    /// <summary>Creates an empty slot.</summary>
    public static CounterBarSlot Empty() => new CounterBarSlot();

    /// <summary>Creates a spell slot, or an empty slot when <paramref name="spell"/> is null/empty.</summary>
    public static CounterBarSlot FromSpell(SpellDefinition spell)
    {
        if (spell == null || spell == SpellDefinition.EmptySpell)
            return Empty();

        return new CounterBarSlot { Type = CounterBarSlotType.Spell, SpellId = spell.ID };
    }

    /// <summary>Creates a macro slot, or an empty slot when <paramref name="macro"/> is null.</summary>
    public static CounterBarSlot FromMacro(Macro macro)
    {
        if (macro == null)
            return Empty();

        return new CounterBarSlot { Type = CounterBarSlotType.Macro, MacroName = macro.Name };
    }

    /// <summary>Creates a script slot, or an empty slot when <paramref name="script"/> is null.</summary>
    public static CounterBarSlot FromScript(ScriptFile script)
    {
        if (script == null)
            return Empty();

        return new CounterBarSlot { Type = CounterBarSlotType.Script, ScriptId = script.RelativePath };
    }

    /// <summary>Creates an ability slot for the primary (<paramref name="primary"/> true) or secondary ability.</summary>
    public static CounterBarSlot FromAbility(bool primary) => new CounterBarSlot { Type = CounterBarSlotType.Ability, AbilityPrimary = primary };

    /// <summary>Creates a skill slot, or an empty slot when <paramref name="skillIndex"/> is negative.</summary>
    public static CounterBarSlot FromSkill(int skillIndex)
    {
        if (skillIndex < 0)
            return Empty();

        return new CounterBarSlot { Type = CounterBarSlotType.Skill, SkillIndex = skillIndex };
    }

    /// <summary>Creates a dress-agent slot, or an empty slot when <paramref name="config"/> is null.</summary>
    public static CounterBarSlot FromDressAgent(DressConfig config, bool undress)
    {
        if (config == null)
            return Empty();

        return new CounterBarSlot
        {
            Type = CounterBarSlotType.DressAgent,
            DressConfigName = config.Name,
            DressAgentUndress = undress
        };
    }

    /// <summary>Performs the action represented by this slot.</summary>
    public void Activate(World world)
    {
        switch (Type)
        {
            case CounterBarSlotType.Spell:
                if (Spell != null && Spell != SpellDefinition.EmptySpell)
                    GameActions.CastSpell(SpellId);
                break;

            case CounterBarSlotType.Macro:
                Macro macro = world?.Macros?.FindMacro(MacroName);
                if (macro != null)
                {
                    world.Macros.SetMacroToExecute(macro.Items as MacroObject);
                    world.Macros.WaitForTargetTimer = 0;
                    world.Macros.Update();
                }
                break;

            case CounterBarSlotType.Ability:
                if (AbilityPrimary)
                    GameActions.UsePrimaryAbility(world);
                else
                    GameActions.UseSecondaryAbility(world);
                break;

            case CounterBarSlotType.Script:
                ScriptFile script = ResolvedScript;
                if (script != null)
                {
                    if (ShouldPlay(script.IsPlaying))
                        ClassicUO.LegionScripting.LegionScripting.PlayScript(script);
                    else
                        ClassicUO.LegionScripting.LegionScripting.StopScript(script);
                }
                break;

            case CounterBarSlotType.Skill:
                if (SkillIndex >= 0)
                    GameActions.UseSkill(SkillIndex);
                break;

            case CounterBarSlotType.DressAgent:
                DressConfig config = DressAgentManager.Instance.CurrentPlayerConfigs.FirstOrDefault(
                    c => c?.Name?.Equals(DressConfigName, System.StringComparison.OrdinalIgnoreCase) == true);
                if (config != null)
                {
                    if (DressAgentUndress)
                        DressAgentManager.Instance.UndressFromConfig(config);
                    else
                        DressAgentManager.Instance.DressFromConfig(config);
                }
                break;
        }
    }

    /// <summary>
    /// Resolves the player's current primary/secondary ability index (1-based), or 0 when none.
    /// </summary>
    public int GetAbilityIndex(World world)
    {
        if (Type != CounterBarSlotType.Ability || world?.Player == null)
            return 0;

        return (byte)world.Player.Abilities[AbilityPrimary ? 0 : 1] & 0x7F;
    }

    /// <summary>Resolves the gump graphic to draw for this slot, or 0 when there is none.</summary>
    public ushort GetIconGraphic(World world)
    {
        switch (Type)
        {
            case CounterBarSlotType.Spell:
                return (ushort)Spell.GumpIconSmallID;

            case CounterBarSlotType.Macro:
                return world?.Macros?.FindMacro(MacroName)?.Graphic ?? 0;

            case CounterBarSlotType.Ability:
                int idx = GetAbilityIndex(world);
                if (idx >= 1 && idx <= AbilityData.Abilities.Length)
                    return AbilityData.Abilities[idx - 1].Icon;
                return 0;
        }

        return 0;
    }

    /// <summary>Gets the tooltip text for this slot. Returns false (and empty text) when none applies.</summary>
    public bool TryGetTooltip(World world, out string text)
    {
        switch (Type)
        {
            case CounterBarSlotType.Spell:
                int cliloc = GetSpellTooltip(SpellId);
                text = cliloc != 0 ? Client.Game.UO.FileManager.Clilocs.GetString(cliloc) : string.Empty;
                return cliloc != 0;

            case CounterBarSlotType.Macro:
                text = MacroName ?? string.Empty;
                return !string.IsNullOrEmpty(text);

            case CounterBarSlotType.Ability:
                int idx = GetAbilityIndex(world);
                if (idx >= 1 && idx <= AbilityData.Abilities.Length)
                {
                    text = Client.Game.UO.FileManager.Clilocs.GetString(1028838 + (idx - 1));
                    return true;
                }
                break;

            case CounterBarSlotType.Script:
                text = ScriptDisplayName;
                return !string.IsNullOrEmpty(text);

            case CounterBarSlotType.Skill:
                text = SkillDisplayName;
                return !string.IsNullOrEmpty(text);

            case CounterBarSlotType.DressAgent:
                text = DressAgentDisplayName;
                return !string.IsNullOrEmpty(text);
        }

        text = string.Empty;
        return false;
    }

    private static int GetSpellTooltip(int id)
    {
        if (id >= 1 && id <= 64) // Magery
            return 3002011 + (id - 1);

        if (id >= 101 && id <= 117) // necro
            return 1060509 + (id - 101);

        if (id >= 201 && id <= 210) return 1060585 + (id - 201);

        if (id >= 401 && id <= 406) return 1060595 + (id - 401);

        if (id >= 501 && id <= 508) return 1060610 + (id - 501);

        if (id >= 601 && id <= 616) return 1071026 + (id - 601);

        if (id >= 678 && id <= 693) return 1031678 + (id - 678);

        if (id >= 701 && id <= 745)
        {
            if (id <= 706) return 1115612 + (id - 701);

            if (id <= 745) return 1155896 + (id - 707);
        }

        return 0;
    }
}
