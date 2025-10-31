using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible player character with full stat and equipment data.
/// Inherits mobile data from <see cref="PyMobile"/>.
/// </summary>
public class PyPlayer : PyMobile
{
    // Location
    public override ushort X => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.X ?? 0);
    public override ushort Y => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Y ?? 0);
    public override sbyte Z => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Z ?? 0);

    // Primary Stats
    public ushort Strength => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Strength ?? 0);
    public ushort Dexterity => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Dexterity ?? 0);
    public ushort Intelligence => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Intelligence ?? 0);

    // Stat Increases
    public short StrengthIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.StrengthIncrease ?? 0);
    public short DexterityIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DexterityIncrease ?? 0);
    public short IntelligenceIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IntelligenceIncrease ?? 0);

    // Stat Locks
    public Lock StrLock => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.StrLock ?? Lock.Up);
    public Lock DexLock => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DexLock ?? Lock.Up);
    public Lock IntLock => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IntLock ?? Lock.Up);

    // Hit/Mana/Stam Stats
    public short HitPointsIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.HitPointsIncrease ?? 0);
    public short ManaIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.ManaIncrease ?? 0);
    public short StaminaIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.StaminaIncrease ?? 0);
    public short HitPointsRegeneration => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.HitPointsRegeneration ?? 0);
    public short ManaRegeneration => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.ManaRegeneration ?? 0);
    public short StaminaRegeneration => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.StaminaRegeneration ?? 0);

    // Resistances
    public short PhysicalResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.PhysicalResistance ?? 0);
    public short FireResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.FireResistance ?? 0);
    public short ColdResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.ColdResistance ?? 0);
    public short PoisonResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.PoisonResistance ?? 0);
    public short EnergyResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.EnergyResistance ?? 0);

    // Max Resistances
    public short MaxPhysicResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxPhysicResistence ?? 0);
    public short MaxFireResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxFireResistence ?? 0);
    public short MaxColdResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxColdResistence ?? 0);
    public short MaxPoisonResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxPoisonResistence ?? 0);
    public short MaxEnergyResistance => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxEnergyResistence ?? 0);

    // Combat Stats
    public short DamageMin => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DamageMin ?? 0);
    public short DamageMax => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DamageMax ?? 0);
    public short DamageIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DamageIncrease ?? 0);
    public short HitChanceIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.HitChanceIncrease ?? 0);
    public short SwingSpeedIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.SwingSpeedIncrease ?? 0);
    public short DefenseChanceIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.DefenseChanceIncrease ?? 0);
    public short MaxDefenseChanceIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxDefenseChanceIncrease ?? 0);
    public short ReflectPhysicalDamage => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.ReflectPhysicalDamage ?? 0);

    // Magic Stats
    public short SpellDamageIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.SpellDamageIncrease ?? 0);
    public short FasterCasting => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.FasterCasting ?? 0);
    public short FasterCastRecovery => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.FasterCastRecovery ?? 0);
    public short LowerManaCost => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.LowerManaCost ?? 0);
    public short LowerReagentCost => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.LowerReagentCost ?? 0);
    public bool IsCasting => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IsCasting ?? false);
    public bool IsRecovering => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IsRecovering ?? false);

    // Other Stats
    public ushort Luck => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Luck ?? 0);
    public uint Gold => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Gold ?? 0);
    public uint TithingPoints => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.TithingPoints ?? 0);
    public ushort Weight => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Weight ?? 0);
    public ushort WeightMax => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.WeightMax ?? 0);
    public short StatsCap => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.StatsCap ?? 0);
    public byte Followers => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.Followers ?? 0);
    public byte FollowersMax => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.FollowersMax ?? 0);
    public short EnhancePotions => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.EnhancePotions ?? 0);

    // Max Stat Increases
    public short MaxHitPointsIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxHitPointsIncrease ?? 0);
    public short MaxManaIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxManaIncrease ?? 0);
    public short MaxStaminaIncrease => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.MaxStaminaIncrease ?? 0);

    public bool IsHidden =>  MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IsHidden ?? false);
    public bool IsWalking =>  MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.IsWalking ?? false);

    public override bool InWarMode
    {
        get => MainThreadQueue.InvokeOnMainThread(() => GetPlayerUnsafe()?.InWarMode ?? false);
        set => MainThreadQueue.InvokeOnMainThread(() =>
        {
            PlayerMobile player = GetPlayerUnsafe();
            if (player != null)
            {
                GameActions.RequestWarMode(player, value);
            }
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PyPlayer"/> class from a <see cref="PlayerMobile"/>.
    /// </summary>
    /// <param name="player">The player mobile to wrap.</param>
    internal PyPlayer(PlayerMobile player) : base(player)
    {
        if (player == null) return; //Prevent crashes for invalid player

        this.player = player;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyPlayer";

    private PlayerMobile player;

    /// <summary>
    /// Gets the PlayerMobile without thread marshalling. Must only be called from code already executing on the main thread.
    /// </summary>
    private PlayerMobile GetPlayerUnsafe()
    {
        if (player != null && player.Serial == Serial) return player;

        if (World.Instance.Player != null)
        {
            return player = World.Instance.Player;
        }

        return null;
    }
}
