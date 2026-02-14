using ClassicUO.Assets;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiItemData
{
    internal ApiItemData(StaticTiles staticTileData)
    {
        Flags = staticTileData.Flags;
        Weight = staticTileData.Weight;
        Layer = staticTileData.Layer;
        Count = staticTileData.Count;
        AnimID = staticTileData.AnimID;
        Hue = staticTileData.Hue;
        LightIndex = staticTileData.LightIndex;
        Height = staticTileData.Height;
        Name = staticTileData.Name;
    }

    public TileFlag Flags { get; private set; }
    public byte Weight { get; private set; }
    public byte Layer { get; private set; }
    public int Count { get; private set; }
    public ushort AnimID { get; private set; }
    public ushort Hue { get; private set; }
    public ushort LightIndex { get; private set; }
    public byte Height { get; private set; }
    public string Name { get; private set; }

    public bool IsAnimated => (Flags & TileFlag.Animation) != 0;
    public bool IsBridge => (Flags & TileFlag.Bridge) != 0;
    public bool IsImpassable => (Flags & TileFlag.Impassable) != 0;
    public bool IsSurface => (Flags & TileFlag.Surface) != 0;
    public bool IsWearable => (Flags & TileFlag.Wearable) != 0;
    public bool IsInternal => (Flags & TileFlag.Internal) != 0;
    public bool IsBackground => (Flags & TileFlag.Background) != 0;
    public bool IsNoDiagonal => (Flags & TileFlag.NoDiagonal) != 0;
    public bool IsWet => (Flags & TileFlag.Wet) != 0;
    public bool IsFoliage => (Flags & TileFlag.Foliage) != 0;
    public bool IsRoof => (Flags & TileFlag.Roof) != 0;
    public bool IsTranslucent => (Flags & TileFlag.Translucent) != 0;
    public bool IsPartialHue => (Flags & TileFlag.PartialHue) != 0;
    public bool IsStackable => (Flags & TileFlag.Generic) != 0;
    public bool IsTransparent => (Flags & TileFlag.Transparent) != 0;
    public bool IsContainer => (Flags & TileFlag.Container) != 0;
    public bool IsDoor => (Flags & TileFlag.Door) != 0;
    public bool IsWall => (Flags & TileFlag.Wall) != 0;
    public bool IsLight => (Flags & TileFlag.LightSource) != 0;
    public bool IsNoShoot => (Flags & TileFlag.NoShoot) != 0;
    public bool IsWeapon => (Flags & TileFlag.Weapon) != 0;
    public bool IsMultiMovable => (Flags & TileFlag.MultiMovable) != 0;
    public bool IsWindow => (Flags & TileFlag.Window) != 0;
}
