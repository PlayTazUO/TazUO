using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Input;
using ClassicUO.LegionScripting;
using ClassicUO.Utility;
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

    public static CounterBarSlot GetSlot(int row, int col)
    {
        if (!enabled)
            return CounterBarSlot.Empty();

        if(SpellBarRows.Count <= row || row < 0) return CounterBarSlot.Empty();
        if(SpellBarRows[row].Slots.Length <= col || col < 0) return CounterBarSlot.Empty();

        return SpellBarRows[row].Slots[col] ?? CounterBarSlot.Empty();
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
        if (!enabled || !spellBarSettings.Enabled || HotKeys.GloballyDisabled)
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
        if (!enabled || !spellBarSettings.Enabled || HotKeys.GloballyDisabled)
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

        CounterBarSlot slot = GetSlot(row, col);

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

    /// <summary>Builds a <see cref="HotkeyBinding"/> describing the current binding for a slot,
    /// so the shared hotkey capture window can be seeded with it.</summary>
    public static HotkeyBinding GetSlotBinding(int slot)
    {
        if (spellBarSettings == null || slot < 0 || slot >= spellBarSettings.HotKeys.Length)
            return new HotkeyBinding();

        var key = (SDL.SDL_Keycode)spellBarSettings.HotKeys[slot];
        var mod = (SDL.SDL_Keymod)spellBarSettings.KeyMod[slot];

        SDL.SDL_GamepadButton[] controllers = null;
        if (spellBarSettings.ControllerButtons != null
            && slot < spellBarSettings.ControllerButtons.Length
            && spellBarSettings.ControllerButtons[slot] is { Length: > 0 } cb)
            controllers = cb.Select(x => (SDL.SDL_GamepadButton)x).ToArray();

        HotkeyBinding binding = key != SDL.SDL_Keycode.SDLK_UNKNOWN
            ? new HotkeyBinding(key, mod)
            : new HotkeyBinding();
        binding.ControllerButtons = controllers;
        return binding;
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

            FileSystemHelper.WriteAllTextSafe(path, JsonSerializer.Serialize(SpellBarRows[CurrentRow], SpellBarRowsContext.Default.SpellBarRow));
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
            FileSystemHelper.WriteAllTextSafe(fullSavePath, JsonSerializer.Serialize(SpellBarRows, SpellBarRowsContext.Default.ListSpellBarRow));
            FileSystemHelper.WriteAllTextSafe(Path.Combine(charPath, "SpellBarSettings.json"), JsonSerializer.Serialize(spellBarSettings, SpellBarSettingsContext.Default.SpellBarSettings));
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static void SetDefaults() => SpellBarRows = [new SpellBarRow()
        .SetSlot(0, CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(29)))
        .SetSlot(1, CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(11)))
        .SetSlot(2, CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(22)))];
}

public class SpellBarRow()
{
    private CounterBarSlot[] _slots = CreateEmptySlots();

    // Always a 10-element array with no null entries, even when deserialized from a
    // malformed file (null, short, or containing nulls), since the array is indexed directly.
    public CounterBarSlot[] Slots
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
                _slots[i] = CounterBarSlot.FromSpell(SpellDefinition.FullIndexGetSpell(value[i]));
        }
    }

    public ushort RowHue { get; set; }

    public SpellBarRow SetSlot(int slot, CounterBarSlot value)
    {
        if ((uint)slot >= (uint)_slots.Length)
            return this;

        _slots[slot] = value ?? CounterBarSlot.Empty();

        return this;
    }

    private static CounterBarSlot[] NormalizeSlots(CounterBarSlot[] value)
    {
        CounterBarSlot[] slots = CreateEmptySlots();

        if (value == null)
            return slots;

        for (int i = 0; i < slots.Length && i < value.Length; i++)
            slots[i] = value[i] ?? CounterBarSlot.Empty();

        return slots;
    }

    private static CounterBarSlot[] CreateEmptySlots()
    {
        var slots = new CounterBarSlot[10];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = CounterBarSlot.Empty();
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
[JsonSerializable(typeof(CounterBarSlot), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CounterBarSlot[]), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SpellBarRowsContext : JsonSerializerContext { }

[JsonSerializable(typeof(SpellBarSettings))]
public partial class SpellBarSettingsContext : JsonSerializerContext { }
