using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a Python-accessible land tile in the game world.
/// Inherits spatial and visual data from <see cref="ApiGameObject"/>.
/// </summary>
public class ApiLand : ApiGameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiLand"/> class from a <see cref="Land"/> tile.
    /// </summary>
    /// <param name="land">The land tile to wrap.</param>
    internal ApiLand(Land land) : base(land)
    {
    }

    /// <summary>
    /// The Python-visible class name of this object.
    /// Accessible in Python as <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiLand";
}
