using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible static object (non-interactive scenery) in the game world.
/// Inherits spatial and visual data from <see cref="PyGameObject"/>.
/// </summary>
public class PyStatic : PyGameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PyStatic"/> class from a <see cref="Static"/> object.
    /// </summary>
    /// <param name="static">The static object to wrap.</param>
    internal PyStatic(Static @static) : base(@static)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyStatic";
}