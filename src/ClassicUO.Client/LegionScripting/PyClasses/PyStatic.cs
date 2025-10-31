#nullable enable
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Data;

namespace ClassicUO.LegionScripting.PyClasses;

/// <summary>
/// Represents a Python-accessible static object (non-interactive scenery) in the game world.
/// Inherits spatial and visual data from <see cref="PyGameObject"/>.
/// </summary>
public class PyStatic : PyGameObject
{
    public bool IsImpassible { get; }
    public bool IsTree { get; }
    public bool IsVegetation { get; }
    public bool IsCave { get; }
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PyStatic"/> class from a <see cref="Static"/> object.
    /// </summary>
    /// <param name="staticObj">The static object to wrap.</param>
    internal PyStatic(Static staticObj) : base(staticObj)
    {
        IsImpassible = staticObj.ItemData.IsImpassable;
        IsTree = StaticFilters.IsTree(staticObj.OriginalGraphic, out _);
        IsVegetation = staticObj.IsVegetation;
        IsCave = StaticFilters.IsCave(staticObj.OriginalGraphic);
        Name = staticObj.Name;
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "PyStatic";
}
