using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.Managers;

public class SpellBarManager
{
    public static List<SpellBarRow> SpellBarRows = [];
    public static int CurrentRow = 0;

    private static bool enabled;
    private static string charPath;
    private static string fullSavePath;
    private static string presetPath;
    private const string SAVE_FILE = "SpellBar.json";
    private static SpellBarSettings spellBarSettings;

    public static SpellBarSlot GetSlot(int row, int col)
    {
        if (!enabled)
            return SpellBarSlot.Empty();

        if(SpellBarRows.Count <= row || row < 0) return SpellBarSlot.Empty();
        if(SpellBarRows[row].Slots.Length <= col || col < 0) return SpellBarSlot.Empty();

        return SpellBarRows[row].Slots[col] ?? SpellBarSlot.Empty();
    }

    public static string GetControllerButtonsName(int slot)
    {
        if(spellBarSettings.ControllerButtons.Length <= slot || slot < 0) return string.Empty;
        return Controller.GetButtonNames(spellBarSettings.ControllerButtons[slot].Select(i => (SDL.SDL_GamepadButton)i).ToArray());
    }

    public static string GetKetNames(int slot)
    {
        var hotKey = (SDL.SDL_Keycode)spellBarSettings.HotKeys[slot];
        var hotMod = (SDL.SDL_Keymod)spellBarSettings.KeyMod[slot];

        return KeysTranslator.TryGetKey(hotKey, hotMod);
    }

    public static void ControllerInput(SDL.SDL_GamepadButton button)
    {
        if (!enabled || !spellBarSettings.Enabled || ProfileManager.CurrentProfile.DisableHotkeys)
            return;

        for (int i = 0; i < 10; i++) //Currently 10 spells per row supported
        {
            if (spellBarSettings.ControllerButtons.Length <= 0)
                return;

            if(Controller.AreButtonsPressed(spellBarSettings.ControllerButtons[i]))
                UseSlot(CurrentRow, i);
        }
    }

    public static void KeyPress(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    {
        if (!enabled || !spellBarSettings.Enabled || ProfileManager.CurrentProfile.DisableHotkeys)
            return;

        // Remove lock keys from modifier checks (these shouldn't affect hotkey matching)
        mod &= ~SDL.SDL_Keymod.SDL_KMOD_NUM;
        mod &= ~SDL.SDL_Keymod.SDL_KMOD_CAPS;
        mod &= ~SDL.SDL_Keymod.SDL_KMOD_SCROLL;
        mod &= ~SDL.SDL_Keymod.SDL_KMOD_MODE;

        // Normalize left/right modifiers to generic modifiers
        if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL)) != 0)
        {
            mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LCTRL | SDL.SDL_Keymod.SDL_KMOD_RCTRL);
            mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
        }
        if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT)) != 0)
        {
            mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LSHIFT | SDL.SDL_Keymod.SDL_KMOD_RSHIFT);
            mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
        }
        if ((mod & (SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT)) != 0)
        {
            mod &= ~(SDL.SDL_Keymod.SDL_KMOD_LALT | SDL.SDL_Keymod.SDL_KMOD_RALT);
            mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
        }

        for (int i = 0; i < 10; i++)
        {
            if (i >= spellBarSettings.HotKeys.Length)
                break;

            var hotKey = (SDL.SDL_Keycode)spellBarSettings.HotKeys[i];
            var hotMod = (SDL.SDL_Keymod)spellBarSettings.KeyMod[i];

            if (key != hotKey)
                continue;

            // If no mod is expected, only allow if none are pressed
            if (hotMod == SDL.SDL_Keymod.SDL_KMOD_NONE)
            {
                if (mod == SDL.SDL_Keymod.SDL_KMOD_NONE)
                    UseSlot(CurrentRow, i);
            }
            else
            {
                // All required mods must be present
                if ((mod & hotMod) == hotMod)
                    UseSlot(CurrentRow, i);
            }
        }
    }

    public static void UseSlot(int row, int col)
    {
        if (!enabled || !spellBarSettings.Enabled)
            return;

        SpellBarSlot slot = GetSlot(row, col);

        if (slot == null || slot.IsEmpty)
            return;

        slot.Activate(Client.Game.UO.World);
    }

    public static SDL.SDL_GamepadButton[][] GetControllerButtons()
    {
        if (!enabled || !spellBarSettings.Enabled)
            return [];

        return spellBarSettings.ControllerButtons
                               .Select(group => group.Select(x => (SDL.SDL_GamepadButton)x).ToArray())
                               .ToArray();
    }

    public static SDL.SDL_Keycode[] GetHotKeys() => spellBarSettings.HotKeys.Select(x => (SDL.SDL_Keycode)x).ToArray();

    public static SDL.SDL_Keymod[] GetModKeys() => spellBarSettings.KeyMod.Select(x=>(SDL.SDL_Keymod)x).ToArray();

    public static void SetButtons(int slot, SDL.SDL_Keymod mod, SDL.SDL_Keycode key, SDL.SDL_GamepadButton[] controllerButtons)
    {
        spellBarSettings.KeyMod[slot] = (int)mod;
        spellBarSettings.HotKeys[slot] = (int)key;
        if( controllerButtons == null) return;
        spellBarSettings.ControllerButtons[slot] = controllerButtons.Select(x => (int)x).ToArray();
    }

    public static bool IsEnabled()
    {
        if(spellBarSettings != null)
            return spellBarSettings.Enabled;
        return false;
    }

    public static bool ToggleEnabled()
    {
        if(spellBarSettings == null)
            spellBarSettings = new SpellBarSettings();

        spellBarSettings.Enabled = !spellBarSettings.Enabled;
        return spellBarSettings.Enabled;
    }

    public static void SaveCurrentRowPreset(string name)
    {
        if (!enabled || !spellBarSettings.Enabled)
            return;

        if (string.IsNullOrEmpty(name))
            return;

        try
        {
            string path = Path.Combine(presetPath, name + ".json");

            if (!Directory.Exists(presetPath))
                Directory.CreateDirectory(presetPath);

            File.WriteAllText(path, JsonSerializer.Serialize(SpellBarRows[CurrentRow], SpellBarRowsContext.Default.SpellBarRow));
            GameActions.Print(Client.Game.UO.World, TazLang.Get("spellbar_savedrow", new[] { name }));
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            GameActions.Print(Client.Game.UO.World, TazLang.Get("spellbar_savedrow_error", new[] { name }), 32);
        }
    }

    public static void ImportPreset(string name)
    {
        if (!enabled || !spellBarSettings.Enabled)
            return;

        if (string.IsNullOrEmpty(name))
            return;

        string path = Path.Combine(presetPath, name + ".json");
        if (!File.Exists(path))
            return;

        try
        {
            SpellBarRow row = JsonSerializer.Deserialize(File.ReadAllText(path), SpellBarRowsContext.Default.SpellBarRow);
            SpellBarRows.Add(row);
            Unload(); //Save
            GameActions.Print(Client.Game.UO.World, TazLang.Get("spellbar_importedpreset", new[] { name }));
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
            GameActions.Print(Client.Game.UO.World, TazLang.Get("spellbar_importpreset_error", new[] { name }), 32);
        }

    }

    public static string[] ListPresets()
    {
        if (!enabled || !spellBarSettings.Enabled)
            return [];

        if (!Directory.Exists(presetPath))
            return [];

        string[] files = Directory.GetFiles(presetPath, "*.json");
        return files.Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
    }

    public static void Load()
    {
        charPath = ProfileManager.ProfilePath;
        presetPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "SpellBarPresets");
        fullSavePath = Path.Combine(charPath, SAVE_FILE);

        if (File.Exists(fullSavePath))
        {
            try
            {
                SpellBarRows = JsonSerializer.Deserialize(File.ReadAllText(fullSavePath), SpellBarRowsContext.Default.ListSpellBarRow);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                SetDefaults();
            }
        }
        else
        {
            SetDefaults();
        }
        if(SpellBarRows.Count == 0)
            SpellBarRows.Add(new SpellBarRow()); //Ensure at least one row is present

        if (File.Exists(Path.Combine(charPath, "SpellBarSettings.json")))
        {
            try
            {
                spellBarSettings = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(charPath, "SpellBarSettings.json")), SpellBarSettingsContext.Default.SpellBarSettings);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        if(spellBarSettings == null)
            spellBarSettings = new SpellBarSettings();

        enabled = true;
    }

    public static void Unload()
    {
        try
        {
            File.WriteAllText(fullSavePath, JsonSerializer.Serialize(SpellBarRows, SpellBarRowsContext.Default.ListSpellBarRow));
            File.WriteAllText(Path.Combine(charPath, "SpellBarSettings.json"), JsonSerializer.Serialize(spellBarSettings, SpellBarSettingsContext.Default.SpellBarSettings));
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static void SetDefaults() => SpellBarRows = [new SpellBarRow()
        .SetSlot(0, SpellBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(29)))
        .SetSlot(1, SpellBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(11)))
        .SetSlot(2, SpellBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(22)))];
}

/// <summary>The kind of action stored in a single spell bar slot.</summary>
public enum SpellBarSlotType { Empty = 0, Spell = 1, Macro = 2, Ability = 3, Script = 4, Skill = 5 }

/// <summary>
/// A single spell bar slot. Holds one of: nothing, a spell, a macro, or a weapon
/// (primary/secondary) ability, and centralizes activation, icon, and tooltip resolution.
/// </summary>
public class SpellBarSlot
{
    /// <summary>What this slot holds (spell, macro, ability, or empty).</summary>
    public SpellBarSlotType Type { get; set; } = SpellBarSlotType.Empty;

    /// <summary>Full spell index when <see cref="Type"/> is <see cref="SpellBarSlotType.Spell"/>.</summary>
    public int SpellId { get; set; } = -2;          // Type == Spell

    /// <summary>Macro name when <see cref="Type"/> is <see cref="SpellBarSlotType.Macro"/>.</summary>
    public string MacroName { get; set; }           // Type == Macro

    /// <summary>Stable relative id (ScriptFile.RelativePath) when <see cref="Type"/> is <see cref="SpellBarSlotType.Script"/>.</summary>
    public string ScriptId { get; set; }            // Type == Script

    /// <summary>Skill index (matches <see cref="GameActions.UseSkill"/>) when <see cref="Type"/> is <see cref="SpellBarSlotType.Skill"/>.</summary>
    public int SkillIndex { get; set; } = -1;       // Type == Skill

    /// <summary>True for the primary ability, false for the secondary, when <see cref="Type"/> is <see cref="SpellBarSlotType.Ability"/>.</summary>
    public bool AbilityPrimary { get; set; }        // Type == Ability (true = primary, false = secondary)

    /// <summary>True when the slot holds nothing.</summary>
    [JsonIgnore]
    public bool IsEmpty => Type == SpellBarSlotType.Empty;

    /// <summary>The resolved spell for spell slots, otherwise <see cref="SpellDefinition.EmptySpell"/>.</summary>
    [JsonIgnore]
    public SpellDefinition Spell => Type == SpellBarSlotType.Spell ? SpellDefinition.FullIndexGetSpell(SpellId) : SpellDefinition.EmptySpell;

    /// <summary>The spell id for spell slots, or -1 for any other type (used to match cast events).</summary>
    [JsonIgnore]
    public int CurrentSpellID => Type == SpellBarSlotType.Spell ? SpellId : -1;

    /// <summary>The loaded script this slot points at (by RelativePath), or null.</summary>
    [JsonIgnore]
    public ScriptFile ResolvedScript =>
        ClassicUO.LegionScripting.LegionScripting.LoadedScripts.FirstOrDefault(f => f.RelativePath == ScriptId);

    /// <summary>True when this is a script slot whose script is currently running.</summary>
    [JsonIgnore]
    public bool IsScriptRunning => Type == SpellBarSlotType.Script && (ResolvedScript?.IsPlaying ?? false);

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
            var skills = Client.Game?.UO?.FileManager?.Skills?.Skills;
            if (skills == null)
                return string.Empty;

            foreach (var s in skills)
                if (s.Index == SkillIndex)
                    return s.Name;

            return string.Empty;
        }
    }

    /// <summary>Toggle decision for a script slot: play when not already running.</summary>
    public static bool ShouldPlay(bool isRunning) => !isRunning;

    /// <summary>Creates an empty slot.</summary>
    public static SpellBarSlot Empty() => new SpellBarSlot();

    /// <summary>Creates a spell slot, or an empty slot when <paramref name="spell"/> is null/empty.</summary>
    public static SpellBarSlot FromSpell(SpellDefinition spell)
    {
        if (spell == null || spell == SpellDefinition.EmptySpell)
            return Empty();

        return new SpellBarSlot { Type = SpellBarSlotType.Spell, SpellId = spell.ID };
    }

    /// <summary>Creates a macro slot, or an empty slot when <paramref name="macro"/> is null.</summary>
    public static SpellBarSlot FromMacro(Macro macro)
    {
        if (macro == null)
            return Empty();

        return new SpellBarSlot { Type = SpellBarSlotType.Macro, MacroName = macro.Name };
    }

    /// <summary>Creates a script slot, or an empty slot when <paramref name="script"/> is null.</summary>
    public static SpellBarSlot FromScript(ScriptFile script)
    {
        if (script == null)
            return Empty();

        return new SpellBarSlot { Type = SpellBarSlotType.Script, ScriptId = script.RelativePath };
    }

    /// <summary>Creates an ability slot for the primary (<paramref name="primary"/> true) or secondary ability.</summary>
    public static SpellBarSlot FromAbility(bool primary) => new SpellBarSlot { Type = SpellBarSlotType.Ability, AbilityPrimary = primary };

    /// <summary>Creates a skill slot, or an empty slot when <paramref name="skillIndex"/> is negative.</summary>
    public static SpellBarSlot FromSkill(int skillIndex)
    {
        if (skillIndex < 0)
            return Empty();

        return new SpellBarSlot { Type = SpellBarSlotType.Skill, SkillIndex = skillIndex };
    }

    /// <summary>Performs the slot's action: casts the spell, runs the macro, or triggers the ability.</summary>
    public void Activate(World world)
    {
        switch (Type)
        {
            case SpellBarSlotType.Spell:
                if (Spell != null && Spell != SpellDefinition.EmptySpell)
                    GameActions.CastSpell(SpellId);
                break;

            case SpellBarSlotType.Macro:
                Macro macro = world?.Macros?.FindMacro(MacroName);
                if (macro != null)
                {
                    world.Macros.SetMacroToExecute(macro.Items as MacroObject);
                    world.Macros.WaitForTargetTimer = 0;
                    world.Macros.Update();
                }
                break;

            case SpellBarSlotType.Ability:
                if (AbilityPrimary)
                    GameActions.UsePrimaryAbility(world);
                else
                    GameActions.UseSecondaryAbility(world);
                break;

            case SpellBarSlotType.Script:
                ScriptFile script = ResolvedScript;
                if (script != null)
                {
                    if (ShouldPlay(script.IsPlaying))
                        ClassicUO.LegionScripting.LegionScripting.PlayScript(script);
                    else
                        ClassicUO.LegionScripting.LegionScripting.StopScript(script);
                }
                break;

            case SpellBarSlotType.Skill:
                if (SkillIndex >= 0)
                    GameActions.UseSkill(SkillIndex);
                break;
        }
    }

    /// <summary>
    /// Resolves the player's current primary/secondary ability index (1-based), or 0 when none.
    /// </summary>
    public int GetAbilityIndex(World world)
    {
        if (Type != SpellBarSlotType.Ability || world?.Player == null)
            return 0;

        return (byte)world.Player.Abilities[AbilityPrimary ? 0 : 1] & 0x7F;
    }

    /// <summary>Resolves the gump graphic to draw for this slot, or 0 when there is none.</summary>
    public ushort GetIconGraphic(World world)
    {
        switch (Type)
        {
            case SpellBarSlotType.Spell:
                return (ushort)Spell.GumpIconSmallID;

            case SpellBarSlotType.Macro:
                return world?.Macros?.FindMacro(MacroName)?.Graphic ?? 0;

            case SpellBarSlotType.Ability:
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
            case SpellBarSlotType.Spell:
                int cliloc = GetSpellTooltip(SpellId);
                text = cliloc != 0 ? Client.Game.UO.FileManager.Clilocs.GetString(cliloc) : string.Empty;
                return cliloc != 0;

            case SpellBarSlotType.Macro:
                text = MacroName ?? string.Empty;
                return !string.IsNullOrEmpty(text);

            case SpellBarSlotType.Ability:
                int idx = GetAbilityIndex(world);
                if (idx >= 1 && idx <= AbilityData.Abilities.Length)
                {
                    text = Client.Game.UO.FileManager.Clilocs.GetString(1028838 + (idx - 1));
                    return true;
                }
                break;

            case SpellBarSlotType.Script:
                text = ScriptDisplayName;
                return !string.IsNullOrEmpty(text);

            case SpellBarSlotType.Skill:
                text = SkillDisplayName;
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

public class SpellBarRow()
{
    private SpellBarSlot[] _slots = CreateEmptySlots();

    // Always a 10-element array with no null entries, even when deserialized from a
    // malformed file (null, short, or containing nulls), since the array is indexed directly.
    public SpellBarSlot[] Slots
    {
        get => _slots;
        set => _slots = NormalizeSlots(value);
    }

    // Legacy migration: older SpellBar.json/preset files only stored spell ids here.
    // Deserialize-only; never written to new files (getter returns null).
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[] SpellSlotIds
    {
        get => null;
        set
        {
            if (value == null)
                return;

            Slots = CreateEmptySlots();
            for (int i = 0; i < _slots.Length && i < value.Length; i++)
                _slots[i] = SpellBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(value[i]));
        }
    }

    public ushort RowHue { get; set; }

    public SpellBarRow SetSlot(int slot, SpellBarSlot value)
    {
        if ((uint)slot >= (uint)_slots.Length)
            return this;

        _slots[slot] = value ?? SpellBarSlot.Empty();

        return this;
    }

    private static SpellBarSlot[] NormalizeSlots(SpellBarSlot[] value)
    {
        var slots = CreateEmptySlots();

        if (value == null)
            return slots;

        for (int i = 0; i < slots.Length && i < value.Length; i++)
            slots[i] = value[i] ?? SpellBarSlot.Empty();

        return slots;
    }

    private static SpellBarSlot[] CreateEmptySlots()
    {
        var slots = new SpellBarSlot[10];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = SpellBarSlot.Empty();
        return slots;
    }
}

public class SpellBarSettings
{
    public bool Enabled { get; set; }

    public int CurrentRow { get; set; } = 0;

    public int[] HotKeys { get; set; } = [(int)SDL.SDL_Keycode.SDLK_F1, (int)SDL.SDL_Keycode.SDLK_F2, (int)SDL.SDL_Keycode.SDLK_F3, (int)SDL.SDL_Keycode.SDLK_F4, (int)SDL.SDL_Keycode.SDLK_F5,
        (int)SDL.SDL_Keycode.SDLK_F6, (int)SDL.SDL_Keycode.SDLK_F7, (int)SDL.SDL_Keycode.SDLK_F8, (int)SDL.SDL_Keycode.SDLK_F9, (int)SDL.SDL_Keycode.SDLK_F10];

    public int[] KeyMod { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    public int[][] ControllerButtons { get; set; } = [[-1],[-1],[-1],[-1],[-1],[-1],[-1],[-1],[-1],[-1]];
}

[JsonSerializable(typeof(List<SpellBarRow>), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SpellBarRow), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SpellBarSlot), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SpellBarSlot[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SpellBarRowsContext : JsonSerializerContext { }

[JsonSerializable(typeof(SpellBarSettings))]
public partial class SpellBarSettingsContext : JsonSerializerContext { }
