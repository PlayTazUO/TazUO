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
    public int HitsDiff => GetMobile()?.HitsDiff ?? 0;
    public int ManaDiff => GetMobile()?.ManaDiff ?? 0;
    public int StamDiff => GetMobile()?.StamDiff ?? 0;
    public bool IsDead => GetMobile()?.IsDead ?? false;
    public bool IsPoisoned => GetMobile()?.IsPoisoned ?? false;
    public int HitsMax => GetMobile()?.HitsMax ?? 0;
    public int Hits => GetMobile()?.Hits ?? 0;
    public int StaminaMax => GetMobile()?.StaminaMax ?? 0;
    public int Stamina => GetMobile()?.Stamina ?? 0;
    public int ManaMax => GetMobile()?.ManaMax ?? 0;
    public int Mana => GetMobile()?.Mana ?? 0;
    public bool IsRenamable => GetMobile()?.IsRenamable ?? false;
    public bool IsHuman => GetMobile()?.IsHuman ?? false;

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
    private Mobile GetMobile()
    {
        if (mobile != null && mobile.Serial == Serial) return mobile;

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.Mobiles.TryGetValue(Serial, out Mobile m)) return mobile = m;

            return null;
        });
    }
}
