using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a Python-accessible player character with full stat and equipment data.
/// Inherits mobile data from <see cref="ApiMobile"/>.
/// </summary>
public class ApiPlayer : ApiMobile
{
    // Location
    public override ushort X => ReadPlayer(static p => p.X);
    public override ushort Y => ReadPlayer(static p => p.Y);
    public override sbyte Z => ReadPlayer(static p => p.Z);

    /// <summary>
    /// Retrieves the player's current position in the game world.
    /// This API is most useful in combination with the Legion API's `GetPath`.
    /// </summary>
    public ApiPoint3D Position => ReadPlayer(static p => new ApiPoint3D { X = p.X, Y = p.Y, Z = p.Z }, new ApiPoint3D());


    // Primary Stats
    public ushort Strength => ReadPlayer(static p => p.Strength);
    public ushort Dexterity => ReadPlayer(static p => p.Dexterity);
    public ushort Intelligence => ReadPlayer(static p => p.Intelligence);

    // Stat Increases
    public short StrengthIncrease => ReadPlayer(static p => p.StrengthIncrease);
    public short DexterityIncrease => ReadPlayer(static p => p.DexterityIncrease);
    public short IntelligenceIncrease => ReadPlayer(static p => p.IntelligenceIncrease);

    // Stat Locks
    public Lock StrLock => ReadPlayer(static p => p.StrLock, Lock.Up);
    public Lock DexLock => ReadPlayer(static p => p.DexLock, Lock.Up);
    public Lock IntLock => ReadPlayer(static p => p.IntLock, Lock.Up);

    // Hit/Mana/Stam Stats
    public short HitPointsIncrease => ReadPlayer(static p => p.HitPointsIncrease);
    public short ManaIncrease => ReadPlayer(static p => p.ManaIncrease);
    public short StaminaIncrease => ReadPlayer(static p => p.StaminaIncrease);
    public short HitPointsRegeneration => ReadPlayer(static p => p.HitPointsRegeneration);
    public short ManaRegeneration => ReadPlayer(static p => p.ManaRegeneration);
    public short StaminaRegeneration => ReadPlayer(static p => p.StaminaRegeneration);

    // Resistances
    public short PhysicalResistance => ReadPlayer(static p => p.PhysicalResistance);
    public short FireResistance => ReadPlayer(static p => p.FireResistance);
    public short ColdResistance => ReadPlayer(static p => p.ColdResistance);
    public short PoisonResistance => ReadPlayer(static p => p.PoisonResistance);
    public short EnergyResistance => ReadPlayer(static p => p.EnergyResistance);

    // Max Resistances
    public short MaxPhysicResistance => ReadPlayer(static p => p.MaxPhysicResistence);
    public short MaxFireResistance => ReadPlayer(static p => p.MaxFireResistence);
    public short MaxColdResistance => ReadPlayer(static p => p.MaxColdResistence);
    public short MaxPoisonResistance => ReadPlayer(static p => p.MaxPoisonResistence);
    public short MaxEnergyResistance => ReadPlayer(static p => p.MaxEnergyResistence);

    // Combat Stats
    public short DamageMin => ReadPlayer(static p => p.DamageMin);
    public short DamageMax => ReadPlayer(static p => p.DamageMax);
    public short DamageIncrease => ReadPlayer(static p => p.DamageIncrease);
    public short HitChanceIncrease => ReadPlayer(static p => p.HitChanceIncrease);
    public short SwingSpeedIncrease => ReadPlayer(static p => p.SwingSpeedIncrease);
    public short DefenseChanceIncrease => ReadPlayer(static p => p.DefenseChanceIncrease);
    public short MaxDefenseChanceIncrease => ReadPlayer(static p => p.MaxDefenseChanceIncrease);
    public short ReflectPhysicalDamage => ReadPlayer(static p => p.ReflectPhysicalDamage);

    // Magic Stats
    public short SpellDamageIncrease => ReadPlayer(static p => p.SpellDamageIncrease);
    public short FasterCasting => ReadPlayer(static p => p.FasterCasting);
    public short FasterCastRecovery => ReadPlayer(static p => p.FasterCastRecovery);
    public short LowerManaCost => ReadPlayer(static p => p.LowerManaCost);
    public short LowerReagentCost => ReadPlayer(static p => p.LowerReagentCost);
    public bool IsCasting => ReadPlayer(static p => p.IsCasting);
    public bool IsRecovering => ReadPlayer(static p => p.IsRecovering);

    // Other Stats
    public ushort Luck => ReadPlayer(static p => p.Luck);
    public uint Gold => ReadPlayer(static p => p.Gold);
    public uint TithingPoints => ReadPlayer(static p => p.TithingPoints);
    public ushort Weight => ReadPlayer(static p => p.Weight);
    public ushort WeightMax => ReadPlayer(static p => p.WeightMax);
    public short StatsCap => ReadPlayer(static p => p.StatsCap);
    public byte Followers => ReadPlayer(static p => p.Followers);
    public byte FollowersMax => ReadPlayer(static p => p.FollowersMax);
    public short EnhancePotions => ReadPlayer(static p => p.EnhancePotions);

    // Max Stat Increases
    public short MaxHitPointsIncrease => ReadPlayer(static p => p.MaxHitPointsIncrease);
    public short MaxManaIncrease => ReadPlayer(static p => p.MaxManaIncrease);
    public short MaxStaminaIncrease => ReadPlayer(static p => p.MaxStaminaIncrease);

    public bool IsHidden => ReadPlayer(static p => p.IsHidden);
    public bool IsWalking => ReadPlayer(static p => p.IsWalking);

    public override bool InWarMode
    {
        get => ReadPlayer(static p => p.InWarMode);
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
    /// Initializes a new instance of the <see cref="ApiPlayer"/> class from a <see cref="PlayerMobile"/>.
    /// </summary>
    /// <param name="player">The player mobile to wrap.</param>
    internal ApiPlayer(PlayerMobile player) : base(player)
    {
        if (player == null) return; //Prevent crashes for invalid player

        this.player = player;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiPlayer";

    private PlayerMobile player;

    /// <summary>
    /// Gets the PlayerMobile without thread marshalling. Must only be called from code already executing on the main thread.
    /// </summary>
    private PlayerMobile GetPlayerUnsafe()
        => ResolveCached(ref player, Serial, static _ => World.Instance.Player);

    /// <summary>
    /// Reads a single field from the backing <see cref="PlayerMobile"/> on the main thread, returning
    /// <paramref name="fallback"/> when the player is no longer available.
    /// </summary>
    private T ReadPlayer<T>(System.Func<PlayerMobile, T> selector, T fallback = default)
        => MainThreadQueue.InvokeOnMainThread(() =>
        {
            PlayerMobile p = GetPlayerUnsafe();

            return p != null ? selector(p) : fallback;
        });
}
