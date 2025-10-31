using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Base class for all Python-accessible game world objects.
/// Encapsulates common spatial and visual properties such as position and graphics.
/// </summary>
public class PyGameObject
{
    /// <summary>
    /// Check if the object is impassible or not based on item data.
    /// </summary>
    public bool Impassible
    {
        get
        {
            if(_gameObject == null) return true;

            switch (_gameObject)
            {
                case Land land:
                    return land.TileData.IsImpassable;
                case Static s:
                    ref StaticTiles staticData = ref Client.Game.UO.FileManager.TileData.StaticData[s.OriginalGraphic];
                    return staticData.IsImpassable || staticData.IsWall;
                case Item i:
                    return i.ItemData.IsImpassable;
                case Multi m:
                    return m.ItemData.IsImpassable;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// The X-coordinate of the object in the game world.
    /// </summary>
    public virtual ushort X { get; }

    /// <summary>
    /// The Y-coordinate of the object in the game world.
    /// </summary>
    public virtual ushort Y { get; }

    /// <summary>
    /// The Z-coordinate (elevation) of the object in the game world.
    /// </summary>
    public virtual sbyte Z { get; }

    /// <summary>
    /// The graphic ID of the object, representing its visual appearance.
    /// </summary>
    public ushort Graphic { get => field;
        set
        {
            if (_gameObject == null || _gameObject.IsDestroyed)
                return;

            MainThreadQueue.EnqueueAction(() => _gameObject.Graphic = value);

            field = value;
        }
    }

    /// <summary>
    /// The hue (color tint) applied to the object.
    /// </summary>
    public ushort Hue { get => field;
        set
        {
            if (_gameObject == null || _gameObject.IsDestroyed)
                return;

            MainThreadQueue.EnqueueAction(() => _gameObject.Hue = value);

            field = value;
        }
    }

    public int Distance => MainThreadQueue.InvokeOnMainThread(() => _gameObject?.Distance ?? 0);

    /// <summary>
    /// Set the hue of a game object.
    /// </summary>
    /// <param name="hue"></param>
    public void SetHue(ushort hue)
    {
        if (_gameObject == null || _gameObject.IsDestroyed)
            return;

        MainThreadQueue.EnqueueAction(() => _gameObject.Hue = hue);
    }

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
        Graphic = gameObject.OriginalGraphic;
        Hue = gameObject.Hue;
    }

    /// <summary>
    /// Returns a readable string representation of the game object.
    /// Used when printing or converting the object to a string in Python scripts.
    /// </summary>
    public override string ToString() => $"<{__class__} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Pos=({X},{Y},{Z})>";

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
