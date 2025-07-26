using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible mobile (NPC, creature, or player character).
/// Inherits entity and positional data from <see cref="PyEntity"/>.
/// </summary>
public class PyMobile : PyEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PyMobile"/> class from a <see cref="Mobile"/>.
    /// </summary>
    /// <param name="mobile">The mobile to wrap.</param>
    internal PyMobile(Mobile mobile) : base(mobile)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyMobile";
}