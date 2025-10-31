using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
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

    public static SpellDefinition GetSpell(int row, int col)
    {
        if (!enabled)
            return SpellDefinition.EmptySpell;

        if(SpellBarRows.Count <= row || row < 0) return SpellDefinition.EmptySpell;
        if(SpellBarRows[row].SpellSlot.Length <= col || col < 0) return SpellDefinition.EmptySpell;

        return SpellBarRows[row].SpellSlot[col];
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

        // Remove NUM lock from modifier checks
        mod &= ~SDL.SDL_Keymod.SDL_KMOD_NUM;

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

        SpellDefinition spell = GetSpell(row, col);

        if (spell == null || spell == SpellDefinition.EmptySpell)
            return;

        GameActions.CastSpell(spell.ID);
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
        spellBarSettings.ControllerButtons[slot] = controllerButtons.Select(x => x == null ? -1 : (int)x).ToArray();
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
            GameActions.Print(Client.Game.UO.World, $"Saved the current row as {name}");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            GameActions.Print(Client.Game.UO.World, $"Error saving the current row as {name}.json..", 32);
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
            GameActions.Print(Client.Game.UO.World, $"Imported {name} preset");
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
            GameActions.Print(Client.Game.UO.World, $"Error importing {name}.json..", 32);
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

    private static void SetDefaults() => SpellBarRows = [new SpellBarRow().SetSpell(0, SpellDefinition.FullIndexGetSpell(29)).SetSpell(1, SpellDefinition.FullIndexGetSpell(11)).SetSpell(2, SpellDefinition.FullIndexGetSpell(22))];
}

public class SpellBarRow()
{
    [JsonIgnore]
    public SpellDefinition[] SpellSlot = new SpellDefinition[10];

    public int[] SpellSlotIds {
        get
        {
            var ids = new List<int>();
            foreach (SpellDefinition spell in SpellSlot)
            {
                if (spell == null)
                    ids.Add(-2);
                else
                    ids.Add(spell.ID);
            }
            return ids.ToArray();
        }
        set
        {
            for (int i = 0; i < 10; i++)
            {
                SpellSlot[i] = SpellDefinition.FullIndexGetSpell(value[i]);
            }
        }
    }

    public ushort RowHue { get; set; }

    public SpellBarRow SetSpell(int slot, SpellDefinition spell)
    {
        SpellSlot[slot] = spell;

        return this;
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
public partial class SpellBarRowsContext : JsonSerializerContext { }

[JsonSerializable(typeof(SpellBarSettings))]
public partial class SpellBarSettingsContext : JsonSerializerContext { }
