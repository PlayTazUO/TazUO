namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a point in a three-dimensional space
/// </summary>
public record struct ApiPoint3D
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public override string ToString() => $"({X}, {Y}, {Z})";

    // Note - this section may be moved to a dedicated abstract class at a later point

    #region Python Emulation

    public string __str__() => ToString();
    public string __repr__() => ToString();

    #endregion
}
