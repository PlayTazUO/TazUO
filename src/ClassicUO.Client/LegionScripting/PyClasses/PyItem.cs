using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible item in the game world.
/// Inherits entity and positional data from <see cref="PyEntity"/>.
/// </summary>
public class PyItem : PyEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PyItem"/> class from an <see cref="Item"/>.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    internal PyItem(Item item) : base(item)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyItem";
}