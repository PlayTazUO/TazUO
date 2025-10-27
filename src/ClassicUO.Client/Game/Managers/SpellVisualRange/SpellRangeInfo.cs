using ClassicUO.Game.Data;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace ClassicUO.Game.Managers.SpellVisualRange;

/// <summary>
/// Encapsulates spell range, cast time, recovery time, and visual indicator configuration for a spell.
/// </summary>
public class SpellRangeInfo
{
    public int ID { get; set; } = -1;
    public string Name { get; set; } = "";
    public string PowerWords { get; set; } = "";
    public int CursorSize { get; set; } = 0;
    public int CastRange { get; set; } = 1;
    public ushort Hue { get; set; } = 32;
    public ushort CursorHue { get; set; } = 10;
    public int MaxDuration { get; set; } = 10;
    public bool IsLinear { get; set; } = false;
    public double CastTime { get; set; } = 0.0;
    public bool ShowCastRangeDuringCasting { get; set; } = false;
    public bool FreezeCharacterWhileCasting { get; set; } = false;
    public bool ExpectTargetCursor { get; set; } = false;
    public double RecoveryTime { get; set; } = 0.0;
    public string School { get; set; } = "";
    public int MaxFasterCasting { get; set; } = 0;
    public int MaxFasterCastRecovery { get; set; } = 0;
    public bool? CapChivalryFasterCasting { get; set; } = null;

    private int? _mageryIndex;
    private int? _mysticismIndex;
    private World world => Client.Game.UO.World;
    private const float ChivalrySkillThreshold = 70.0f;

    public SpellRangeInfo() { }

    public static SpellRangeInfo FromSpellDef(SpellDefinition spell) => new SpellRangeInfo() { ID = spell.ID, Name = spell.Name, PowerWords = spell.PowerWords };

    /// <summary>
    /// Calculates the effective cast time for this spell, accounting for the player's Faster Casting stat
    /// and any school-specific caps (e.g., Chivalry with Magery/Mysticism requirements).
    /// </summary>
    /// <returns>The effective cast time in seconds, with a minimum of 0.25 seconds.</returns>
    public double GetEffectiveCastTime()
    {
        if (world?.Player == null)
            return CastTime;

        int maxFasterCasting = MaxFasterCasting;
        if (School == "Chivalry" && CapChivalryFasterCasting == true)
        {
            _mageryIndex ??= world.Player.Skills.FirstOrDefault(x => x.Name == "Magery")?.Index;
            _mysticismIndex ??= world.Player.Skills.FirstOrDefault(x => x.Name == "Mysticism")?.Index;

            float mageryValue = _mageryIndex.HasValue ? world.Player.Skills[_mageryIndex.Value].Value : 0;
            float mysticismValue = _mysticismIndex.HasValue ? world.Player.Skills[_mysticismIndex.Value].Value : 0;
            maxFasterCasting = mageryValue > ChivalrySkillThreshold || mysticismValue > ChivalrySkillThreshold ? 2 : 4;
        }

        int fasterCasting = Math.Min(world.Player.FasterCasting, maxFasterCasting);
        double time = CastTime - (0.25 * fasterCasting);
        return time < 0.25 ? 0.25 : time;
    }

    /// <summary>
    /// Calculates the effective recovery time for this spell, accounting for the player's Faster Cast Recovery stat.
    /// </summary>
    /// <returns>The effective recovery time in seconds, with a minimum of 0 seconds.</returns>
    public double GetEffectiveRecoveryTime()
    {
        if (world?.Player == null)
            return RecoveryTime;

        int fasterCastRecovery = Math.Min(world.Player.FasterCastRecovery, MaxFasterCastRecovery);
        double time = RecoveryTime - (0.25 * fasterCastRecovery);
        return time < 0 ? 0 : time;
    }
}

[JsonSerializable(typeof(SpellRangeInfo))]
[JsonSerializable(typeof(SpellRangeInfo[]))]
public partial class SpellRangeInfoJsonContext : JsonSerializerContext
{
}
