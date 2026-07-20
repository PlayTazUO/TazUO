using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a Python-accessible entity in the game world, such as a mobile or item.
/// Inherits basic spatial and visual data from <see cref="ApiGameObject"/>.
/// </summary>
public class ApiEntity : ApiGameObject
{
    /// <summary>
    /// The unique serial identifier of the entity.
    /// </summary>
    public readonly uint Serial;

    public string Name => GetEntity()?.Name ?? "";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiEntity"/> class from an <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity to wrap.</param>
    internal ApiEntity(Entity entity) : base(entity)
    {
        if (entity == null) return; //Prevent crashes for invalid entities.

        Serial = entity.Serial;
        this.entity = entity;
    }

    /// <summary>
    /// Returns a readable string representation of the entity.
    /// Used when printing or converting the object to a string in Python scripts.
    /// </summary>
    public override string ToString() => $"<{__class__} Serial=0x{Serial:X8} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Pos=({X},{Y},{Z})>";

    /// <summary>
    /// Implicitly converts a <see cref="ApiEntity"/> to its underlying <see cref="uint"/> serial.
    /// </summary>
    /// <param name="entity">The <see cref="ApiEntity"/> instance to convert.</param>
    /// <returns>The <see cref="Serial"/> value of the entity.</returns>
    public static implicit operator uint(ApiEntity entity)
    {
        if (entity == null) return 0;

        return entity.Serial;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiEntity";

    /// <summary>
    /// This will remove the item from the client, it will reappear if you leave the area and come back.
    /// This object will also no longer be available and may cause issues if you try to interact with it further.
    /// </summary>
    public void Destroy()
    {
        Entity e = GetEntity();

        if (e == null) return;

        MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.Instance != null && e.Serial > 0)
            {
                if (SerialHelper.IsMobile(e))
                    World.Instance.RemoveMobile(e);
                else
                    World.Instance.RemoveItem(e);
            }
        });

        entity = null;
    }

    protected Entity entity;
    protected Entity GetEntity()
    {
        if (entity != null && !entity.IsDestroyed && entity.Serial == Serial) return entity;

        return MainThreadQueue.InvokeOnMainThread(() => ResolveCached(ref entity, Serial, static s => Client.Game.UO.World.Get(s)));
    }

    /// <summary>
    /// Returns the cached backing object if it still matches this wrapper's <see cref="Serial"/>
    /// and has not been destroyed; otherwise looks it up via <paramref name="lookup"/> and caches the result.
    /// Does not marshal to the main thread — callers that touch world state must wrap this appropriately.
    /// </summary>
    protected static T ResolveCached<T>(ref T cache, uint serial, System.Func<uint, T> lookup) where T : Entity
    {
        if (cache != null && !cache.IsDestroyed && cache.Serial == serial) return cache;

        return cache = lookup(serial);
    }

    /// <summary>
    /// Gets this entity's name and properties (tooltip text) as a single newline-joined string,
    /// optionally waiting for the object property list (OPL) to arrive from the server.
    /// </summary>
    /// <param name="wait">True to wait for the name and properties to be received.</param>
    /// <param name="timeout">Timeout in seconds while waiting.</param>
    /// <returns>Name and properties joined by a newline, or an empty string if unavailable.</returns>
    protected string GetNameAndProps(bool wait, int timeout)
    {
        if (wait)
        {
            System.DateTime expire = System.DateTime.UtcNow.AddSeconds(timeout);

            while (!MainThreadQueue.InvokeOnMainThread(() => Client.Game.UO.World.OPL.Contains(Serial)) && System.DateTime.UtcNow < expire)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (Client.Game.UO.World.OPL.TryGetNameAndData(Serial, out string n, out string d))
            {
                return n + "\n" + d;
            }

            return string.Empty;
        });
    }
}
