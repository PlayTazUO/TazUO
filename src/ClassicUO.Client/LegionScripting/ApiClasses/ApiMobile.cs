using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using static ClassicUO.LegionScripting.LegionAPI;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a Python-accessible mobile (NPC, creature, or player character).
/// Inherits entity and positional data from <see cref="ApiEntity"/>.
/// </summary>
public class ApiMobile : ApiEntity
{
    public override ushort X => Read(static m => m.X);
    public override ushort Y => Read(static m => m.Y);
    public override sbyte Z => Read(static m => m.Z);

    public int HitsDiff => Read(static m => m.HitsDiff);
    public int ManaDiff => Read(static m => m.ManaDiff);
    public int StamDiff => Read(static m => m.StamDiff);
    public bool IsDead => Read(static m => m.IsDead);
    public bool IsPoisoned => Read(static m => m.IsPoisoned);
    public int HitsMax => Read(static m => m.HitsMax);
    public int Hits => Read(static m => m.Hits);
    public int StaminaMax => Read(static m => m.StaminaMax);
    public int Stamina => Read(static m => m.Stamina);
    public int ManaMax => Read(static m => m.ManaMax);
    public int Mana => Read(static m => m.Mana);
    public bool IsRenamable => Read(static m => m.IsRenamable);
    public bool IsHuman => Read(static m => m.IsHuman);
    public bool IsYellowHits => Read(static m => m.IsYellowHits);
    public bool IsHidden => Read(static m => m.IsHidden);
    public bool IsGargoyle => Read(static m => m.IsGargoyle);
    public bool IsMounted => Read(static m => m.IsMounted);
    public bool IsDrivingBoat => Read(static m => m.IsDrivingBoat);
    public bool IsRunning => Read(static m => m.IsRunning);
    public bool IsParalyzed => Read(static m => m.IsParalyzed);
    /// <summary>
    /// Get this mobiles direction as a string, for example: "west", "east", etc
    /// </summary>
    public string Direction => Read(static m => Utility.GetDirectionString(m.Direction), "");
    public Notoriety Notoriety => Read(static m => (Notoriety)m.NotorietyFlag, Notoriety.Unknown);

    public virtual bool InWarMode
    {
        get => Read(static m => m.InWarMode);
        set { } // Dispose of value - only overrides can set
    }

    /// <summary>
    /// Get the mobile's Backpack item
    /// </summary>
    public ApiItem Backpack => Read(static m => m.Backpack != null ? new ApiItem(m.Backpack) : null);

    /// <summary>
    /// Get the mobile's Mount item (if mounted)
    /// </summary>
    public ApiItem Mount => Read(static m => m.Mount != null ? new ApiItem(m.Mount) : null);

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiMobile"/> class from a <see cref="Mobile"/>.
    /// </summary>
    /// <param name="mobile">The mobile to wrap.</param>
    internal ApiMobile(Mobile mobile) : base(mobile)
    {
        if (mobile == null) return; //Prevent crashes for invalid mobiles

        this.mobile = mobile;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiMobile";

    private Mobile mobile;

    /// <summary>
    /// Gets the Mobile without thread marshalling. Must only be called from code already executing on the main thread.
    /// </summary>
    private Mobile GetMobileUnsafe()
        => ResolveCached(ref mobile, Serial, static s => Client.Game.UO.World.Mobiles.TryGetValue(s, out Mobile m) ? m : null);

    /// <summary>
    /// Reads a single field from the backing <see cref="Mobile"/> on the main thread, returning
    /// <paramref name="fallback"/> when the mobile is no longer available.
    /// </summary>
    protected T Read<T>(System.Func<Mobile, T> selector, T fallback = default)
        => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Mobile m = GetMobileUnsafe();

            return m != null ? selector(m) : fallback;
        });

    /// <summary>
    /// Highlight this mobile and all of its equipped items with the given hue.
    /// The original hues are remembered so they can be restored.
    /// Call with <c>None</c> to restore the mobile and its equipped items to their original hues.
    /// Example:
    /// ```py
    /// mob.Highlight(0x0021)
    /// mob.Highlight(None)
    /// ```
    /// </summary>
    /// <param name="hue">The hue to apply, or <c>null</c> to restore the original hues.</param>
    public override void Highlight(ushort? hue = null) => MainThreadQueue.EnqueueAction(() =>
                                                               {
                                                                   Mobile m = GetMobileUnsafe();

                                                                   if (m == null || m.IsDestroyed)
                                                                       return;

                                                                   ApplyHighlight(m, hue);

                                                                   for (LinkedObject i = m.Items; i != null; i = i.Next)
                                                                   {
                                                                       if (i is Item it)
                                                                           ApplyHighlight(it, hue);
                                                                   }
                                                               });

    /// <summary>
    /// Gets the mobile name and properties (tooltip text).
    /// This returns the name and properties in a single string. You can split it by newline if you want to separate them.
    /// </summary>
    /// <param name="wait">True or false to wait for name and props</param>
    /// <param name="timeout">Timeout in seconds</param>
    /// <returns>Mobile name and properties, or empty string if we don't have them.</returns>
    public string NameAndProps(bool wait = false, int timeout = 10) => GetNameAndProps(wait, timeout);
}
