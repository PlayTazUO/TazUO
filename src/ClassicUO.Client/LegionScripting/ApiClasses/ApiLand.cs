using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents Python-accessible land tile in game world.
/// Inherits spatial visual data from <see cref="ApiGameObject"/>.
/// </summary>
public class ApiLand : ApiGameObject
{
    public TileFlag Flags { get; }
    public ulong FlagsValue => (ulong)Flags;
    public string Name { get; } = string.Empty;
    public int Height { get; }
    public bool IsSurface => (Flags & TileFlag.Surface) != 0;
    public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
    public bool IsWet => (Flags & TileFlag.Wet) != 0;
    public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
    public bool IsWall => (Flags & TileFlag.Wall) != 0;
    public bool IsDoor => (Flags & TileFlag.Door) != 0;
    public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
    public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
    public bool IsNoHouse => (Flags & TileFlag.NoHouse) != 0;
    public bool IsRoof => (Flags & TileFlag.Roof) != 0;
    public bool IsBackground => (Flags & TileFlag.Background) != 0;

    /// <summary>
    /// Initializes new instance <see cref="ApiLand"/> class from <see cref="Land"/> tile.
    /// </summary>
    /// <param name="land">The land tile wrap.</param>
    internal ApiLand(Land land) : base(land)
    {
        if (land == null)
            return;

        Flags = land.TileData.Flags;
        Name = land.TileData.Name;
        Height = 0;
    }

    /// <summary>
    /// Python-visible class name object.
    /// Accessible in Python <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiLand";
}
