namespace ClassicUO.Game.UI;

/// <summary>
///     An enumeration representing the different types of cursors that can be displayed in the game.
///     Note that this list is not exhaustive - several other cursor styles exist.
/// </summary>
public enum GameCursorVisualType : ushort
{
    PointingNorthWest = 0,
    PointingNorth = 1,
    PointingNorthEast = 2,
    PointingEast = 3,
    PointingSouthEast = 4,
    PointingSouth = 5,
    PointingSouthWest = 6,
    PointingWest = 7,
    Dragging = 8,
    FatPointingWest = 9,
    Targeting = 12,
    Loading = 13,
    Editing = 14,
}
