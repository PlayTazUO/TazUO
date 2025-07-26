using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible multi-tile structure (e.g., player buildings or player ships) in the game world.
/// Inherits spatial and visual data from <see cref="PyGameObject"/>.
/// </summary>
public class PyMulti : PyGameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PyMulti"/> class from a <see cref="Multi"/> object.
    /// </summary>
    /// <param name="multi">The multi-tile object to wrap.</param>
    internal PyMulti(Multi multi) : base(multi)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyMulti";
}