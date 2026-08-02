using System;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// States the player can be in that an overlay may want to react to. A mask rather than a set of
/// booleans so a condition maps to a reaction through a table instead of a branch each.
/// </summary>
[Flags]
public enum PlayerCondition
{
    None = 0,

    Poisoned = 1 << 0,
    Paralyzed = 1 << 1,
    Hidden = 1 << 2,
    Dead = 1 << 3,

    /// <summary>Buff-derived, so answerable for the player only - the server sends buff icons for
    /// nobody else.</summary>
    Bleeding = 1 << 4,

    /// <inheritdoc cref="Bleeding"/>
    MortalStruck = 1 << 5
}
