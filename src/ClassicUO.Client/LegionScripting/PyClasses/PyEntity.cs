using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible entity in the game world, such as a mobile or item.
/// Inherits basic spatial and visual data from <see cref="PyGameObject"/>.
/// </summary>
public class PyEntity : PyGameObject
{
    /// <summary>
    /// The unique serial identifier of the entity.
    /// </summary>
    public readonly uint Serial;

    public string Name => GetEntity()?.Name ?? "";

    /// <summary>
    /// Initializes a new instance of the <see cref="PyEntity"/> class from an <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity to wrap.</param>
    internal PyEntity(Entity entity) : base(entity)
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
    /// Implicitly converts a <see cref="PyEntity"/> to its underlying <see cref="uint"/> serial.
    /// </summary>
    /// <param name="entity">The <see cref="PyEntity"/> instance to convert.</param>
    /// <returns>The <see cref="Serial"/> value of the entity.</returns>
    public static implicit operator uint(PyEntity entity)
    {
        if (entity == null) return 0;

        return entity.Serial;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyEntity";

    /// <summary>
    /// This will remove the item from the client, it will reappear if you leave the area and come back.
    /// This object will also no longer be available and may cause issues if you try to interact with it further.
    /// </summary>
    public void Destroy()
    {
        GetEntity()?.Destroy();
        entity = null;
    }

    protected Entity entity;
    protected Entity GetEntity()
    {
        if (entity != null && entity.Serial == Serial) return entity;

        return MainThreadQueue.InvokeOnMainThread(() =>
        {
            return entity = Client.Game.UO.World.Get(Serial);
        });
    }
}
