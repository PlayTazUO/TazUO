using ClassicUO;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using System;
using System.Linq;

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

    private World World;

    public SpellRangeInfo()
    {
        World = Client.Game.UO.World;
    }

    public static SpellRangeInfo FromSpellDef(SpellDefinition spell)
    {
        return new SpellRangeInfo() { ID = spell.ID, Name = spell.Name, PowerWords = spell.PowerWords };
    }

    public double GetEffectiveCastTime()
    {
        int maxFasterCasting = MaxFasterCasting;
        if (School == "Chivalry" && CapChivalryFasterCasting == true)
        {
            float mageryValue = World.Player.Skills.FirstOrDefault(x => x.Name == "Magery")?.Value ?? 0;
            float mysticismValue = World.Player.Skills.FirstOrDefault(x => x.Name == "Mysticism")?.Value ?? 0;
            maxFasterCasting = mageryValue > 70 || mysticismValue > 70 ? 2 : 4;
        }

        int fasterCasting = Math.Min(World.Player.FasterCasting, maxFasterCasting);
        double time = CastTime - (0.25 * fasterCasting);
        return time < 0.25 ? 0.25 : time;
    }

    public double GetEffectiveRecoveryTime()
    {
        int fasterCastRecovery = Math.Min(World.Player.FasterCastRecovery, MaxFasterCastRecovery);
        double time = RecoveryTime - (0.25 * fasterCastRecovery);
        return time < 0 ? 0 : time;
    }
}
