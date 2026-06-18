using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents Python-accessible multi-tile structure (e.g., player buildings or ships) in game world.
/// Inherits spatial visual data from <see cref="ApiGameObject"/>.
/// </summary>
public class ApiMulti : ApiGameObject
{
    public uint HouseSerial { get; }
    public string HouseSerialHex => $"0x{HouseSerial:X8}";
    public uint MultiID { get; }
    public string MultiIDHex => $"0x{MultiID:X4}";
    public TileFlag Flags { get; }
    public ulong FlagsValue => (ulong)Flags;
    public int Height { get; }
    public string Name { get; } = string.Empty;
    public int MultiOffsetX { get; }
    public int MultiOffsetY { get; }
    public int MultiOffsetZ { get; }
    public bool IsCustom { get; }
    public bool IsMovable { get; }
    public bool IsSurface => (Flags & TileFlag.Surface) != 0;
    public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
    public bool IsWet => (Flags & TileFlag.Wet) != 0;
    public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
    public bool IsWall => (Flags & TileFlag.Wall) != 0;
    public bool IsDoor => (Flags & TileFlag.Door) != 0;
    public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
    public bool IsNoHouse => (Flags & TileFlag.NoHouse) != 0;

    /// <summary>
    /// Initializes new instance <see cref="ApiMulti"/> class from <see cref="Multi"/> object.
    /// </summary>
    /// <param name="multi">The multi-tile object wrap.</param>
    internal ApiMulti(Multi multi) : this(multi, 0)
    {
    }

    internal ApiMulti(Multi multi, uint houseSerial) : base(multi)
    {
        if (multi == null)
            return;

        HouseSerial = houseSerial;
        MultiID = multi.OriginalGraphic;
        Flags = multi.ItemData.Flags;
        Height = multi.ItemData.Height;
        Name = multi.Name;
        MultiOffsetX = multi.MultiOffsetX;
        MultiOffsetY = multi.MultiOffsetY;
        MultiOffsetZ = multi.MultiOffsetZ;
        IsCustom = multi.IsCustom;
        IsMovable = multi.IsMovable;
    }

    /// <summary>
    /// Python-visible class name object.
    /// Accessible in Python <c>obj.__class__</c>.
    /// </summary>
    public override string __class__ => "ApiMulti";
}
