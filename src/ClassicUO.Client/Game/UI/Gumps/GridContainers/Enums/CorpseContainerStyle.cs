namespace ClassicUO.Game.UI.Gumps;

/// <summary>
///     Controls which container style corpses open in, independent of the global
///     <see cref="ClassicUO.Configuration.Profile.ContainerStyle" /> preference.
/// </summary>
public enum CorpseContainerStyle
{
    /// <summary>Open corpses as grid containers.</summary>
    Grid,

    /// <summary>Open corpses as original-style containers.</summary>
    Original,

    /// <summary>Open corpses using the old grid loot gump only.</summary>
    OldGridLoot,

    /// <summary>Open corpses using the old grid loot gump alongside the normal container.</summary>
    OldGridLootAndContainer
}
