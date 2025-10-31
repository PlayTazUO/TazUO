using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible mobile (NPC, creature, or player character).
/// Inherits entity and positional data from <see cref="PyEntity"/>.
/// </summary>
public class PyMobile : PyEntity
{
    public override ushort X => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.X ?? 0);
    public override ushort Y => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.Y ?? 0);
    public override sbyte Z => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.Z ?? 0);

    public int HitsDiff => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.HitsDiff ?? 0);
    public int ManaDiff => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.ManaDiff ?? 0);
    public int StamDiff => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.StamDiff ?? 0);
    public bool IsDead => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.IsDead ?? false);
    public bool IsPoisoned => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.IsPoisoned ?? false);
    public int HitsMax => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.HitsMax ?? 0);
    public int Hits => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.Hits ?? 0);
    public int StaminaMax => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.StaminaMax ?? 0);
    public int Stamina => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.Stamina ?? 0);
    public int ManaMax => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.ManaMax ?? 0);
    public int Mana => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.Mana ?? 0);
    public bool IsRenamable => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.IsRenamable ?? false);
    public bool IsHuman => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.IsHuman ?? false);

    public virtual bool InWarMode
    {
        get => MainThreadQueue.InvokeOnMainThread(() => GetMobileUnsafe()?.InWarMode ?? false);
        set { } // Dispose of value - only overrides can set
    }

    /// <summary>
    /// Get the mobile's Backpack item
    /// </summary>
    public PyItem Backpack => MainThreadQueue.InvokeOnMainThread(() =>
    {
        Item backpack = GetMobileUnsafe()?.Backpack;

        return backpack != null ? new PyItem(backpack) : null;
    });

    /// <summary>
    /// Get the mobile's Mount item (if mounted)
    /// </summary>
    public PyItem Mount => MainThreadQueue.InvokeOnMainThread(() =>
    {
        Item mount = GetMobileUnsafe()?.Mount;

        return mount != null ? new PyItem(mount) : null;
    });

    /// <summary>
    /// Initializes a new instance of the <see cref="PyMobile"/> class from a <see cref="Mobile"/>.
    /// </summary>
    /// <param name="mobile">The mobile to wrap.</param>
    internal PyMobile(Mobile mobile) : base(mobile)
    {
        if (mobile == null) return; //Prevent crashes for invalid mobiles

        this.mobile = mobile;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyMobile";

    private Mobile mobile;

    /// <summary>
    /// Gets the Mobile without thread marshalling. Must only be called from code already executing on the main thread.
    /// </summary>
    private Mobile GetMobileUnsafe()
    {
        if (mobile != null && mobile.Serial == Serial) return mobile;

        if (Client.Game.UO.World.Mobiles.TryGetValue(Serial, out Mobile m))
        {
            return mobile = m;
        }

        return null;
    }
}
