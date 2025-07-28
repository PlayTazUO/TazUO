using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Base class for all Python-accessible game world objects.
/// Encapsulates common spatial and visual properties such as position and graphics.
/// </summary>
public class PyGameObject
{
    /// <summary>
    /// The X-coordinate of the object in the game world.
    /// </summary>
    public readonly ushort X;

    /// <summary>
    /// The Y-coordinate of the object in the game world.
    /// </summary>
    public readonly ushort Y;

    /// <summary>
    /// The Z-coordinate (elevation) of the object in the game world.
    /// </summary>
    public readonly sbyte Z;

    /// <summary>
    /// The graphic ID of the object, representing its visual appearance.
    /// </summary>
    public readonly ushort Graphic;

    /// <summary>
    /// The hue (color tint) applied to the object.
    /// </summary>
    public ushort Hue;

    /// <summary>
    /// Determines if there is line of sight from the specified observer to this object.
    /// If no observer is specified, it defaults to the player.
    /// </summary>
    /// <param name="observer">The observing GameObject (optional).</param>
    /// <returns>True if the observer has line of sight to this object; otherwise, false.</returns>
    public bool HasLineOfSightFrom(PyGameObject observer = null)
    {
        GameObject observerObj = observer?._gameObject;
        return _gameObject?.HasLineOfSightFrom(observerObj) ?? false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PyGameObject"/> class from a <see cref="GameObject"/>.
    /// </summary>
    /// <param name="gameObject">The game object to wrap.</param>
    internal PyGameObject(GameObject gameObject)
    {
        if (gameObject == null) return; //Prevent crashes for invalid objects.

        _gameObject = gameObject;

        X = gameObject.X;
        Y = gameObject.Y;
        Z = gameObject.Z;
        Graphic = gameObject.Graphic;
        Hue = gameObject.Hue;
    }

    /// <summary>
    /// Returns a readable string representation of the game object.
    /// Used when printing or converting the object to a string in Python scripts.
    /// </summary>
    public override string ToString()
    {
        return $"<{__class__} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Pos=({X},{Y},{Z})>";
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public virtual string __class__ => "PyGameObject";

    /// <summary>
    /// Returns a detailed string representation of the object.
    /// This string is used by Python’s built-in <c>repr()</c> function.
    /// </summary>
    /// <returns>A string suitable for debugging and inspection in Python.</returns>
    public virtual string __repr__() => ToString();

    private readonly GameObject _gameObject;
}
