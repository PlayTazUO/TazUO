using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data;

public enum HotKeyActionType
{
    Spell = 0,
    Macro = 1,
    Script = 2,
    Skill = 3,
    Consumable = 4,
    Ability = 5,
    ToggleHotkeys = 6,
    SelfHeal = 7,
}

public class CustomConsumable
{
    public string Label { get; set; } = "";
    public int Graphic { get; set; }
    public int Hue { get; set; } = -1; // -1 = any
}

public class HotKeyAction
{
    public HotKeyActionType Type { get; set; }
    public int SpellId { get; set; }
    public string MacroName { get; set; } = "";
    public string ScriptPath { get; set; } = "";
    public int SkillIndex { get; set; } = -1;
    public int ConsumableGraphic { get; set; }
    public int ConsumableHue { get; set; } = -1;
    public string ConsumableLabel { get; set; } = "";
    public bool AbilityPrimary { get; set; } = true;
    public bool SelfHealChivalry { get; set; } = false; // false = Magery, true = Chivalry

    public string DisplayName(World world)
    {
        switch (Type)
        {
            case HotKeyActionType.Spell:
            {
                var def = SpellDefinition.FullIndexGetSpell(SpellId);
                return def != null && !string.IsNullOrEmpty(def.Name) ? def.Name : "Spell (missing)";
            }
            case HotKeyActionType.Macro:
            {
                var macro = world?.Macros?.FindMacro(MacroName);
                return macro != null ? $"Macro: {MacroName}" : $"Macro: {MacroName} (missing)";
            }
            case HotKeyActionType.Script:
            {
                var script = ResolveScript();
                return script != null ? $"Script: {script.FileName}" : $"Script: {ScriptPath} (missing)";
            }
            case HotKeyActionType.Skill:
            {
                var skills = world?.Player?.Skills;
                if (skills != null && SkillIndex >= 0 && SkillIndex < skills.Length)
                    return $"Skill: {skills[SkillIndex].Name}";
                return "Skill (missing)";
            }
            case HotKeyActionType.Consumable:
                return string.IsNullOrEmpty(ConsumableLabel) ? "Consumable" : ConsumableLabel;
            case HotKeyActionType.Ability:
                return AbilityPrimary ? "Primary Ability" : "Secondary Ability";
            case HotKeyActionType.ToggleHotkeys:
                return "Toggle Hotkeys";
            case HotKeyActionType.SelfHeal:
                return SelfHealChivalry ? "Self Heal (Chivalry)" : "Self Heal (Magery)";
            default:
                return "Unknown";
        }
    }

    public void Activate(World world)
    {
        if (world == null)
            return;

        switch (Type)
        {
            case HotKeyActionType.Spell:
                GameActions.CastSpell(SpellId);
                break;

            case HotKeyActionType.Macro:
            {
                var macro = world.Macros?.FindMacro(MacroName);
                if (macro?.Items is MacroObject mo)
                {
                    world.Macros.SetMacroToExecute(mo);
                    world.Macros.WaitForTargetTimer = 0;
                    world.Macros.Update();
                }
                else
                    GameActions.Print(world, $"HotKey: macro '{MacroName}' not found.");
                break;
            }

            case HotKeyActionType.Script:
            {
                var script = ResolveScript();
                if (script == null)
                {
                    GameActions.Print(world, $"HotKey: script '{ScriptPath}' not found.");
                    break;
                }
                if (script.IsPlaying)
                    LegionScripting.LegionScripting.StopScript(script);
                else
                    LegionScripting.LegionScripting.PlayScript(script);
                break;
            }

            case HotKeyActionType.Skill:
                if (SkillIndex < 0)
                {
                    GameActions.Print(world, "HotKey: no skill set.");
                    break;
                }
                GameActions.UseSkill(SkillIndex);
                break;

            case HotKeyActionType.Consumable:
            {
                ushort? hue = ConsumableHue < 0 ? null : (ushort)ConsumableHue;
                var item = world.Player?.FindItemByGraphicAndHue((ushort)ConsumableGraphic, hue);
                if (item != null)
                    GameActions.DoubleClick(world, item.Serial);
                else
                    GameActions.Print(world, $"HotKey: no '{ConsumableLabel}' found.");
                break;
            }

            case HotKeyActionType.Ability:
                if (AbilityPrimary)
                    GameActions.UsePrimaryAbility(world);
                else
                    GameActions.UseSecondaryAbility(world);
                break;

            case HotKeyActionType.ToggleHotkeys:
            {
                var profile = ProfileManager.CurrentProfile;
                if (profile != null)
                {
                    profile.DisableHotkeys = !profile.DisableHotkeys;
                    GameActions.Print(world, $"Hotkeys {(profile.DisableHotkeys ? "disabled" : "enabled")}.");
                }
                break;
            }

            case HotKeyActionType.SelfHeal:
                // NO-OP: SelfHealManager performs the real hold-to-heal via the profile.
                // Matching here returns handled=true so the macro fallback is suppressed.
                break;
        }
    }

    private LegionScripting.ScriptFile ResolveScript()
        => LegionScripting.LegionScripting.LoadedScripts.FirstOrDefault(s => s.RelativePath == ScriptPath);
}
