namespace ClassicUO.Common;

/// <summary>
/// Represents a 3D point in the world
/// </summary>
/// <param name="X">The X coordinate of the point</param>
/// <param name="Y">The Y coordinate of the point</param>
/// <param name="Z">The Z coordinate of the point</param>
public record struct Point3D(int X, int Y, int Z);
