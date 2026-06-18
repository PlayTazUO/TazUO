#nullable enable
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents Python-accessible static object (non-interactive scenery) in game world.
/// Inherits spatial visual data from <see cref="ApiGameObject"/>.
/// </summary>
public class ApiStatic : ApiGameObject
{
    public TileFlag Flags { get; }
    public ulong FlagsValue => (ulong)Flags;
    public int Height { get; }
    public bool IsImpassible { get; }
    public bool IsImpassable => IsImpassible;
    public bool IsSurface => (Flags & TileFlag.Surface) != 0;
    public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
    public bool IsWet => (Flags & TileFlag.Wet) != 0;
    public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
    public bool IsWall => (Flags & TileFlag.Wall) != 0;
    public bool IsDoor => (Flags & TileFlag.Door) != 0;
    public bool IsNoHouse => (Flags & TileFlag.NoHouse) != 0;
    public bool IsRoof => (Flags & TileFlag.Roof) != 0;
    public bool IsBackground => (Flags & TileFlag.Background) != 0;
    public bool IsTree { get; }
    public bool IsVegetation { get; }
    public bool IsCave { get; }
    public string Name { get; } = string.Empty;

    /// <summary>
    /// Initializes new instance <see cref="ApiStatic"/> class from <see cref="Static"/> object.
    /// </summary>
    /// <param name="staticObj">The static object wrap.</param>
    internal ApiStatic(Static staticObj) : base(staticObj)
    {
        if (staticObj == null)
            return;

        Flags = staticObj.ItemData.Flags;
        Height = staticObj.ItemData.Height;
        IsImpassible = staticObj.ItemData.IsImpassable;
        IsTree = StaticFilters.IsTree(staticObj.OriginalGraphic, out _);
        IsVegetation = staticObj.IsVegetation;
        IsCave = StaticFilters.IsCave(staticObj.OriginalGraphic);
        Name = staticObj.Name;
    }

    /// <summary>
    /// Python-visible class name object.
    /// Accessible in Python <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiStatic";
}
