using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible land tile in the game world.
/// Inherits spatial and visual data from <see cref="PyGameObject"/>.
/// </summary>
public class PyLand : PyGameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PyLand"/> class from a <see cref="Land"/> tile.
    /// </summary>
    /// <param name="land">The land tile to wrap.</param>
    internal PyLand(Land land) : base(land)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyLand";
}